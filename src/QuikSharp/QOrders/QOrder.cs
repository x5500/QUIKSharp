// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using NLog;
using QUIKSharp.Converters;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System;

namespace QUIKSharp.QOrders
{
    public delegate void QOrderDelegate(QOrder sender);
    public delegate void QOrderDelegateQty(QOrder sender, long last_filled_qty);
    public abstract class QOrder : ICloneable
    {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public ITradeSecurity TradeSecurity { protected get; set; }
        public Operation Operation { get; set; }
        public decimal Price { get; set; }

        /// <summary>
        /// Номер размещенного в системе Ордера
        /// Если стоп-заявка активна, то ее номер, если исполнена и при ее исполнении появился лимитный ордер - то его номер
        /// </summary>
        public long? OrderNum { get; set; }

        /// <summary>
        /// Номер транзакции, с помощью которой был размещен этот ордер
        /// </summary>
        public long? TransID { get; set; }

        //  В limit-заявке нужно помнить volume, QtyTraded, QtyLeft.
        //  Обычно QtyTraded + volumeLeft == volume, но иногда при снятии частично исполненной заявки становится понятно,
        //  чему равен QtyLeft и надо дождаться событий OnTrade(), которые ещё пока не пришли.
        //  При приходе OnTrade() нужно отбрасывать дубликаты (в 7-й версии) и корректировать volumeTraded, volumeLeft.
        //  Как только volumeLeft == 0, так удалять заявку из таблицы актуальных вместе со всеми связанными с ней kill-заявками и ответами на них.

        /// <summary>
        /// Дата экспирации ордера (время значения не имеет)
        /// </summary>
        public DateTime Expiry { get; protected set; }

        public bool ExpireEndOfDay
        {
            get => Expiry.Date == DateTime.Today;
            set { Expiry = DateTime.Today + Expiry.TimeOfDay; }
        }

        private long _qty;
        /// <summary>
        /// Заказанный (Выставленный) объем заявки
        /// </summary>        
        public long Qty
        {
            get => _qty;
            set => SetQty(value, value);
        }
        /// <summary>
        /// Проторгованный объем (Подсчитывается на основании событий Ontrade)
        ///  Обычно QtyTraded + QtyLeft == volume, но иногда при снятии частично исполненной заявки становится понятно,
        /// </summary>
        public long QtyTraded { get; protected set; }

        /// <summary>
        /// Неисполненнный Остаток
        /// Берется или из OnOrder или на основании вычисления QtyTraded
        /// для транзакции на снятие неисполненной (в т.ч. частично) заявки.
        /// </summary>
        public long QtyLeft { get; protected set; }

        /// <summary>
        /// Состояние размещения Q-ордера
        /// </summary>
        public QOrderState State { get => _state; set => SetState(value, false); }

        private QOrderState _state;

        /// <summary>
        /// Дата и время последнего обновления State
        /// </summary>
        public DateTime StateUpdated { get; private set; }

        /// <summary>
        ///  Состояние отмены ордера
        /// </summary>
        private QOrderKillState _killstate;

        public QOrderKillState Killstate { get => _killstate; set { _killstate = value; KillstateUpdated = DateTime.Now; } }

        /// <summary>
        /// Дата и время последнего обновления Killstate
        /// </summary>
        public DateTime KillstateUpdated { get; private set; }

        public State QuikState { get; protected set; }

        public string SecCode
        {
            get => TradeSecurity.SecCode;
        }
        public string ClassCode
        {
            get => TradeSecurity.ClassCode;
        }
        public string AccountID
        {
            get => TradeSecurity.AccountID;
        }
        public string ClientCode
        {
            get => TradeSecurity.ClientCode;
        }

        public event QOrderDelegate OnPlaced;

        public event QOrderDelegate OnExecuted;

        /// <summary>
        /// Событие при полном исполнении ордера
        /// </summary>
        public event QOrderDelegate OnFilled;

