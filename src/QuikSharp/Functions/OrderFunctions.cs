﻿// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Transport;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    /// <summary>
    /// Класс, содержащий методы работы с заявками.
    /// </summary>
    public class OrderFunctions : FunctionsBase, IOrderFunctions
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        internal OrderFunctions(IQuikService quikService) : base(quikService) { }

        /// <summary>
        /// Возвращает заявку из хранилища терминала по её номеру.
        /// На основе: http://help.qlua.org/ch4_5_1_1.htm
        /// </summary>
        /// <param name="classCode">Класс инструмента.</param>
        /// <param name="orderId">Номер заявки.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Order> GetOrder(string classCode, ulong orderId, CancellationToken cancellationToken)
        {
            var message = new MessageS(new string[] { classCode, orderId.ToString() }, "get_order_by_number");
            return QuikService.SendAsync<Order>(message, cancellationToken);
        }

        /// <summary>
        /// Возвращает список всех заявок.
        /// </summary>
        /// <returns></returns>
        public Task<List<Order>> GetOrders(CancellationToken cancellationToken)
        {
            var message = new Message<string>("", "get_orders");
            return QuikService.SendAsync<List<Order>>(message, cancellationToken);
        }

        /// <summary>
        /// Возвращает список заявок для заданного инструмента.
        /// </summary>
        public Task<List<Order>> GetOrders(ISecurity sec, CancellationToken cancellationToken)
        {
            var message = new MessageS(new string[] { sec.ClassCode, sec.SecCode }, "get_orders");
            return QuikService.SendAsync<List<Order>>(message, cancellationToken);
        }

        /// <summary>
        /// Возвращает заявку для заданного инструмента по ID.
        /// </summary>
        public Task<Order> GetOrder_by_transID(ISecurity sec, long trans_id, CancellationToken cancellationToken)
        {
            var message = new MessageS(new string[] { sec.ClassCode, sec.SecCode, trans_id.ToString() }, "getOrder_by_ID");
            return QuikService.SendAsync<Order>(message, cancellationToken);
        }

        /// <summary>
        /// Возвращает заявку по номеру.
        /// </summary>
        public Task<Order> GetOrder_by_Number(ulong order_num, CancellationToken cancellationToken)
        {
            var message = new Message<string>(order_num.ToString(), "getOrder_by_Number");
            return QuikService.SendAsync<Order>(message, cancellationToken);
        }

        /// <summary>
        /// Возвращает список всех стоп-заявок.
        /// </summary>
        /// <returns></returns>
        public Task<List<StopOrder>> GetStopOrders(CancellationToken cancellationToken)
        {
            var message = new Message<string>("", "get_stop_orders");
            return QuikService.SendAsync<List<StopOrder>>(message, cancellationToken);
        }

        /// <summary>
        /// Возвращает список стоп-заявок для заданного инструмента.
        /// </summary>
        public Task<List<StopOrder>> GetStopOrders(ISecurity sec, CancellationToken cancellationToken)
        {
            var message = new MessageS(new string[] { sec.ClassCode, sec.SecCode }, "get_stop_orders");
            return QuikService.SendAsync<List<StopOrder>>(message, cancellationToken);
        }
    }
}