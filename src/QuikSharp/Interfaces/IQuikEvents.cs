// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System;

namespace QUIKSharp
{
    /// <summary>
    /// Implements all Quik callback functions to be processed on .NET side.
    /// These functions are called by Quik inside QLUA.
    ///
    /// Функции обратного вызова
    /// Функции вызываются при получении следующих данных или событий терминалом QUIK от сервера:
    /// main - реализация основного потока исполнения в скрипте
    /// OnAccountBalance - изменение позиции по счету
    /// OnAccountPosition - изменение позиции по счету
    /// OnAllTrade - новая обезличенная сделка
    /// OnCleanUp - смена торговой сессии и при выгрузке файла qlua.dll
    /// OnClose - закрытие терминала QUIK
    /// OnConnected - установление связи с сервером QUIK
    /// OnDepoLimit - изменение бумажного лимита
    /// OnDepoLimitDelete - удаление бумажного лимита
    /// OnDisconnected - отключение от сервера QUIK
    /// OnFirm - описание новой фирмы
    /// OnFuturesClientHolding - изменение позиции по срочному рынку
    /// OnFuturesLimitChange - изменение ограничений по срочному рынку
    /// OnFuturesLimitDelete - удаление лимита по срочному рынку
    /// OnInit - инициализация функции main
    /// OnMoneyLimit - изменение денежного лимита
    /// OnMoneyLimitDelete - удаление денежного лимита
    /// OnNegDeal - новая заявка на внебиржевую сделку
    /// OnNegTrade - новая сделка для исполнения
    /// OnOrder - новая заявка или изменение параметров существующей заявки
    /// OnParam - изменение текущих параметров
    /// OnQuote - изменение стакана котировок
    /// OnStop - остановка скрипта из диалога управления
    /// OnStopOrder - новая стоп-заявка или изменение параметров существующей стоп-заявки
    /// OnTrade - новая сделка
    /// OnTransReply - ответ на транзакцию
    /// </summary>

    /// <summary>
    /// A handler for events without arguments
    /// </summary>
    public delegate void VoidHandler();

    /// <summary>
    /// Обработчик события OnInit
    /// </summary>
    /// <param name="port">Порт обмена данными</param>
    public delegate void InitHandler(int port);

    /// <summary>
    ///
    /// </summary>
    /// <param name="orderbook"></param>
    public delegate void QuoteHandler(OrderBook orderbook);

    /// <summary>
    /// Обработчик события OnStop
    /// </summary>
    public delegate void StopHandler(int signal);

    /// <summary>
    /// Обработчик события OnAllTrade
    /// </summary>
    /// <param name="allTrade"></param>
    public delegate void AllTradeHandler(AllTrade allTrade);

    /// <summary>
    ///
    /// </summary>
    /// <param name="transReply"></param>
    public delegate void TransReplyHandler(TransactionReply transReply);

    /// <summary>
    /// Обработчик события OnOrder
    /// </summary>
    /// <param name="order"></param>
    public delegate void OrderHandler(Order order);

    /// <summary>
    /// Обработчик события OnTrade
    /// </summary>
    /// <param name="trade"></param>
    public delegate void TradeHandler(Trade trade);

    /// <summary>
    /// Обработчик события OnParam
    /// </summary>
    /// <param name="par">lua table with class_code, sec_code</param>
    public delegate void ParamHandler(Param par);

    /// <summary>
    /// Обработчик события OnStopOrder
    /// </summary>
    /// <param name="stopOrder"></param>
    public delegate void StopOrderHandler(StopOrder stopOrder);

    /// <summary>
    /// Обработчик события OnAccountBalance
    /// </summary>
    /// <param name="accBal"></param>
    public delegate void AccountBalanceHandler(AccountBalance accBal);

    /// <summary>
    /// Обработчик события OnAccountPosition
    /// </summary>
    /// <param name="accPos"></param>
    public delegate void AccountPositionHandler(AccountPosition accPos);

    /// <summary>
    /// Обработчик события OnDepoLimit
    /// </summary>
    /// <param name="dLimit"></param>
    public delegate void DepoLimitHandler(DepoLimitEx dLimit);

    /// <summary>
    /// Обработчик события OnDepoLimitDelete
    /// </summary>
    /// <param name="dLimitDel"></param>
    public delegate void DepoLimitDeleteHandler(DepoLimitDelete dLimitDel);

    /// <summary>
    /// Обработчик события OnFirm
    /// </summary>
    /// <param name="frm"></param>
    public delegate void FirmHandler(Firm frm);

    /// <summary>
    /// Обработчик события OnFuturesClientHolding
    /// </summary>
    /// <param name="futPos"></param>
    public delegate void FuturesClientHoldingHandler(FuturesClientHolding futPos);

    /// <summary>
    /// Обработчик события OnFuturesLimitChange
    /// </summary>
    /// <param name="futLimit"></param>
    public delegate void FuturesLimitHandler(FuturesLimits futLimit);

