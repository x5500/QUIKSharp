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

        /// <summary>
        /// Static const value, if type T has interface to IWithLuaTimeStamp
        /// </summary>
        private static readonly bool IsWithLuaTimeStamp;
        static RequestReplyState()
        {
            IsWithLuaTimeStamp = typeof(IWithLuaTimeStamp).IsAssignableFrom(typeof(T));
        }

        internal RequestReplyState(IMessage request, TimeSpan defaultSendTimeout, CancellationToken task_cancel, CancellationToken service_stop)
            : base(request, typeof(Message<T>), defaultSendTimeout, task_cancel, service_stop)
        {
            tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        protected override TaskStatus TaskStatus { get => tcs.Task.Status; }
        protected override bool TrySetResult(object data) => tcs.TrySetResult((T)data);
        protected override bool TrySetException(Exception e) => (tcs != null) && tcs.TrySetException(e);
        protected override bool TrySetCancelled(CancellationToken cancellationToken) => (tcs != null) && tcs.TrySetCanceled(cancellationToken);
        protected override object TypedFromJToken(JToken jToken)
        {
            var messsage_t = jToken.FromJToken<Message<T>>();
            if (IsWithLuaTimeStamp)
                ((IWithLuaTimeStamp)messsage_t.Data).lua_timestamp = messsage_t.CreatedTime;
            return messsage_t;
        }

        public new void Dispose()
        {
            base.Dispose();
            ((IDisposable)ResultTask).Dispose();
        }
    }
}