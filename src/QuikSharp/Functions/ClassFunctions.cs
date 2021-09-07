// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.Transport;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    /// <summary>
    /// Функции для обращения к спискам доступных параметров
    /// </summary>
    public class ClassFunctions : FunctionsBase, IClassFunctions
    {
        internal ClassFunctions(IQuikService quikService) : base(quikService)
        {
        }

        public async Task<string[]> GetClassesList()
        {
            var response = await QuikService.SendAsync<string>(new Message<string>("", "getClassesList")).ConfigureAwait(false);
            return response == null ? Array.Empty<string>(): response.TrimEnd(',').Split(new[] { "," }, StringSplitOptions.None);
        }

        public Task<ClassInfo> GetClassInfo(string classID) => QuikService.SendAsync<ClassInfo>(new Message<string>(classID, "getClassInfo"));

        public Task<SecurityInfo> GetSecurityInfo(string classCode, string secCode) => QuikService.SendAsync<SecurityInfo>(new Message<string>(classCode + "|" + secCode, "getSecurityInfo"));

        public Task<SecurityInfo> GetSecurityInfo(ISecurity security) => GetSecurityInfo(security.ClassCode, security.SecCode);

        public async Task<string[]> GetClassSecurities(string classID)
        {
            var response = await QuikService.SendAsync<string>(new Message<string>(classID, "getClassSecurities")).ConfigureAwait(false);
            return response == null ? Array.Empty<string>() : response.TrimEnd(',').Split(new[] { "," }, StringSplitOptions.None);
        }

        public Task<string> GetSecurityClass(string classesList, string secCode) => QuikService.SendAsync<string>(new Message<string>(classesList + "|" + secCode, "getSecurityClass"));

        public Task<string> GetClientCode() => QuikService.SendAsync<string>(new Message<string>("", "getClientCode"));

        public Task<List<string>> GetClientCodes() => QuikService.SendAsync<List<string>>(new Message<string>("", "getClientCodes"));

        public Task<string> GetTradeAccount(string classCode)
        => QuikService.SendAsync<string>(new Message<string>(classCode, "getTradeAccount"));

        public Task<List<TradesAccounts>> GetTradeAccounts()
        => QuikService.SendAsync<List<TradesAccounts>>(new Message<string>("", "getTradeAccounts"));

        public Task<List<AccountPosition>> GetAccountPositions()
        => QuikService.SendAsync<List<AccountPosition>>(new Message<string>("account_positions", "get_table"));

        public Task<List<MoneyLimitEx>> GetMoneyLimits()
        => QuikService.SendAsync<List<MoneyLimitEx>>(new Message<string>("money_limits", "get_table"));

        public Task<List<DepoLimits>> GetDepoLimits()
        => QuikService.SendAsync<List<DepoLimits>>(new Message<string>("depo_limits", "get_table"));

        /// <summary>
        /// Функция для получения информации по фьючерсным лимитам
        /// </summary>
        public Task<FuturesLimits> GetFuturesLimit(string firmId, string accId, int limitType, string currCode)
        => QuikService.SendAsync<FuturesLimits>(new Message<string>(firmId + "|" + accId + "|" + limitType + "|" + currCode, "getFuturesLimit"));

        /// <summary>
        ///  функция для получения информации по фьючерсным лимитам всех клиентских счетов
        /// </summary>
        public Task<List<FuturesLimits>> GetFuturesClientLimits() => QuikService.SendAsync<List<FuturesLimits>>(
                new Message<string>("", "getFuturesClientLimits"));

        /// <summary>
        /// getFuturesHolding - функция для получения информации по фьючерсной позиции
        /// </summary>
        public Task<FuturesClientHolding> GetFuturesHolding(string firmId, string accId, string secCode, int posType) => QuikService.SendAsync<FuturesClientHolding>(
                new Message<string>(firmId + "|" + accId + "|" + secCode + "|" + posType.ToString(), "getFuturesHolding"));

        /// <summary>
        /// getFuturesHoldings - функция для получения информации по фьючерсным позициям
        /// </summary>
        public  Task<List<FuturesClientHolding>> GetFuturesHoldings()
        => QuikService.SendAsync<List<FuturesClientHolding>>(new Message<string>("", "getFuturesHoldings"));

        /// <summary>
        /// getFuturesHoldings - функция для получения информации по фьючерсным позициям
        /// </summary>
        public Task<List<FuturesClientHolding>> GetFuturesHoldingsNotZero()
        => QuikService.SendAsync<List<FuturesClientHolding>>(new Message<string>("", "getFuturesHoldingsNotZero"));

        /// <summary>
        /// getFuturesHolding - функция для получения информации по фьючерсной позиции
        /// TABLE getBuySellInfoEx(STRING firm_id, STRING client_code, STRING class_code, STRING sec_code, NUMBER price)
        /// </summary>
        public Task<List<BuySellInfoEx>> GetBuySellInfoEx(string firmId, string ClientCode, string ClassCode, string SecCode, decimal price)
        => QuikService.SendAsync<List<BuySellInfoEx>>(new Message<string>(firmId + "|" + ClientCode + "|" + ClassCode + "|" + SecCode + "|" + price.ToString(), "getBuySellInfoEx"));


    }
}