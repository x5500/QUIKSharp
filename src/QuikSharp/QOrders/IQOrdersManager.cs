// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using QUIKSharp.DataStructures.Transaction;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.QOrders
{
    public class StopOrderEventArgs : EventArgs
    {
        public QStopOrder stopOrder;
    };

    public delegate void LimitOrderEventHandler(object sender, LimitOrderEventArgs eventArgs);
    public delegate void StopOrderEventHandler(object sender, StopOrderEventArgs eventArgs);
    public delegate void TransacErrorHandler(QOrdersActionType action, QOrder qOrder, TransactionReply transReply);


    public interface IQOrdersManager
    {
        int Delay_on_Timeout { get; set; }
        int Timeout_ms { get; set; }

        event LimitOrderEventHandler OnNewLimitOrder;
        event StopOrderEventHandler OnNewStopOrder;
        event TransacErrorHandler OnTransacError;
        event LimitOrderEventHandler OnUpdateLimitOrder;
        event StopOrderEventHandler OnUpdateStopOrder;

        void ClearTables();
        Task<QOrderActionResult> KillOrderAsync(QOrder qOrder, CancellationToken cancellation_token, int retry = 1);
        void LinkQuik(IQuik quik);
        Task<QOrderActionResult> MoveLimOrderAsync(QLimitOrder qOrder, decimal new_price, long new_qty, CancellationToken cancellation_token, int retry = 1);
        Task<QOrderActionResult> PlaceOrderAsync(QOrder qOrder, CancellationToken cancellation_token, int retry = 1);
        void RemoveOrder(QLimitOrder order);
        void RemoveOrder(QStopOrder order);
        void RequestKillOrder(QOrder qOrder);
        void RequestMoveOrder(QLimitOrder qOrder, decimal new_price, long new_qty);
        void RequestPlaceOrder(QOrder qOrder);
    }
}