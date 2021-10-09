// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;

namespace QUIKSharp.QOrders
{
    public class QTakeOrder : QStopOrder, ITakeOrder
    {
        public decimal TakePrice { get; set; }
        public decimal Offset { get; set; }
        public decimal Spread { get; set; }

        /// <summary>
        /// Price => TakePrice
        /// </summary>
        public new decimal Price { get => TakePrice; set => TakePrice = value; }

        public override object Clone()
        {
            return BaseClone<QTakeOrder>();
        }

        public QTakeOrder(ITradeSecurity ins, Operation operation, decimal take_price, decimal offset, decimal spread, long qty) : base(ins, operation, 0m, qty)
        {
            this.TakePrice = take_price;
            this.Spread = spread;
            this.Offset = offset;
        }

        internal QTakeOrder(StopOrder stopOrder, bool useBalance = false) : base(stopOrder, useBalance)
        {
            Offset = stopOrder.Offset;
            Spread = stopOrder.Spread;
        }

        public override Transaction PlaceOrderTransaction()
        {
            var t = base.PlaceOrderTransaction();
            t.STOP_ORDER_KIND = IsActiveOrderExecution ? StopOrderKind.ACTIVATED_BY_ORDER_TAKE_PROFIT_STOP_ORDER : StopOrderKind.TAKE_PROFIT_STOP_ORDER;
            t.STOPPRICE = this.TakePrice; // -- тэйк-профит
            t.OFFSET = Offset;
            t.OFFSET_UNITS = OffsetUnits.PRICE_UNITS;
            t.SPREAD = Spread;
            t.SPREAD_UNITS = OffsetUnits.PRICE_UNITS;
            return t;
        }
        internal override void UpdateFrom(StopOrder stopOrder, bool noCallEvents)
        {
            TakePrice = stopOrder.ConditionPrice;
            base.UpdateFrom(stopOrder, noCallEvents);
        }
    }
}