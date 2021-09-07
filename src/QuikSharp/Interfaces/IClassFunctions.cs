// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QUIKSharp
{
    /// <summary>
    /// Функции для обращения к спискам доступных параметров
    /// </summary>
    public interface IClassFunctions
    {
        /// <summary>
        /// Функция предназначена для получения списка кодов классов, переданных с сервера в ходе сеанса связи.
        /// </summary>
        /// <returns></returns>
        Task<string[]> GetClassesList();

        /// <summary>
        /// Функция предназначена для получения информации о классе.
        /// </summary>
        /// <param name="classID"></param>
        Task<ClassInfo> GetClassInfo(string classID);

        /// <summary>
        /// Функция предназначена для получения информации по бумаге.
        /// </summary>
        Task<SecurityInfo> GetSecurityInfo(ISecurity security);

        /// <summary>
        /// Функция предназначена для получения информации по бумаге.
        /// </summary>
        Task<SecurityInfo> GetSecurityInfo(string ClassCode, string SecCode);

        /// <summary>
        /// Функция предназначена для получения списка кодов бумаг для списка классов, заданного списком кодов.
        /// </summary>
        Task<string[]> GetClassSecurities(string classID);

        /// <summary>
        /// Функция предназначена для определения класса по коду инструмента из заданного списка классов.
        /// </summary>
        Task<string> GetSecurityClass(string classesList, string secCode);

        /// <summary>
        /// Функция возвращает код клиента.
        /// </summary>
        Task<string> GetClientCode();

        /// <summary>
        /// Функция возвращает список всех кодов клиента.
        /// </summary>
        Task<List<string>> GetClientCodes();

        /// <summary>
        /// Функция возвращает таблицу с описанием торгового счета для запрашиваемого кода класса.
        /// </summary>
        Task<string> GetTradeAccount(string classCode);

        /// <summary>
        /// Функция возвращает таблицу всех счетов в торговой системе.
        /// </summary>
        /// <returns></returns>
        Task<List<TradesAccounts>> GetTradeAccounts();

        Task<List<AccountPosition>> GetAccountPositions();

        Task<List<MoneyLimitEx>> GetMoneyLimits();

        Task<List<DepoLimits>> GetDepoLimits();

        /// <summary>
        /// Функция предназначена для получения параметров таблицы «Купить/Продать».
        /// Функция возвращает таблицу Lua с параметрами из таблицы QUIK «Купить/Продать», 
        /// означающими возможность купить либо продать указанный инструмент «sec_code» класса «class_code», 
        /// указанным клиентом «client_code» фирмы «firmid», по указанной цене «price». 
        /// Если цена равна «0», то используются лучшие значения спроса/предложения.
        /// </summary>
        /// <returns></returns>
        Task<List<BuySellInfoEx>> GetBuySellInfoEx(string firmId, string ClientCode, string ClassCode, string SecCode, decimal price);

        /// <summary>
        /// Функция для получения информации по фьючерсным лимитам
        /// </summary>
        /// <param name="firmId"></param>
        /// <param name="accId"></param>
        /// <param name="limitType"></param>
        /// <param name="currCode"></param>
        /// <returns></returns>
        Task<FuturesLimits> GetFuturesLimit(string firmId, string accId, int limitType, string currCode);

        /// <summary>
        ///  функция для получения информации по фьючерсным лимитам всех клиентских счетов
        /// </summary>
        Task<List<FuturesLimits>> GetFuturesClientLimits();

        /// <summary>
        /// Функция для получения информации по фьючерсной позициии
        /// </summary>
        /// <param name="firmId"></param>
        /// <param name="accId"></param>
        /// <param name="secCode"></param>
        /// <param name="posType"></param>
        /// <returns></returns>
        Task<FuturesClientHolding> GetFuturesHolding(string firmId, string accId, string secCode, int posType);

        /// <summary>
        /// Функция для получения информации по фьючерсным позициям
        /// </summary>
        Task<List<FuturesClientHolding>> GetFuturesHoldings();

        /// <summary>
        /// Функция для получения информации по фьючерсным позициям
        /// </summary>
        Task<List<FuturesClientHolding>> GetFuturesHoldingsNotZero();
    }
}