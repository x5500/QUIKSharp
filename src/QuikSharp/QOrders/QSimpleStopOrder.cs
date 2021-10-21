// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;

namespace QUIKSharp.QOrders
{
    public class QSimpleStopOrder : QStopOrder, IStopOrder
    {
        public decimal StopPrice { get; set; }
        public decimal StopDealPrice { get => base.Price; set => base.Price = value; }

        /// <summary>
        /// Price => StopPrice
        /// </summary>
        public new decimal Price { get => StopPrice; set => StopPrice = value; }

        public QSimpleStopOrder(ITradeSecurity ins, Operation operation, decimal stop_price, decimal dealprice, long qty)
            : base(ins, operation, dealprice, qty)
        {
            StopPrice = stop_price;
        }

        internal QSimpleStopOrder(StopOrder stopOrder, bool useBalance = false) : base(stopOrder, useBalance)
        {
            StopPrice = stopOrder.ConditionPrice;
        }

        public override Transaction PlaceOrderTransaction()
        {
            var t = base.PlaceOrderTransaction();
            t.STOPPRICE = StopPrice;
            t.STOP_ORDER_KIND = IsActiveOrderExecution ? StopOrderKind.ACTIVATED_BY_ORDER_SIMPLE_STOP_ORDER : StopOrderKind.SIMPLE_STOP_ORDER;
            return t;
        }

        internal override void UpdateFrom(StopOrder stopOrder, bool noCallEvents)
        {
            StopPrice = stopOrder.ConditionPrice;
            base.UpdateFrom(stopOrder, noCallEvents);
        }

    }
}