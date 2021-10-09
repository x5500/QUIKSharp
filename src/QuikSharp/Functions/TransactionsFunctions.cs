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

        private readonly ConcurrentDictionary<long, TaskCompletionSource<TransactionWaitResult>> Transactions = new ConcurrentDictionary<long, TaskCompletionSource<TransactionWaitResult>>();

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
                    t.Value.TrySetCanceled();

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
        public async Task<TransactionResult> SendTransactionAsync(Transaction t, CancellationToken cancellationToken)
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
                return new TransactionResult() { Result = TransactionStatus.LuaException, ResultMsg = e.Message, TransId = 0 };
            }

            try
            {
                bool sent_ok = await QuikService.SendAsync<bool>(new Message<Transaction>(t, "sendTransaction"), cancellationToken).ConfigureAwait(false);
                if (sent_ok) // bool, true if transaction was sent
                    return new TransactionResult() { Result = TransactionStatus.Success, ResultMsg = string.Empty, TransId = TRANS_ID };

                string ResultMsg = "Failed call LUA function SendTransaction: result == false";
                logger.ConditionalDebug(ResultMsg);
                return new TransactionResult() { Result = TransactionStatus.FailedToSend, ResultMsg = ResultMsg, TransId = TRANS_ID };
            }
            catch (TransactionException e)
            {
                if (logger.IsDebugEnabled)
                    logger.Debug("TransactionException: " + e.Message);
                return new TransactionResult() { Result = TransactionStatus.TransactionException, ResultMsg = e.Message, TransId = TRANS_ID };
            }
            catch (TimeoutException)
            {
                // Не дождались отправки/получения , задача завершена по таймауту
                var ResultMsg = "Timeout while SendTransaction using service Quik";
                return new TransactionResult() { Result = TransactionStatus.SendRecieveTimeout, ResultMsg = ResultMsg, TransId = TRANS_ID };
            }
            catch (Exception e)
            {
                // Что то пошло не так и сервис Quik кинул нам exception.
                logger.Error(string.Concat("SendTransaction (TRANS_ID: ", TRANS_ID, ") Caught Exception from Quik service: ", e.Message));
                throw;
            }
        }

        /// <summary>
        /// Send a single transaction to Quik server
        /// LEGACY
        /// </summary>
        /// <param name="t"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>TRANS_ID if success, -1 or -TRANS_ID if fails</returns>
        public async Task<long> SendTransaction(Transaction t, CancellationToken cancellationToken)
        {
            // dirty hack: if transaction was sent we return its id,
            // else we return negative id so the caller will know that
            // the transaction was not sent
            var result = await SendTransactionAsync(t, cancellationToken).ConfigureAwait(false);
            if (result.Result == TransactionStatus.Success)
                return result.TransId;
            else
                return -result.TransId;
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
        public async Task<TransactionWaitResult> SendWaitTransactionAsync(Transaction t, CancellationToken cancellationToken)
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
                return new TransactionWaitResult { transReply = null, Status = TransactionStatus.LuaException, ResultMsg = e.Message };
            }

            //  Функция отправляет транзакцию на сервер QUIK и возвращает true в случае успеха,
            //  в случае неудачи возращает false и текст ошибки в свойстве ErrorMessage транзакции.
            // Сервис quik может сообщить об ошибке так же и бросив Exception, например на Timeout
            try
            {
                bool sent_ok = await QuikService.SendAsync<bool>(new Message<Transaction>(t, "sendTransaction"), cancellationToken).ConfigureAwait(false);
                if (!sent_ok)
                {
                    string ResultMsg = "SendTransactionAsync: Call for LUA function 'sendTransaction' failed!";
                    logger.ConditionalDebug(ResultMsg);
                    return new TransactionWaitResult { transReply = null, Status = TransactionStatus.FailedToSend, ResultMsg = ResultMsg };
                }
            }
            catch (TransactionException e)
            {
                if (logger.IsDebugEnabled)
                    logger.Debug("TransactionException: " + e.Message);

                var status = (e.Message == "Not connected") ? TransactionStatus.NoConnection : TransactionStatus.TransactionException;
                return new TransactionWaitResult { transReply = null, Status = status, ResultMsg = e.Message };
            }
            catch (TimeoutException)
            {
                // Не дождались отправки/получения , задача завершена по таймауту
                var ResultMsg = "Timeout while SendTransaction using service Quik";
                return new TransactionWaitResult { transReply = null, Status = TransactionStatus.SendRecieveTimeout, ResultMsg = ResultMsg };
            }
            catch (Exception e)
            {
                // Что то пошло не так и сервис Quik кинул нам exception.
                logger.Error(string.Concat("SendTransaction (TRANS_ID: ", TRANS_ID, ") Caught Exception from Quik service: ", e.Message));
                throw;
            }

            // Иначе ожидаем ответ через Events_OnTransReply
            var tcs = new TaskCompletionSource<TransactionWaitResult>(TaskCreationOptions.AttachedToParent);
            if (!Transactions.TryAdd(TRANS_ID, tcs))
            {
                throw new Exception("Can't add QTransaction (TRANS_ID:  " + TRANS_ID + ") to Transactions dictionary");
            }

            // this callback will be executed when token is cancelled
            try
            {
                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                {
                    // Получаем долгожданный ответ
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // Не дождались, задача завершена по таймауту
                    var ResultMsg = "Timeout waiting for TransactionReply from Quik Server/Broker";
                    logger.ConditionalDebug(ResultMsg);
                    return new TransactionWaitResult { transReply = null, Status = TransactionStatus.TimeoutWaitReply, ResultMsg = ResultMsg };
                }
                else throw;
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
            if (this.Transactions.TryRemove(TRANS_ID, out TaskCompletionSource<TransactionWaitResult> tcs))
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

                tcs.TrySetResult(result);
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
            if ((TRANS_ID != 0) && this.Transactions.TryRemove(TRANS_ID, out TaskCompletionSource<TransactionWaitResult> tcs))
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

                tcs.TrySetResult(result);
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
            if ((TRANS_ID != 0) && this.Transactions.TryRemove(TRANS_ID, out TaskCompletionSource<TransactionWaitResult> tcs))
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

                tcs.TrySetResult(result);
            }
        }
    }
}