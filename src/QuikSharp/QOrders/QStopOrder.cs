// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;

namespace QUIKSharp.QOrders
{
    public abstract class QStopOrder : QOrder
    {
        /// <summary>
        /// Для SL, TP, TPSL: Номер ордера (лимитного), к которому привязан (и зависит от него) этот стоп-ордер
        /// Для QStopOrderWLinked - Номер ордера (лимитного), который привязан и зависит от этого стоп-ордера
        /// </summary>
        public long CoOrderNum { get; set; } = 0;
        public QLimitOrder CoOrder { get; protected set; }
        /// <summary>
        /// Является ли Стоп-Ордером, активируемым по исполнению связанного ордера
        /// </summary>
        public virtual bool IsActiveOrderExecution => (CoOrderNum != 0);

        /// <summary>
        /// Номер заявки в торговой системе, зарегистрированной по наступлению условия стоп-цены.
        /// </summary>
        public long ChildLimitOrderNum { get; private set; }

        /// <summary>
        /// Лимитный ордер, который был инициирован этим стоп-ордером.
        /// </summary>
        public QLimitOrder ChildLimitOrder { get; protected set; }

        /// <summary>
        /// Активировать по частичному исполнению (лимитной) заявки, указанной как BaseOrderNum
        /// </summary>
        public virtual bool ActivateOnPartlyFilled { get; set; } = true;

        /// <summary>
        ///
        /// </summary>
        public bool UseBaseOrderBalance { get; set; } = true;

        /// <summary>
        /// QuikFlags, нужны для корректной обработки статуса Quik
        /// </summary>
        public StopOrderFlags QuikFlags { get; internal set; }

        new protected virtual T BaseClone<T>() where T : QStopOrder
        {
            var o = base.BaseClone<T>();
            o.CoOrder = null;
            o.ChildLimitOrder = null;
            return o;
        }

        public override object Clone()
        {
            return BaseClone<QStopOrder>();
        }

        public override Transaction PlaceOrderTransaction()
        {
            var t = base.PlaceOrderTransaction();
            t.ACTION = TransactionAction.NEW_STOP_ORDER;

            if (IsActiveOrderExecution)
            {
                t.BASE_ORDER_KEY = CoOrderNum;
                t.ACTIVATE_IF_BASE_ORDER_PARTLY_FILLED = this.ActivateOnPartlyFilled ? YesOrNo.YES : YesOrNo.NO;
                t.USE_BASE_ORDER_BALANCE = this.UseBaseOrderBalance ? YesOrNo.YES : YesOrNo.NO;
            }

            if (!IsActiveOrderExecution)
                SetTransacExpityDate(t);

            return t;
        }

        public override Transaction KillOrderTransaction()
        {
            var t = new Transaction
            {
                ACTION = TransactionAction.KILL_STOP_ORDER,
                ClassCode = this.ClassCode,
                ACCOUNT = TradeSecurity.AccountID,
                SecCode = this.SecCode,
                STOP_ORDER_KEY = OrderNum,
            };
            return t;
        }
        public QStopOrder(ITradeSecurity ins, Operation operation, decimal price, long qty) : base(ins, operation, price, qty) { }
        internal QStopOrder(StopOrder stopOrder, bool useBalance = false)
            : base(
                  new UnattendedTradeSecurity { AccountID = stopOrder.Account, ClientCode = stopOrder.ClientCode, ClassCode = stopOrder.ClassCode, SecCode = stopOrder.SecCode },
                operation: stopOrder.Flags.HasFlag(StopOrderFlags.Sell) ? Operation.Sell : Operation.Buy,
                stopOrder.Price, useBalance ? stopOrder.Balance : stopOrder.Quantity
            )
        {
            TransID = stopOrder.TransID;
            OrderNum = stopOrder.OrderNum;
            CoOrderNum = stopOrder.co_order_num;

            // Для Стоп-ордера эти поля заполняем по факту исполнения лимитных заявок
            QtyLeft = stopOrder.Quantity;
            QtyTraded = 0;

            this.ActivateOnPartlyFilled = stopOrder.StopFlags.HasFlag(StopBehaviorFlags.ActivateOnPartial);
            this.UseBaseOrderBalance = stopOrder.StopFlags.HasFlag(StopBehaviorFlags.UseRemains);
            this.ExpireEndOfDay = stopOrder.StopFlags.HasFlag(StopBehaviorFlags.ExpireEndOfDay);
            this.Expiry = stopOrder.Expiry;

            this.SetQuikState(stopOrder.State, stopOrder.Flags, true);
        }

