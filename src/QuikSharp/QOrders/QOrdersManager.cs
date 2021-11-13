// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using NLog;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Functions;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.QOrders
{
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
        public QOrder NewOrder;
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

    public class QOrdersManager : Events.TryCatchWrapEvent, IDisposable, IQOrdersManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #region Class params
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

        private readonly CancellationTokenSource stop_all_cancellation = new CancellationTokenSource();

        private readonly AsyncManualResetEvent NotifyOnConnected = new AsyncManualResetEvent();

        private bool IsInitialized = false;

        private IQuik quik;

        /// <summary>
        /// Пауза (ms) перед повторной попыткой, если словили Таймаут от сервера квик.
        /// </summary>
        public int Delay_on_Timeout { get; set; } = 20;

        /// <summary>
        /// Wait Order timeout. Таймаут ожидания ответа на транзакцию о размещении/перемещении/снятии ордера
        /// </summary>
        public int Timeout_ms { get; set; } = 20000;

        #endregion

        #region Constructor

        /// <summary>
        ///  Менеджер заявок Quik
        /// </summary>
        /// <param name="quik"></param>
        /// <param name="timeout_wait_order">Макс. время (мкс.) ожидания выполнения транзакции сервером брокера. Если превышен, запрос будет повторен, и так до победного.</param>
        /// <param name="delay_on_timeout"> Пауза перед повторной попыткой, если словили Таймаут от сервера.</param>
        public QOrdersManager(IQuik quik, int timeout_wait_order = 20000, int delay_on_timeout = 10)
        {
            Timeout_ms = timeout_wait_order;
            Delay_on_Timeout = delay_on_timeout;

            // Request Task
            // NB we use the token for signalling, could use a simple TCS
            var task_options = TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.PreferFairness;
            _actionTask = Task.Factory.StartNew(RequestTaskLoop, CancellationToken.None, task_options, TaskScheduler.Default);

            LinkQuik(quik);
        }

        #endregion

        #region Link Unlink QUIK

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
                _ = InitOrdersListAsync().ContinueWith((task) =>
                {
                    logger.Debug($"InitOrdersListAsync() task completed, status: {task.Status}");
                });
            }
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

        #endregion

        #region Tasks Loop

        /// <summary>
        /// Обработчик очереди задач на асинхронное выполнение
        /// </summary>
        private Task _actionTask;

        /// <summary>
        /// Очередь задач на асинхронное выполнение
        /// </summary>
        private readonly ConcurrentQueue<QOrdersAction> ActionQueue = new ConcurrentQueue<QOrdersAction>();
        private readonly ManualResetEventSlim ActionQueue_Avail = new ManualResetEventSlim(false);

        private void RequestTaskLoop()
        {
            var cancelToken = stop_all_cancellation.Token;
            // Enter the listening loop.
            while (!cancelToken.IsCancellationRequested)
            {
                if (!ActionQueue.TryDequeue(out var _action))
                {
                    ActionQueue_Avail.Reset();
                    ActionQueue_Avail.Wait(10000, cancelToken);
                    continue;
                }
                try
                {
                    Task task;
                    switch (_action.action)
                    {
                        case QOrdersActionType.PlaceOrder:
                            task = PlaceOrderAsync(_action.qOrder, _action.cancellationToken);
                            break;

                        case QOrdersActionType.MoveOrder:
                            task = MoveLimOrderAsync((QLimitOrder)_action.qOrder, _action.new_price, _action.new_qty, _action.cancellationToken);
                            break;

                        case QOrdersActionType.KIllOrder:
                            task  = KillOrderAsync(_action.qOrder, _action.cancellationToken);
                            break;

                        default:
                            logger.Fatal($"QOrder ActionBlockConsumer Failed: Not implemented for '{_action.action}'");
                            continue;
                    }
                    task.ContinueWith(LogTaskException, _action.action.ToString(), continuationOptions: TaskContinuationOptions.OnlyOnFaulted); ;
                }
                catch (Exception e)
                {
                    logger.Fatal(e, $"Exception in RequestTaskAction loop: {e.Message}\n  --- Exception Trace: ---- \n{e.StackTrace}\n--- Exception trace ----");
                }
            }
        }
        private static void LogTaskException(Task task, object task_name)
        {
            var exc = task.Exception.InnerException ?? task.Exception;
            logger.Fatal(exc, $"Exception running task {task_name}: {exc.Message}\n  --- Exception Trace: ---- \n{exc.StackTrace}\n--- Exception trace ----");
        }

        #endregion

        #region Place Tasks in Query

        /// <summary>
        /// Поместить в очередь на выполнение задачу
        /// </summary>
        /// <param name="qOrder"></param>
        public void RequestKillOrder(QOrder qOrder)
        {
            qOrder.KillMoveState = QOrderKillMoveState.WaitKill;

            ActionQueue.Enqueue(new QOrdersAction
            {
                action = QOrdersActionType.KIllOrder,
                qOrder = qOrder,
                cancellationToken = CancellationToken.None,
            });
            ActionQueue_Avail.Set();
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
            ActionQueue.Enqueue(new QOrdersAction
            {
                action = QOrdersActionType.MoveOrder,
                qOrder = qOrder,
                cancellationToken = CancellationToken.None,
                new_price = new_price,
                new_qty = new_qty,
            });
            ActionQueue_Avail.Set();
        }

        /// <summary>
        /// Поместить в очередь на выполнение задачу
        /// </summary>
        /// <param name="qOrder"></param>
        public void RequestPlaceOrder(QOrder qOrder)
        {
            ActionQueue.Enqueue(new QOrdersAction
            {
                action = QOrdersActionType.PlaceOrder,
                qOrder = qOrder,
                cancellationToken = CancellationToken.None,
            });
            ActionQueue_Avail.Set();
        }

        #endregion

        #region Run Tasks

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
                var my_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation_token, stop_all_cancellation.Token);
                my_cancellation_token = my_cts.Token;
            }
            else
            {
                my_cancellation_token = stop_all_cancellation.Token;
            }

            while (!stop_all_cancellation.IsCancellationRequested)
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
                            Call_OnTransacError(QOrdersActionType.KIllOrder, qOrder, reply.transReply);
                            return new QOrderActionResult() { Result = false, ResultMsg = reply.ResultMsg };

                        case TransactionStatus.TimeoutWaitReply:
                        case TransactionStatus.SendRecieveTimeout:
                            qOrder.KillMoveState = QOrderKillMoveState.WaitKill;
                            Call_OnTransacError(QOrdersActionType.KIllOrder, qOrder, reply.transReply);
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
                var my_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation_token, stop_all_cancellation.Token);
                my_cancellation_token = my_cts.Token;
            }
            else
                my_cancellation_token = stop_all_cancellation.Token;

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
                TransactionWaitResult reply = await quik.Transactions.SendWaitTransactionAsync(t, linked_CTS.Token).ConfigureAwait(false);
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
                                newqOrder = new QLimitOrder(qOrder, CopyQtyMode.QtyLeft)
                                {
                                    OrderNum = NewOrderNum
                                };

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

                        return new QOrderActionResult() { Result = true, NewOrder =newqOrder, ResultMsg = reply.ResultMsg };

                    case TransactionStatus.QuikError:
                        if (reply.transReply.Status == DataStructures.TransactionReplyStatus.RejectedByBroker)
                        {
                            // Ошибка перестановки заявок. [GW][332] "Нехватка средств по лимитам клиента.".
                            qOrder.KillMoveState = QOrderKillMoveState.NoKill;
                            Call_OnTransacError(QOrdersActionType.MoveOrder, qOrder, reply.transReply);
                            return new QOrderActionResult() { Result = false, ResultMsg = reply.ResultMsg };
                        }
                        if (reply.transReply.Status == DataStructures.TransactionReplyStatus.RejectedBylimits)
                        {
                            if (reply.transReply.ErrorCode == 8635282)
                            {
                                // Не найдена активная заявка для перестановки
                                qOrder.KillMoveState = QOrderKillMoveState.Killed;
                                Call_OnTransacError(QOrdersActionType.MoveOrder, qOrder, reply.transReply);
                                return new QOrderActionResult() { Result = false, ResultMsg = reply.ResultMsg };
                            }
                            if (reply.transReply.ErrorCode == -1065103583)
                            {
                                // "Вы не можете заменить заявку xxxxxxxxxxxx. Повторите попытку позже."
                                qOrder.KillMoveState = QOrderKillMoveState.WaitMove;
                                if (Delay_on_Timeout > 0) await Task.Delay(Delay_on_Timeout, my_cancellation_token).ConfigureAwait(false);
                                // Call_OnTransacError(QOrdersActionType.MoveOrder, qOrder, reply.transReply);
                                break;
                            }
                        }
                        qOrder.State = QOrderState.ErrorRejected;
                        qOrder.KillMoveState = QOrderKillMoveState.Killed;
                        Call_OnTransacError(QOrdersActionType.MoveOrder, qOrder, reply.transReply);
                        return new QOrderActionResult() { Result = false, ResultMsg = reply.ResultMsg };
                    case TransactionStatus.LuaException:
                    case TransactionStatus.FailedToSend:
                    case TransactionStatus.TransactionException:
                        qOrder.KillMoveState = QOrderKillMoveState.NoKill;
                        Call_OnTransacError(QOrdersActionType.MoveOrder, qOrder, reply.transReply);
                        return new QOrderActionResult() { Result = false, ResultMsg = reply.ResultMsg };

                    case TransactionStatus.TimeoutWaitReply:
                    case TransactionStatus.SendRecieveTimeout:
                        qOrder.KillMoveState = QOrderKillMoveState.WaitMove;
                        if (Delay_on_Timeout > 0) await Task.Delay(Delay_on_Timeout, my_cancellation_token).ConfigureAwait(false);
                        Call_OnTransacError(QOrdersActionType.MoveOrder, qOrder, reply.transReply);
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

            while (!stop_all_cancellation.IsCancellationRequested)
            {
                if (retry-- <= 0)
                    break;

                if (qOrder.State != QOrderState.WaitPlacement)
                    return new QOrderActionResult() { Result = false, ResultMsg = "qOrder.State != QOrderState.WaitPlacement" };

                var t = qOrder.PlaceOrderTransaction();

                var linked_CTS = CancellationTokenSource.CreateLinkedTokenSource(stop_all_cancellation.Token, cancellation_token);
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

                        return new QOrderActionResult() { Result = true, NewOrder = qOrder, ResultMsg = reply.ResultMsg };

                    case TransactionStatus.LuaException:
                    case TransactionStatus.TransactionException:
                    case TransactionStatus.QuikError:
                    case TransactionStatus.FailedToSend:
                        qOrder.State = QOrderState.ErrorRejected;
                        qorderByTransId.TryRemove(trans_id, out _);

                        Call_OnTransacError(QOrdersActionType.PlaceOrder, qOrder, reply.transReply);
                        return new QOrderActionResult() { Result = false, ResultMsg = reply.ResultMsg };

                    case TransactionStatus.TimeoutWaitReply:
                    case TransactionStatus.SendRecieveTimeout:
                        qOrder.State = QOrderState.WaitPlacement;
                        qorderByTransId.TryRemove(trans_id, out _);

                        Call_OnTransacError(QOrdersActionType.PlaceOrder, qOrder, reply.transReply);
                        if (Delay_on_Timeout > 0)
                        {
                            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(stop_all_cancellation.Token, cancellation_token))
                            {
                                await Task.Delay(Delay_on_Timeout, cts.Token).ConfigureAwait(false);
                            }
                        }
                        break;

                    case TransactionStatus.NoConnection:
                        qOrder.State = QOrderState.WaitPlacement;
                        qorderByTransId.TryRemove(trans_id, out _);

                        Call_OnTransacError(QOrdersActionType.PlaceOrder, qOrder, reply.transReply);
                        await NotifyOnConnected.WaitAsync.ConfigureAwait(false);
                        break;
                }
                // Try one more time
            }
            return new QOrderActionResult() { Result = false, ResultMsg = "Task cancelled." }; ;
        }

        #endregion

        #region Internal tables
        // ------------------------------------------------------------------------------------------------
        /// <summary>
        ///  Инициализация внутренних таблиц: LimitOrders, StopOrders, Trades
        /// </summary>
        /// <returns></returns>
        private Task InitOrdersListAsync()
        {
            var limit_orders = quik.Orders.GetOrders(stop_all_cancellation.Token);
            var stop_orders = quik.Orders.GetStopOrders(stop_all_cancellation.Token);
            var trades = quik.Trading.GetTrades(stop_all_cancellation.Token);

            return Task.WhenAll(new Task[] { limit_orders, stop_orders, trades }).ContinueWith((t) =>
                {
                    foreach (var order in limit_orders.Result)
                        if (order.OrderNum > 0)
                            Events_OnOrder(order);

                    foreach (var order in stop_orders.Result)
                        Events_OnStopOrder(order);

                    rwLock.EnterWriteLock();
                    try
                    {
                        foreach (var trade in trades.Result)
                            TryAddNewTrade(trade, true);
                    }
                    finally
                    {
                        rwLock.ExitWriteLock();
                    }

                }, cancellationToken: stop_all_cancellation.Token,
                continuationOptions: TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

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

        #endregion

        #region Dispose
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
                    stop_all_cancellation.Cancel();

                    UnlinkQuik();

                    /// Clear task query
                    while (ActionQueue.TryDequeue(out _)) ;
                    
                    // Wait for all messages to propagate through the network.
                    if (_actionTask != null)
                    {
                        // here all tasks must exit gracefully
                        var isCleanExit = Task.WaitAll(new[] { _actionTask }, 5000);
                        if (!isCleanExit)
                            logger.Error("All tasks must finish gracefully after cancellation token is cancelled!");

                        _actionTask = null;
                    }

                    ActionQueue_Avail.Dispose();

                    limit_orders.Clear();
                    stop_orders.Clear();
                    limit_trades.Clear();

                    stop_all_cancellation.Dispose();
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // TODO: установить значение NULL для больших полей
                disposedValue = true;
            }
        }

        #endregion

        #region Add new orders to tables
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

        #endregion

        #region Events

        public event LimitOrderEventHandler OnNewLimitOrder;
        public event StopOrderEventHandler OnNewStopOrder;
        public event LimitOrderEventHandler OnUpdateLimitOrder;
        public event StopOrderEventHandler OnUpdateStopOrder;
        public event TransacErrorHandler OnTransacError;

        private void Call_OnNewLimitOrder(QLimitOrder order) => RunTheEvent(OnNewLimitOrder, this, new LimitOrderEventArgs() { limitOrder = order });

        private void Call_OnNewStopOrder(QStopOrder order) => RunTheEvent(OnNewStopOrder, this, new StopOrderEventArgs() { stopOrder = order });

        private void Call_OnUpdateLimitOrder(QLimitOrder order) => RunTheEvent(OnUpdateLimitOrder, this, new LimitOrderEventArgs() { limitOrder = order });

        private void Call_OnUpdateStopOrder(QStopOrder order) => RunTheEvent(OnUpdateStopOrder, this, new StopOrderEventArgs() { stopOrder = order });

        private void Call_OnTransacError(QOrdersActionType action, QOrder qOrder, TransactionReply transactionReply)
        {
            // Вызывается из задачи
            if (logger.IsDebugEnabled)
                logger.Debug($"Call_OnTransacError on action {action}: {transactionReply.ResultMsg}, Err.code:{transactionReply.ErrorCode}, Err.src:{transactionReply.ErrorSource} for order {qOrder.ClassCode}:{qOrder.SecCode} {qOrder.Operation} {qOrder.Qty} qty on {qOrder.Price}");
            RunTheEvent(OnTransacError, action, qOrder, transactionReply);
        }

        #endregion

        #region QUIK events handlers
        // ------------------------------------------------------------------------------------------------
        // Обработчики событий

        private void Events_OnConnected()
        {
            Task.Run(async () =>
           {
               logger.Debug("Events_OnConnectedToQuik: NotifyOnConnected Set/Reset");
               NotifyOnConnected.Set();
               await Task.Delay(500).ConfigureAwait(false);
               NotifyOnConnected.Reset();
           }, stop_all_cancellation.Token);
        }

        private void Events_OnConnectedToQuik(int port)
        {
           quik.Service.IsConnected(stop_all_cancellation.Token).ContinueWith((isConnected) =>
           {
           if (isConnected.Result && !IsInitialized)
           {
               IsInitialized = true;
               logger.Debug("Events_OnConnectedToQuik: NotifyOnConnected Set/Reset");
               // ----------- rescan all lists ------------
               _ = InitOrdersListAsync().ContinueWith( (_) => NotifyOnConnected.Set(),
                   cancellationToken: stop_all_cancellation.Token,
                   continuationOptions: TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
                   TaskScheduler.Default
                   ).ContinueWith( (_) => Task.Delay(500, stop_all_cancellation.Token))
                    .ContinueWith( (_) => NotifyOnConnected.Reset());
               }
           }, stop_all_cancellation.Token);
        }

        private void Events_OnDisconnectedFromQuik()
        {
            IsInitialized = false;
        }

        private void Events_OnOrder(Order order)
        {
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

        private void Events_OnTrade(Trade trade)
        {
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

        #endregion

        #region Trades logic
       
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

            if (!limit_orders.TryGetValue(trade.OrderNum, out var limitOrder))
                return;

            ProcessOrderTrade(trade.Quantity, limitOrder, noCallEvents);
        }

        #endregion
    }

}