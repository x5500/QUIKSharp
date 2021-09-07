// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using NLog;
using QUIKSharp.Functions;
using QUIKSharp.Transport;
using System;
using System.Net;
using System.Reflection;

namespace QUIKSharp
{
    /// <summary>
    /// Quik interface in .NET
    /// </summary>
    public sealed class Quik : IDisposable, IQuik
    {
        //public static readonly Char separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private bool disposedValue;

        // Если запуск "сервиса" (потоков работы с Lua) происходит в конструкторе Quik, то возможности остановить "сервис" нет.
        // QuikService объявлен как private.
        private IQuikService quikService { get; set; }

        /// <summary>
        /// Default host is localhost
        /// </summary>
        public static readonly IPAddress DefaultHost = IPAddress.Loopback;

        /// <summary>
        /// Default port 34130, callback +1
        /// </summary>
        public const int DefaultPort = 34130;

        /// <summary>
        /// Quik current data is all in local time. This property allows to convert it to UTC datetime
        /// </summary>
        //public TimeZoneInfo TimeZoneInfo { get; set; }
        private DebugFunctions _Debug;

        private ServiceFunctions _Service;
        private ClassFunctions _Class;
        private OrderBookFunctions _OrderBook;
        private TradingFunctions _Trading;
        private OrderFunctions _Orders;
        private CandleFunctions _Candles;
        private TransactionsFunctions _Transactions;

        /// <summary>
        /// Quik interface in .NET constructor
        /// </summary>
        /// <param name="port">Порт сервиса QUIKSharp для приема/передачи запросов и ответов на них. Callback порт = Порт + 1</param>
        /// <param name="host">Хост сервиса QUIKSharp</param>
        /// <param name="defaultSendTimeout_sec">Таймаут ожидания ответа на запрос (по умолчанию)</param>
        /// <param name="identifyTransaction">Что использовать для идентификации транзакции, ответа на транзакции, ордеров, стоп-ордеров. По умолчанию: LuaIdProvider</param>
        public Quik(int port = DefaultPort, string host = null, double defaultSendTimeout_sec = 20.0, IIdentifyTransaction identifyTransaction = null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            logger.ConditionalDebug(string.Concat(assembly.FullName, " image runtime ver.: ", assembly.ImageRuntimeVersion));

            //quikService = new QuikService3(QuikTransportService.Create(port, host));
            var host_ip = (string.IsNullOrEmpty(host)) ? DefaultHost : IPAddress.Parse(host);
            quikService = QuikService.Create(host_ip, port, port+1);
            quikService.DefaultSendTimeout = TimeSpan.FromSeconds(defaultSendTimeout_sec);

            // poor man's DI
            _Debug = new DebugFunctions(quikService);
            _Service = new ServiceFunctions(quikService);
            _Class = new ClassFunctions(quikService);
            _OrderBook = new OrderBookFunctions(quikService);
            _Trading = new TradingFunctions(quikService);
            _Orders = new OrderFunctions(quikService);
            _Candles = new CandleFunctions(quikService);
            if (identifyTransaction == null)
                identifyTransaction = new LuaIdProvider(this);
            _Transactions = new TransactionsFunctions(quikService, identifyTransaction);
        }



        /// <summary>
        /// Установлено ли соединение с LUA сервисом QUIKSharp на клиенском рабочем месте Quik
        /// </summary>
        public bool IsServiceConnected => quikService.IsServiceConnected();

        /// <summary>
        /// Default timeout to use for send operations if no specific timeout supplied.
        /// </summary>
        public TimeSpan DefaultSendTimeout
        {
            get => quikService.DefaultSendTimeout;
            set => quikService.DefaultSendTimeout = value;
        }

        /// <summary>
        /// Функции обратного вызова
        /// </summary>
        public IQuikEvents Events => quikService.Events;

        /// <summary>
        /// Debug functions
        /// </summary>
        public IDebugFunctions Debug => _Debug;

        /// <summary>
        /// Сервисные функции
        /// </summary>
        public IServiceFunctions Service => _Service;

        /// <summary>
        /// Функции для обращения к спискам доступных параметров
        /// </summary>
        public IClassFunctions Class => _Class;

        /// <summary>
        /// Функции для работы со стаканом котировок
        /// </summary>
        public IOrderBookFunctions OrderBook => _OrderBook;

        /// <summary>
        /// Функции взаимодействия скрипта Lua и Рабочего места QUIK
        /// </summary>
        public ITradingFunctions Trading => _Trading;

        /// <summary>
        /// Функции для работы с заявками
        /// </summary>
        public IOrderFunctions Orders => _Orders;

        /// <summary>
        /// Функции для работы со свечами
        /// </summary>
        public ICandleFunctions Candles => _Candles;

        /// <summary>
        /// Функции для работы с транзакциями
        /// </summary>
        public ITransactionsFunctions Transactions => _Transactions;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты)
                    _Debug = null;
                    _Service = null;
                    _Class = null;
                    _OrderBook = null;
                    _Trading = null;
                    _Orders = null;
                    _Candles = null;
                    _Transactions = null;

                    if (quikService != null)
                    {
                        quikService.Stop();
                        quikService = null;
                    }
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // TODO: установить значение NULL для больших полей
                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}