        internal virtual void UpdateFrom(StopOrder stopOrder, bool noCallEvents)
        {
            this.TransID = stopOrder.TransID;
            this.OrderNum = stopOrder.OrderNum;
            this.CoOrderNum = stopOrder.co_order_num;
            this.ChildLimitOrderNum = stopOrder.LinkedOrder;
            this.Price = stopOrder.Price;

            // Для Стоп-ордера эти поля заполняем по факту исполнения лимитных заявок
            // QtyLeft, QtyTraded
            // TODO: Применимость поля stopOrder.FilledQuantity?

            //this.SetQty(stopOrder.Quantity, stopOrder.Balance);
            //this.QtyTraded = stopOrder.FilledQuantity;

            this.SetQuikState(stopOrder.State, stopOrder.Flags, noCallEvents);
        }

        internal virtual void UpdateFrom(TransactionReply transReply)
        {
            this.TransID = transReply.TransID;
            this.OrderNum = transReply.OrderNum;

            if (transReply.Price.HasValue)
                this.Price = transReply.Price.Value;

            if (transReply.Quantity.HasValue && transReply.Balance.HasValue)
                this.SetQty(transReply.Quantity.Value, transReply.Balance.Value);
        }


        /// <summary>
        /// Обрабатывает quik_state, для корректной работы нужен  StopOrderFlags
        /// Используйте SetQuikState(State new_state, StopOrderFlags quik_flags, bool noCallEvents)
        /// </summary>
        /// <param name="new_state">State</param>
        /// <param name="noCallEvents">НЕ Вызывать обработчики событий на изменение Order State</param>
        internal override void SetQuikState(State new_state, bool noCallEvents)
        {
            QuikState = new_state;
            switch (QuikState)
            {
                case QUIKSharp.DataStructures.State.Active:
                    SetState(QOrderState.Placed, noCallEvents);
                    break;

                case QUIKSharp.DataStructures.State.Rejected:
                    SetState(QOrderState.ErrorRejected, noCallEvents);
                    break;

                case QUIKSharp.DataStructures.State.Canceled:
                    if (QuikFlags.HasFlag(StopOrderFlags.RejectedOnActivation))
                    {   // Rejected
                        SetState(QOrderState.ErrorRejected, noCallEvents);
                    }
                    else
                    if (QuikFlags.HasFlag(StopOrderFlags.WithdrawOnLinked))
                    {   // Cancelled on Linked Kill
                        SetState(QOrderState.Killed, noCallEvents);
                    }
                    else
                    if (QuikFlags.HasFlag(StopOrderFlags.WithdrawOnExecuted))
                    {
                        // Cancelled on Linked Executed
                        SetState(QOrderState.Filled, noCallEvents);
                    }
                    else
                        SetState(QOrderState.Killed, noCallEvents);

                    if ((Killstate != QOrderKillState.NoKill) && (Killstate != QOrderKillState.Killed))
                        Killstate = QOrderKillState.Killed;
                    break;

                case QUIKSharp.DataStructures.State.Completed:
                    // Для исполненного стоп-ордера надо выяснить были ли исполнены лимитные заявки, размещенные по результату исполнения этого ордера
                    // Пока не пришли уведомления о дочерних стоп-заявках, не оповещаем
                    if (QuikFlags.HasFlag(StopOrderFlags.RejectedOnActivation))
                    {   // Rejected
                        SetState(QOrderState.ErrorRejected, noCallEvents);
                    }
                    else
                    if (QuikFlags.HasFlag(StopOrderFlags.RejectedOnLimits))
                    {   // Rejected
                        SetState(QOrderState.ErrorRejected, noCallEvents);
                    }
                    else
                    if (QuikFlags.HasFlag(StopOrderFlags.WithdrawOnExecuted))
                    {
                        // Completed on Linked Executed
                        SetState(QOrderState.Filled, noCallEvents);
                    }
                    else
                    if (ChildLimitOrderNum == 0)
                    {
                        // Если заявка отклонена торговой системой, то она имеет статус
                        // «Исполнена», т.к.условие стоп-заявки наступило, но в поле «Номер заявки» указан «0» (ноль).
                        SetState(QOrderState.ErrorRejected, noCallEvents);
                    }
                    else
                    {   // проверка на (limitOrder == null) будет внутри функции
                        if (State != QOrderState.Filled)
                        {
                            SetState(QOrderState.Executed, noCallEvents);
                            LinkedOrderChangedState(ChildLimitOrder, noCallEvents);
                        }
                    }
                    break;
            }
        }
        /// <summary>
        /// Обрабатывает quik_state, для корректной работы нужен  StopOrderFlags
        /// </summary>
        /// <param name="new_state">State</param>
        /// <param name="quik_flags">StopOrderFlags</param>
        /// <param name="noCallEvents">НЕ Вызывать обработчики событий на изменение Order State</param>
        internal void SetQuikState(State new_state, StopOrderFlags quik_flags, bool noCallEvents)
        {
            QuikFlags = quik_flags;
            SetQuikState(new_state, noCallEvents);
        }

