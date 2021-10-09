// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp
{
    public interface IQuikService
    {
        TimeSpan DefaultSendTimeout { get; set; }

        QuikEvents Events { get; }

        bool IsServiceConnected();

        void Start();

        void Stop();

        Task<TResult> SendAsync<TResult>(IMessage request);

        Task<TResult> SendAsync<TResult>(IMessage request, CancellationToken task_cancel);

        /// <summary>
        /// Возвращает текущее значение счетчиков статистики работы сервиса
        /// </summary>
        void GetNetStats(out ServiceNetworkStats networkStats);
    }
}