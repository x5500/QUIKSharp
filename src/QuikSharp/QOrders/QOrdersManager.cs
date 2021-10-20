// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using HellBrick.Collections;
using NLog;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Functions;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.QOrders
{
    public delegate void LimitOrderEventHandler(Object sender, LimitOrderEventArgs eventArgs);

    public delegate void StopOrderEventHandler(Object sender, StopOrderEventArgs eventArgs);

    public enum QOrdersActionType
    {
        PlaceOrder,
        MoveOrder,
        KIllOrder,
    }

    public struct QOrderActionResult
    {
        public bool Result;
        public String ResultMsg;
    }

    public struct QOrdersAction
    {
        public QOrdersActionType action;
        public CancellationToken cancellationToken;
        public QOrder qOrder;
        /// <summary>
        /// Для перемещения ордеров
        /// </summary>
        public decimal new_price;
        /// <summary>
        /// Для перемещения ордеров
        /// </summary>
        public long new_qty;
        public int retry;
    }

    public class LimitOrderEventArgs : EventArgs
    {
        public QLimitOrder limitOrder;
    };

    public class QOrdersManager : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Нужна таблица актуальных limit-заявок
        /// </summary>
        public readonly ConcurrentDictionary<ulong, QLimitOrder> limit_orders = new ConcurrentDictionary<ulong, QLimitOrder>();

        /// <summary>
        ///  Нужна таблица ответов о сделках на актуальные limit-заявки.
        ///  TODO: Либо удалить, либо как то использовать
        /// </summary>
        public readonly ConcurrentDictionary<long, Trade> limit_trades = new ConcurrentDictionary<long, Trade>();

        /// <summary>
        /// Нужна таблица актуальных Stop-заявок
        /// </summary>
        public readonly ConcurrentDictionary<ulong, QStopOrder> stop_orders = new ConcurrentDictionary<ulong, QStopOrder>();

        /// <summary>
        /// Здесь храним связи между номерами отправленных транзакций и ордерами
        /// </summary>
        private readonly ConcurrentDictionary<long, QOrder> qorderByTransId = new ConcurrentDictionary<long, QOrder>();

        /// <summary>
        /// Лок на обработку событий ордера.
        /// Обрабатываем только одно событие за раз
        /// </summary>
        private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// Очередь задач на асинхронное выполнение
        /// </summary>
        private readonly AsyncQueue<QOrdersAction> actionQuery = new AsyncQueue<QOrdersAction>();

        private Task _actionTask;

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly AsyncManualResetEvent NotifyOnConnected = new AsyncManualResetEvent();

        private bool IsInitialized = false;

        private IQuik quik;

        /// <summary>
        ///  Менеджер заявок Quik
        /// </summary>
        /// <param name="quik"></param>
        /// <param name="timeout_wait_order">Макс. время (мкс.) ожидания выполнения транзакции сервером брокера. Если превышен, запрос будет повторен, и так до победного.</param>
        /// <param name="max_parallel_tasks">Макс. количество параллельно выполняемых задач ( -1 - не ограниченно, параллелизм будет ограничен лишь возможностями используемого планировщика задач)</param>
        /// <param name="tasks_query_capacity">Макс. длина очереди задач по операциям над ордерами</param>
        /// <param name="delay_on_timeout"> Пауза перед повторной попыткой, если словили Таймаут от сервера.</param>
        public QOrdersManager(IQuik quik, int timeout_wait_order = 20000, int max_parallel_tasks = -1, int tasks_query_capacity = 100, int delay_on_timeout = 10)
        {
            Timeout_ms = timeout_wait_order;
            this.Delay_on_Timeout = delay_on_timeout;

/*            actionQuery = new ActionBlock<QOrdersAction>(action: ActionBlockConsumer, dataflowBlockOptions: new ExecutionDataflowBlockOptions()
            {
                SingleProducerConstrained = false,
                BoundedCapacity = tasks_query_capacity,
                MaxDegreeOfParallelism = max_parallel_tasks,
                TaskScheduler = TaskScheduler.Current,
                CancellationToken = cancellation.Token,
            });
*/
            // Request Task
            // NB we use the token for signalling, could use a simple TCS
            var task_options = TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.PreferFairness;
            _actionTask = Task.Factory.StartNew(RequestTaskAction, CancellationToken.None, task_options, TaskScheduler.Default);

            LinkQuik(quik);
        }

        private async void RequestTaskAction()
        {
            var cancelToken = cancellation.Token;
            // Enter the listening loop.
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    QOrdersAction action = await actionQuery.TakeAsync(cancelToken).ConfigureAwait(false);
                    await ActionBlockConsumer(action).ConfigureAwait(false);
                }
                catch (Exception e) 
                {
                    logger.Fatal(e, $"Exception in RequestTaskAction loop: {e.Message}\n  --- Exception Trace: ---- \n{e.StackTrace}\n--- Exception trace ----");
                }
            }
        }


        public event LimitOrderEventHandler OnNewLimitOrder;

        public event StopOrderEventHandler OnNewStopOrder;

        public event LimitOrderEventHandler OnUpdateLimitOrder;

        public event StopOrderEventHandler OnUpdateStopOrder;

        /// <summary>
        /// Пауза (ms) перед повторной попыткой, если словили Таймаут от сервера квик.
        /// </summary>
        public int Delay_on_Timeout { get; set; } = 20;

        /// <summary>
        /// Wait Order timeout. Таймаут ожидания ответа на транзакцию о размещении/перемещении/снятии ордера
        /// </summary>
        public int Timeout_ms { get; set; } = 20000;

        /// <summary>
        /// Очищает все таблицы менеджера
        /// </summary>
        public void ClearTables()
        {
            limit_orders.Clear();
            stop_orders.Clear();
            limit_trades.Clear();
        }

        /// <summary>
        /// Задача на снятие ордера. Выполняется до результата (успех, ошибка, отмена)
        /// </summary>
        /// <param name="qOrder">Снимаемый ордер</param>
        /// <param name="retry">Количество попыток выполнить задачу</param>
        /// <param name="cancellation_token">Внешний CancellationToken на отмену задачи</param>
        /// <returns></returns>
        public async Task<QOrderActionResult> KillOrderAsync(QOrder qOrder, CancellationToken cancellation_token, int retry = 1)
        {
            switch (qOrder.KillMoveState)
            {
                case QOrderKillMoveState.ErrorRejected:
                case QOrderKillMoveState.NoKill:
                case QOrderKillMoveState.WaitMove:
                    qOrder.KillMoveState = QOrderKillMoveState.WaitKill;
                    break;
                case QOrderKillMoveState.WaitKill:
                    break;
                case QOrderKillMoveState.RequestedKill:
                case QOrderKillMoveState.RequestedMove:
                    // Already in process
                    return new QOrderActionResult() { Result = false, ResultMsg = "Already executing KillOrder Task: " + qOrder.KillMoveState.ToString() }; ;
                case QOrderKillMoveState.Killed:
                    return new QOrderActionResult() { Result = true, ResultMsg = "Order " + qOrder.State.ToString() }; ;
            }

            CancellationToken my_cancellation_token;
            if (cancellation_token != CancellationToken.None)
            {
                var my_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation_token, cancellation.Token);
                my_cancellation_token = my_cts.Token;
            }
            else
            {
                my_cancellation_token = cancellation.Token;
            }

            while (!cancellation.IsCancellationRequested)
            {
                if (qOrder.KillMoveState != QOrderKillMoveState.WaitKill)
                    break;

                if (qOrder.State == QOrderState.None)
                {
                    logger.Warn($"Trying to kill Order {qOrder.OrderNum} with state = {qOrder.State}. QuikState: {qOrder.QuikState} ");
                    break;
                }

                if (retry-- <= 0)
                    break;

                if (qOrder.State == QOrderState.WaitPlacement)
                {
                    // Запрос на размещение еще не отправлен -> просто удаляем его из обработки
                    qOrder.State = QOrderState.Killed;
                    qOrder.KillMoveState = QOrderKillMoveState.Killed;
                    // TODO: удалить из списка ожидающих отправку на размещение
                    return new QOrderActionResult() { Result = true, ResultMsg = "Order removed from Wait Query" }; ;
                }

                if ((qOrder.State == QOrderState.RequestedPlacement) || (!qOrder.OrderNum.HasValue))
                {
                    // Отправлен запрос на размещение ордера, но ответ еще не пришел
                    // Прийдется дождаться ответа, получить номер ордера и сразу же отправить запрос на удаление
                    qOrder.KillMoveState = QOrderKillMoveState.WaitKill;
                    // TODO: Как то ждать появления OrderNum
                    await Task.Delay(50, my_cancellation_token).ConfigureAwait(false);
                    continue;
                }

                if (qOrder.State == QOrderState.Placed)
                {
                    // акутальный ордер, он размещен на бирже (и не находится в процессе размещения)
                    var t = qOrder.KillOrderTransaction();
                    qOrder.KillMoveState = QOrderKillMoveState.RequestedKill;
                    var timeoutCancel = new CancellationTokenSource(Timeout_ms);
                    var linked_CTS = CancellationTokenSource.CreateLinkedTokenSource(my_cancellation_token, timeoutCancel.Token);
                    var reply = await quik.Transactions.SendWaitTransactionAsync(t, linked_CTS.Token).ConfigureAwait(false);
                    switch (reply.Status)
                    {
                        case TransactionStatus.Success:
                            qOrder.KillMoveState = QOrderKillMoveState.Killed;
                            if (reply.transReply != null)
                            {
                                long Balance = reply.transReply.Balance.Value;
                                qOrder.SetBalance(Balance, false);
                                qOrder.State = QOrderState.Killed;
                            }
                            return new QOrderActionResult() { Result = true, ResultMsg = reply.ResultMsg };

                        case TransactionStatus.LuaException:
                        case TransactionStatus.TransactionException:
                        case TransactionStatus.QuikError:
                        case TransactionStatus.FailedToSend:
                            qOrder.KillMoveState = QOrderKillMoveState.ErrorRejected;
                            Call_OnTransacError(QOrdersActionType.KIllOrder, qOrder, reply.ResultMsg);
                            return new QOrderActionResult() { Result = false, ResultMsg = reply.ResultMsg };

                        case TransactionStatus.TimeoutWaitReply:
                        case TransactionStatus.SendRecieveTimeout:
                            qOrder.KillMoveState = QOrderKillMoveState.WaitKill;
                            Call_OnTransacError(QOrdersActionType.KIllOrder, qOrder, reply.ResultMsg);
                            if (Delay_on_Timeout > 0) await Task.Delay(Delay_on_Timeout, my_cancellation_token).ConfigureAwait(false);
                            continue;
                        case TransactionStatus.NoConnection:
                            qOrder.KillMoveState = QOrderKillMoveState.WaitKill;
                            // TODO: Надо придумать алгоритм поведения при отсутствии связи, через сколько пробуем еще раз, чтобы не спамить
                            logger.Debug($"Call_OnNoConnection on action 'KillOrderAsync' for order {qOrder.ClassCode}:{qOrder.SecCode} {qOrder.Operation} {qOrder.Qty} qty on {qOrder.Price}");
                            await NotifyOnConnected.WaitAsync.ConfigureAwait(false);
                            continue;
                    }
                }

                if ((qOrder.State == QOrderState.Executed) || (qOrder.State == QOrderState.Killed))
                {
                    qOrder.KillMoveState = QOrderKillMoveState.Killed;
                    return new QOrderActionResult() { Result = true, ResultMsg = "Order " + qOrder.State.ToString() }; ;
                }
                // retry;
            }

            return new QOrderActionResult() { Result = false, ResultMsg = "Task cancelled." }; ;
        }

        /// <summary>
        /// Линкуется к экземпляру квика, подписывается на события
        /// </summary>
        /// <param name="quik"></param>
        public void LinkQuik(IQuik quik)
        {
            IsInitialized = false;
            UnlinkQuik();

            this.quik = quik;
            quik.Events.OnOrder += Events_OnOrder;
            quik.Events.OnStopOrder += Events_OnStopOrder;
            quik.Events.OnTrade += Events_OnTrade;
            quik.Events.OnConnected += Events_OnConnected;
            quik.Events.OnConnectedToQuik += Events_OnConnectedToQuik;
            quik.Events.OnDisconnectedFromQuik += Events_OnDisconnectedFromQuik;

            bool isServiceConnected = quik.IsServiceConnected;
            if (isServiceConnected && !IsInitialized)
            {
                IsInitialized = true;
                Task.Factory.StartNew(() => InitOrdersListAsync(), cancellation.Token)
                .ContinueWith((task) =>
                {
                    logger.Debug($"InitOrdersListAsync() task completed, status: {task.Status}");
                });
            }
        }

        /// <summary>
        /// Задача на перемещение Лимитного ордера на срочном рынке. Выполняется до результата (успех, ошибка, отмена)
        /// Ордер по номеру OrderNum перемещается на цену new_price и количество new_qty
        /// После успешного выполнения, будет размещен новый лимитный ордер, а старый закрыт.
        /// (По факту, биржа убивает старый лимитник и размещает новый, будет новый номер лимитника.)
        /// </summary>
        /// <param name="qOrder">Передвигаемый ордер</param>
        /// <param name="new_price">Новая цена</param>
        /// <param name="new_qty">Новое количество</param>
        /// <param name="cancellation_token">Токен отмены задачи</param>
        /// <param name="retry">Количество попыток выполнить задачу</param>
        /// <returns></returns>
        public async Task<QOrderActionResult> MoveLimOrderAsync(QLimitOrder qOrder, decimal new_price, long new_qty, CancellationToken cancellation_token, int retry = 1)
        {
            if (qOrder.State != QOrderState.Placed)
                return new QOrderActionResult() { Result = false, ResultMsg = $"Order is not in placed state. [{qOrder.State}]" };

            switch (qOrder.KillMoveState)
            {
                case QOrderKillMoveState.WaitKill:
                case QOrderKillMoveState.RequestedKill:
                case QOrderKillMoveState.Killed:
                case QOrderKillMoveState.ErrorRejected:
                    return new QOrderActionResult() { Result = false, ResultMsg = $"Can't move order: [{qOrder.KillMoveState}]" };
                case QOrderKillMoveState.NoKill:
                    // Let's work
                    qOrder.KillMoveState = QOrderKillMoveState.WaitMove;
                    break;
                case QOrderKillMoveState.WaitMove:
                case QOrderKillMoveState.RequestedMove:
                    break;
            }

            CancellationToken my_cancellation_token;
            if (cancellation_token != CancellationToken.None)
            {
                var my_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation_token, cancellation.Token);
                my_cancellation_token = my_cts.Token;
            }
            else
                my_cancellation_token = cancellation.Token;

            while (!my_cancellation_token.IsCancellationRequested)
            {
                if (qOrder.KillMoveState != QOrderKillMoveState.WaitMove)
                    return new QOrderActionResult() { Result = false, ResultMsg = "qOrder.State != QOrderState.WaitMove" };

                if (retry-- <= 0)
                    break;

                var t = qOrder.MoveOrderTransaction(new_price, new_qty);
                var timeoutCancel = new CancellationTokenSource(Timeout_ms);
                var linked_CTS = CancellationTokenSource.CreateLinkedTokenSource(my_cancellation_token, timeoutCancel.Token);

                qOrder.KillMoveState = QOrderKillMoveState.RequestedMove;
                var reply = await quik.Transactions.SendWaitTransactionAsync(t, linked_CTS.Token).ConfigureAwait(false);
                switch (reply.Status)
                {
                    case TransactionStatus.Success:

                        ulong NewOrderNum = reply.transReply?.OrderNum ?? reply.OrderNum;
                        if (NewOrderNum <= 0)
                            throw new ArgumentOutOfRangeException("OrderNum", "Expected OrderNum > 0");

                        // Prev Order Killed. 
                        // Небольшой хак, чтобы обеспечить последовательность вызова коллбеков:
                        // 1. сначала о перемещении ордера.
                        // 2. потом о снятии старого.
                        // 3. потом о размещении нового.
                        bool just_killed_old = false;
                        bool just_placed_new = false;
                        QLimitOrder newqOrder = null;
                        rwLock.EnterWriteLock();
                        try
                        {
                            just_killed_old = qOrder.State != QOrderState.Killed;
                            if (just_killed_old)
                                qOrder.SetQuikState(DataStructures.State.Canceled, true); // no call events

                            if (!limit_orders.TryGetValue(NewOrderNum, out newqOrder))
                            {
                                just_placed_new = true;
                                newqOrder = new QLimitOrder(qOrder, CopyQtyMode.QtyLeft);
                                newqOrder.OrderNum = NewOrderNum;

                                // Небольшой хак, чтобы обеспечить последовательность вызова коллбеков
                                newqOrder.SetQuikState(DataStructures.State.Active, true); // no call events

                                if (!limit_orders.TryAdd(NewOrderNum, newqOrder))
                                    throw new Exception($"AddNewLimitOrder: Failed TryAdd order {NewOrderNum} to limit_orders");

                                // При перемещении заявок, в TransReply поле Balance = 0.
                                // Поэтому мы не используем функцию UpdateFrom(transReply), а заполняем поля тут.                            
                                // Если мы получили ответ через TransReply, то обрабатываем поля из него
                                if (reply.transReply != null)
                                {
                                    newqOrder.TransID = reply.transReply.TransID;
                                    if (reply.transReply.Price.HasValue)
                                        newqOrder.Price = reply.transReply.Price.Value;

                                    if (reply.transReply.Quantity.HasValue)
                                        newqOrder.SetQty(reply.transReply.Quantity.Value, reply.transReply.Quantity.Value);
                                }
                            }
                        }
                        finally
                        {
                            rwLock.ExitWriteLock();
                        }

                        // Небольшой хак, чтобы обеспечить последовательность вызова коллбеков:
                        qOrder.CallEvent_OnMoved(newqOrder);

                        if (just_killed_old)
                            qOrder.CallEvent_OnKilled();

                        if (just_placed_new)
                            newqOrder.CallEvent_OnPlaced();

                        return new QOrderActionResult() { Result = true, ResultMsg = reply.ResultMsg };

                    case TransactionStatus.LuaException:
                    case TransactionStatus.TransactionException:
                    case TransactionStatus.QuikError:
                    case TransactionStatus.FailedToSend:
                        qOrder.State = QOrderState.ErrorRejected;
                        Call_OnTransacError(QOrdersActionType.MoveOrder, qOrder, reply.ResultMsg);
                        return new QOrderActionResult() { Result = false, ResultMsg = reply.ResultMsg };

                    case TransactionStatus.TimeoutWaitReply:
                    case TransactionStatus.SendRecieveTimeout:
                        qOrder.KillMoveState = QOrderKillMoveState.WaitMove;
                        if (Delay_on_Timeout > 0) await Task.Delay(Delay_on_Timeout, my_cancellation_token).ConfigureAwait(false);
                        Call_OnTransacError(QOrdersActionType.MoveOrder, qOrder, reply.ResultMsg);
                        break;

                    case TransactionStatus.NoConnection:
                        qOrder.KillMoveState = QOrderKillMoveState.WaitMove;
                        // TODO: Надо придумать алгоритм поведения при отсутствии связи, через сколько пробуем еще раз, чтобы не спамить
                        logger.Debug($"Call_OnNoConnection on action 'MoveOrderAsync' for order {qOrder.ClassCode}:{qOrder.SecCode} {qOrder.Operation} {qOrder.Qty} qty on {qOrder.Price}");
                        await NotifyOnConnected.WaitAsync.ConfigureAwait(false);
                        break;
                }
                // Try one more time
            }

            if (qOrder.KillMoveState == QOrderKillMoveState.WaitMove)
                qOrder.KillMoveState = QOrderKillMoveState.NoKill;
            return new QOrderActionResult() { Result = false, ResultMsg = "Task cancelled." }; ;
        }

        /// <summary>
        /// Задача на размещение ордера. Выполняется до результата (успех, ошибка, отмена)
        /// </summary>
        /// <param name="qOrder">Размещаемый ордер</param>
        /// <param name="retry">Количество попыток выполнить задачу</param>
        /// <param name="cancellation_token">Внешний CancellationToken на отмену задачи</param>
        /// <returns></returns>
        public async Task<QOrderActionResult> PlaceOrderAsync(QOrder qOrder, CancellationToken cancellation_token, int retry = 1)
        {
            if (logger.IsTraceEnabled)
                logger.Trace($"PlaceOrderAsync for order {qOrder.ClassCode}:{qOrder.SecCode} {qOrder.Operation} {qOrder.Qty} qty on {qOrder.Price}");

            if (qOrder.State == QOrderState.None)
                qOrder.State = QOrderState.WaitPlacement;
            
            while (!cancellation.IsCancellationRequested)
            {
                if (retry-- <= 0)
                    break;

                if (qOrder.State != QOrderState.WaitPlacement)
                    return new QOrderActionResult() { Result = false, ResultMsg = "qOrder.State != QOrderState.WaitPlacement" };

                var t = qOrder.PlaceOrderTransaction();

                var linked_CTS = CancellationTokenSource.CreateLinkedTokenSource(this.cancellation.Token, cancellation_token);
                linked_CTS.CancelAfter(Timeout_ms);
                qOrder.State = QOrderState.RequestedPlacement;

                var trans_id = quik.Transactions.IdProvider.IdentifyTransaction(t);
                qorderByTransId.AddOrUpdate(trans_id, qOrder, (k, v) => qOrder);

                TransactionWaitResult reply = await quik.Transactions.SendWaitTransactionAsync(t, linked_CTS.Token).ConfigureAwait(false);

                if (logger.IsTraceEnabled)
                    logger.Trace($"PlaceOrderAsync for order {qOrder.ClassCode}:{qOrder.SecCode} {qOrder.Operation} {qOrder.Qty} qty on {qOrder.Price} => TransID: {t.TRANS_ID} => {reply.Status}, OrderNum: {reply.transReply?.OrderNum}");

                switch (reply.Status)
                {
                    case TransactionStatus.Success:
                        bool event_new_lo = false;
                        bool event_new_so = false;
                        if (reply.transReply != null)
                        {
                            rwLock.EnterWriteLock();
                            try
                            {
                                if (qorderByTransId.TryRemove(trans_id, out _))
                                {
                                    ulong OrderNum = reply.transReply?.OrderNum ?? reply.OrderNum;
                                    if (OrderNum <= 0)
                                        throw new ArgumentOutOfRangeException("OrderNum", "Expected OrderNum > 0");

                                    qOrder.OrderNum = OrderNum;
                                    qOrder.State = QOrderState.Placed;

                                    if (typeof(QLimitOrder).IsInstanceOfType(qOrder))
                                    {
                                        ((QLimitOrder)qOrder).UpdateFrom(reply.transReply);

                                        // Проверяем, есть ли стоп-ордера, которые зависят от этого лимитного ордера
                                        AddCoLinkedStopOrders((QLimitOrder)qOrder, false);

                                        // проверять, не стало ли известно, каким лимитным заявкам соответствуют OnTrade(), которые пришли раньше, чем OnTransReply().
                                        AddAdvanceTrades((QLimitOrder)qOrder);

                                        event_new_lo = limit_orders.TryAdd(OrderNum, (QLimitOrder)qOrder);
                                    }
                                    else
                                    {
                                        ((QStopOrder)qOrder).UpdateFrom(reply.transReply);
                                        event_new_so = stop_orders.TryAdd(OrderNum, (QStopOrder)qOrder);
                                        AddLinkedorCoOrders((QStopOrder)qOrder, false);
                                    }
                                }
                            }
                            finally
                            {
                                rwLock.ExitWriteLock();
                            }
                        }

                        if (event_new_lo)
                            Call_OnNewLimitOrder((QLimitOrder)qOrder);

                        if (event_new_so)
                            Call_OnNewStopOrder((QStopOrder)qOrder);

                        return new QOrderActionResult() { Result = true, ResultMsg = reply.ResultMsg };

                    case TransactionStatus.LuaException:
                    case TransactionStatus.TransactionException:
                    case TransactionStatus.QuikError:
                    case TransactionStatus.FailedToSend:
                        qOrder.State = QOrderState.ErrorRejected;
                        qorderByTransId.TryRemove(trans_id, out _);

                        Call_OnTransacError(QOrdersActionType.PlaceOrder, qOrder, reply.ResultMsg);
                        return new QOrderActionResult() { Result = false, ResultMsg = reply.ResultMsg };

                    case TransactionStatus.TimeoutWaitReply:
                    case TransactionStatus.SendRecieveTimeout:
                        qOrder.State = QOrderState.WaitPlacement;
                        qorderByTransId.TryRemove(trans_id, out _);

                        Call_OnTransacError(QOrdersActionType.PlaceOrder, qOrder, reply.ResultMsg);
                        if (Delay_on_Timeout > 0)
                        {
                            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(this.cancellation.Token, cancellation_token))
                            {
                                await Task.Delay(Delay_on_Timeout, cts.Token).ConfigureAwait(false);
                            }
                        }
                        break;

                    case TransactionStatus.NoConnection:
                        qOrder.State = QOrderState.WaitPlacement;
                        qorderByTransId.TryRemove(trans_id, out _);

                        Call_OnTransacError(QOrdersActionType.PlaceOrder, qOrder, reply.ResultMsg);
                        await NotifyOnConnected.WaitAsync.ConfigureAwait(false);
                        break;
                }
                // Try one more time
            }
            return new QOrderActionResult() { Result = false, ResultMsg = "Task cancelled." }; ;
        }

        /// <summary>
        /// Удаляет лимитный ордер из таблиц
        /// НЕ проверяет состояние ордера (активный или нет)
        /// </summary>
        /// <param name="order"></param>
        public void RemoveOrder(QLimitOrder order)
        {
            if (!order.OrderNum.HasValue)
                return;

            limit_orders.TryRemove(order.OrderNum.Value, out order);
            foreach (var tr in limit_trades)
            {
                if (tr.Value.OrderNum == order.OrderNum)
                    limit_trades.TryRemove(tr.Key, out var _);
            }
        }

        /// <summary>
        /// Удаляет стоп-ордер и все связанные с ним лимитные ордера из таблиц
        /// НЕ проверяет состояние ордера (активный или нет)
        /// </summary>
        /// <param name="order"></param>
        public void RemoveOrder(QStopOrder order)
        {
            if (!order.OrderNum.HasValue)
                return;

            stop_orders.TryRemove(order.OrderNum.Value, out order);
            if (order.ChildLimitOrder != null)
                RemoveOrder(order.ChildLimitOrder);
            if (order.CoOrder != null)
                RemoveOrder(order.CoOrder);
        }

        /// <summary>
        /// Поместить в очередь на выполнение задачу
        /// </summary>
        /// <param name="qOrder"></param>
        public void RequestKillOrder(QOrder qOrder)
        {
            qOrder.KillMoveState = QOrderKillMoveState.WaitKill;
            actionQuery.Add(new QOrdersAction
            {
                action = QOrdersActionType.KIllOrder,
                qOrder = qOrder,
                cancellationToken = CancellationToken.None,
            });            
        }

        /// <summary>
        /// Поместить в очередь на выполнение задачу
        /// Переместить лимитный ордер на рынке FORTS. (По факту, биржа убивает старый лимитник и размещает новый, будет новый номер лимитника)
        /// </summary>
        /// <param name="qOrder">Перемещаемый ордер (По факту, биржа убивает старый лимитник и размещает новый, будет новый номер лимитника)</param>
        /// <param name="new_price">Новая цена</param>
        /// <param name="new_qty">Новое количество</param>
        public void RequestMoveOrder(QLimitOrder qOrder, decimal new_price, long new_qty)
        {
            actionQuery.Add(new QOrdersAction
            {
                action = QOrdersActionType.MoveOrder,
                qOrder = qOrder,
                cancellationToken = CancellationToken.None,
                new_price = new_price,
                new_qty = new_qty,
            });
        }

        /// <summary>
        /// Поместить в очередь на выполнение задачу
        /// </summary>
        /// <param name="qOrder"></param>
        public void RequestPlaceOrder(QOrder qOrder)
        {
            actionQuery.Add(new QOrdersAction
            {
                action = QOrdersActionType.PlaceOrder,
                qOrder = qOrder,
                cancellationToken = CancellationToken.None,
            });
        }

        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты)

                    // Сообщает блоку потока данных, что он больше не должен принимать и создавать сообщения и поглощать отложенные сообщения.
                    NotifyOnConnected.Cancel();

                    cancellation.Cancel();

                    // Wait for all messages to propagate through the network.
                    if (_actionTask != null)
                    {
                        _actionTask.Wait(5000);
                        _actionTask = null;
                    }

                    UnlinkQuik();

                    limit_orders.Clear();
                    stop_orders.Clear();
                    limit_trades.Clear();

                    cancellation.Dispose();
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // TODO: установить значение NULL для больших полей
                disposedValue = true;
            }
        }

        private static void Call_OnTransacError(QOrdersActionType action, QOrder qOrder, string resultMsg)
        {
            // Вызывается из задачи
            logger.Debug($"Call_OnTransacError on action {action}: {resultMsg}  for order {qOrder.ClassCode}:{qOrder.SecCode} {qOrder.Operation} {qOrder.Qty} qty on {qOrder.Price}");
        }

        private Task ActionBlockConsumer(QOrdersAction action)
        {
            try
            {
                switch (action.action)
                {
                    case QOrdersActionType.PlaceOrder:
                        return PlaceOrderAsync(action.qOrder, action.cancellationToken);

                    case QOrdersActionType.MoveOrder:
                        return MoveLimOrderAsync((QLimitOrder)action.qOrder, action.new_price, action.new_qty, action.cancellationToken);

                    case QOrdersActionType.KIllOrder:
                        return KillOrderAsync(action.qOrder, action.cancellationToken);

                    default:
                        return Task.FromException(new InvalidOperationException($"QOrder ActionBlockConsumer Failed: Not implemented for '{action.action}'"));
                }
            }
            catch (Exception e)
            {
                logger.Fatal(e, $"Exception in ActionBlockConsumer: {e.Message}\n  --- Exception Trace: ---- \n{e.StackTrace}\n--- Exception trace ----");
                return Task.FromException(e);
            }
        }

        /// <summary>
        /// Периодически проверять, не стало ли известно, каким лимитным заявкам соответствуют OnTrade(), которые пришли раньше, чем OnTransReply().
        /// </summary>
        /// <param name="limitOrder"></param>
        private void AddAdvanceTrades(QLimitOrder limitOrder)
        {
            long traded_qty = 0;
            foreach (var trade in limit_trades)
                if (trade.Value.OrderNum == limitOrder.OrderNum)
                    traded_qty += trade.Value.Quantity;

            if (traded_qty > 0)
                ProcessOrderTrade(traded_qty, limitOrder, false);
        }

        /// <summary>
        /// Добавляем новый лимитный ордер во внутреннюю таблицу лимитных ордеров
        /// Таблица должна соответствовать таблице лимитных ордеров в Quik
        /// </summary>
        /// <param name="order">Лимитный ордер из таблицы Quik</param>
        /// <param name="noCallEvents">Не вызывать события на смену статуса ордера</param>
        private QLimitOrder AddNewLimitOrder(Order order, bool noCallEvents)
        {
            QLimitOrder limitOrder = new QLimitOrder(order, CopyQtyMode.Qty);
            if (!limit_orders.TryAdd(order.OrderNum, limitOrder))
                throw new Exception($"AddNewLimitOrder: Failed TryAdd order {order.OrderNum} to limit_orders");

            // Проверяем, есть ли стоп-ордера, которые зависят от этого лимитного ордера
            AddCoLinkedStopOrders(limitOrder, noCallEvents);

            Call_OnNewLimitOrder(limitOrder);
            return limitOrder;
        }

        /// <summary>
        /// Проверяем, есть ли стоп-ордера, которые зависят от этого лимитного ордера
        /// </summary>
        /// <param name="limitOrder"></param>
        /// <param name="noCallEvents"></param>
        private void AddCoLinkedStopOrders(QLimitOrder limitOrder, bool noCallEvents)
        {
            // Проверяем, есть ли стоп-ордера, которые зависят от этого лимитного ордера
            foreach (var kv in stop_orders)
            {
                var stopOrder = kv.Value;
                if (stopOrder.CoOrderNum == limitOrder.OrderNum)
                {
                    // У нас есть связанный лимитный ордер, который зависит от нас или от которого зависим мы
                    stopOrder.SetCoOrder(limitOrder, noCallEvents);
                }
                else
                if ((limitOrder.LinkedOrder == null) && limitOrder.IsSetLinkedOrderNum && stopOrder.OrderNum.HasValue && (limitOrder.LinkedOrderNum == stopOrder.OrderNum))
                {
                    // Это заявка выставленная при исполнении стоп-ордера
                    limitOrder.SetLinkedWith(stopOrder, QOrderLinkedRole.PlacedByStopOrder, noCallEvents);
                    stopOrder.AddChildLimitOrder(limitOrder, noCallEvents);
                }
            }
        }

        private void AddNewStopOrder(QUIKSharp.DataStructures.StopOrder order, bool noCallEvents)
        {
            QStopOrder stopOrder = QStopOrder.New(order);
            if (!stop_orders.TryAdd(order.OrderNum, stopOrder))
                throw new Exception($"AddNewLimitOrder: Failed TryAdd order {order.OrderNum} to stop_orders, already exists..");

            // Теперь ищем, есть ли у нас лимитные ордера, созданные этим стоп-ордером
            // или связанные с ним
            AddLinkedorCoOrders(stopOrder, noCallEvents);
            Call_OnNewStopOrder(stopOrder);
        }

        /// <summary>
        /// Ищем, есть ли у нас лимитные ордера, созданные этим стоп-ордером или связанные с ним
        /// </summary>
        /// <param name="noCallEvents"></param>
        /// <param name="stopOrder"></param>
        private void AddLinkedorCoOrders(QStopOrder stopOrder, bool noCallEvents)
        {
            if (stopOrder.OrderNum.HasValue)
            {
                foreach (var kv in limit_orders)
                {
                    var limitOrder = kv.Value;
                    if (stopOrder.CoOrderNum == kv.Key) // == LimitOrderNum
                    {
                        // У нас есть связанный лимитный ордер, который зависит от нас или от которого зависим мы
                        stopOrder.SetCoOrder(limitOrder, noCallEvents);
                    }
                    else
                    if (limitOrder.LinkedOrderNum == stopOrder.OrderNum.Value)
                    {
                        limitOrder.SetLinkedWith(stopOrder, QOrderLinkedRole.PlacedByStopOrder, noCallEvents);
                        stopOrder.AddChildLimitOrder(limitOrder, noCallEvents);
                    }
                }
            }
        }

        private void Call_OnNewLimitOrder(QLimitOrder order)
        {
            OnNewLimitOrder?.Invoke(this, new LimitOrderEventArgs() { limitOrder = order });
        }

        private void Call_OnNewStopOrder(QStopOrder order)
        {
            OnNewStopOrder?.Invoke(this, new StopOrderEventArgs() { stopOrder = order });
        }

        private void Call_OnUpdateLimitOrder(QLimitOrder order)
        {
            OnUpdateLimitOrder?.Invoke(this, new LimitOrderEventArgs() { limitOrder = order });
        }

        private void Call_OnUpdateStopOrder(QStopOrder order)
        {
            OnUpdateStopOrder?.Invoke(this, new StopOrderEventArgs() { stopOrder = order });
        }

        private void Events_OnConnected()
        {
            Task.Run(async () =>
           {
               logger.Debug("Events_OnConnectedToQuik: NotifyOnConnected Set/Reset");
               NotifyOnConnected.Set();
               await Task.Delay(500).ConfigureAwait(false);
               NotifyOnConnected.Reset();
           }, cancellation.Token);
        }

        private void Events_OnConnectedToQuik(int port)
        {
            quik.Service.IsConnected(cancellation.Token).ContinueWith(async (isConnected) =>
           {
               if (isConnected.Result && !IsInitialized)
               {
                   IsInitialized = true;
                   logger.Debug("Events_OnConnectedToQuik: NotifyOnConnected Set/Reset");
                   // ----------- rescan all lists ------------
                   await InitOrdersListAsync().ConfigureAwait(false);
                   NotifyOnConnected.Set();
                   await Task.Delay(500).ConfigureAwait(false);
                   NotifyOnConnected.Reset();
               }
           }, cancellation.Token);
        }

        private void Events_OnDisconnectedFromQuik()
        {
            IsInitialized = false;
        }

        private void Events_OnOrder(Order order)
        {
            if (logger.IsDebugEnabled)
                logger.Debug($"Events_OnOrder: OrderNum: {order.OrderNum}, TransId: {order.TransID}, Qty:{order.Quantity}|{order.Balance} Status: {order.State}, Flags: {order.Flags}, ExtFlags: {order.ExtOrderFlags}, LinkedOrder: {order.Linkedorder} ");

            if (order.OrderNum <= 0)
                return;

            bool event_update_lim = false;
            QLimitOrder limitOrder;

            rwLock.EnterWriteLock();
            try
            {
                if (!limit_orders.TryGetValue(order.OrderNum, out limitOrder))
                {
                    long trans_id = quik.Transactions.IdProvider.IdentifyOrder(order);
                    if (qorderByTransId.TryGetValue(trans_id, out var qorder))
                    {
                        if (typeof(QLimitOrder).IsInstanceOfType(qorder))
                        {
                            limitOrder = (QLimitOrder)qorder;
                            qorderByTransId.TryRemove(trans_id, out _);
                            if (!limit_orders.TryAdd(order.OrderNum, limitOrder))
                                throw new Exception($"Can't add new order to limit_orders. OrderNum: {order.OrderNum} already exists!");
                        }
                    }
                    if (limitOrder == null)
                    {
                        // Add new Order
                        limitOrder = AddNewLimitOrder(order, false);
                        // Периодически проверять, не стало ли известно, каким лимитным заявкам соответствуют OnTrade(), которые пришли раньше, чем OnTransReply().
                        AddAdvanceTrades(limitOrder);
                        return;
                    }
                }

                event_update_lim = (limitOrder.TransID != order.TransID) || (order.State != limitOrder.QuikState) || (order.Balance != limitOrder.QtyLeft);

                limitOrder.UpdateFrom(order, false);

                // Связываем этот лимитный ордер со стоп-ордером, если связки еще нет
                if (limitOrder.IsSetLinkedOrderNum && (limitOrder.LinkedOrder == null))
                {
                    // Если этот лимитный ордер был поставлен стоп-ордером
                    if (stop_orders.TryGetValue(order.Linkedorder, out var stopOrder))
                    {
                        if (stopOrder.CoOrderNum == order.OrderNum)
                        {
                            // Это связанная заявка
                            stopOrder.SetCoOrder(limitOrder, false);
                            // Очищаем переменную, чтобы не добавлять дочерний лимитный ордер к стоп-ордеру ниже
                        }
                        else
                        {   // Это заявка выставленная при исполнении стоп-ордера
                            stopOrder.AddChildLimitOrder(limitOrder, false);
                            limitOrder.SetLinkedWith(stopOrder, QOrderLinkedRole.PlacedByStopOrder, false);
                        }
                    }
                    event_update_lim = true;
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }

            if (event_update_lim)
            {
                if (event_update_lim)
                    Call_OnUpdateLimitOrder(limitOrder);

                // Вызываем событие на изменения в стоп-ордере.
                if (limitOrder.LinkedOrder != null)
                    Call_OnUpdateStopOrder(limitOrder.LinkedOrder);
            }
        }

        private void Events_OnStopOrder(DataStructures.StopOrder order)
        {
            if (logger.IsDebugEnabled)
                logger.Debug($"Events_OnStopOrder: OrderNum: {order.OrderNum}, TransId: {order.TransID}, Qty:{order.Quantity}|{order.Balance} Status: {order.State}, Flags: {order.Flags}, StopFlags: {order.StopFlags}, CoOrder: {order.co_order_num}");

            bool event_update_stop = false;
            bool event_update_lim = false;
            QStopOrder stopOrder;
            QLimitOrder qLimitOrder = null;
            rwLock.EnterWriteLock();
            try
            {
                if (!stop_orders.TryGetValue(order.OrderNum, out stopOrder))
                {
                    long trans_id = quik.Transactions.IdProvider.IdentifyOrder(order);
                    if (qorderByTransId.TryGetValue(trans_id, out var qorder))
                    {
                        if (typeof(QStopOrder).IsInstanceOfType(qorder))
                        {
                            stopOrder = (QStopOrder)qorder;
                            qorderByTransId.TryRemove(trans_id, out _);
                            if (!stop_orders.TryAdd(order.OrderNum, stopOrder))
                                throw new Exception($"Can't add new order to stop_orders. OrderNum: {order.OrderNum} already exists!");
                        }
                    }
                    if (stopOrder == null)
                    {
                        AddNewStopOrder(order, false);
                        return;
                    }
                }

                event_update_stop = ((order.State != stopOrder.QuikState) || (stopOrder.QuikFlags != order.Flags));

                stopOrder.UpdateFrom(order, false);
                if ((order.co_order_num != 0) && (stopOrder.CoOrder == null))
                {
                    // У нас появился связанный лимитный ордер, который зависит от нас или от которого зависим мы
                    if (limit_orders.TryGetValue(stopOrder.CoOrderNum, out qLimitOrder))
                    {
                        stopOrder.SetCoOrder(qLimitOrder, false);
                        event_update_stop = true;
                    }
                }

                if ((order.LinkedOrder != 0) && (stopOrder.ChildLimitOrder == null))
                {
                    // У нас появился лимитный ордер, размещенный по результату активации стоп-заявки
                    if (limit_orders.TryGetValue(order.LinkedOrder, out qLimitOrder))
                    {
                        stopOrder.AddChildLimitOrder(qLimitOrder, false);
                        qLimitOrder.SetLinkedWith(stopOrder, QOrderLinkedRole.PlacedByStopOrder, false);
                        event_update_stop = true;
                        event_update_lim = true;
                    }
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }

            if (event_update_stop)
            {
                Call_OnUpdateStopOrder(stopOrder);
            }

            if (event_update_lim)
                Call_OnUpdateLimitOrder(qLimitOrder);
        }

        // ------------------------------------------------------------------------------------------------
        // Обработчики события
        private void Events_OnTrade(Trade trade)
        {
            if (logger.IsDebugEnabled)
                logger.Debug($"Events_OnTrade: OrderNum: {trade.OrderNum}, TransId: {trade.TransID}, Kind: {trade.Kind}, Flags: {trade.Flags}, TradeNum: {trade.TradeNum},\n Price: {trade.Price}, Qty: {trade.Quantity} OrderQty: {trade.OrderQty}");

            rwLock.EnterWriteLock();
            try
            {
                TryAddNewTrade(trade, false);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Инициализация
        private async Task InitOrdersListAsync()
        {
            var limit_orders = quik.Orders.GetOrders(cancellation.Token);
            var stop_orders = quik.Orders.GetStopOrders(cancellation.Token);
            var trades = quik.Trading.GetTrades(cancellation.Token);

            await Task.WhenAll(new Task[] { limit_orders, stop_orders, trades }).ConfigureAwait(false);

            foreach (var order in limit_orders.Result)
                if (order.OrderNum > 0)
                    AddNewLimitOrder(order, true);

            foreach (var order in stop_orders.Result)
                AddNewStopOrder(order, true);

            foreach (var trade in trades.Result)
                TryAddNewTrade(trade, true);
        }

        /// <summary>
        /// Вносит информацию по событию проторгованного обьема в ордер
        /// Если событие OnTrade происходит раньше чем OnOrder, то по этому событию мы сможем отловить полное исполнение ордера
        /// </summary>
        /// <param name="qty_traded"></param>
        /// <param name="limitOrder"></param>
        /// <param name="noCallEvents">Не вызывать события по исполнению или частичному исполнению ордера</param>
        private void ProcessOrderTrade(long qty_traded, QLimitOrder limitOrder, bool noCallEvents)
        {
            long traded;
            bool event_on_update_so;

            traded = limitOrder.AddTraded(qty_traded, noCallEvents);
            event_on_update_so = (limitOrder.LinkedOrder != null);

            // Вызываем события менеджера по факту внесения изменений в ордера
            if (traded > 0)
            {
                Call_OnUpdateLimitOrder(limitOrder);
                if (event_on_update_so)
                    Call_OnUpdateStopOrder(limitOrder.LinkedOrder);
            }
        }

        /// <summary>
        /// Вносит иоформацию о трейде во внутреннюю таблицу и обрабатывает событие onTrade, если оно не было обработано
        /// </summary>
        /// <param name="trade"></param>
        /// <param name="noCallEvents">Не вызывать события по изменению состояния ордера или исполнению его обьемов</param>
        private void TryAddNewTrade(Trade trade, bool noCallEvents)
        {
            if (!limit_trades.TryAdd(trade.TradeNum, trade))
                return;

            if (!this.limit_orders.TryGetValue(trade.OrderNum, out var limitOrder))
                return;

            ProcessOrderTrade(trade.Quantity, limitOrder, noCallEvents);
        }

        private void UnlinkQuik()
        {
            if (quik == null) return;

            quik.Events.OnOrder -= Events_OnOrder;
            quik.Events.OnStopOrder -= Events_OnStopOrder;
            quik.Events.OnTrade -= Events_OnTrade;
            quik.Events.OnConnected -= Events_OnConnected;
            quik.Events.OnConnectedToQuik -= Events_OnConnectedToQuik;
            quik.Events.OnDisconnectedFromQuik -= Events_OnDisconnectedFromQuik;
        }
    }

    public class StopOrderEventArgs : EventArgs
    {
        public QStopOrder stopOrder;
    };
}