        /// <summary>
        /// Добавляем связанный лимитный ордер, который зависит от этого стоп-ордера или этот стоп-ордер зависит от этого лимитного ордера
        /// </summary>
        /// <param name="limitOrder">лимитный ордер связанный со стоп-ордером</param>
        /// <param name="noCallEvents"></param>
        internal virtual void SetCoOrder(QLimitOrder limitOrder, bool noCallEvents)
        {
            if (limitOrder == null)
                return;

            CoOrder = limitOrder;
            if (CoOrderNum != limitOrder.OrderNum && limitOrder.OrderNum.HasValue)
                CoOrderNum = limitOrder.OrderNum.Value;

            // Это стоп-ордер, активируемый по исполнению лимитного ордера
            // Linked with limit order
            limitOrder.DependentOrder = this;
            //limitOrder.SetLinkedWith(this, QOrderLinkedRole.MasterOrder);

            // Это стоп-ордер, у которого есть связанная лимитная заявки
            // Лимитная заявка была выставлена сервером Quik при постановке стоп-ордера
            if (ChildLimitOrder == limitOrder)
            {
                ChildLimitOrder = null;
            }
            OnLinkedOrderQty(limitOrder, noCallEvents);
        }

        /// <summary>
        /// Делает этот стоп-ордер активируемым по исполнению лимитного ордера.
        /// </summary>
        /// <param name="limitOrder"></param>
        /// <returns></returns>
        public virtual bool SetDependsOn(QLimitOrder limitOrder)
        {
            if (this.State != QOrderState.None)
                return false;

            SetCoOrder(limitOrder, true);
            return true;
        }

        /// <summary>
        /// Добалвяем информацию о порожденном лимитном ордере
        /// Так же проверяем, возникла ли необходимость сообщить о полностью исполненном стоп-ордере
        /// (т.е. исполнились порожденные лимитные ордера)
        /// </summary>
        /// <param name="limitOrder">Лимитный ордер, созданный этим стоп-ордером</param>
        /// <param name="noCallEvents">Не вызывать события (при добавлении ордеров из истории)</param>
        internal void AddChildLimitOrder(QLimitOrder limitOrder, bool noCallEvents)
        {
            if (limitOrder.OrderNum.HasValue)
            {
                if (ChildLimitOrder != null)
                {
                    logger.Error($"{this.GetType().Name}: (OrderNum:{this?.OrderNum}): AddLinkedOrder: field LinkedWith already have a value (OrderNum: {ChildLimitOrder?.OrderNum}) and will be overwritten with (OrderNum: {limitOrder?.OrderNum})");
                }

                ChildLimitOrder = limitOrder;

                if ((ChildLimitOrderNum != 0) && (ChildLimitOrderNum != limitOrder.OrderNum.Value))
                {
                    logger.Error($"{this.GetType().Name}: (OrderNum:{this?.OrderNum}): AddLinkedOrder: field ChildLimitOrderNum already have a value (OrderNum: {ChildLimitOrderNum}) and will be overwritten with (OrderNum: {limitOrder?.OrderNum})");
                }

                ChildLimitOrderNum = limitOrder.OrderNum.Value;

                if ((State == QOrderState.Placed) || (State == QOrderState.None))
                {
                    // У нас стоп-ордер в состоянии QuikState.Completed
                    // Но мы не выставляли State = Executed или Filled так как ждали информацию о дочерних заявках
                    LinkedOrderChangedState(limitOrder, noCallEvents);
                }
            }
        }

        /// <summary>
        /// Новый QStopOrder на основе Quik StopOrder
        /// </summary>
        /// <param name="orderNew"></param>
        /// <param name="useBalance"></param>
        /// <returns></returns>
        public static QStopOrder New(StopOrder orderNew, bool useBalance = false)
        {
            QStopOrder order = null;
            switch (orderNew.StopOrderType)
            {
                case StopOrderType.SimpleStopOrder:
                case StopOrderType.StopLimitOnActiveOrderExecution:
                    order = new QSimpleStopOrder(orderNew, useBalance);
                    break;

                case StopOrderType.TakeProfit:
                case StopOrderType.TakeProfitOnActiveOrderExecution:
                    order = new QTakeOrder(orderNew, useBalance);
                    break;

                case StopOrderType.TakeProfitStopLimit:
                case StopOrderType.TPSLOnActiveOrderExecution:
                    order = new QTPSLOrder(orderNew, useBalance);
                    break;

                case StopOrderType.WithLinkedOrder:
                    order = new QStopOrderWLinked(orderNew, useBalance);
                    break;

                case StopOrderType.NotImplemented:
                case StopOrderType.AnotherInstCondition:
                    return null;
            }

            switch (orderNew.StopOrderType)
            {
                case StopOrderType.StopLimitOnActiveOrderExecution:
                case StopOrderType.TakeProfitOnActiveOrderExecution:
                case StopOrderType.TPSLOnActiveOrderExecution:
                    order.CoOrderNum = orderNew.co_order_num;
                    order.ActivateOnPartlyFilled = orderNew.StopFlags.HasFlag(StopBehaviorFlags.ActivateOnPartial);
                    order.UseBaseOrderBalance = orderNew.StopFlags.HasFlag(StopBehaviorFlags.UseRemains);
                    break;
            }

            return order;
        }

