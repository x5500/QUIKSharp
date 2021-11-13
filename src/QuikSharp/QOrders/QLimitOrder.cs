// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System;

namespace QUIKSharp.QOrders
{
    public class QLimitOrder : QOrder
    {
        /// <summary>
        /// Способ исполнения лимитного ордера
        /// </summary>
        public ExecutionCondition Execution = ExecutionCondition.PUT_IN_QUEUE;

        /// <summary>
        /// Тип заявки (рыночная или лимитная)
        /// </summary>
        public TransactionType OrderType { get; set; }

        // --------------------- Связанный стоп-ордер --------------------------------------------
        ///  Стоп-ордер, который инициировал постановку этого лимитного ордера
        // ----- Поля, получаемые из Quik -----
        /// <summary>
        ///  Номер стоп-ордера, который инициировал постановку этого лимитного ордера
        ///  Поле linkedOrder
        /// </summary>
        public ulong LinkedOrderNum { get; internal set; }

        /// <summary>
        /// Есть ли привязанный стоп-ордер, который инициировал постановку этого лимитного ордера?
        /// </summary>
        public bool IsSetLinkedOrderNum { get => LinkedOrderNum != 0; }

        // ----- Поля, заполныемые при внутренней привязке -----
        /// <summary>
        /// Стоп-ордер, с которым связан текущий ордер
        /// </summary>
        public QStopOrder LinkedOrder { get; protected set; }

        /// <summary>
        /// Роль этого лимитного ордера по отношению к стоп-ордеру
        /// </summary>
        public QOrderLinkedRole LinkedRole { get; protected set; }

        // --------------------- Связанный стоп-ордер --------------------------------------------
        ///  Стоп-ордер, который зависит от этого лимитного ордера
        public QStopOrder DependentOrder { get; internal set; }

        // --------------------- Связанный стоп-ордер --------------------------------------------

        public event QOrderDelegateMoved OnMoved;

        public QLimitOrder(ITradeSecurity ins, Operation operation, decimal dealprice, long qty, ExecutionCondition execution = ExecutionCondition.PUT_IN_QUEUE, TransactionType transactionType = TransactionType.L)
            : base(ins, operation, dealprice, qty)
        {
            Execution = execution;
            OrderType = transactionType;
        }

        /// <summary>
        /// Создает экземпляр QLimitOrder на основе ордера Order
        /// </summary>
        /// <param name="order"></param>
        /// <param name="copyQtyMode">Использовать неисполненный объем или полный обьем ордера?</param>
        public QLimitOrder(Order order, CopyQtyMode copyQtyMode) : base(
                new UnattendedTradeSecurity { AccountID = order.Account, ClientCode = order.ClientCode, ClassCode = order.ClassCode, SecCode = order.SecCode },
                operation: order.Operation,
                order.Price,
                copyQtyMode == CopyQtyMode.Qty ? order.Quantity : copyQtyMode == CopyQtyMode.QtyLeft ? order.Balance : order.Quantity - order.Balance )
        {
            OrderType = order.IsLimit ? TransactionType.L : TransactionType.M;
            switch (order.ExecType)
            {
                case OrderExecType.FillOrKill:
                    Execution = ExecutionCondition.FILL_OR_KILL;
                    break;

                case OrderExecType.PlaceInQuery:
                    Execution = ExecutionCondition.PUT_IN_QUEUE;
                    break;

                case OrderExecType.ImmediateOrCancel:
                    Execution = ExecutionCondition.KILL_BALANCE;
                    break;

                default:
                    Execution = ExecutionCondition.PUT_IN_QUEUE;
                    break;
            };
            Expiry = order.ExecType == OrderExecType.WhileThisSession ? DateTime.Today
                : (order.ExecType != OrderExecType.GoodTillCancelled) && (order.ExpiryDate > DateTime.Today) ? order.ExpiryDate
                : DateTime.MinValue;

            TransID = order.TransID;
            OrderNum = order.OrderNum;
            LinkedOrderNum = order.Linkedorder;

            QtyLeft = order.Balance;
            QtyTraded = 0;

            SetQuikState(order.State, true);
        }

        public QLimitOrder(QLimitOrder copy_from, CopyQtyMode copyQty) : base(copy_from, copyQty)
        {
            Execution = copy_from.Execution;
            OrderType = copy_from.OrderType;
        }
        internal void UpdateFrom(TransactionReply transReply)
        {
            TransID = transReply.TransID;
            OrderNum = transReply.OrderNum;

            if (transReply.Price.HasValue)
                Price = transReply.Price.Value;

            // transReply.Balance == 0 for new orders...
            //   if (transReply.Quantity.HasValue && transReply.Balance.HasValue)
            //      this.SetQty(transReply.Quantity.Value, transReply.Balance.Value);

            if (transReply.Quantity.HasValue)
                SetQty(transReply.Quantity.Value, transReply.Quantity.Value);

        }

