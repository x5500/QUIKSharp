// Copyright (c) 2014-2020 QUIKSharp Authors ht//tps://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using QUIKSharp.Functions;
using System;

namespace QUIKSharp
{
    public interface IQuik
    {
        /// <summary>
        /// Default timeout to use for send operations if no specific timeout supplied.
        /// </summary>
        TimeSpan DefaultSendTimeout { get; set; }
        
        /// <summary>
        /// Текущее системное время
        /// </summary>
        DateTime CurrentTimestamp { get;  }
        /// <summary>
        /// Установлено ли соединение с LUA сервисом QUIKSharp на клиенском рабочем месте Quik
        /// </summary>
        bool IsServiceConnected { get; }
        /// <summary>
        /// Функции для получения свечей
        /// </summary>
        ICandleFunctions Candles { get; }
        /// <summary>
        /// Функции для обращения к спискам доступных параметров
        /// </summary>
        IClassFunctions Class { get; }
        IDebugFunctions Debug { get; }
        /// <summary>
        /// Implements all Quik callback functions to be processed on .NET side.
        /// These functions are called by Quik inside QLUA.
        /// </summary>
        IQuikEvents Events { get; }
        /// <summary>
        /// Функции для работы со стаканом заявок (L2).
        /// </summary>
        IOrderBookFunctions OrderBook { get; }
        /// <summary>
        /// Функции для работы с заявками.
        /// </summary>
        IOrderFunctions Orders { get; }
        /// <summary>
        /// Service functions implementations
        /// </summary>
        IServiceFunctions Service { get; }
        /// <summary>
        /// Функции взаимодействия скрипта Lua и Рабочего места QUIK
        /// </summary>
        ITradingFunctions Trading { get; }
        /// <summary>
        /// Функции для отправки транзакций
        /// </summary>
        ITransactionsFunctions Transactions { get; }
    }
}