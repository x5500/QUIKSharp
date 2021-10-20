// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using QUIKSharp.Functions;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.DataStructures;
using System.Collections.Concurrent;

#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
namespace QUIKSharp.TestQuik
{
    public class TestQuikEmulatorReply
    {
        public long TransId;
        public Order limitOrder;
        public StopOrder stopOrder;
    }

    public class TestQuikEmulator : IQuik, ITradeSecurity, ITransactionsFunctions, IIdentifyTransaction
    {
        bool IQuik.IsServiceConnected => true;
        public TimeSpan DefaultSendTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ICandleFunctions Candles => throw new NotImplementedException();
        public IClassFunctions Class => throw new NotImplementedException();
        public IDebugFunctions Debug { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IOrderBookFunctions OrderBook { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        private readonly TestOrders testOrders = new TestOrders();
        public IOrderFunctions Orders => testOrders;
        private readonly TestService testService = new TestService();
        public IServiceFunctions Service => testService;
        private readonly TestTrading testTrading = new TestTrading();
        public ITradingFunctions Trading => testTrading;
        public ITransactionsFunctions Transactions => this;
        public readonly TestEvents EventsCaller = new TestEvents();
        public IQuikEvents Events => EventsCaller;
        public string ClassCode => "ABC";
        public string SecCode => "DEF";
        public string AccountID => "ACCOUNT";
        public string ClientCode => "CLIENT";
        public string FirmId => "FIRM";
        private static long trans_id = 1000;

        /// <summary>
        /// Таблица лимитных ордеров, с ключом по ClientCode
        /// </summary>
        public readonly ConcurrentDictionary<string, Order> limit_orders = new ConcurrentDictionary<string, Order>();
        public Order GetLimitOrder(string ClientCode)
        {
            limit_orders.TryGetValue(ClientCode, out var order);
            return order;
        }

        /// <summary>
        /// Таблица стоп-ордеров, с ключем по string.Concat(SecCode, ":", ClassCode)
        /// </summary>
        public readonly ConcurrentDictionary<string, StopOrder> stop_orders = new ConcurrentDictionary<string, StopOrder>();


        protected readonly ConcurrentDictionary<string, TaskCompletionSource<TestQuikEmulatorReply>> Awaiters = new ConcurrentDictionary<string, TaskCompletionSource<TestQuikEmulatorReply>>();
        public void AddAwaiter(string ClientCode, TaskCompletionSource<TestQuikEmulatorReply> tcs)
        {
            if (!Awaiters.TryAdd(ClientCode, tcs))
            {
                Awaiters.TryRemove(ClientCode, out _);
                Awaiters.TryAdd(ClientCode, tcs);
            }
        }

        public StopOrder GetStopOrder(string ClientCode)
        {
            stop_orders.TryGetValue(ClientCode, out var order);
            return order;
        }

        public TestQuikEmulator()
        {
        }

        // ----------------------------------------- IIdentifyTransaction ------------------------------------------
        public long GetNextId() => Interlocked.Increment(ref trans_id);
        public long IdentifyTransaction(Transaction t)
        {
            if (!t.TRANS_ID.HasValue)
                t.TRANS_ID = GetNextId();
            return t.TRANS_ID.Value;
        }
        public long IdentifyTransactionReply(TransactionReply transReply) => transReply.TransID;
        public long IdentifyOrder(Order order) => order.TransID;
        public long IdentifyOrder(StopOrder order) => order.TransID;

        // ----------------------------------------- Test transactions ------------------------------------------
        IIdentifyTransaction ITransactionsFunctions.IdProvider { get => this; set => throw new NotImplementedException(); }
        public async Task<long> LuaNewTransactionID(long step, CancellationToken cancellationToken) => Interlocked.Increment(ref trans_id);
        public Task<long> SendTransaction(Transaction t, CancellationToken cancellationToken) => throw new NotImplementedException();
        // -------------------------------------------------------------------------------------
        /// <summary>
        /// При вызове SendTransactionAsync заполняется transaction
        /// ответом возвращаем transStatus
        /// Для ответа на этот асинхронный запрос вызывай метод SendTransReply()
        /// </summary>
        public async Task<TransactionResult> SendTransactionAsync(Transaction t, CancellationToken cancellationToken)
        {
            if (stop_orders.ContainsKey(t.CLIENT_CODE) || limit_orders.ContainsKey(t.CLIENT_CODE))
            {
                // Deny, alreay have orders for these keys
                var transReply = new TransactionReply()
                {
                    TransID = t.TRANS_ID.Value,
                    ClassCode = t.ClassCode,
                    SecCode = t.SecCode,
                    Account = t.ACCOUNT,
                    FirmID = t.FIRM_ID,
                    Brokerref = t.CLIENT_CODE,
                    Status = TransactionReplyStatus.RejectedByQuik,
                    ErrorCode = 1111,
                    ErrorSource = 2,
                };

                if (Awaiters.TryGetValue(t.CLIENT_CODE, out var tcs))
                {
                    tcs.TrySetResult(null);
                }

                var response = new TransactionResult
                {
                    Result = TransactionStatus.QuikError,
                    ResultMsg = $"Orders with ClientCode {t.CLIENT_CODE} already in the Dictionary, use Unique ClientCode each new request",
                    TransId = t.TRANS_ID.Value,
                };
                return response;
            }
            else
            {
                FormTransReply(t, out var transReply, out var limOrder, out var stopOrder);
                _ = Task.Run(() => EventsCaller.OnTransReplyCall(transReply));

                if (Awaiters.TryGetValue(t.CLIENT_CODE, out var tcs))
                {
                    tcs.TrySetResult(new TestQuikEmulatorReply { limitOrder = limOrder, stopOrder = stopOrder, TransId = t.TRANS_ID.Value });
                }

                var response = new TransactionResult
                {
                    Result = TransactionStatus.Success,
                    ResultMsg = "",
                    TransId = t.TRANS_ID.Value,
                };
                return response;

            }
        }
        // -------------------------------------------------------------------------------------
        internal static StopOrderType ConvertStopOrderType(StopOrderKind stopOrderKind)
        {
            switch (stopOrderKind)
            {
                case StopOrderKind.ACTIVATED_BY_ORDER_TAKE_PROFIT_STOP_ORDER:
                    return StopOrderType.TakeProfitOnActiveOrderExecution;
                case StopOrderKind.ACTIVATED_BY_ORDER_TAKE_PROFIT_AND_STOP_LIMIT_ORDER:
                    return StopOrderType.TPSLOnActiveOrderExecution;
                case StopOrderKind.ACTIVATED_BY_ORDER_SIMPLE_STOP_ORDER:
                    return StopOrderType.StopLimitOnActiveOrderExecution;
                case StopOrderKind.SIMPLE_STOP_ORDER:
                    return StopOrderType.SimpleStopOrder;
                case StopOrderKind.WITH_LINKED_LIMIT_ORDER:
                    return StopOrderType.WithLinkedOrder;
                case StopOrderKind.TAKE_PROFIT_STOP_ORDER:
                    return StopOrderType.TakeProfit;
                case StopOrderKind.TAKE_PROFIT_AND_STOP_LIMIT_ORDER:
                    return StopOrderType.TakeProfitStopLimit;
                default:
                    throw new Exception("Not implemented stop order type: " + stopOrderKind);
            }
        }
        // -------------------------------------------------------------------------------------

        public TimeSpan delay_lo = new TimeSpan(100);
        public TimeSpan delay_so = new TimeSpan(200);
        public TimeSpan delay_tr = new TimeSpan(500);

        /// <summary>
        /// Автоматически отвечать на все транзакции "TransactionStatus.Success",
        /// эмулировать успешное размещение ордеров
        /// </summary>
        /// 
        public async Task<TransactionWaitResult> SendWaitTransactionAsync(Transaction t, CancellationToken cancellationToken)
        {
            TransactionReply transReply;
            Order limOrder;
            StopOrder stopOrder;

            if (stop_orders.ContainsKey(t.CLIENT_CODE) || limit_orders.ContainsKey(t.CLIENT_CODE))
            {
                // Deny, alreay have orders for these keys
                limOrder = null;
                stopOrder = null;
                transReply = new TransactionReply()
                {
                    TransID = t.TRANS_ID.Value,
                    ClassCode = t.ClassCode,
                    SecCode = t.SecCode,
                    Account = t.ACCOUNT,
                    FirmID = t.FIRM_ID,
                    Brokerref = t.CLIENT_CODE,
                    Status = TransactionReplyStatus.RejectedByQuik,
                    ErrorCode = 1111,
                    ErrorSource = 2,
                };

                if (Awaiters.TryGetValue(t.CLIENT_CODE, out var tcs))
                {
                    tcs.TrySetResult(null);
                }

                var waitResult = new TransactionWaitResult()
                {
                    ResultMsg = $"Orders with ClientCode {t.CLIENT_CODE} already in the Dictionary, use Unique ClientCode each new request",
                    Status = TransactionStatus.QuikError,
                    transReply = transReply,
                    OrderNum = transReply?.OrderNum.Value ?? 0L,
                    TransId = t.TRANS_ID.Value,
                };
                return waitResult;
            }
            else
            {
                FormTransReply(t, out transReply, out limOrder, out stopOrder);
                if (limOrder != null)
                {
                    limit_orders.TryAdd(t.CLIENT_CODE, limOrder);
                    _ = Task.Delay(delay_lo).ContinueWith((ttt) => EventsCaller.OnOrderCall(limOrder));
                }

                if (stopOrder != null)
                {
                    stop_orders.TryAdd(t.CLIENT_CODE, stopOrder);
                    _ = Task.Delay(delay_so).ContinueWith((ttt) => EventsCaller.OnStopOrderCall(stopOrder));
                }

                if (Awaiters.TryGetValue(t.CLIENT_CODE, out var tcs))
                {
                    tcs.TrySetResult(new TestQuikEmulatorReply { limitOrder = limOrder, stopOrder = stopOrder, TransId = t.TRANS_ID.Value });
                }

                var waitResult = new TransactionWaitResult()
                {
                    ResultMsg = "TESTER",
                    Status = TransactionStatus.Success,
                    transReply = transReply,
                    TransId = t.TRANS_ID.Value,
                    OrderNum = transReply?.OrderNum.Value ?? 0L,
                };

                await Task.Delay(delay_tr).ConfigureAwait(false);
                return waitResult;
            }
        }

        private void FormTransReply(Transaction t, out TransactionReply transReply, out Order limOrder, out StopOrder stopOrder)
        {
            if (!t.TRANS_ID.HasValue || t.TRANS_ID <= 0)
                t.TRANS_ID = Interlocked.Increment(ref trans_id);

            transReply = new TransactionReply()
            {
                TransID = t.TRANS_ID.Value,
                ClassCode = t.ClassCode,
                SecCode = t.SecCode,
                Account = t.ACCOUNT,
                FirmID = t.FIRM_ID,
                Brokerref = t.CLIENT_CODE,
                Price = t.PRICE,
                OrderNum = (ulong)Interlocked.Increment(ref trans_id),
                Quantity = t.QUANTITY,
                Balance = t.QUANTITY,
                Status = TransactionReplyStatus.Executed,
            };

            limOrder = null;
            stopOrder = null;
            if (t.ACTION == TransactionAction.NEW_ORDER)
            {
                limOrder = new Order()
                {
                    TransID = t.TRANS_ID.Value,
                    OrderNum = transReply.OrderNum.Value,
                    SecCode = t.SecCode,
                    ClassCode = t.ClassCode,
                    ClientCode = t.CLIENT_CODE,
                    Account = t.ACCOUNT,
                    FirmId = t.FIRM_ID,
                    Price = t.PRICE,
                    Balance = t.QUANTITY,
                    Quantity = t.QUANTITY,
                    Operation = t.OPERATION == TransactionOperation.B ? Operation.Buy : Operation.Sell,
                };
                SetOrderState(State.Active, limOrder);
            }
            if (t.ACTION == TransactionAction.NEW_STOP_ORDER)
            {
                stopOrder = new StopOrder()
                {
                    TransID = t.TRANS_ID.Value,
                    OrderNum = transReply.OrderNum.Value,
                    SecCode = t.SecCode,
                    ClassCode = t.ClassCode,
                    ClientCode = t.CLIENT_CODE,
                    Account = t.ACCOUNT,
                    Price = t.PRICE,
                    Balance = t.QUANTITY,
                    Quantity = t.QUANTITY,
                    Operation = t.OPERATION == TransactionOperation.B ? Operation.Buy : Operation.Sell,
                    StopOrderType = ConvertStopOrderType(t.STOP_ORDER_KIND.Value),
                    ConditionPrice = t.STOPPRICE ?? 0m,
                    ConditionPrice2 = t.STOPPRICE2 ?? 0m,
                };

                SetOrderState(State.Active, stopOrder);

                if (t.STOP_ORDER_KIND == StopOrderKind.WITH_LINKED_LIMIT_ORDER)
                {
                    limOrder = new Order()
                    {
                        TransID = t.TRANS_ID.Value,
                        OrderNum = (ulong)Interlocked.Increment(ref trans_id),
                        SecCode = t.SecCode,
                        ClassCode = t.ClassCode,
                        ClientCode = t.CLIENT_CODE,
                        Account = t.ACCOUNT,
                        FirmId = t.FIRM_ID,
                        Price = t.PRICE,
                        Balance = t.QUANTITY,
                        Quantity = t.QUANTITY,
                        Operation = t.OPERATION == TransactionOperation.B ? Operation.Buy : Operation.Sell,
                    };
                    SetOrderState(State.Active, limOrder);

                    stopOrder.co_order_num = limOrder.OrderNum;
                    stopOrder.co_order_price = limOrder.Price;
                }
            }
        }

        public void CallOrderEvent(Order order, long Balance, State quikState)
        {
            SetOrderState(quikState, order);
            order.Balance = Balance;
            EventsCaller.OnOrderCall(order);
        }

        public void SetOrderState(State quikState, Order order)
        {
            order.Flags &= ~(OrderTradeFlags.Active | OrderTradeFlags.Canceled | OrderTradeFlags.Rejected);
            switch (quikState)
            {
                case State.Active:
                    order.Flags |= OrderTradeFlags.Active;
                    break;
                case State.Completed:
                    break;
                case State.Canceled:
                    order.Flags |= OrderTradeFlags.Canceled;
                    break;
                case State.Rejected:
                    order.Flags |= OrderTradeFlags.Rejected;
                    break;
            }
        }

        public void SetOrderState(State quikState, StopOrder order)
        {
            switch (quikState)
            {
                case State.Active:
                    order.Flags = (order.Flags | StopOrderFlags.Active) & ~StopOrderFlags.Canceled;
                    break;
                case State.Completed:
                    order.Flags &= ~(StopOrderFlags.Active | StopOrderFlags.Canceled);
                    break;
                case State.Canceled:
                    order.Flags = (order.Flags | StopOrderFlags.Canceled) & ~StopOrderFlags.Active;
                    break;
                case State.Rejected:
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void CallOrderEvent(Order order)
        {
            var copy = new Order();
            TestUtils.CopyObject(order, copy);
            EventsCaller.OnOrderCall(copy);
        }
        public Order MakeLinkedOrder(StopOrder stopOrder)
        {
            var Order = new Order()
            {
                TransID = stopOrder.TransID,
                OrderNum = (ulong)Interlocked.Increment(ref trans_id),
                SecCode = stopOrder.SecCode,
                ClassCode = stopOrder.ClassCode,
                ClientCode = stopOrder.ClientCode,
                Account = stopOrder.Account,
                Price = stopOrder.Price,
                Balance = stopOrder.Quantity,
                Quantity = stopOrder.Quantity,
                Operation = stopOrder.IsSell ? Operation.Sell : Operation.Buy,
                Linkedorder = stopOrder.OrderNum,
                Flags = OrderTradeFlags.Active | OrderTradeFlags.IsLimit | OrderTradeFlags.LinkedOrder | OrderTradeFlags.AllowDiffPrice,
            };

            if (stopOrder.IsSell)
                Order.Flags |= OrderTradeFlags.IsSell;

            return Order;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        /// <param name="Price"></param>
        /// <param name="Qty">Last deal traded qty</param>
        public void CallTradeEvent(Order order, decimal Price, long Qty)
        {
            var trade = new Trade()
            {
                Account = order.Account,
                ClassCode = order.ClassCode,
                SecCode = order.SecCode,
                ClientCode = order.ClientCode,
                FirmId = order.FirmId,
                OrderNum = order.OrderNum,
                TradeNum = Interlocked.Increment(ref trans_id),
                Price = Price,
                Quantity = Qty,
                BrokerComission = 0,
            };
            EventsCaller.OnTradeCall(trade);
        }

        public void CallStopOrderEvent(StopOrder order)
        {
            var copy = new StopOrder();
            TestUtils.CopyObject(order, copy);
            EventsCaller.OnStopOrderCall(copy);
        }
    }
}
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
