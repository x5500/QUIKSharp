// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;

namespace QUIKSharp
{
    /// <summary>
    /// Провайдер идентификаторов транзакций
    /// Этот идентификатор служит для сопоставления ответа на транзакцию с транзакцией
    /// Для идентификации транзакции используется функция IdentifyTransaction(t)
    /// Для идентификации ответа на транзакцию используется функция IdentifyTransactionReply(r)
    /// </summary>
    public interface IIdentifyTransaction
    {
        /// <summary>
        /// Идентифицирует транзакцию, возвращая идентификатор типа long,
        /// Этот идентификатор служит для сопоставления ответа на транзакцию с транзакцией
        /// Для идентификации ответа на транзакцию используется функция IdentifyTransactionReply
        /// </summary>
        /// <param name="t">Transaction</param>
        /// <returns>Unique TRANS_ID</returns>
        long IdentifyTransaction(Transaction t);

        /// <summary>
        /// Идентифицирует ответ на транзакцию, возвращая идентификатор типа long,
        /// Этот идентификатор служит для сопоставления ответа на транзакцию с транзакцией
        /// Для идентификации транзакции используется функция IdentifyTransaction
        /// </summary>
        /// <returns>Unique TRANS_ID</returns>
        long IdentifyTransactionReply(TransactionReply transReply);

        /// <summary>
        /// Идентифицирует Order (по событию OnOrder), возвращая идентификатор типа long,
        /// Этот идентификатор служит для сопоставления события с транзакцией
        /// Для идентификации транзакции используется функция IdentifyTransaction
        /// </summary>
        /// <returns>Unique TRANS_ID</returns>
        long IdentifyOrder(Order order);

        /// <summary>
        /// Идентифицирует Order (по событию OnOrder), возвращая идентификатор типа long,
        /// Этот идентификатор служит для сопоставления события с транзакцией
        /// Для идентификации транзакции используется функция IdentifyTransaction
        /// </summary>
        /// <returns>Unique TRANS_ID</returns>
        long IdentifyOrder(StopOrder order);

    }
}