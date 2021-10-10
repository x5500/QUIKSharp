// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.


using NLog;
using QUIKSharp.Converters;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Functions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Orders
{
    public enum OrderResultCode
    {
        /// <summary>
        /// Успех
        /// </summary>
        Success = 0,

        /// <summary>
        /// Не удачно
        /// </summary>
        Error = -1,

        /// <summary>
        ///  «2» - ошибка при передаче транзакции в торговую систему, поскольку отсутствует подключение шлюза биржи, повторно транзакция не отправляется.
        /// </summary>
        RejectNoConnection = 2,

        /// <summary>
        ///  Не удалось дождаться ответа на транзакцию (ожидание прервано по таймауту или отмене задачи ожидания)
        ///  Возможно заявка и будет выставлена, если ответ потерялся в сети по пути
        /// </summary>
        Timeout = 3,

        /// <summary>
        /// Task Cancelled
        /// </summary>
        TaskCancelled = 4,
    }

    public struct OrderResult
    {
        public OrderResultCode Result;
        public long? TransID;
        public long? OrderNum;
        public string ResultMsg;

        /// <summary>
        /// Неисполненнный Остаток
        /// для транзакции на снятие неисполненной (в т.ч. частично) заявки.
        /// </summary>
        public long? Balance;
    }

    public class QuikOrders
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public int Timeout_ms { get; set; }
        private Quik quik { get; }

        /// <summary>
        ///  Быстрое создание простых ордеров
        /// </summary>
        /// <param name="quik">Quik</param>
        /// <param name="timeout_ms">Таймаут ожидания ответа на транзакцию (исполнения ордера)</param>
        public QuikOrders(Quik quik, int timeout_ms = 20000)
        {
            this.Timeout_ms = timeout_ms;
            this.quik = quik;
        }

        protected Task<OrderResult> SendWaitTransactionAsync(Transaction t)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(Timeout_ms);
            return quik.Transactions.SendWaitTransactionAsync(t, cancellationTokenSource.Token).ContinueWith<OrderResult>((tt) =>
            {
                if (tt.IsCanceled)
                    return new OrderResult { Result = OrderResultCode.TaskCancelled };
                if (tt.Exception != null)
                    throw tt.Exception;

                var reply = tt.Result;
                if (logger.IsTraceEnabled)
                    logger.Trace(string.Concat("SendWaitTransactionAsync (id: ", t.TRANS_ID, ") Status: ", reply.Status, " ResultMsg: ", reply.ResultMsg));

                switch (reply.Status)
                {
                    case TransactionStatus.Success:
                        return new OrderResult
                        {
                            TransID = t.TRANS_ID,
                            OrderNum = reply.transReply.OrderNum,
                            Balance = reply.transReply.Balance,
                            Result = OrderResultCode.Success,
                            ResultMsg = reply.ResultMsg,
                        };

                    case TransactionStatus.TimeoutWaitReply:
                    case TransactionStatus.SendRecieveTimeout:
                        return new OrderResult { TransID = t.TRANS_ID, OrderNum = null, Result = OrderResultCode.Timeout, ResultMsg = reply.ResultMsg };

                    case TransactionStatus.QuikError:
                    case TransactionStatus.FailedToSend:
                    case TransactionStatus.LuaException:
                    case TransactionStatus.TransactionException:
                        return new OrderResult { TransID = t.TRANS_ID, OrderNum = null, Result = OrderResultCode.Error, ResultMsg = reply.ResultMsg };

                    case TransactionStatus.NoConnection:
                        return new OrderResult { TransID = t.TRANS_ID, OrderNum = null, Result = OrderResultCode.RejectNoConnection, ResultMsg = reply.ResultMsg };

                    default:
                        throw new ArgumentOutOfRangeException("TransactionResult.Status", "SendWaitTransactionAsync: reply.Status out of switch cases range!");
                }
            }, continuationOptions: TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Создание "лимитрированной"заявки.
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="operation">Операция заявки (покупка/продажа)</param>
        /// <param name="price">Цена заявки</param>
        /// <param name="qty">Количество (в лотах)</param>
        /// <param name="executionCondition">Условие исполнения заявки (PUT_IN_QUEUE, FILL_OR_KILL, KILL_BALANCE)</param>
        public Task<OrderResult> SendLimitOrder(ITradeSecurity trsec, Operation operation, decimal price, long qty, ExecutionCondition executionCondition = ExecutionCondition.PUT_IN_QUEUE)
        {
            Transaction t = new Transaction
            {
                ACTION = TransactionAction.NEW_ORDER,
                ACCOUNT = trsec.AccountID,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                QUANTITY = qty,
                OPERATION = operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
                PRICE = price,
                TYPE = TransactionType.L,
                EXECUTION_CONDITION = executionCondition,
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Создание "рыночной"заявки.
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="operation">Операция заявки (покупка/продажа)</param>
        /// <param name="qty">Количество (в лотах)</param>
        public Task<OrderResult> SendMarketOrder(ITradeSecurity trsec, Operation operation, long qty)
        {
            Transaction newOrderTransaction = new Transaction
            {
                ACTION = TransactionAction.NEW_ORDER,
                ACCOUNT = trsec.AccountID,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                QUANTITY = qty,
                OPERATION = operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
                PRICE = 0,
                TYPE = TransactionType.M
            };
            return SendWaitTransactionAsync(newOrderTransaction);
        }

        /// <summary>
        /// -- отправка простой стоп-заявки (лимитной)
	    /// -- все параметры кроме кода клиента,коментария и времени жизни должны быть не нил
	    /// -- если код клиента нил - подлставляем счет
	    /// -- если время жизни не указано - то заявка "До Отмены"
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="operation">Операция заявки (покупка/продажа)</param>
        /// <param name="stopprice">Цена заявки</param>
        /// <param name="dealprice">Цена исполнения заявки</param>
        /// <param name="qty">Количество (в лотах)</param>
        public Task<OrderResult> SendStopOrder(ITradeSecurity trsec, Operation operation, decimal stopprice, decimal dealprice, long qty)
        {
            Transaction newOrderTransaction = new Transaction
            {
                ACTION = TransactionAction.NEW_STOP_ORDER,
                STOP_ORDER_KIND = StopOrderKind.SIMPLE_STOP_ORDER,
                ACCOUNT = trsec.AccountID,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                QUANTITY = qty,
                OPERATION = operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
                STOPPRICE = stopprice,
                PRICE = dealprice,
                EXPIRY_DATE = "GTC",
            };
            return SendWaitTransactionAsync(newOrderTransaction);
        }

        /// <summary>
        /// -- отправка TakeProfit стоп-заявки (лимитной)
	    /// -- все параметры кроме кода клиента,коментария и времени жизни должны быть не нил
	    /// -- если код клиента нил - подлставляем счет
	    /// -- если время жизни не указано - то заявка "До Отмены"
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="operation">Операция заявки (покупка/продажа)</param>
        /// <param name="TPprice">Цена TakeProfit</param>
        /// <param name="offset">Отсутп для TakeProfit</param>
        /// <param name="spread">Спред для TakeProfit</param>
        /// <param name="qty">Количество (в лотах)</param>
        public Task<OrderResult> SendTakeOrder(ITradeSecurity trsec, Operation operation, decimal TPprice, decimal offset, decimal spread, long qty)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.NEW_STOP_ORDER,
                STOP_ORDER_KIND = StopOrderKind.TAKE_PROFIT_STOP_ORDER,
                ACCOUNT = trsec.AccountID,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                QUANTITY = qty,
                OPERATION = operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
                STOPPRICE = TPprice, // -- тэйк-профит
                OFFSET = offset,
                OFFSET_UNITS = OffsetUnits.PRICE_UNITS,
                SPREAD = spread,
                SPREAD_UNITS = OffsetUnits.PRICE_UNITS,
                EXPIRY_DATE = "GTC",
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// -- отправка TakeProfit and StopLoss стоп-заявки (лимитной)
	    /// -- все параметры кроме кода клиента,коментария и времени жизни должны быть не нил
	    /// -- если код клиента нил - подлставляем счет
	    /// -- если время жизни не указано - то заявка "До Отмены"
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="operation">Операция заявки (покупка/продажа)</param>
        /// <param name="TPprice">Цена TakeProfit</param>
        /// <param name="offset">Отсутп для TakeProfit</param>
        /// <param name="SLprice">Цена StopLoss</param>
        /// <param name="dealSLprice">Цена исполнения StopLoss</param>
        /// <param name="spread">Спред для TakeProfit</param>
        /// <param name="qty">Количество (в лотах)</param>
        public Task<OrderResult> SendTPSLOrder(ITradeSecurity trsec, Operation operation, decimal TPprice, decimal offset, decimal SLprice, decimal dealSLprice, decimal spread, long qty)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.NEW_STOP_ORDER,
                STOP_ORDER_KIND = StopOrderKind.TAKE_PROFIT_AND_STOP_LIMIT_ORDER,
                ACCOUNT = trsec.AccountID,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                QUANTITY = qty,
                OPERATION = operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
                STOPPRICE = TPprice,  // -- тэйк-профит
                STOPPRICE2 = SLprice, // -- стоп-лимит
                PRICE = dealSLprice,  // -- Цена заявки, за единицу инструмента.
                OFFSET = offset,
                OFFSET_UNITS = OffsetUnits.PRICE_UNITS,
                MARKET_STOP_LIMIT = YesOrNo.NO,
                SPREAD = spread,
                SPREAD_UNITS = OffsetUnits.PRICE_UNITS,
                MARKET_TAKE_PROFIT = YesOrNo.NO,
                EXPIRY_DATE = "GTC",
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Создание заявки.
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="operation">Операция заявки (покупка/продажа)</param>
        /// <param name="price">Цена заявки</param>
        /// <param name="qty">Количество (в лотах)</param>
        /// <param name="orderType">Тип заявки (L - лимитная, M - рыночная)</param>
        /// <param name="executionCondition">Условие исполнения заявки (PUT_IN_QUEUE, FILL_OR_KILL, KILL_BALANCE)</param>
        public Task<OrderResult> SendOrder(ITradeSecurity trsec, Operation operation, decimal price, long qty, TransactionType orderType, ExecutionCondition executionCondition)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.NEW_ORDER,
                ACCOUNT = trsec.AccountID,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                QUANTITY = qty,
                OPERATION = operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
                PRICE = price,
                TYPE = orderType,
                EXECUTION_CONDITION = executionCondition,
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Отмена заявки.
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="OrderNum">Номер заявки</param>
        public Task<OrderResult> KillOrder(ITradeSecurity trsec, long OrderNum)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.KILL_ORDER,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                ACCOUNT = trsec.AccountID,
                ORDER_KEY = OrderNum,
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Отмена заявки.
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="OrderNum">Номер заявки</param>
        public Task<OrderResult> KillStopOrder(ITradeSecurity trsec, long OrderNum)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.KILL_STOP_ORDER,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                ACCOUNT = trsec.AccountID,
                STOP_ORDER_KEY = OrderNum,
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Отмена заявки.
        /// </summary>
        /// <param name="order">Информация по заявке, которую требуется отменить.</param>
        public Task<OrderResult> KillOrder(Order order)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.KILL_ORDER,
                ClassCode = order.ClassCode,
                SecCode = order.SecCode,
                ACCOUNT = order.Account,
                ORDER_KEY = order.OrderNum,
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Отмена Stop заявки.
        /// </summary>
        /// <param name="order">Информация по заявке, которую требуется отменить.</param>
        public Task<OrderResult> KillStopOrder(StopOrder order)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.KILL_STOP_ORDER,
                ClassCode = order.ClassCode,
                SecCode = order.SecCode,
                ACCOUNT = order.Account,
                STOP_ORDER_KEY = order.OrderNum,
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Отмена всех Stop заявок.
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// </summary>
        public Task<OrderResult> KillAllStopOrders(ITradeSecurity trsec)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.KILL_ALL_STOP_ORDERS,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                ACCOUNT = trsec.AccountID,
                OPERATION = TransactionOperation.B,
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Отмена всех Limit заявок.
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// </summary>
        public Task<OrderResult> KillAllOrders(ITradeSecurity trsec)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.KILL_ALL_ORDERS,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                ACCOUNT = trsec.AccountID,
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Отмена всех заявок на срочном рынке.
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="BaseSec">Базовый актив - обязательный параметр на срочном рынке</param>
        public Task<OrderResult> KillAllFuturesOrders(ITradeSecurity trsec, string BaseSec)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.KILL_ALL_FUTURES_ORDERS,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                ACCOUNT = trsec.AccountID,
                BASE_CONTRACT = BaseSec,
            };
            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Перемещение заявки на новую цену
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="OrderNum"> Перемещаемый ордер</param>
        /// <param name="new_price"> Новая цена</param>
        /// <param name="new_qty"> Новое количество (если менять количество)</param>
        /// <param name="mode"> Режим перемещения </param>
        /// <returns>OrderResult - результат выполнения</returns>
        public Task<OrderResult> MoveOrder(ITradeSecurity trsec, long OrderNum, decimal new_price, long new_qty, TransactionMode mode = TransactionMode.NewQty)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.MOVE_ORDERS,
                MODE = mode,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                ACCOUNT = trsec.AccountID,
                FIRST_ORDER_NUMBER = OrderNum,
                FIRST_ORDER_NEW_PRICE = new_price,
                FIRST_ORDER_NEW_QUANTITY = new_qty
            };

            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Перемещение 2х заявок на новую цену (По номеру ордера, новая цена и обьем берутся из параметров order1 и order2)
        /// </summary>
        /// <param name="order1"> Перемещаемый ордер 1 </param>
        /// <param name="order2"> Перемещаемый ордер 2 (опционально) </param>
        /// <param name="mode"> Режим перемещения </param>
        /// <returns>OrderResult - результат выполнения</returns>
        public Task<OrderResult> MoveOrders(Order order1, Order order2 = null, TransactionMode mode = TransactionMode.NewQty)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.MOVE_ORDERS,
                MODE = mode,
                ClassCode = order1.ClassCode,
                SecCode = order1.SecCode,
                ACCOUNT = order1.Account,
                FIRST_ORDER_NUMBER = order1.OrderNum,
                FIRST_ORDER_NEW_PRICE = order1.Price,
                FIRST_ORDER_NEW_QUANTITY = order1.Quantity,
            };

            if (order2 != null)
            {
                t.SECOND_ORDER_NUMBER = order2.OrderNum;
                t.SECOND_ORDER_NEW_PRICE = order2.Price;
                t.SECOND_ORDER_NEW_QUANTITY = order2.Quantity;
            };

            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        /// Перемещение заявки на новую цену
        /// </summary>
        /// <param name="trsec">Код класса инструмента, Код инструмента, Счет клиента</param>
        /// <param name="OrderNum"> Номер перемещаемой заявки</param>
        /// <param name="new_price"> Новая цена</param>
        /// <param name="new_qty"> Новое количество (если менять количество)</param>
        /// <returns>OrderResult - результат выполнения</returns>
        public Task<OrderResult> Move_Order(ITradeSecurity trsec, long OrderNum, decimal new_price, long? new_qty)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.MOVE_ORDERS,
                MODE = new_qty.HasValue ? TransactionMode.NewQty : TransactionMode.SameQty,
                ClassCode = trsec.ClassCode,
                SecCode = trsec.SecCode,
                ACCOUNT = trsec.AccountID,
                FIRST_ORDER_NUMBER = OrderNum,
                FIRST_ORDER_NEW_PRICE = new_price,
                FIRST_ORDER_NEW_QUANTITY = new_qty
            };

            return SendWaitTransactionAsync(t);
        }

        /// <summary>
        ///  Отправляет транзакцию на новый ордер на основе переданного orderNew
        ///  Возвращает ответ обработки транзакции
        /// </summary>
        /// <param name="orderNew"></param>
        /// <returns></returns>
        public Task<OrderResult> CreateOrder(Order orderNew)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.NEW_ORDER,
                ACCOUNT = orderNew.Account,
                ClassCode = orderNew.ClassCode,
                SecCode = orderNew.SecCode,
                QUANTITY = orderNew.Quantity,
                PRICE = orderNew.Price,
                OPERATION = orderNew.Flags.HasFlag(OrderTradeFlags.IsSell) ? TransactionOperation.S : TransactionOperation.B,
                TYPE = orderNew.Flags.HasFlag(OrderTradeFlags.IsLimit) ? TransactionType.L : TransactionType.M,
                CLIENT_CODE = orderNew.ClientCode,
            };

            //(orderNew.Flags.HasFlag(OrderTradeFlags.AllowDiffPrice)
            switch (orderNew.ExecType)
            {
                case OrderExecType.FillOrKill:
                    t.EXECUTION_CONDITION = ExecutionCondition.FILL_OR_KILL;
                    break;

                case OrderExecType.PlaceInQuery:
                    t.EXECUTION_CONDITION = ExecutionCondition.PUT_IN_QUEUE;
                    break;

                case OrderExecType.ImmediateOrCancel:
                    t.EXECUTION_CONDITION = ExecutionCondition.KILL_BALANCE;
                    break;
            };

            t.EXPIRY_DATE = orderNew.ExecType == OrderExecType.WhileThisSession ? "TODAY"
                : orderNew.ExecType == OrderExecType.GoodTillCancelled ? "GTC"
                : orderNew.ExpiryDate > DateTime.MinValue ? QuikDateTimeConverter.DateTimeToYYYYMMDD(orderNew.ExpiryDate)
                : "GTC";

            return SendWaitTransactionAsync(t);
        }

        internal static StopOrderKind ConvertStopOrderType(StopOrderType stopOrderType)
        {
            switch (stopOrderType)
            {
                case StopOrderType.SimpleStopOrder:
                    return StopOrderKind.SIMPLE_STOP_ORDER;

                case StopOrderType.TakeProfit:
                    return StopOrderKind.TAKE_PROFIT_STOP_ORDER;

                case StopOrderType.TakeProfitStopLimit:
                    return StopOrderKind.TAKE_PROFIT_AND_STOP_LIMIT_ORDER;

                /*
                 * TODO: case StopOrderType.AnotherInstCondition:
                 * return StopOrderKind.CONDITION_PRICE_BY_OTHER_SEC;
                 */

                case StopOrderType.StopLimitOnActiveOrderExecution:
                    return StopOrderKind.ACTIVATED_BY_ORDER_SIMPLE_STOP_ORDER;

                case StopOrderType.TakeProfitOnActiveOrderExecution:
                    return StopOrderKind.ACTIVATED_BY_ORDER_TAKE_PROFIT_STOP_ORDER;

                case StopOrderType.TPSLOnActiveOrderExecution:
                    return StopOrderKind.ACTIVATED_BY_ORDER_TAKE_PROFIT_AND_STOP_LIMIT_ORDER;

                default:
                    throw new Exception("Not implemented stop order type: " + stopOrderType);
            }
        }

        /// <summary>
        ///  Отправляет транзакцию на новый ордер на основе переданного orderNew
        ///  Возвращает ответ обработки транзакции
        /// </summary>
        /// <param name="orderNew"></param>
        /// <returns></returns>
        public Task<OrderResult> CreateStopOrder(StopOrder orderNew)
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.NEW_STOP_ORDER,
                ACCOUNT = orderNew.Account,
                ClassCode = orderNew.ClassCode,
                SecCode = orderNew.SecCode,
                STOPPRICE = orderNew.ConditionPrice,
                PRICE = orderNew.Price,
                QUANTITY = orderNew.Quantity,
                STOP_ORDER_KIND = ConvertStopOrderType(orderNew.StopOrderType),
                OPERATION = orderNew.Operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
                CLIENT_CODE = orderNew.ClientCode,
            };

            t.EXPIRY_DATE = orderNew.StopFlags.HasFlag(StopBehaviorFlags.ExpireEndOfDay)
                ? "TODAY"
                : orderNew.Expiry > DateTime.MinValue
                ? QuikDateTimeConverter.DateTimeToYYYYMMDD(orderNew.Expiry)
                : "GTC";

            if (orderNew.StopOrderType == StopOrderType.TakeProfitStopLimit)
            {
                if (orderNew.ActiveFromTime < orderNew.ActiveToTime && orderNew.ActiveToTime > TimeSpan.Zero)
                {
                    t.IS_ACTIVE_IN_TIME = YesOrNo.YES;
                    t.ACTIVE_FROM_TIME = orderNew.ActiveFromTime;
                    t.ACTIVE_TO_TIME = orderNew.ActiveToTime;
                }
                else
                {
                    t.IS_ACTIVE_IN_TIME = YesOrNo.NO;
                }
            }

            if (orderNew.StopOrderType == StopOrderType.TakeProfit || orderNew.StopOrderType == StopOrderType.TakeProfitOnActiveOrderExecution ||
                orderNew.StopOrderType == StopOrderType.TakeProfitStopLimit || orderNew.StopOrderType == StopOrderType.TPSLOnActiveOrderExecution)
            {
                t.OFFSET = orderNew.Offset;
                t.SPREAD = orderNew.Spread;
                t.OFFSET_UNITS = orderNew.OffsetUnit;
                t.SPREAD_UNITS = orderNew.SpreadUnit;
            }

            if (orderNew.StopOrderType == StopOrderType.TakeProfitStopLimit || orderNew.StopOrderType == StopOrderType.TPSLOnActiveOrderExecution)
            {
                t.STOPPRICE2 = orderNew.ConditionPrice2;
                t.MARKET_TAKE_PROFIT = orderNew.StopFlags.HasFlag(StopBehaviorFlags.MarketTakeProfit) ? YesOrNo.YES : YesOrNo.NO;
                t.MARKET_STOP_LIMIT = orderNew.StopFlags.HasFlag(StopBehaviorFlags.MarketStop) ? YesOrNo.YES : YesOrNo.NO;
            }

            if (orderNew.StopOrderType == StopOrderType.StopLimitOnActiveOrderExecution || orderNew.StopOrderType == StopOrderType.TakeProfitOnActiveOrderExecution
                || orderNew.StopOrderType == StopOrderType.TPSLOnActiveOrderExecution)
            {
                t.BASE_ORDER_KEY = orderNew.co_order_num;
                t.ACTIVATE_IF_BASE_ORDER_PARTLY_FILLED = orderNew.StopFlags.HasFlag(StopBehaviorFlags.ActivateOnPartial) ? YesOrNo.YES : YesOrNo.NO;
                t.USE_BASE_ORDER_BALANCE = orderNew.StopFlags.HasFlag(StopBehaviorFlags.UseRemains) ? YesOrNo.YES : YesOrNo.NO;
            }

            return SendWaitTransactionAsync(t);
        }
    }
}