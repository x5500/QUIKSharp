// Copyright (c) 2021 alex.mishin@me.com77
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    internal class TransactionResultWaiter : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly TaskCompletionSource<TransactionWaitResult> tcs = new TaskCompletionSource<TransactionWaitResult>(TaskCreationOptions.AttachedToParent);
        internal Task<TransactionWaitResult> ResultTask => tcs.Task;
        internal Task<bool> RequestTask;
        internal TransactionResultWaiter(Task<bool> request_task, CancellationToken cancellationToken)
        {
            RequestTask = request_task;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cts.Token.Register(SetCanceled, useSynchronizationContext: false);
        }
        internal void SetResult(TransactionWaitResult result) => tcs.TrySetResult(result);
        internal void SetCanceled() => tcs.TrySetCanceled();
        internal void SetException(Exception e) => tcs.TrySetException(e);
        public void Dispose()
        {
            ((IDisposable)_cts).Dispose();
            RequestTask.Dispose();
            ResultTask.Dispose();
        }
    }
}