        /// <summary>
        /// проверяем, возникла ли необходимость сообщить о полностью исполненном стоп-ордере
        /// (т.е. исполнились порожденные лимитные ордера)
        /// </summary>
        /// <param name="limitOrder">Лимитный ордер, созданный этим стоп-ордером</param>
        /// <param name="noCallEvents">Не вызывать события (при добавлении ордеров из истории)</param>
        internal void LinkedOrderChangedState(QLimitOrder limitOrder, bool noCallEvents)
        {
            // Это может быть как дочерний стоп-ордер, так и стоп-ордер от которого зависит эта стоп-заявка

            if (limitOrder == null) return;
            OnLinkedOrderQty(limitOrder, noCallEvents);

            switch (this.State)
            {
                case QOrderState.None:
                case QOrderState.Placed:
                case QOrderState.Executed:
                    break;

                case QOrderState.Filled:
                case QOrderState.Killed:
                case QOrderState.ErrorRejected:
                default:
                    return;
            }

            if (limitOrder == CoOrder)
                CoOrderChangedState(noCallEvents);
            else
                ChildOrderChangedState(limitOrder, noCallEvents);
        }

        /// <summary>
        /// Обработка сосотояния дочернего лимитного ордера (который был размещен автоматически при сработки стоп-заявки)
        /// </summary>
        /// <param name="limitOrder"></param>
        /// <param name="noCallEvents">Не вызывать события (при добавлении ордеров из истории)</param>
        private void ChildOrderChangedState(QLimitOrder limitOrder, bool noCallEvents)
        {
            // Это дочерний ордер
            switch (limitOrder.QuikState)
            {
                case QUIKSharp.DataStructures.State.Active:
                    SetState(QOrderState.Executed, noCallEvents);
                    break;

                case QUIKSharp.DataStructures.State.Completed:
                    SetState(QOrderState.Filled, noCallEvents);
                    break;

                case QUIKSharp.DataStructures.State.Canceled:
                    SetState(QOrderState.Killed, noCallEvents);
                    break;

                case QUIKSharp.DataStructures.State.Rejected:
                    SetState(QOrderState.ErrorRejected, noCallEvents);
                    break;
            }
        }

        /// <summary>
        /// Для Стоп-оредров по исполнению лимитной заявки обрабатываем состояние лимитной заявки, от которой мы зависим
        /// </summary>
        /// <param name="noCallEvents">Не вызывать события (при добавлении ордеров из истории)</param>
        protected virtual void CoOrderChangedState(bool noCallEvents)
        {
            // Это стоп-ордер, активируемый по исполнению лимитного ордера
            // Если лимитный ордер был отменен, так что нас тоже должны снять.
            // Если лимитный ордер был исполнен, то наш стоп-ордер будет активен
            switch (CoOrder.QuikState)
            {
                case QUIKSharp.DataStructures.State.Active:
                case QUIKSharp.DataStructures.State.Completed:
                    break;

                case QUIKSharp.DataStructures.State.Canceled:
                    // Это стоп-ордер, активируемый по исполнению лимитного ордера
                    // Лимитный ордер был отменен, так что нас тоже должны снять.
                    if (CoOrder.Qty != CoOrder.QtyLeft)
                        SetState(QOrderState.Filled, noCallEvents); // partial filled
                    else
                        SetState(QOrderState.Killed, noCallEvents);
                    break;

                case QUIKSharp.DataStructures.State.Rejected:
                    SetState(QOrderState.ErrorRejected, noCallEvents);
                    break;
            }
        }

        internal void OnLinkedOrderQty(QLimitOrder limitOrder, bool noCallEvents)
        {
            long traded = this.QtyLeft - limitOrder.QtyLeft;
            if (traded > 0)
            {
                this.QtyLeft = limitOrder.QtyLeft;

                if (!noCallEvents)
                    CallEvent_OnPartial(traded);
            }
        }
    }
}