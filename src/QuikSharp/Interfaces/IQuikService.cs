// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using System;
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

        Task<TResult> SendAsync<TResult>(IMessage request, int timeout_ms = 0);

        /// <summary>
        /// Возвращает текущее значение счетчиков статистики работы сервиса
        /// </summary>
        /// <param name="bytes_sent"></param>
        /// <param name="bytes_recieved"></param>
        /// <param name="bytes_callback"></param>
        /// <param name="request_query_size"></param>
        void GetNetStats(out long bytes_sent, out long bytes_recieved, out long bytes_callback, out long request_query_size);
    }
}