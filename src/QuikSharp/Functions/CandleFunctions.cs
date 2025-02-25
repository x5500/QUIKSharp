﻿// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.Transport;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    /// <summary>
    /// Функции для получения свечей
    /// </summary>
    public class CandleFunctions : FunctionsBase, ICandleFunctions
    {
        public CandleFunctions(IQuikService quikService) : base(quikService)
        {
        }

        /// <summary>
        /// Функция предназначена для получения количества свечей по тегу
        /// </summary>
        /// <param name="graphicTag">Строковый идентификатор графика или индикатора</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<long> GetNumCandles(string graphicTag, CancellationToken cancellationToken)
        {
            var message = new Message<string>(graphicTag, "get_num_candles");
            return QuikService.SendAsync<long>(message, cancellationToken);
        }

        /// <summary>
        /// Функция предназначена для получения информации о свечках по идентификатору (заказ данных для построения графика плагин не осуществляет, поэтому для успешного доступа нужный график должен быть открыт). Возвращаются все доступные свечки.
        /// </summary>
        /// <param name="graphicTag">Строковый идентификатор графика или индикатора</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<List<Candle>> GetAllCandles(string graphicTag, CancellationToken cancellationToken)
        {
            return GetCandles(graphicTag, 0, 0, 0, cancellationToken);
        }

        /// <summary>
        /// Функция предназначена для получения информации о свечках по идентификатору (заказ данных для построения графика плагин не осуществляет, поэтому для успешного доступа нужный график должен быть открыт).
        /// </summary>
        /// <param name="graphicTag">Строковый идентификатор графика или индикатора</param>
        /// <param name="line">Номер линии графика или индикатора. Первая линия имеет номер 0</param>
        /// <param name="first">Индекс первой свечки. Первая (самая левая) свечка имеет индекс 0</param>
        /// <param name="count">Количество запрашиваемых свечек</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<List<Candle>> GetCandles(string graphicTag, long line, long first, long count, CancellationToken cancellationToken)
        {
            var message = new MessageS(new string[] { graphicTag, line.ToString(), first.ToString(), count.ToString() }, "get_candles");
            return QuikService.SendAsync<List<Candle>>(message, cancellationToken);
        }

        /// <summary>
        /// Функция возвращает список свечек указанного инструмента заданного интервала.
        /// </summary>
        /// <param name="sec">Класс инструмента, Код инструмента.</param>
        /// <param name="interval">Интервал свечей.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Список свечей.</returns>
        public Task<List<Candle>> GetAllCandles(ISecurity sec, CandleInterval interval, CancellationToken cancellationToken)
        {
            //Параметр count == 0 говорт о том, что возвращаются все доступные свечи
            return GetLastCandles(sec, interval, 0, cancellationToken);
        }

        /// <summary>s
        /// Возвращает заданное количество свечек указанного инструмента и интервала с конца.
        /// </summary>
        /// <param name="sec">Класс инструмента, Код инструмента.</param>
        /// <param name="interval">Интервал свечей.</param>
        /// <param name="count">Количество возвращаемых свечей с конца.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Список свечей.</returns>
        public Task<List<Candle>> GetLastCandles(ISecurity sec, CandleInterval interval, long count, CancellationToken cancellationToken)
        {
            var message = new MessageS(new string[] { sec.ClassCode, sec.SecCode, ((int)interval).ToString(), count.ToString() }, "get_candles_from_data_source");
            return QuikService.SendAsync<List<Candle>>(message, cancellationToken);
        }

        /// <summary>
        /// Осуществляет подписку на получение исторических данных (свечи)
        /// </summary>
        /// <param name="sec">Класс инструмента, Код инструмента.</param>
        /// <param name="interval">интервал свечей (тайм-фрейм).</param>
        /// <param name="cancellationToken"></param>
        public Task Subscribe(ISecurity sec, CandleInterval interval, CancellationToken cancellationToken)
        {
            var message = new MessageS(new string[] { sec.ClassCode, sec.SecCode, ((int)interval).ToString() }, "subscribe_to_candles");
            var t = QuikService.SendAsync<string>(message, cancellationToken);
            return t;
        }

        /// <summary>
        /// Отписывается от получения исторических данных (свечей)
        /// </summary>
        /// <param name="sec">Класс инструмента, Код инструмента.</param>
        /// <param name="interval">интервал свечей (тайм-фрейм).</param>
        /// <param name="cancellationToken"></param>
        public Task Unsubscribe(ISecurity sec, CandleInterval interval, CancellationToken cancellationToken)
        {
            var message = new MessageS(new string[] { sec.ClassCode, sec.SecCode, ((int)interval).ToString() }, "unsubscribe_from_candles");
            var t = QuikService.SendAsync<string>(message, cancellationToken);
            return t;
        }

        /// <summary>
        /// Проверка состояния подписки на исторические данные (свечи)
        /// </summary>
        /// <param name="sec">Класс инструмента, Код инструмента.</param>
        /// <param name="interval">интервал свечей (тайм-фрейм).</param>
        /// <param name="cancellationToken"></param>
        public Task<bool> IsSubscribed(ISecurity sec, CandleInterval interval, CancellationToken cancellationToken)
        {
            var message = new MessageS(new string[] { sec.ClassCode, sec.SecCode, ((int)interval).ToString() }, "is_subscribed");
            return QuikService.SendAsync<bool>(message, cancellationToken);
        }
    }
}