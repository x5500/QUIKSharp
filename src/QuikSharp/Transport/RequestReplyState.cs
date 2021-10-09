// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json.Linq;
using NLog;
using QUIKSharp.Converters;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Transport
{
    /// <summary>
    /// QuikService RequestReplyState state
    /// </summary>
    public sealed class RequestReplyState<T> : RequestReplyStateBase, IDisposable
    {
        /// <summary>
        /// Здесь сохраняем полученный ответ от Квика
        /// и тем самым информируем заказчика
        /// </summary>
        private readonly TaskCompletionSource<T> tcs;
        /// <summary>
        /// Задача TaskCompletionSource <typeparamref name="T"/> для ожидания результата
        /// </summary>
        internal Task<T> ResultTask { get => tcs.Task; }

        internal RequestReplyState(IMessage request, CancellationToken task_cancel, CancellationToken service_stop)
            : base(request, typeof(Message<T>), task_cancel, service_stop)
        {
            tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        protected override bool TrySetResult(object message) => tcs.TrySetResult((T)message);
        protected override bool TrySetException(Exception e) => tcs != null && tcs.TrySetException(e);
        public override TaskStatus TaskStatus { get => tcs.Task.Status;  }
        public new void Dispose()
        {
            base.Dispose();
            ((IDisposable)ResultTask).Dispose();
        }
    }
}