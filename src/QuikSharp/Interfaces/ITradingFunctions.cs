// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QUIKSharp
{
    /// <summary>
    /// Функции взаимодействия скрипта Lua и Рабочего места QUIK
    /// getDepo - функция для получения информации по бумажным лимитам
    /// getMoney - функция для получения информации по денежным лимитам
    /// getMoneyEx - функция для получения информации по денежным лимитам указанного типа
    /// paramRequest - Функция заказывает получение параметров Таблицы текущих торгов
    /// cancelParamRequest - Функция отменяет заказ на получение параметров Таблицы текущих торгов
    /// getParamEx - функция для получения значений Таблицы текущих значений параметров
    /// getParamEx2 - функция для получения всех значений Таблицы текущих значений параметров
    /// getTradeDate - функция для получения даты торговой сессии
    /// sendTransaction - функция для работы с заявками
    /// CulcBuySell - функция для расчета максимально возможного количества лотов в заявке
    /// getPortfolioInfo - функция для получения значений параметров таблицы «Клиентский портфель»
    /// getPortfolioInfoEx - функция для получения значений параметров таблицы «Клиентский портфель» с учетом вида лимита
    /// getBuySellInfo - функция для получения параметров таблицы «Купить/Продать»
    /// getBuySellInfoEx - функция для получения параметров (включая вид лимита) таблицы «Купить/Продать»
    /// getTrdAccByClientCode - Функция возвращает торговый счет срочного рынка, соответствующий коду клиента фондового рынка с единой денежной позицией
    /// getClientCodeByTrdAcc - Функция возвращает код клиента фондового рынка с единой денежной позицией, соответствующий торговому счету срочного рынка
    /// isUcpClient - Функция предназначена для получения признака, указывающего имеет ли клиент единую денежную позицию
    /// </summary>
    public interface ITradingFunctions
    {
        /// <summary>
        /// Функция для получения информации по бумажным лимитам
        /// </summary>
        Task<DepoLimit> GetDepo(ITrader trader, string secCode);

        /// <summary>
        /// Функция для получения информации по бумажным лимитам указанного типа
        /// </summary>
        Task<DepoLimitEx> GetDepoEx(ITrader trader, string secCode, int limitKind);

        /// <summary>
        /// Возвращает список записей из таблицы 'Лимиты по бумагам'.
        /// </summary>
        Task<List<DepoLimitEx>> GetDepoLimits();

        /// <summary>
        /// Возвращает список записей из таблицы 'Лимиты по бумагам', отфильтрованных по коду инструмента.
        /// </summary>
        /// <param name="secCode">Код инструментаю</param>
        /// <returns></returns>
        Task<List<DepoLimitEx>> GetDepoLimits(string secCode);

        /// <summary>
        /// Функция для получения информации по денежным лимитам
        /// </summary>
        ///
        Task<MoneyLimit> GetMoney(ITrader trader, string tag, string currCode);

        /// <summary>
        ///  функция для получения информации по денежным лимитам указанного типа
        /// </summary>
        Task<MoneyLimitEx> GetMoneyEx(ITrader trader, string tag, string currCode, int limitKind);

        /// <summary>
        ///  функция для получения информации по денежным лимитам всех торговых счетов (кроме фьючерсных) и валют
        ///  Лучшее место для получения связки clientCode + firmid
        /// </summary>
        Task<List<MoneyLimitEx>> GetMoneyLimits();

        /// <summary>
        /// Функция получения доски опционов
        /// </summary>
        /// <returns></returns>
        Task<List<OptionBoard>> GetOptionBoard(ISecurity security);

        /// <summary>
        /// Функция заказывает получение параметров Таблицы текущих торгов
        /// </summary>
        /// <returns></returns>
        Task<bool> ParamRequest(IStructClassSecParam secParam);

        /// <summary>
        /// Функция заказывает получение параметров Таблицы текущих торгов
        /// </summary>
        /// <param name="security"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        Task<bool> ParamRequest(ISecurity security, ParamNames paramName);

        /// <summary>
        /// Функция принимает список формате class_code|sec_code|param_name, вызывает функцию paramRequest для каждой строки.
        /// Возвращает список ответов в том же порядке
        /// </summary>
        /// <param name="classSecParams"></param>
        /// <returns></returns>
        Task<List<bool>> ParamRequestBulk(IEnumerable<IStructClassSecParam> classSecParams);

        /// <summary>
        /// Функция отменяет заказ на получение параметров Таблицы текущих торгов
        /// </summary>
        /// <returns></returns>
        Task<bool> CancelParamRequest(IStructClassSecParam secParam);

        /// <summary>
        /// Функция отменяет заказ на получение параметров Таблицы текущих торгов
        /// </summary>
        /// <param name="security"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        Task<bool> CancelParamRequest(ISecurity security, ParamNames paramName);

        /// <summary>
        /// Функция принимает список формате class_code|sec_code|param_name, вызывает функцию CancelParamRequest для каждой строки.
        /// Возвращает список ответов в том же порядке
        /// </summary>
        Task<List<bool>> CancelParamRequestBulk(IEnumerable<IStructClassSecParam> classSecParams);

        /// <summary>
        /// Функция для получения значений Таблицы текущих значений параметров
        /// </summary>
        /// <returns></returns>
        Task<ParamTable> GetParamEx(IStructClassSecParam secParam);

        Task<ParamTable> GetParamEx(ISecurity security, ParamNames paramName);

        /// <summary>
        /// Функция для получения всех значений Таблицы текущих значений параметров
        /// </summary>
        /// <returns></returns>
        Task<ParamTable> GetParamEx2(IStructClassSecParam secParam);

        Task<ParamTable> GetParamEx2(ISecurity security, ParamNames paramName);

        /// <summary>
        /// Функция принимает список в формате class_code|sec_code|param_name и возвращает результаты вызова
        /// функции getParamEx2 для каждой строки запроса в виде списка в таком же порядке, как в запросе
        /// </summary>
        Task<List<ParamTable>> GetParamEx2Bulk(IEnumerable<IStructClassSecParam> classSecParams);

        /// <summary>
        /// функция для получения таблицы сделок по заданному инструменту
        /// </summary>
        Task<List<Trade>> GetTrades();

        /// <summary>
        /// функция для получения таблицы обезличенных сделок
        /// </summary>
        Task<List<AllTrade>> GetAllTrades();

        /// <summary>
        /// функция для получения таблицы обезличенных сделок по заданному инструменту
        /// </summary>
        /// <returns></returns>
        Task<List<AllTrade>> GetAllTrades(ISecurity security);

        /// <summary>
        /// функция для получения таблицы сделок по заданному инструменту
        /// </summary>
        /// <returns></returns>
        Task<List<Trade>> GetTrades(ISecurity security);

        /// <summary>
        /// функция для получения таблицы сделок номеру заявки
        /// </summary>
        /// <param name="orderNum"></param>
        /// <returns></returns>
        Task<List<Trade>> GetTrades_by_OdrerNumber(long orderNum);

        ///// <summary>
        /////  функция для получения информации по инструменту
        ///// </summary>
        //Task<string> getSecurityInfo();
        ///// <summary>
        /////  функция для получения даты торговой сессии
        ///// </summary>
        //Task<string> getTradeDate();

        ///// <summary>
        /////  функция для расчета максимально возможного количества лотов в заявке
        ///// </summary>
        //Task<string> CulcBuySell();

        /// <summary>
        ///  функция для получения значений параметров таблицы «Клиентский портфель»
        /// </summary>
        Task<PortfolioInfo> GetPortfolioInfo(ITrader trader);

        /// <summary>
        ///  функция для получения значений параметров таблицы «Клиентский портфель» с учетом вида лимита
        ///  Для получения значений параметров таблицы «Клиентский портфель» для клиентов срочного рынка без единой денежной позиции
        ///  необходимо указать в качестве «clientCode» – торговый счет на срочном рынке, а в качестве «limitKind» – 0.
        /// </summary>
        Task<PortfolioInfoEx> GetPortfolioInfoEx(ITrader trader, int limitKind);

        ///// <summary>
        /////  функция для получения параметров таблицы «Купить/Продать»
        ///// </summary>
        //Task<string> getBuySellInfo();
        ///// <summary>
        /////  функция для получения параметров (включая вид лимита) таблицы «Купить/Продать»
        ///// </summary>
        //Task<string> getBuySellInfoEx();

        /// <summary>
        /// Функция возвращает торговый счет срочного рынка, соответствующий коду клиента фондового рынка с единой денежной позицией
        /// </summary>
        /// <returns></returns>
        Task<string> GetTrdAccByClientCode(ITrader trader);

        /// <summary>
        /// Функция возвращает код клиента фондового рынка с единой денежной позицией, соответствующий торговому счету срочного рынка
        /// </summary>
        /// <returns></returns>
        Task<string> GetClientCodeByTrdAcc(ITrader trader);

        /// <summary>
        /// Функция предназначена для получения признака, указывающего имеет ли клиент единую денежную позицию
        /// </summary>
        /// <returns></returns>
        Task<bool> IsUcpClient(ITrader trader);
    }
}