        public event QOrderDelegate OnKilled;

        /// <summary>
        /// Событие при частичном исполнении ордера
        /// </summary>
        public event QOrderDelegateQty OnPartial;

        public QOrder(ITradeSecurity ins, Operation operation, decimal price, long qty)
        {
            this.TradeSecurity = ins;
            this.Operation = operation;
            this.Price = price;
            this._qty = qty;
            this.QtyLeft = qty;
            this.QtyTraded = 0;

            _state = QOrderState.None;
            Killstate = QOrderKillState.NoKill;
        }

        /// <summary>
        /// Возвращает true если ордер активный (размещен и не исполнен полностью).
        /// </summary>
        /// <returns></returns>
        public bool isActive()
        {
            switch (this.State)
            {
                case QOrderState.Placed:
                case QOrderState.Filled:
                    return true;
                default:
                    return false;
            }
        }

        public virtual Transaction PlaceOrderTransaction() => new Transaction
        {
            ACCOUNT = TradeSecurity.AccountID,
            CLIENT_CODE = TradeSecurity.ClientCode,
            ClassCode = TradeSecurity.ClassCode,
            SecCode = TradeSecurity.SecCode,
            QUANTITY = this.Qty,
            OPERATION = this.Operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
            PRICE = this.Price,
        };

        /// <summary>
        /// Устанавливаем значения объема заявки
        /// (Конечный вариант, например при исполнении ордера или при снятии)
        /// </summary>
        /// <param name="qty">Заказанный (Выставленный) объем заявки</param>
        /// <param name="balance">Неисполненный объем заявки</param>
        internal void SetQty(long qty, long balance)
        {
            this._qty = qty;
            this.QtyLeft = balance;
            // TODO: Решить что то с QtyTraded
        }

        protected virtual void ProcessTradedQty(long partial, bool noCallEvent)
        {
            // Есть дополнительный проторгованный объем
            this.QtyLeft -= partial;

            if (QtyLeft == 0)
            {
                // Completed
                if (QuikState != DataStructures.State.Completed)
                    SetQuikState(DataStructures.State.Completed, noCallEvent);
            }

            if (!noCallEvent)
                CallEvent_OnPartial(partial);
        }

        /// <summary>
        /// Вносим проторгованный обьем и вызываем событие OnPartial
        /// Возвращает реально протогованный объем на основании имеющейся информации
        /// </summary>
        /// <param name="traded">проторгованный обьем  заявки</param>
        /// <param name="noCallEvent">Не вызывать событие</param>
        internal long AddTraded(long traded, bool noCallEvent)
        {
            this.QtyTraded += traded;
            long left = this.Qty - this.QtyTraded;
            long partial = this.QtyLeft - left;
            if (partial > 0)
            {
                // Есть дополнительный проторгованный объем
                ProcessTradedQty(partial, noCallEvent);
            }
            return partial;
        }

        /// <summary>
        /// Вносим остаточный неисполненный обьем и вызываем событие OnPartial
        /// Возвращает реально протогованный объем на основании имеющейся информации
        /// </summary>
        /// <param name="balance">Неисполненный объем заявки</param>
        /// <param name="noCallEvent">Не вызывать событие</param>
        internal long SetBalance(long balance, bool noCallEvent)
        {
            long partial = this.QtyLeft - balance;
            if (partial > 0)
            {
                // Есть дополнительный проторгованный объем
                ProcessTradedQty(partial, noCallEvent);
            }
            return partial;
        }
        public abstract Transaction KillOrderTransaction();

