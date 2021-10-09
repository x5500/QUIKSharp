// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using System.Collections.Generic;
using System.Threading;
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
        Task<string[]> GetClassesList(CancellationToken cancellationToken);

        /// <summary>
        /// Функция предназначена для получения информации о классе.
        /// </summary>
        /// <param name="classID"></param>
        /// <param name="cancellationToken"></param>
        Task<ClassInfo> GetClassInfo(string classID, CancellationToken cancellationToken);

        /// <summary>
        /// Функция предназначена для получения информации по бумаге.
        /// </summary>
        Task<SecurityInfo> GetSecurityInfo(ISecurity security, CancellationToken cancellationToken);

        /// <summary>
        /// Функция предназначена для получения информации по бумаге.
        /// </summary>
        Task<SecurityInfo> GetSecurityInfo(string ClassCode, string SecCode, CancellationToken cancellationToken);

        /// <summary>
        /// Функция предназначена для получения списка кодов бумаг для списка классов, заданного списком кодов.
        /// </summary>
        Task<string[]> GetClassSecurities(string classID, CancellationToken cancellationToken);

        /// <summary>
        /// Функция предназначена для определения класса по коду инструмента из заданного списка классов.
        /// </summary>
        Task<string> GetSecurityClass(string classesList, string secCode, CancellationToken cancellationToken);

        /// <summary>
        /// Функция возвращает код клиента.
        /// </summary>
        Task<string> GetClientCode(CancellationToken cancellationToken);

        /// <summary>
        /// Функция возвращает список всех кодов клиента.
        /// </summary>
        Task<List<string>> GetClientCodes(CancellationToken cancellationToken);

        /// <summary>
        /// Функция возвращает таблицу с описанием торгового счета для запрашиваемого кода класса.
        /// </summary>
        Task<string> GetTradeAccount(string classCode, CancellationToken cancellationToken);

        /// <summary>
        /// Функция возвращает таблицу всех счетов в торговой системе.
        /// </summary>
        /// <returns></returns>
        Task<List<TradesAccounts>> GetTradeAccounts(CancellationToken cancellationToken);

        Task<List<AccountPosition>> GetAccountPositions(CancellationToken cancellationToken);

        Task<List<MoneyLimitEx>> GetMoneyLimits(CancellationToken cancellationToken);

        Task<List<DepoLimits>> GetDepoLimits(CancellationToken cancellationToken);

        /// <summary>
        /// Функция предназначена для получения параметров таблицы «Купить/Продать».
        /// Функция возвращает таблицу Lua с параметрами из таблицы QUIK «Купить/Продать», 
        /// означающими возможность купить либо продать указанный инструмент «sec_code» класса «class_code», 
        /// указанным клиентом «client_code» фирмы «firmid», по указанной цене «price». 
        /// Если цена равна «0», то используются лучшие значения спроса/предложения.
        /// </summary>
        /// <returns></returns>
        Task<List<BuySellInfoEx>> GetBuySellInfoEx(ITradeSecurity tradeSec, decimal price, CancellationToken cancellationToken);

        /// <summary>
        ///  функция для расчета максимально возможного количества лотов в заявке
        ///  При заданном параметре is_market=true, необходимо передать параметр price=0, иначе будет рассчитано максимально возможное количество лотов в заявке по цене price.
        /// </summary>
        Task<CalcBuySellResult> CalcBuySell(ITradeSecurity tradeSec, decimal price, bool isBuy, bool isMarket, CancellationToken cancellationToken);

        /// <summary>
        /// Функция для получения информации по фьючерсным лимитам
        /// </summary>
        /// <param name="trader"></param>
        /// <param name="limitType"></param>
        /// <param name="currCode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<FuturesLimits> GetFuturesLimit(ITrader trader, int limitType, string currCode, CancellationToken cancellationToken);
        /// <summary>
        ///  функция для получения информации по фьючерсным лимитам всех клиентских счетов
        /// </summary>
        Task<List<FuturesLimits>> GetFuturesClientLimits(CancellationToken cancellationToken);

        /// <summary>
        /// Функция для получения информации по фьючерсной позициии
        /// </summary>
        /// <param name="tradeSec"></param>
        /// <param name="posType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<FuturesClientHolding> GetFuturesHolding(ITradeSecurity tradeSec, int posType, CancellationToken cancellationToken);

        /// <summary>
        /// Функция для получения информации по фьючерсным позициям
        /// </summary>
        Task<List<FuturesClientHolding>> GetFuturesHoldings(CancellationToken cancellationToken);

        /// <summary>
        /// Функция для получения информации по фьючерсным позициям
        /// </summary>
        Task<List<FuturesClientHolding>> GetFuturesHoldingsNotZero(CancellationToken cancellationToken);
    }
}