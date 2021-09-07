// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp
{
    /// <summary>
    /// Сервисные функции
    /// </summary>
    public interface IServiceFunctions
    {
        /// <summary>
        /// Функция возвращает путь, по которому находится файл info.exe, исполняющий данный скрипт, без завершающего обратного слэша («\»). Например, C:\QuikFront.
        /// </summary>
        /// <returns></returns>
        Task<string> GetWorkingFolder();

        /// <summary>
        /// Функция предназначена для определения состояния подключения клиентского места к серверу. Возвращает «1», если клиентское место подключено и «0», если не подключено.
        /// </summary>
        /// <returns></returns>
        Task<bool> IsConnected(int timeout = Timeout.Infinite);

        /// <summary>
        /// Функция возвращает путь, по которому находится запускаемый скрипт, без завершающего обратного слэша («\»). Например, C:\QuikFront\Scripts
        /// </summary>
        /// <returns></returns>
        Task<string> GetScriptPath();

        /// <summary>
        /// Функция возвращает значения параметров информационного окна (пункт меню Связь / Информационное окно…).
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        Task<string> GetInfoParam(InfoParams param);

        /// <summary>
        /// Функция отображает сообщения в терминале QUIK.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="iconType"></param>
        /// <returns></returns>
        Task<string> Message(string message, NotificationType iconType);

        Task<string> PrintDbgStr(string message);

        /// <summary>
        /// Добавляет метку с заданными параметрами. Хотя бы один из параметров text или imagePath должен быть задан.
        /// </summary>
        /// <param name="chartTag">тег графика, к которому привязывается метка</param>
        /// <param name="label_params">Парметры метки</param>
        /// <returns>Возвращает Id метки</returns>
        Task<long> AddLabel(string chartTag, Label label_params);

        /// <summary>
        /// Функция задает параметры для метки с указанным идентификатором.
        /// </summary>
        /// <param name="chartTag">тег графика, к которому привязывается метка</param>
        /// <param name="labelId">идентификатор метки.</param>
        /// <param name="label_params">Таблица параметров метки</param>
        /// <returns></returns>
        Task<bool> SetLabelParams(string chartTag, long labelId, Label label_params);

        /// <summary>
        /// Функция возвращает таблицу с параметрами метки. В случае неуспешного завершения функция возвращает «nil».
        /// </summary>
        /// <param name="chartTag">тег графика, к которому привязывается метка</param>
        /// <param name="labelId">идентификатор метки.</param>
        /// <returns></returns>
        Task<Label> GetLabelParams(string chartTag, long labelId);

        /// <summary>
        /// Удаляет метку по ее Id
        /// </summary>
        /// <param name="chartTag">тег графика, к которому привязывается метка</param>
        /// <param name="labelId">Id метки</param>
        /// <returns></returns>
        Task<bool> DelLabel(string chartTag, long labelId);

        /// <summary>
        /// Удаляет все метки с графика
        /// </summary>
        /// <param name="chartTag">тег графика, к которому привязывается метка</param>
        /// <returns></returns>
        Task DelAllLabels(string chartTag);

        /// <summary>
        ///  Функция предназначена для оповещения скрипта о том, что клиент собирается отсоединяться
        /// </summary>
        /// <returns></returns>
        Task PrepareToDisconnect();

        /// <summary>
        ///  Функция предназначена для оповещения скрипта о том, что клиент собирается отсоединяться
        /// </summary>
        /// <returns></returns>
        Task<DateTime> GetTradeDate();

        /// <summary>
        /// Возвращает статистику сетевого соединения
        /// </summary>
        /// <param name="bytes_sent"></param>
        /// <param name="bytes_recieved"></param>
        /// <param name="bytes_callback"></param>
        /// <param name="requests_query_size">Количество отправленных запросов, ожидающих и еще не получивших ответа</param>
        void GetNetStats(out long bytes_sent, out long bytes_recieved, out long bytes_callback, out long requests_query_size);
    }
}