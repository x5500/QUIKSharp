// Copyright (c) 2021 alex.mishin@me.com77
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Transport;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    public sealed class TransactionsFunctions : FunctionsBase, ITransactionsFunctions, IDisposable
    {
        private readonly IIdentifyTransaction default_idProvider;
        private IIdentifyTransaction _idProvider;

        /// <summary>
        /// Провайдер идентификации транзакций и ответов на них,
        /// по умолчанию (при инициализации) QuikSharp передает нам экземпляр LuaIdProvider, который мы сохраняем себе в default_idProvider
        /// Так что теперь можно задать свойство idProvider в кастомный провайдер или обнулить, чтобы использовался тот, что мы получили при инициализации
        /// </summary>
        public IIdentifyTransaction IdProvider { get => _idProvider; set => _idProvider = value ?? default_idProvider; }

        private readonly ConcurrentDictionary<long, TransactionResultWaiter> Transactions = new ConcurrentDictionary<long, TransactionResultWaiter>();

        internal TransactionsFunctions(IQuikService quikService, IIdentifyTransaction idProvider) : base(quikService)
        {
            this.default_idProvider = idProvider;
            this._idProvider = idProvider;
            QuikService.Events.OnTransReply += Events_OnTransReply;
            QuikService.Events.OnOrder += Events_OnOrder;
            QuikService.Events.OnStopOrder += Events_OnStopOrder;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                // TODO: освободить управляемое состояние (управляемые объекты)
                if (QuikService != null)
                {
                    QuikService.Events.OnTransReply -= Events_OnTransReply;
                    QuikService.Events.OnOrder -= Events_OnOrder;
                    QuikService.Events.OnStopOrder -= Events_OnStopOrder;
                }

                foreach (var t in Transactions)
                    t.Value.SetCanceled();

                Transactions.Clear();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Функция отправляет запрос на создание уникального TransactionID (скриптом на терминале quik)
        /// Возвращает число пригодное для использования как идентификатор транзакции trans_id
        /// В качестве параметра функции можно передать шаг с которым выдавать следующий TransanctionID - полезно если нужно зарезервировать сразу несколько id
        /// </summary>
        public Task<long> LuaNewTransactionID(long step, CancellationToken cancellationToken)
        {
            return QuikService.SendAsync<long>(new Message<long>(step, "NewTransactionID"), cancellationToken);
        }

        /// <summary>
        /// Выставляем TRANS_ID для транзакции
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public void SetTransactionId(Transaction transaction)
        {
            if (IdProvider == null)
                throw new NullReferenceException("SendWaitTransactionAsync: idProvider == null");

            if (transaction == null)
                throw new NullReferenceException("SendWaitTransactionAsync: transaction == null");

            transaction.TRANS_ID = IdProvider.IdentifyTransaction(transaction);
        }
        /// <summary>
        /// Функция отправляет транзакцию на сервер QUIK и возвращает true в случае успеха,
        /// в случае неудачи возращает false и текст ошибки в свойтве ErrorMessage транзакции.
        /// </summary>
        /// <param name="t">Transaction</param>
        /// <param name="cancellationToken"></param>
        /// <returns>bool - result</returns>
        public Task<TransactionResult> SendTransactionAsync(Transaction t, CancellationToken cancellationToken)
        {
            if (IdProvider == null)
                throw new NullReferenceException("SendWaitTransactionAsync: idProvider == null");

            long TRANS_ID;
            try
            {
                TRANS_ID = IdProvider.IdentifyTransaction(t);
            }
            catch (LuaException e)
            {
                return Task.FromResult( new TransactionResult { Result = TransactionStatus.LuaException, ResultMsg = e.Message, TransId = 0 });
            }

            return QuikService.SendAsync<bool>(new Message<Transaction>(t, "sendTransaction"), cancellationToken).ContinueWith<TransactionResult>((_st) =>
            {
            
                if (_st.Status == TaskStatus.RanToCompletion)
                {
                    if (_st.Result)
                    {
                        return new TransactionResult { Result = TransactionStatus.Success, ResultMsg = string.Empty, TransId = TRANS_ID };
                    }
                    else
                    {
                        string ResultMsg = "Failed call LUA function SendTransactionAsync: result == false";
                        logger.ConditionalDebug(ResultMsg);
                        return new TransactionResult { Result = TransactionStatus.FailedToSend, ResultMsg = ResultMsg, TransId = TRANS_ID };
                    }
                }
                else if (_st.IsCanceled)
                {
                    throw new TaskCanceledException();
                }
                if (_st.Exception != null)
                {
                    var e = _st.Exception.InnerException;
                    if (typeof(TransactionException).IsInstanceOfType(e))
                    {
                        if (logger.IsDebugEnabled)
                            logger.Debug("SendTransactionAsync TransactionException: " + e.Message);
                        return new TransactionResult { Result = TransactionStatus.TransactionException, ResultMsg = e.Message, TransId = TRANS_ID };
                    }
                    else
                    if (typeof(TimeoutException).IsInstanceOfType(e))
                    {
                        // Не дождались отправки/получения , задача завершена по таймауту
                        var ResultMsg = "Timeout while SendTransactionAsync using service Quik";
                        return new TransactionResult { Result = TransactionStatus.SendRecieveTimeout, ResultMsg = ResultMsg, TransId = TRANS_ID };
                    }
                    if (typeof(TaskCanceledException).IsInstanceOfType(e))
                    {
                        throw e;
                    }
                    else
                    {
                        // Что то пошло не так и сервис Quik кинул нам exception.
                        logger.Error(string.Concat("SendTransactionAsync (TRANS_ID: ", TRANS_ID, ") Caught Exception from Quik service: ", e.Message));
                        throw e;
                    }
                }
                var msg = string.Concat("SendTransactionAsync (TRANS_ID: ", TRANS_ID, ") Unhandled behavior! ");
                logger.Fatal(msg);
                throw new Exception(msg);
            }, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Send a single transaction to Quik server
        /// LEGACY
        /// </summary>
        /// <param name="t"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>TRANS_ID if success, -1 or -TRANS_ID if fails</returns>
        public Task<long> SendTransaction(Transaction t, CancellationToken cancellationToken)
        {
            return SendTransactionAsync(t, cancellationToken).ContinueWith<long>((_st) =>
                {
                    if (_st.Result.Result == TransactionStatus.Success)
                        return _st.Result.TransId;
                    else
                        return -_st.Result.TransId;

                }, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Функция отправляет транзакцию на сервер QUIK и ожидает ответа (используя задачи)
        /// Для идентификации транзакции используется интерфейс IIdProvider
        ///
        /// Если терминал QUIK отклонил транзакцию, то результатом будет null, а ошибка заполнена в поле ErrorMessage транзакции
        /// Если же транзакцию удалось отправить, то ответ на нее будет вернут в качестве результата
        /// Если задача ожидания результа будет прервана CancellationToken, то ответом будет TransactionReply со Status == QuikTransactionStatus.FailOnTimeout.
        ///
        /// При успешной отправке транзакции, будет создана запись в (ConcurrentDictionary)Transactions
        /// с помощью которой и будет связан результат, полученный Events_OnTransReply с задачей, ожидающей ответа на транзакцию.
        /// Обработанные транзакции ( успешно или нет, но не ожидающие более ответа) будут удалены из словаря Transactions.
        ///
        /// </summary>
        /// <param name="t">Транзакция (Transaction) </param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<TransactionWaitResult> SendWaitTransactionAsync(Transaction t, CancellationToken cancellationToken)
        {
            if (IdProvider == null)
                throw new NullReferenceException("SendWaitTransactionAsync: idProvider == null");

            long TRANS_ID;
            try
            {
                TRANS_ID = IdProvider.IdentifyTransaction(t);
            }
            catch (LuaException e)
            {
                return Task.FromResult<TransactionWaitResult>( new TransactionWaitResult { transReply = null, Status = TransactionStatus.LuaException, ResultMsg = e.Message } );
            }

            //  Функция отправляет транзакцию на сервер QUIK и возвращает true в случае успеха,
            //  в случае неудачи возращает false и текст ошибки в свойстве ErrorMessage транзакции.
            // Сервис quik может сообщить об ошибке так же и бросив Exception, например на Timeout
            // Иначе ожидаем ответ через Events_OnTransReply

            var request_task = QuikService.SendAsync<bool>(new Message<Transaction>(t, "sendTransaction"), cancellationToken);
            var waiter = new TransactionResultWaiter(request_task, cancellationToken);
            if (!Transactions.TryAdd(TRANS_ID, waiter))
            {
                throw new Exception("Can't add QTransaction (TRANS_ID:  " + TRANS_ID + ") to Transactions dictionary");
            }
            
            // Clearance on task complete/cancelled/failed
            _ = waiter.ResultTask.ContinueWith((w, id) =>
            {
                Transactions.TryRemove((long)id, out var temp);
            }, TRANS_ID);

            request_task.ContinueWith(OnRequestResult, state: waiter, continuationOptions: TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
            return waiter.ResultTask;

            void OnRequestResult(Task<bool> rt, object w)
            {
                var _waiter = w as TransactionResultWaiter;
                if (rt.Status == TaskStatus.RanToCompletion)
                {
                    if (!rt.Result)
                    {
                        string ResultMsg = "SendTransactionAsync: Call for LUA function 'sendTransaction' failed!";
                        logger.ConditionalDebug(ResultMsg);
                        _waiter.SetResult(new TransactionWaitResult { transReply = null, Status = TransactionStatus.FailedToSend, ResultMsg = ResultMsg });
                    }
                }
                else
                if (rt.IsCanceled)
                {
                    _waiter.SetException(new TaskCanceledException());
                }
                else
                if (rt.IsFaulted && (rt.Exception != null))
                {
                    var e = rt.Exception.InnerException;
                    if (typeof(TransactionException).IsInstanceOfType(e))
                    {
                        if (logger.IsDebugEnabled)
                            logger.Debug("TransactionException: " + e.Message);

                        var status = (e.Message == "Not connected") ? TransactionStatus.NoConnection : TransactionStatus.TransactionException;
                        _waiter.SetResult( new TransactionWaitResult { transReply = null, Status = status, ResultMsg = e.Message });
                    }
                    else
                    if (typeof(TimeoutException).IsInstanceOfType(e))
                    {
                        // Не дождались отправки/получения , задача завершена по таймауту
                        var ResultMsg = "Timeout while SendTransaction using service Quik";
                        _waiter.SetResult(new TransactionWaitResult { transReply = null, Status = TransactionStatus.SendRecieveTimeout, ResultMsg = ResultMsg });
                    }
                    if (typeof(TaskCanceledException).IsInstanceOfType(e))
                    {
                        _waiter.SetException(e);
                    }
                    else
                    {
                        // Что то пошло не так и сервис Quik кинул нам exception.
                        logger.Error(string.Concat("SendTransaction (TRANS_ID: ", TRANS_ID, ") Caught Exception from Quik service: ", e.Message));
                        _waiter.SetException(rt.Exception);
                    }
                }
            }
        }

        /// <summary>
        /// Устанавливает результат для TaskCompletionSource
        /// </summary>
        /// <param name="transReply">TransactionReply</param>
        /// <returns></returns>
        private void Events_OnTransReply(TransactionReply transReply)
        {
            if (IdProvider == null) return;

            TransactionStatus status;

            if (transReply.ErrorCode != 0)
            { // Транзакция завершилась с ошибкой
                status = TransactionStatus.QuikError;
            }
            else
            {
                switch (transReply.Status)
                {
                    case TransactionReplyStatus.Sent:
                    case TransactionReplyStatus.RecievedByBroker:
                        // В процессе - ждем следующее событие
                        return;

                    case TransactionReplyStatus.Executed:
                        // All OK
                        status = TransactionStatus.Success;
                        break;

                    case TransactionReplyStatus.RejectNoConnection:
                        // No connection - not fatal
                        status = TransactionStatus.NoConnection;
                        break;

                    case TransactionReplyStatus.FailOnTimeout:
                        // Timeout - reply may arrive later
                        status = TransactionStatus.TimeoutWaitReply;
                        break;

                    case TransactionReplyStatus.RejectedByBroker:
                    case TransactionReplyStatus.RejectedByQuik:
                    case TransactionReplyStatus.RejectedBylimits:
                    case TransactionReplyStatus.NotSupported:
                    case TransactionReplyStatus.NoValidSign:
                    case TransactionReplyStatus.RejectedAsCrossTrade:
                    default:
                        status = TransactionStatus.QuikError;
                        break;
                }
            }

            long TRANS_ID = IdProvider.IdentifyTransactionReply(transReply);
            if (this.Transactions.TryRemove(TRANS_ID, out var waiter))
            {
                var ResultMsg = transReply.ResultMsg ?? $" Status: {transReply.Status}, ErrorCode {transReply.ErrorCode}, ErrorSource {transReply.ErrorSource}";
                var result = new TransactionWaitResult()
                {
                    transReply = transReply,
                    Status = status,
                    ResultMsg = ResultMsg,
                };

                if (logger.IsTraceEnabled)
                {
                    logger.Trace(string.Concat("Events_OnTransReply (TransID: ", transReply.TransID, " result: ", ResultMsg));
                }

                waiter.SetResult(result);
            }
            else
            {
                if (logger.IsDebugEnabled)
                {
                    var ResultMsg = transReply.ResultMsg ?? $" Status: {transReply.Status}, ErrorCode {transReply.ErrorCode}, ErrorSource {transReply.ErrorSource}";
                    logger.Debug(string.Concat("Events_OnTransReply: Can't find TaskCompletionSource for TransID: ", transReply.TransID, ResultMsg));
                }
            }
        }

        /// <summary>
        /// Устанавливает результат для TaskCompletionSource
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns></returns>
        private void Events_OnOrder(Order order)
        {
            if (IdProvider == null) return;

            long TRANS_ID = IdProvider.IdentifyOrder(order);
            if ((TRANS_ID != 0) && this.Transactions.TryRemove(TRANS_ID, out var waiter))
            {
                bool rejected = order.State == QUIKSharp.DataStructures.State.Rejected;
                var ResultMsg = rejected ? $"Order (TRANS_ID: {TRANS_ID}  RejectReason: {order.RejectReason}"
                    : $"Order (TRANS_ID: {TRANS_ID} State: {order.State}";

                if (logger.IsTraceEnabled)
                    logger.Trace("Events_OnOrder: " + ResultMsg);

                var result = new TransactionWaitResult()
                {
                    TransId = TRANS_ID,
                    OrderNum = order.OrderNum,
                    transReply = null,
                    Status = rejected ? TransactionStatus.QuikError : TransactionStatus.Success,
                    ResultMsg = ResultMsg,
                };

                waiter.SetResult(result);
            }
        }

        /// <summary>
        /// Устанавливает результат для TaskCompletionSource
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns></returns>
        private void Events_OnStopOrder(StopOrder order)
        {
            if (IdProvider == null) return;

            long TRANS_ID = IdProvider.IdentifyOrder(order);
            if ((TRANS_ID != 0) && this.Transactions.TryRemove(TRANS_ID, out var waiter))
            {
                bool rejected = order.State == State.Rejected;
                var ResultMsg = rejected ? $"StopOrder Rejected (TRANS_ID: {TRANS_ID} {order.Flags} {order.StopFlags})"
                    : $"Order (TRANS_ID: {TRANS_ID} State: {order.State}";

                if (logger.IsTraceEnabled)
                    logger.Trace("Events_OnOrder: " + ResultMsg);

                var result = new TransactionWaitResult()
                {
                    transReply = null,
                    TransId = TRANS_ID,
                    OrderNum = order.OrderNum,
                    Status = rejected ? TransactionStatus.QuikError : TransactionStatus.Success,
                    ResultMsg = ResultMsg,
                };

                waiter.SetResult(result);
            }
        }
    }
}