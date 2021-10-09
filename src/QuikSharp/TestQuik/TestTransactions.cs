// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Functions;
using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
namespace QUIKSharp.TestQuik
{
    public class TestTransactions : ITransactionsFunctions
    {
        static long trans_id = 1000;

        public IIdentifyTransaction IdProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public async Task<long> LuaNewTransactionID(long step, CancellationToken cancellationToken)
        {            
            return ++trans_id;
        }

        public Task<long> SendTransaction(Transaction t, CancellationToken task_cancel)
        {
            return LuaNewTransactionID(1, task_cancel);
        }

        public async Task<TransactionResult> SendTransactionAsync(Transaction t, CancellationToken task_cancel)
        {
            long new_id = ++trans_id;
            return new TransactionResult { Result = TransactionStatus.Success, ResultMsg = "", TransId = new_id };
        }

        public Task<TransactionWaitResult> SendWaitTransactionAsync(Transaction t, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
