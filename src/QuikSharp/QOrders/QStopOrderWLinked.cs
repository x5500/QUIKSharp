// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System;

namespace QUIKSharp.QOrders
{
    /// <summary>
    /// Стоп-ордер со связанной лимитной заявкой (WITH_LINKED_LIMIT_ORDER)
    /// Одним ордером выставляется и лимитная и стоп заявка., обе в одном направлении
    /// Цена исполнения связанной заявки - linked_price
    /// Направление сделки связанной заявки - такое же как и у стоп-заявки
    /// </summary>
    public class QStopOrderWLinked : QSimpleStopOrder, IStopOrder, ITakeOrder
    {
        /// <summary>
        /// Не используется для стоп-ордера со связанной лимитной заявкой (WITH_LINKED_LIMIT_ORDER)
        /// </summary>
        public override bool IsActiveOrderExecution => false;

        /// <summary>
        /// Не используется для стоп-ордера со связанной лимитной заявкой (WITH_LINKED_LIMIT_ORDER)
        /// </summary>
        public override bool ActivateOnPartlyFilled => false;

        public bool KillIfPartlyFilled { get; set; } = false;

        /// <summary>
        /// Цена исполнения связанной лимитной заявки (ТейкПрофит)
        /// </summary>
        public decimal TakePrice { get; set; }

        /// <summary>
        /// Так как ТейкОрдер - это связанная лимитная заявка, то у нее нет параметров Offset и Spread
        ///  throws new NotImplementedException()
        /// </summary>
        decimal ITakeOrder.Offset { get => 0m; set => throw new NotImplementedException(); }

        /// <summary>
        ///  Так как ТейкОрдер - это связанная лимитная заявка, то у нее нет параметров Offset и Spread
        ///  throws new NotImplementedException()
        /// </summary>
        decimal ITakeOrder.Spread { get => 0m; set => throw new NotImplementedException(); }

        public override object Clone()
        {
            return BaseClone<QStopOrderWLinked>();
        }

        public QStopOrderWLinked(ITradeSecurity ins, Operation operation, decimal linked_price, decimal stop_price, decimal dealprice, long qty)
            : base(ins, operation, stop_price, dealprice, qty)
        {
            TakePrice = linked_price;
        }

        internal QStopOrderWLinked(StopOrder stopOrder, bool useBalance = false) : base(stopOrder, useBalance)
        {
            TakePrice = stopOrder.co_order_price;
            KillIfPartlyFilled = stopOrder.StopFlags.HasFlag(StopBehaviorFlags.KillIfPartlyFilled);
        }

        internal override void UpdateFrom(StopOrder stopOrder, bool noCallEvents)
        {
            base.UpdateFrom(stopOrder, noCallEvents);
            TakePrice = stopOrder.co_order_price;
        }

        public override Transaction PlaceOrderTransaction()
        {
            var t = base.PlaceOrderTransaction();
            t.STOP_ORDER_KIND = StopOrderKind.WITH_LINKED_LIMIT_ORDER;
            //t.STOPPRICE = this.StopPrice;
            t.LINKED_ORDER_PRICE = TakePrice;
            t.KILL_IF_LINKED_ORDER_PARTLY_FILLED = KillIfPartlyFilled ? YesOrNo.YES : YesOrNo.NO;
            return t;
        }

        /// <summary>
        /// throws InvalidOperationException();
        /// QStopOrderWLinked не может исполняться по условию выполенния лимитной заявки
        /// </summary>
        /// <param name="limitOrder"></param>
        /// <returns></returns>
        public override bool SetDependsOn(QLimitOrder limitOrder)
        {
            throw new InvalidOperationException("QStopOrderWLinked не может исполняться по условию выполенния лимитной заявки");
        }

        /// <summary>
        /// Добавляем связанный лимитный ордер, который зависит от этого стоп-ордера
        /// </summary>
        /// <param name="limitOrder">лимитный ордер связанный со стоп-ордером</param>
        /// <param name="noCallEvents">Не вызывать события (при добавлении ордеров из истории)</param>
        internal override void SetCoOrder(QLimitOrder limitOrder, bool noCallEvents)
        {
            CoOrder = limitOrder;
            if (CoOrderNum != limitOrder.OrderNum && limitOrder.OrderNum.HasValue)
                CoOrderNum = limitOrder.OrderNum.Value;

            // Это стоп-ордер, у которого есть связанная лимитная заявки
            // Лимитная заявка была выставлена сервером Quik при постановке стоп-ордера
            if (ChildLimitOrder == limitOrder)
            {
                ChildLimitOrder = null;
            }
            limitOrder.SetLinkedWith(this, QOrderLinkedRole.ControlledByStopOrder, noCallEvents);
        }

        /// <summary>
        /// Обработка изменений лимитной заявки для Стоп-ордера со связанной лимитной заявкой (WITH_LINKED_LIMIT_ORDER)
        /// </summary>
        /// <param name="noCallEvents">Не вызывать события (при добавлении ордеров из истории)</param>
        protected override void CoOrderChangedState(bool noCallEvents)
        {
            // Это стоп-ордер, у которого есть связанная лимитная заявки
            // Связанные ордера могут быть отменены сервером, если сработал стоп-ордер
            // Тогда это не ошибка.
            // Так что мы не реагируем на Canceled для связанных ордеров

            switch (CoOrder.QuikState)
            {
                case QUIKSharp.DataStructures.State.Active:
                case QUIKSharp.DataStructures.State.Completed:
                    // Связанный лимтитный ордер был исполнен
                    SetState(QOrderState.Filled, noCallEvents);
                    break;

                case QUIKSharp.DataStructures.State.Canceled:
                    // Связанный лимтитный ордер (Со-Order) был отменен
                    //SetState(QOrderState.Killed, noCallEvents);
                    switch (QuikState)
                    {
                        case DataStructures.State.Active:
                            // Стоп-ордер не отменен, отмена только лимитной заявки (втб или глюк?)
                            break;
                        case DataStructures.State.Canceled:
                            // Стоп-ордер отменен, Со-Order тоже - значит заявка была снята
                            SetState(QOrderState.Killed, noCallEvents);
                            break;
                    }
                    break;

                case QUIKSharp.DataStructures.State.Rejected:
                    SetState(QOrderState.ErrorRejected, noCallEvents);
                    break;
            }
        }

        internal override void SetQuikState(State new_state, bool noCallEvents)
        {
            // Dirty Hack
            // Обрабатываем снятие стоп-ордера со связанной лимитной заявкой при частичном исполнении связанного ордера.
            // Если не прилетело событие на снятие ко-ордера, то игнорируем и ждем его
            // TODO: Может быть проблемой, если событие никогда не прилетит

            if ((new_state == QUIKSharp.DataStructures.State.Canceled) && (!QuikFlags.HasFlag(StopOrderFlags.WithdrawOnExecuted | StopOrderFlags.WithdrawOnLinked | StopOrderFlags.RejectedOnActivation)))
            {
                QuikState = new_state;

                if ((Killstate != QOrderKillState.NoKill) && (Killstate != QOrderKillState.Killed))
                    Killstate = QOrderKillState.Killed;


                if (CoOrder != null)
                {
                    if (CoOrder.State == QOrderState.Placed)
                    {
                        // Лимитная заявка еще не снята, возможно прилетит событие с проторгованным объемом
                        // Ignore
                    }
                    else
                        SetState(CoOrder.State, noCallEvents);
                }
                else
                    SetState(QOrderState.Killed, noCallEvents);
                return;
            }
            else
            {
                // Else call common event handler
                base.SetQuikState(new_state, noCallEvents);
            }
        }
    }
}