        /// <summary>
        /// Вызывается по событию OnOrder
        /// Обновляем QOrder в соответствии с изменениями order
        /// </summary>
        /// <param name="order"></param>
        /// <param name="noCallEvents"></param>
        internal void UpdateFrom(Order order, bool noCallEvents)
        {
            TransID = order.TransID;
            OrderNum = order.OrderNum;
            LinkedOrderNum = order.Linkedorder;

            Operation = order.Operation;
            Price = order.Price;

            OrderType = order.IsLimit ? TransactionType.L : TransactionType.M;
            switch (order.ExecType)
            {
                case OrderExecType.FillOrKill:
                    Execution = ExecutionCondition.FILL_OR_KILL;
                    break;

                case OrderExecType.PlaceInQuery:
                    Execution = ExecutionCondition.PUT_IN_QUEUE;
                    break;

                case OrderExecType.ImmediateOrCancel:
                    Execution = ExecutionCondition.KILL_BALANCE;
                    break;

                default:
                    Execution = ExecutionCondition.PUT_IN_QUEUE;
                    break;
            };
            Expiry = order.ExecType == OrderExecType.WhileThisSession ? DateTime.Today
                : (order.ExecType != OrderExecType.GoodTillCancelled) && (order.ExpiryDate > DateTime.Today) ? order.ExpiryDate
                : DateTime.MinValue;

            //this.SetQty(order.Quantity, order.Balance);
            //Обрабатываем изменения в протогрованном обьеме, если есть.
            SetBalance(order.Balance, noCallEvents);
            SetQuikState(order.State, noCallEvents);
        }

        public override Transaction PlaceOrderTransaction()
        {
            var t = base.PlaceOrderTransaction();
            t.ACTION = TransactionAction.NEW_ORDER;
            t.TYPE = OrderType;
            t.EXECUTION_CONDITION = Execution;
            SetTransacExpityDate(t);
            return t;
        }

        public Transaction MoveOrderTransaction(decimal new_price, long new_qty) => new Transaction
        {
            ACTION = TransactionAction.MOVE_ORDERS,
            MoveMode = (QtyLeft != new_qty) ? TransactionMode.NewQty : TransactionMode.SameQty,
            ClassCode = TradeSecurity.ClassCode,
            SecCode = TradeSecurity.SecCode,
            ACCOUNT = TradeSecurity.AccountID,
            FIRST_ORDER_NUMBER = OrderNum,
            FIRST_ORDER_NEW_PRICE = new_price,
            FIRST_ORDER_NEW_QUANTITY = new_qty,
        };

        public override Transaction KillOrderTransaction() => new Transaction
        {
            ACTION = TransactionAction.KILL_ORDER,
            ClassCode = TradeSecurity.ClassCode,
            SecCode = TradeSecurity.SecCode,
            ACCOUNT = TradeSecurity.AccountID,
            ORDER_KEY = OrderNum,
        };

        internal void CallEvent_OnMoved(QLimitOrder new_qOrder)
        {
            OnMoved?.Invoke(this, new_qOrder);
        }

        protected override void ProcessTradedQty(long partial, bool noCallEvent)
        {
            base.ProcessTradedQty(partial, noCallEvent);
            if (LinkedOrder != null)
            {
                // надо известить родительский стоп-ордер о исполнении объема
                LinkedOrder.OnLinkedOrderQty(this, noCallEvent);
            }
        }
        protected override void SetState(QOrderState new_state, bool noCallEvents)
        {
            if (State == new_state) return;

            base.SetState(new_state, noCallEvents);
            if (LinkedOrder != null)
            {
                // надо известить родительский стоп-ордер о смене статуса дочернего ордера
                LinkedOrder.LinkedOrderChangedState(this, noCallEvents);
            }
        }

        internal void SetLinkedWith(QStopOrder stopOrder, QOrderLinkedRole role, bool noCallEvents)
        {
            if (LinkedOrderNum == 0)
            {
                LinkedOrder = stopOrder;
                LinkedRole = role;
                if (stopOrder.OrderNum.HasValue)
                    LinkedOrderNum = stopOrder.OrderNum.Value;
                LinkedOrder.OnLinkedOrderQty(this, noCallEvents);
            }
            else
            if (stopOrder.OrderNum.HasValue && (LinkedOrderNum == stopOrder.OrderNum.Value))
            {
                LinkedOrder = stopOrder;
                LinkedRole = role;
                LinkedOrder.OnLinkedOrderQty(this, noCallEvents);
            }
        }

        new protected virtual T BaseClone<T>() where T : QLimitOrder
        {
            var o = base.BaseClone<T>();
            o.LinkedOrder = null;
            o.LinkedOrderNum = 0;
            o.LinkedRole = QOrderLinkedRole.StandAlone;
            o.DependentOrder = null;
            return o;
        }

        public override object Clone()
        {
            return BaseClone<QLimitOrder>();
        }


    }
}