    /// <summary>
    /// Обработчик события OnFuturesLimitDelete
    /// </summary>
    /// <param name="limDel"></param>
    public delegate void FuturesLimitDeleteHandler(FuturesLimitDelete limDel);

    /// <summary>
    /// Обработчик события OnMoneyLimit
    /// </summary>
    /// <param name="mLimit"></param>
    public delegate void MoneyLimitHandler(MoneyLimitEx mLimit);

    /// <summary>
    /// Обработчик события OnMoneyLimitDelete
    /// </summary>
    /// <param name="mLimitDel"></param>
    public delegate void MoneyLimitDeleteHandler(MoneyLimitDelete mLimitDel);

    /// <summary>
    /// Обработчик события получения новой свечи.
    /// </summary>
    public delegate void CandleHandler(Candle candle);

    public interface IQuikEvents
    {
        /// <summary>
        /// Событие вызывается когда библиотека QuikSharp успешно подключилась к Quik'у
        /// </summary>
        event InitHandler OnConnectedToQuik;

        /// <summary>
        /// Событие вызывается когда библиотека QuikSharp была отключена от Quik'а
        /// </summary>
        event VoidHandler OnDisconnectedFromQuik;

        /// <summary>
        /// Событие вызывается при получении изменений текущей позиции по счету.
        /// </summary>
        event AccountBalanceHandler OnAccountBalance;

        /// <summary>
        /// Событие вызывается при изменении денежной позиции по счету.
        /// </summary>
        event AccountPositionHandler OnAccountPosition;

        /// <summary>
        /// Новая обезличенная сделка
        /// </summary>
        event AllTradeHandler OnAllTrade;

        /// <summary>
        /// Функция вызывается терминалом QUIK при смене сессии и при выгрузке файла qlua.dll
        /// </summary>
        event VoidHandler OnCleanUp;

        /// <summary>
        /// Функция вызывается перед закрытием терминала QUIK.
        /// </summary>
        event VoidHandler OnClose;

        /// <summary>
        /// Функция вызывается терминалом QUIK при установлении связи с сервером QUIK.
        /// </summary>
        event VoidHandler OnConnected;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений лимита по бумагам.
        /// </summary>
        event DepoLimitHandler OnDepoLimit;

        /// <summary>
        /// Функция вызывается терминалом QUIK при удалении клиентского лимита по бумагам.
        /// </summary>
        event DepoLimitDeleteHandler OnDepoLimitDelete;

        /// <summary>
        /// Функция вызывается терминалом QUIK при отключении от сервера QUIK.
        /// </summary>
        event VoidHandler OnDisconnected;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении описания новой фирмы от сервера.
        /// </summary>
        event FirmHandler OnFirm;

        /// <summary>
        /// Функция вызывается терминалом QUIK при изменении позиции по срочному рынку.
        /// </summary>
        event FuturesClientHoldingHandler OnFuturesClientHolding;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений ограничений по срочному рынку.
        /// </summary>
        event FuturesLimitHandler OnFuturesLimitChange;

        /// <summary>
        /// Функция вызывается терминалом QUIK при удалении лимита по срочному рынку.
        /// </summary>
        event FuturesLimitDeleteHandler OnFuturesLimitDelete;

        /// <summary>
        /// Depricated
        /// </summary>
        event InitHandler OnInit;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменений по денежному лимиту клиента.
        /// </summary>
        event MoneyLimitHandler OnMoneyLimit;

        /// <summary>
        /// Функция вызывается терминалом QUIK при удалении денежного лимита.
        /// </summary>
        event MoneyLimitDeleteHandler OnMoneyLimitDelete;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении внебиржевой заявки.
        /// </summary>
        event EventHandler OnNegDeal;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении сделки для исполнения.
        /// </summary>
        event EventHandler OnNegTrade;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении новой заявки или при изменении параметров существующей заявки.
        /// </summary>
        event OrderHandler OnOrder;

        /// <summary>
        /// Функция вызывается терминалом QUIK при при изменении текущих параметров.
        /// </summary>
        event ParamHandler OnParam;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении изменения стакана котировок.
        /// </summary>
        event QuoteHandler OnQuote;

        /// <summary>
        /// Функция вызывается терминалом QUIK при остановке скрипта из диалога управления.
        /// Примечание: Значение параметра «stop_flag» – «1».После окончания выполнения функции таймаут завершения работы скрипта 5 секунд. По истечении этого интервала функция main() завершается принудительно. При этом возможна потеря системных ресурсов.
        /// </summary>
        event StopHandler OnStop;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении новой стоп-заявки или при изменении параметров существующей стоп-заявки.
        /// </summary>
        event StopOrderHandler OnStopOrder;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении сделки.
        /// </summary>
        event TradeHandler OnTrade;

        /// <summary>
        /// Функция вызывается терминалом QUIK при получении ответа на транзакцию пользователя.
        /// </summary>
        event TransReplyHandler OnTransReply;

        /// <summary>
        /// Событие получения новой свечи. Для срабатывания необходимо подписаться с помощью метода Subscribe.
        /// </summary>
        event CandleHandler OnNewCandle;
    }
}