        protected void SetTransacExpityDate(Transaction t)
        {
            t.EXPIRY_DATE = ExpireEndOfDay ? "TODAY" : Expiry.Date > DateTime.MinValue ? QuikDateTimeConverter.DateTimeToYYYYMMDD(this.Expiry.Date) : "GTC";
        }
        internal virtual void SetQuikState(State new_state, bool noCallEvents)
        {
            QuikState = new_state;
            switch (QuikState)
            {
                case QUIKSharp.DataStructures.State.Rejected:
                    SetState(QOrderState.ErrorRejected, noCallEvents);
                    break;

                case QUIKSharp.DataStructures.State.Canceled:
                    if ((Killstate != QOrderKillState.NoKill) && (Killstate != QOrderKillState.Killed))
                        Killstate = QOrderKillState.Killed;
                    SetState(QOrderState.Killed, noCallEvents);
                    break;

                case QUIKSharp.DataStructures.State.Completed:
                    SetState(QOrderState.Filled, noCallEvents);
                    break;

                case QUIKSharp.DataStructures.State.Active:
                    SetState(QOrderState.Placed, noCallEvents);
                    break;
            }
        }
        protected virtual void SetState(QOrderState new_state, bool noCallEvents)
        {
            if (this._state == new_state) return;
            switch (this._state)
            {
                // Allow change state
                case QOrderState.None:
                case QOrderState.WaitPlacement:
                case QOrderState.RequestedPlacement:
                case QOrderState.ErrorRejected:
                    break;
                // Immune: No change state.
                case QOrderState.Filled:
                case QOrderState.Killed:
                    throw new Exception($"No change state [{_state}] => [{new_state}]");
                case QOrderState.Placed:
                    switch (new_state)
                    {   // Immune: Allowed only:
                        case QOrderState.Executed:
                        case QOrderState.ErrorRejected:
                        case QOrderState.Filled:
                        case QOrderState.Killed:
                        case QOrderState.WaitMove:
                        case QOrderState.RequestedMove:
                            break;
                        default:
                            throw new Exception($"No change state [{_state}] => [{new_state}]");
                    }
                    break;
                case QOrderState.Executed:
                    switch (new_state)
                    {   // Immune: Allowed only:
                        case QOrderState.Filled:
                        case QOrderState.Killed:
                            break;
                        default:
                            throw new Exception($"No change state [{_state}] => [{new_state}]");
                    }
                    break;
                case QOrderState.WaitMove:
                case QOrderState.RequestedMove:
                    switch (new_state)
                    {   // Immune: Allowed only:
                        case QOrderState.WaitMove:
                        case QOrderState.RequestedMove:
                        case QOrderState.ErrorRejected:
                        case QOrderState.Placed:
                            break;
                        default:
                            throw new Exception($"No change state [{_state}] => [{new_state}]");
                    }
                    break;
            }

            this._state = new_state;
            StateUpdated = DateTime.Now;

            if (noCallEvents) return;

            switch (this._state)
            {
                case QOrderState.Placed:
                    CallEvent_OnPlaced();
                    break;

                case QOrderState.Executed:
                    CallEvent_OnExecuted();
                    break;

                case QOrderState.Filled:
                    CallEvent_OnFilled();
                    break;

                case QOrderState.ErrorRejected:
                // TODO: Maybe call another event
                case QOrderState.Killed:
                    CallEvent_OnKilled();
                    break;
            }
        }

        protected void CallEvent_OnPlaced()
        {
            OnPlaced?.Invoke(this);
        }

        protected void CallEvent_OnKilled()
        {
            OnKilled?.Invoke(this);
        }

        protected void CallEvent_OnExecuted()
        {
            OnExecuted?.Invoke(this);
        }

        protected virtual void CallEvent_OnPartial(long filled_qty)
        {
            OnPartial?.Invoke(this, filled_qty);
        }

        protected void CallEvent_OnFilled()
        {
            OnFilled?.Invoke(this);
        }

        public void ResetEvents()
        {
            OnPlaced = null;
            OnExecuted = null;
            OnPartial = null;
            OnFilled = null;
            OnKilled = null;
        }
        protected virtual T BaseClone<T>() where T : QOrder
        {
            var clone = (T)base.MemberwiseClone();
            clone.ResetEvents();
            return clone;
        }

        public abstract object Clone();
    }
}