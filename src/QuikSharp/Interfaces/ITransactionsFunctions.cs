// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Functions;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp
{
    public interface ITransactionsFunctions
    {
        /// <summary>
        /// Провайдер идентификаторов транзакций
        /// Этот идентификатор служит для сопоставления ответа на транзакцию с транзакцией
        /// Для идентификации транзакции используется функция IdentifyTransaction(t)
        /// Для идентификации ответа на транзакцию используется функция IdentifyTransactionReply(r)
        /// </summary>
        IIdentifyTransaction IdProvider { get; set; }

        /// <summary>
        /// Функция отправляет запрос на создание уникального TransactionID (скриптом на терминале quik)
        /// Возвращает число пригодное для использования как идентификатор транзакции trans_id
        /// В качестве параметра функции можно передать шаг с которым выдавать следующий TransanctionID - полезно если нужно зарезервировать сразу несколько id
        /// </summary>
        Task<long> LuaNewTransactionID(long step = 1);

        /// <summary>
        /// Send a single transaction to Quik server
        /// LEGACY
        /// </summary>
        /// <param name="t"></param>
        /// <returns>TRANS_ID if success, -1 or -TRANS_ID if fails</returns>
        Task<long> SendTransaction(Transaction t);

        /// <summary>
        /// Функция отправляет транзакцию на сервер QUIK и возвращает true в случае успеха,
        /// в случае неудачи возращает false и текст ошибки в свойтве ErrorMessage транзакции.
        /// </summary>
        /// <param name="t">Transaction</param>
        /// <returns>bool - result</returns>
        Task<TransactionResult> SendTransactionAsync(Transaction t);

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
        Task<TransactionWaitResult> SendWaitTransactionAsync(Transaction t, CancellationToken cancellationToken);
    }
}