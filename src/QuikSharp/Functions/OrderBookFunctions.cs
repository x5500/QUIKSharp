// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using QUIKSharp.DataStructures;
using QUIKSharp.Transport;
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
        Task<bool> Subscribe(ISecurity security);

        /// <summary>
        /// Функция отменяет заказ на получение с сервера стакана по указанному классу и бумаге.
        /// </summary>
        Task<bool> Unsubscribe(ISecurity security);

        /// <summary>
        /// Функция позволяет узнать, заказан ли с сервера стакан по указанному классу и бумаге.
        /// </summary>
        Task<bool> IsSubscribed(ISecurity security);

        /// <summary>
        /// Функция предназначена для получения стакана по указанному классу и инструменту
        /// </summary>
        Task<OrderBook> GetQuoteLevel2(ISecurity security);
    }

    /// <summary>
    /// Функции для работы со стаканом котировок
    /// </summary>
    public class OrderBookFunctions : FunctionsBase, IOrderBookFunctions
    {
        internal OrderBookFunctions(IQuikService quikService) : base(quikService)
        {
        }

        public Task<bool> Subscribe(ISecurity security)
        {
            return QuikService.SendAsync<bool>(new MessageS(new[] { security.ClassCode, security.SecCode }, "Subscribe_Level_II_Quotes"));
        }

        public Task<bool> Unsubscribe(ISecurity security)
        {
            return QuikService.SendAsync<bool>(new MessageS(new[] { security.ClassCode, security.SecCode }, "Unsubscribe_Level_II_Quotes"));
        }

        public Task<bool> IsSubscribed(ISecurity security)
        {
            return QuikService.SendAsync<bool>(new MessageS(new[] { security.ClassCode, security.SecCode }, "IsSubscribed_Level_II_Quotes"));
        }

        public Task<OrderBook> GetQuoteLevel2(ISecurity security)
        {
            return QuikService.SendAsync<OrderBook>(new MessageS(new[] { security.ClassCode, security.SecCode }, "GetQuoteLevel2"));
        }
    }
}