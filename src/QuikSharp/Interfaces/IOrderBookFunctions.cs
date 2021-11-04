// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using QUIKSharp.DataStructures;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    /// <summary>
    /// Функции для работы со стаканом котировок
    /// </summary>
    public interface IOrderBookFunctions
    {
        /// <summary>
        /// Функция заказывает на сервер получение стакана по указанному классу и бумаге.
        /// </summary>
        Task<bool> Subscribe(ISecurity security, CancellationToken cancellationToken);

        /// <summary>
        /// Функция отменяет заказ на получение с сервера стакана по указанному классу и бумаге.
        /// </summary>
        Task<bool> Unsubscribe(ISecurity security, CancellationToken cancellationToken);

        /// <summary>
        /// Функция позволяет узнать, заказан ли с сервера стакан по указанному классу и бумаге.
        /// </summary>
        Task<bool> IsSubscribed(ISecurity security, CancellationToken cancellationToken);

        /// <summary>
        /// Функция предназначена для получения стакана по указанному классу и инструменту
        /// </summary>
        Task<OrderBook> GetQuoteLevel2(ISecurity security, CancellationToken cancellationToken);
    }
}