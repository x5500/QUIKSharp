// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System;

namespace QUIKSharp.TestQuik
{
    public class TestEvents : IQuikEvents
    {
        public TestEvents() {}
        /// <summary>
        /// Событие вызывается когда библиотека QuikSharp успешно подключилась к Quik'у
        /// </summary>
        public event InitHandler OnConnectedToQuik;

        public void OnConnectedToQuikCall(int port)
        {
            OnConnectedToQuik?.Invoke(port);
            OnInit?.Invoke(port);
        }

        /// <summary>
        /// Событие вызывается когда библиотека QuikSharp была отключена от Quik'а
        /// </summary>
        public event VoidHandler OnDisconnectedFromQuik;

        public void OnDisconnectedFromQuikCall()
        {
            OnDisconnectedFromQuik?.Invoke();
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK перед вызовом функции main().
        /// В качестве параметра принимает значение полного пути к запускаемому скрипту.
        /// </summary>
        public event InitHandler OnInit;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений текущей позиции по счету.
        /// </summary>
        public event AccountBalanceHandler OnAccountBalance;

        public void OnAccountBalanceCall(AccountBalance accBal)
        {
            OnAccountBalance?.Invoke(accBal);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при изменении денежной позиции по счету.
        /// </summary>
        public event AccountPositionHandler OnAccountPosition;

        public void OnAccountPositionCall(AccountPosition accPos)
        {
            OnAccountPosition?.Invoke(accPos);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении обезличенной сделки.
        /// </summary>
        public event AllTradeHandler OnAllTrade;

        public void OnAllTradeCall(AllTrade allTrade) => OnAllTrade?.Invoke(allTrade);

        /// <summary>
        /// Функция вызывается терминалом QUIK при смене сессии и при выгрузке файла qlua.dll
        /// </summary>
        public event VoidHandler OnCleanUp;

        public void OnCleanUpCall()
        {
            OnCleanUp?.Invoke();
        }

        /// <summary>
        /// Функция вызывается перед закрытием терминала QUIK.
        /// </summary>
        public event VoidHandler OnClose;

        public void OnCloseCall()
        {
            OnClose?.Invoke();
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при установлении связи с сервером QUIK.
        /// </summary>
        public event VoidHandler OnConnected;

        public void OnConnectedCall()
        {
            OnConnected?.Invoke();
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений лимита по бумагам.
        /// </summary>
        public event DepoLimitHandler OnDepoLimit;

        public void OnDepoLimitCall(DepoLimitEx dLimit)
        {
            OnDepoLimit?.Invoke(dLimit);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при удалении клиентского лимита по бумагам.
        /// </summary>
        public event DepoLimitDeleteHandler OnDepoLimitDelete;

        public void OnDepoLimitDeleteCall(DepoLimitDelete dLimitDel)
        {
            OnDepoLimitDelete?.Invoke(dLimitDel);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при отключении от сервера QUIK.
        /// </summary>
        public event VoidHandler OnDisconnected;

        public void OnDisconnectedCall()
        {
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении описания новой фирмы от сервера.
        /// </summary>
        public event FirmHandler OnFirm;

        public void OnFirmCall(Firm frm)
        {
            OnFirm?.Invoke(frm);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при изменении позиции по срочному рынку.
        /// </summary>
        public event FuturesClientHoldingHandler OnFuturesClientHolding;

        public void OnFuturesClientHoldingCall(FuturesClientHolding futPos)
        {
            OnFuturesClientHolding?.Invoke(futPos);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений ограничений по срочному рынку.
        /// </summary>
        public event FuturesLimitHandler OnFuturesLimitChange;

        public void OnFuturesLimitChangeCall(FuturesLimits futLimit)
        {
            OnFuturesLimitChange?.Invoke(futLimit);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при удалении лимита по срочному рынку.
        /// </summary>
        public event FuturesLimitDeleteHandler OnFuturesLimitDelete;

        public void OnFuturesLimitDeleteCall(FuturesLimitDelete limDel)
        {
            OnFuturesLimitDelete?.Invoke(limDel);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений по денежному лимиту клиента.
        /// </summary>
        public event MoneyLimitHandler OnMoneyLimit;

        public void OnMoneyLimitCall(MoneyLimitEx mLimit)
        {
            OnMoneyLimit?.Invoke(mLimit);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при удалении денежного лимита.
        /// </summary>
        public event MoneyLimitDeleteHandler OnMoneyLimitDelete;

        public void OnMoneyLimitDeleteCall(MoneyLimitDelete mLimitDel)
        {
            OnMoneyLimitDelete?.Invoke(mLimitDel);
        }

        public event EventHandler OnNegDeal;

        public void OnNegDealCall(object sender, EventArgs e)
        {
            OnNegDeal?.Invoke(sender, e);
        }

        public event EventHandler OnNegTrade;

        public void OnNegTradeCall(object sender, EventArgs e)
        {
            OnNegTrade?.Invoke(sender, e);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении заявки или изменении параметров существующей заявки.
        /// </summary>
        public event OrderHandler OnOrder;

        public void OnOrderCall(Order order)
        {
            OnOrder?.Invoke(order);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при при изменении текущих параметров.
        /// </summary>
        public event ParamHandler OnParam;

        public void OnParamCall(Param par)
        {
            OnParam?.Invoke(par);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменения стакана котировок.
        /// </summary>
        public event QuoteHandler OnQuote;

        public void OnQuoteCall(OrderBook orderBook)
        {
            OnQuote?.Invoke(orderBook);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при остановке скрипта из диалога управления и при закрытии терминала QUIK.
        /// </summary>
        public event StopHandler OnStop;

        public void OnStopCall(int signal)
        {
            OnStop?.Invoke(signal);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении новой стоп-заявки или при изменении параметров существующей стоп-заявки.
        /// </summary>
        public event StopOrderHandler OnStopOrder;

        public void OnStopOrderCall(StopOrder stopOrder)
        {
            OnStopOrder?.Invoke(stopOrder);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении сделки.
        /// </summary>
        public event TradeHandler OnTrade;

        public void OnTradeCall(Trade trade)
        {
            OnTrade?.Invoke(trade);
        }

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении ответа на транзакцию пользователя.
        /// </summary>
        public event TransReplyHandler OnTransReply;

        public void OnTransReplyCall(TransactionReply reply)
        {
            OnTransReply?.Invoke(reply);
        }

        /// <summary>
        /// Событие получения новой свечи. Для срабатывания необходимо подписаться с помощью метода Subscribe.
        /// </summary>
        public event CandleHandler OnNewCandle;

        public void OnNewCandleEvent(Candle candle)
        {
            OnNewCandle?.Invoke(candle);
        }
    }
}