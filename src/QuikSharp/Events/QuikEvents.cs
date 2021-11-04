// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Events;
using System;

namespace QUIKSharp
{
    public class QuikEvents : TryCatchWrapEvent, IQuikEvents
    {
        public QuikEvents() { }

        /// <summary>
        /// Событие вызывается когда библиотека QuikSharp успешно подключилась к Quik'у
        /// </summary>
        public event InitHandler OnConnectedToQuik;

        /// <summary>
        /// Функция вызывается терминалом QUIK перед вызовом функции main().
        /// В качестве параметра принимает значение полного пути к запускаемому скрипту.
        /// </summary>
        public event InitHandler OnInit;

        internal void OnConnectedToQuikCall(int port)
        {
            RunTheEvent(OnConnectedToQuik, port);
            RunTheEvent(OnInit, port);
        }

        /// <summary>
        /// Событие вызывается когда библиотека QuikSharp была отключена от Quik'а
        /// </summary>
        public event VoidHandler OnDisconnectedFromQuik;
        internal void OnDisconnectedFromQuikCall() => RunTheEvent(OnDisconnectedFromQuik);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений текущей позиции по счету.
        /// </summary>
        public event AccountBalanceHandler OnAccountBalance;

        internal void OnAccountBalanceCall(AccountBalance accBal) => RunTheEvent(OnAccountBalance, accBal);

        /// <summary>
        /// Функция вызывается терминалом QUIK при изменении денежной позиции по счету.
        /// </summary>
        public event AccountPositionHandler OnAccountPosition;

        internal void OnAccountPositionCall(AccountPosition accPos) => RunTheEvent(OnAccountPosition, accPos);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении обезличенной сделки.
        /// </summary>
        public event AllTradeHandler OnAllTrade;

        internal void OnAllTradeCall(AllTrade allTrade) => RunTheEvent(OnAllTrade, allTrade);

        /// <summary>
        /// Функция вызывается терминалом QUIK при смене сессии и при выгрузке файла qlua.dll
        /// </summary>
        public event VoidHandler OnCleanUp;

        internal void OnCleanUpCall() => RunTheEvent(OnCleanUp);

        /// <summary>
        /// Функция вызывается перед закрытием терминала QUIK.
        /// </summary>
        public event VoidHandler OnClose;

        internal void OnCloseCall() => RunTheEvent(OnClose);

        /// <summary>
        /// Функция вызывается терминалом QUIK при установлении связи с сервером QUIK.
        /// </summary>
        public event VoidHandler OnConnected;

        internal void OnConnectedCall() => RunTheEvent(OnConnected);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений лимита по бумагам.
        /// </summary>
        public event DepoLimitHandler OnDepoLimit;

        internal void OnDepoLimitCall(DepoLimitEx dLimit) => RunTheEvent(OnDepoLimit, dLimit);

        /// <summary>
        /// Функция вызывается терминалом QUIK при удалении клиентского лимита по бумагам.
        /// </summary>
        public event DepoLimitDeleteHandler OnDepoLimitDelete;

        internal void OnDepoLimitDeleteCall(DepoLimitDelete dLimitDel) => RunTheEvent(OnDepoLimitDelete, dLimitDel);

        /// <summary>
        /// Функция вызывается терминалом QUIK при отключении от сервера QUIK.
        /// </summary>
        public event VoidHandler OnDisconnected;

        internal void OnDisconnectedCall() => RunTheEvent(OnDisconnected);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении описания новой фирмы от сервера.
        /// </summary>
        public event FirmHandler OnFirm;

        internal void OnFirmCall(Firm frm) => RunTheEvent(OnFirm, frm);

        /// <summary>
        /// Функция вызывается терминалом QUIK при изменении позиции по срочному рынку.
        /// </summary>
        public event FuturesClientHoldingHandler OnFuturesClientHolding;

        internal void OnFuturesClientHoldingCall(FuturesClientHolding futPos) => RunTheEvent(OnFuturesClientHolding, futPos);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений ограничений по срочному рынку.
        /// </summary>
        public event FuturesLimitHandler OnFuturesLimitChange;

        internal void OnFuturesLimitChangeCall(FuturesLimits futLimit) => RunTheEvent(OnFuturesLimitChange, futLimit);

        /// <summary>
        /// Функция вызывается терминалом QUIK при удалении лимита по срочному рынку.
        /// </summary>
        public event FuturesLimitDeleteHandler OnFuturesLimitDelete;

        internal void OnFuturesLimitDeleteCall(FuturesLimitDelete limDel) => RunTheEvent(OnFuturesLimitDelete, limDel);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений по денежному лимиту клиента.
        /// </summary>
        public event MoneyLimitHandler OnMoneyLimit;
        internal void OnMoneyLimitCall(MoneyLimitEx mLimit) => RunTheEvent(OnMoneyLimit, mLimit);

        /// <summary>
        /// Функция вызывается терминалом QUIK при удалении денежного лимита.
        /// </summary>
        public event MoneyLimitDeleteHandler OnMoneyLimitDelete;
        internal void OnMoneyLimitDeleteCall(MoneyLimitDelete mLimitDel) => RunTheEvent(OnMoneyLimitDelete, mLimitDel);

        public event EventHandler OnNegDeal;
        internal void OnNegDealCall(object sender, EventArgs e) => RunTheEvent(OnNegDeal, sender, e);

        public event EventHandler OnNegTrade;
        internal void OnNegTradeCall(object sender, EventArgs e) => RunTheEvent(OnNegTrade, sender, e);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении заявки или изменении параметров существующей заявки.
        /// </summary>
        public event OrderHandler OnOrder;
        internal void OnOrderCall(Order order) => RunTheEvent(OnOrder, order);

        /// <summary>
        /// Функция вызывается терминалом QUIK при при изменении текущих параметров.
        /// </summary>
        public event ParamHandler OnParam;
        internal void OnParamCall(Param par) => RunTheEvent(OnParam, par);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменения стакана котировок.
        /// </summary>
        public event QuoteHandler OnQuote;
        internal void OnQuoteCall(OrderBook orderBook) => RunTheEvent(OnQuote, orderBook);

        /// <summary>
        /// Функция вызывается терминалом QUIK при остановке скрипта из диалога управления и при закрытии терминала QUIK.
        /// </summary>
        public event StopHandler OnStop;
        internal void OnStopCall(int signal) => RunTheEvent(OnStop, signal);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении новой стоп-заявки или при изменении параметров существующей стоп-заявки.
        /// </summary>
        public event StopOrderHandler OnStopOrder;
        internal void OnStopOrderCall(StopOrder stopOrder) => RunTheEvent(OnStopOrder, stopOrder);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении сделки.
        /// </summary>
        public event TradeHandler OnTrade;
        internal void OnTradeCall(Trade trade) => RunTheEvent(OnTrade, trade);

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении ответа на транзакцию пользователя.
        /// </summary>
        public event TransReplyHandler OnTransReply;
        internal void OnTransReplyCall(TransactionReply reply) => RunTheEvent(OnTransReply, reply);

        /// <summary>
        /// Событие получения новой свечи. Для срабатывания необходимо подписаться с помощью метода Subscribe.
        /// </summary>
        public event CandleHandler OnNewCandle;
        internal void OnNewCandleCall(Candle candle) => RunTheEvent(OnNewCandle, candle);

    }
}