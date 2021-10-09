// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp
{
    public interface IOrderFunctions
    {
        Task<Order> GetOrder(string classCode, long orderId, CancellationToken cancellationToken);

        Task<List<Order>> GetOrders(CancellationToken cancellationToken);

        Task<List<Order>> GetOrders(ISecurity sec, CancellationToken cancellationToken);

        Task<Order> GetOrder_by_Number(long order_num, CancellationToken cancellationToken);

        Task<Order> GetOrder_by_transID(ISecurity sec, long trans_id, CancellationToken cancellationToken);

        Task<List<StopOrder>> GetStopOrders(CancellationToken cancellationToken);

        Task<List<StopOrder>> GetStopOrders(ISecurity sec, CancellationToken cancellationToken);
    }
}