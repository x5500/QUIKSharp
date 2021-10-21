// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System;

namespace QUIKSharp.QOrders
{
    public class QTPSLOrder : QStopOrder, ITakeOrder, IStopOrder
    {
        public decimal TakePrice { get; set; }
        public decimal StopPrice { get; set; }

        public decimal StopDealPrice
        {
            get => Price;
            set { Price = value; MarketStopLoss = false; }
        }

        public bool MarketStopLoss { get; set; }
        public decimal Offset { get; set; }

        protected decimal _spread;

        public decimal Spread
        {
            get => _spread;
            set { _spread = value; MarketTakeProfit = false; }
        }

        public bool MarketTakeProfit { get; set; }

        /// <summary>
        /// Время начала действия заявки типа «Тэйк-профит и стоп-лимит» в формате «ЧЧММСС»
        /// Не поддерживается для Стоп-ордера активируемого по исполнению лимитной заявки
        /// </summary>
        public TimeSpan ActiveFromTime { get; set; }

        /// <summary>
        /// Время окончания действия заявки типа «Тэйк-профит и стоп-лимит» в формате «ЧЧММСС»
        /// Не поддерживается для Стоп-ордера активируемого по исполнению лимитной заявки
        /// </summary>
        public TimeSpan ActiveToTime { get; set; }

        public override object Clone()
        {
            return BaseClone<QTPSLOrder>();
        }

        public QTPSLOrder(ITradeSecurity ins, Operation operation, decimal tp_price, decimal sl_price, decimal? deal_sl_price,
            decimal offset, decimal? spread, long qty) : base(ins, operation, deal_sl_price.GetValueOrDefault(0), qty)
        {
            TakePrice = tp_price;
            StopPrice = sl_price;
            Spread = spread.GetValueOrDefault(0);
            Offset = offset;

            MarketStopLoss = !deal_sl_price.HasValue;
            MarketTakeProfit = !spread.HasValue;
        }

        internal QTPSLOrder(StopOrder stopOrder, bool useBalance = false) : base(stopOrder, useBalance)
        {
            TakePrice = stopOrder.ConditionPrice;
            StopPrice = stopOrder.ConditionPrice2;
            StopDealPrice = stopOrder.Price;

            MarketTakeProfit = stopOrder.StopFlags.HasFlag(StopBehaviorFlags.MarketTakeProfit);
            MarketStopLoss = stopOrder.StopFlags.HasFlag(StopBehaviorFlags.MarketStop);

            Offset = stopOrder.Offset;
            Spread = stopOrder.Spread;

            MarketTakeProfit = stopOrder.StopFlags.HasFlag(StopBehaviorFlags.MarketTakeProfit);
            MarketStopLoss = stopOrder.StopFlags.HasFlag(StopBehaviorFlags.MarketStop);

            if (stopOrder.ActiveFromTime < stopOrder.ActiveToTime && stopOrder.ActiveToTime > TimeSpan.Zero)
            {
                ActiveFromTime = stopOrder.ActiveFromTime;
                ActiveToTime = stopOrder.ActiveToTime;
            }
        }

        internal override void UpdateFrom(StopOrder stopOrder, bool noCallEvents)
        {
            TakePrice = stopOrder.ConditionPrice;
            StopPrice = stopOrder.ConditionPrice2;
            base.UpdateFrom(stopOrder, noCallEvents);
        }

        public override Transaction PlaceOrderTransaction()
        {
            var t = base.PlaceOrderTransaction();
            /// t.PRICE = deal_sl_price;  // -- Цена заявки, за единицу инструмента.
            t.STOP_ORDER_KIND = IsActiveOrderExecution ? StopOrderKind.ACTIVATED_BY_ORDER_TAKE_PROFIT_AND_STOP_LIMIT_ORDER : StopOrderKind.TAKE_PROFIT_AND_STOP_LIMIT_ORDER;
            t.STOPPRICE = TakePrice;  // -- тэйк-профит
            t.STOPPRICE2 = StopPrice; // -- стоп-лимит
            t.OFFSET = Offset;
            t.OFFSET_UNITS = OffsetUnits.PRICE_UNITS;
            t.MARKET_STOP_LIMIT = YesOrNo.NO;
            t.SPREAD = Spread;
            t.SPREAD_UNITS = OffsetUnits.PRICE_UNITS;

            if (!IsActiveOrderExecution)
            {
                if (ActiveFromTime < ActiveToTime && ActiveToTime > TimeSpan.Zero)
                {
                    t.IS_ACTIVE_IN_TIME = YesOrNo.YES;
                    t.ACTIVE_FROM_TIME = ActiveFromTime;
                    t.ACTIVE_TO_TIME = ActiveToTime;
                }
                else
                {
                    t.IS_ACTIVE_IN_TIME = YesOrNo.NO;
                }
            }
            else
            {
                t.EXPIRY_DATE = "TODAY";
            }

            t.MARKET_TAKE_PROFIT = MarketTakeProfit ? YesOrNo.YES : YesOrNo.NO;
            t.MARKET_STOP_LIMIT = MarketStopLoss ? YesOrNo.YES : YesOrNo.NO;

            return t;
        }

    }
}