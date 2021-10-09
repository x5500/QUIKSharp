// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
namespace QUIKSharp.TestQuik
{
    public class TestOrders : IOrderFunctions
    {
        private readonly Dictionary<long, Order> orderTable = new Dictionary<long, Order>();
        private readonly Dictionary<long, StopOrder> stopOrderTable = new Dictionary<long, StopOrder>();

        public TestOrders()
        {
        }

        public void ClearAll()
        {
            orderTable.Clear();
            stopOrderTable.Clear();
        }

        public Task<Order> GetOrder(string classCode, long orderId, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<Order>> GetOrders(CancellationToken cancellationToken)
        {
            var l = new List<Order>();
           foreach (var kv in orderTable)
            {
                l.Add(kv.Value);
            }
            return l;
        }

        public async Task<List<Order>> GetOrders(ISecurity sec, CancellationToken cancellationToken)
        {
            var l = new List<Order>();
            foreach (var kv in orderTable)
            {
                if (string.Compare(kv.Value.SecCode, sec.SecCode, true)==0)
                    l.Add(kv.Value);
            }
            return l;
        }

        public async Task<Order> GetOrder_by_Number(long order_num, CancellationToken cancellationToken)
        {
            if (orderTable.TryGetValue(order_num, out var order))
                return order;
            return null;
        }

        public async Task<Order> GetOrder_by_transID(ISecurity sec, long trans_id, CancellationToken cancellationToken)
        {
            foreach (var kv in orderTable)
            {
                if ((string.Compare(kv.Value.SecCode, sec.SecCode, true) == 0)
                    &&(kv.Value.TransID == trans_id))
                    return kv.Value;
            }
            return null;
        }

        public async Task<List<StopOrder>> GetStopOrders(CancellationToken cancellationToken)
        {
            var l = new List<StopOrder>();
            foreach (var kv in stopOrderTable)
            {
                l.Add(kv.Value);
            }
            return l;
        }

        public async Task<List<StopOrder>> GetStopOrders(ISecurity sec, CancellationToken cancellationToken)
        {
            var l = new List<StopOrder>();
            foreach (var kv in stopOrderTable)
            {
                if (string.Compare(kv.Value.SecCode, sec.SecCode, true) == 0)
                    l.Add(kv.Value);
            }
            return l;
        }

        public void OnOrder(Order order)
        {
            if (order.OrderNum > 0)
            {
                orderTable[order.OrderNum] = order;
            }
        }

        public void OnStopOrder(StopOrder order)
        {
            if (order.OrderNum > 0)
            {
                stopOrderTable[order.OrderNum] = order;
            }
        }
    }
}
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена