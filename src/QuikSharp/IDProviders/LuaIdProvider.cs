// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;

namespace QUIKSharp
{
    public class LuaIdProvider : IIdentifyTransaction
    {
        protected Quik quik;

        public LuaIdProvider(Quik quik)
        {
            this.quik = quik;
        }

        /// <summary>
        /// Запрашивает у LUA функции в адаптере квика 'NewTransactionID' новый идентификатор транзакции
        /// </summary>
        /// <returns></returns>
        virtual public long GetNextId()
        {
            return quik.Transactions.LuaNewTransactionID().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Функция идентифицирует транзакцию, возвращая идентификатор типа long
        /// Для идентификации используется поле TRANS_ID
        /// Если поле не задано или значение меньше нуля,
        /// то Идентификатор будет присвоен используя функцию GetNextId()
        /// </summary>
        /// <param name="t">Транзакция типа Transaction</param>
        /// <returns>TRANS_ID</returns>
        long IIdentifyTransaction.IdentifyTransaction(Transaction t)
        {
            if (!t.TRANS_ID.HasValue || t.TRANS_ID.Value <= 0)
            {
                t.TRANS_ID = GetNextId();
            }
            return t.TRANS_ID.Value;
        }

        /// <summary>
        /// Функция идентифицирует транзакцию, возвращая идентификатор типа long
        /// Для идентификации используется поле TRANS_ID
        /// </summary>
        /// <param name="transReply">Ответ на транзакцию, типа TransactionReply</param>
        /// <returns>TRANS_ID</returns>
        long IIdentifyTransaction.IdentifyTransactionReply(TransactionReply transReply)
        {
            return transReply.TransID;
        }

        /// <summary>
        /// Функция идентифицирует Order (по событию OnOrder), возвращая идентификатор типа long
        /// Для идентификации используется поле TRANS_ID
        /// </summary>
        /// <param name="order">Order (по событию OnOrder)</param>
        /// <returns>TRANS_ID</returns>
        long IIdentifyTransaction.IdentifyOrder(Order order)
        {
            return order.TransID;
        }
        long IIdentifyTransaction.IdentifyOrder(StopOrder order)
        {
            return order.TransID;
        }
    }
}