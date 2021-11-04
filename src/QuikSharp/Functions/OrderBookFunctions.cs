// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using QUIKSharp.DataStructures;
using QUIKSharp.Transport;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    /// <summary>
    /// Функции для работы со стаканом котировок
    /// </summary>
    public class OrderBookFunctions : FunctionsBase, IOrderBookFunctions
    {
        internal OrderBookFunctions(IQuikService quikService) : base(quikService)
        {
        }

        public Task<bool> Subscribe(ISecurity security, CancellationToken cancellationToken)
        {
            return QuikService.SendAsync<bool>(new MessageS(new[] { security.ClassCode, security.SecCode }, "Subscribe_Level_II_Quotes"), cancellationToken);
        }

        public Task<bool> Unsubscribe(ISecurity security, CancellationToken cancellationToken)
        {
            return QuikService.SendAsync<bool>(new MessageS(new[] { security.ClassCode, security.SecCode }, "Unsubscribe_Level_II_Quotes"), cancellationToken);
        }

        public Task<bool> IsSubscribed(ISecurity security, CancellationToken cancellationToken)
        {
            return QuikService.SendAsync<bool>(new MessageS(new[] { security.ClassCode, security.SecCode }, "IsSubscribed_Level_II_Quotes"), cancellationToken);
        }

        public Task<OrderBook> GetQuoteLevel2(ISecurity security, CancellationToken cancellationToken)
        {
            return QuikService.SendAsync<OrderBook>(new MessageS(new[] { security.ClassCode, security.SecCode }, "GetQuoteLevel2"), cancellationToken);
        }
    }
}