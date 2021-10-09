// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.Converters;
using QUIKSharp.DataStructures;
using QUIKSharp.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
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

        public async Task<string[]> GetClassesList(CancellationToken cancellationToken)
        {
            var response = await QuikService.SendAsync<string>(new Message<string>("", "getClassesList"), cancellationToken).ConfigureAwait(false);
            return response == null ? Array.Empty<string>(): response.TrimEnd(',').Split(new[] { "," }, StringSplitOptions.None);
        }

        public Task<ClassInfo> GetClassInfo(string classID, CancellationToken cancellationToken) => QuikService.SendAsync<ClassInfo>(new Message<string>(classID, "getClassInfo"), cancellationToken);

        public Task<SecurityInfo> GetSecurityInfo(string classCode, string secCode, CancellationToken cancellationToken) => QuikService.SendAsync<SecurityInfo>(new Message<string>(classCode + "|" + secCode, "getSecurityInfo"), cancellationToken);

        public Task<SecurityInfo> GetSecurityInfo(ISecurity security, CancellationToken cancellationToken) => GetSecurityInfo(security.ClassCode, security.SecCode, cancellationToken);

        public async Task<string[]> GetClassSecurities(string classID, CancellationToken cancellationToken)
        {
            var response = await QuikService.SendAsync<string>(new Message<string>(classID, "getClassSecurities"), cancellationToken).ConfigureAwait(false);
            return response == null ? Array.Empty<string>() : response.TrimEnd(',').Split(new[] { "," }, StringSplitOptions.None);
        }

        public Task<string> GetSecurityClass(string classesList, string secCode, CancellationToken cancellationToken) => QuikService.SendAsync<string>(new Message<string>(classesList + "|" + secCode, "getSecurityClass"), cancellationToken);

        public Task<string> GetClientCode(CancellationToken cancellationToken) => QuikService.SendAsync<string>(new Message<string>("", "getClientCode"), cancellationToken);

        public Task<List<string>> GetClientCodes(CancellationToken cancellationToken) => QuikService.SendAsync<List<string>>(new Message<string>("", "getClientCodes"), cancellationToken);

        public Task<string> GetTradeAccount(string classCode, CancellationToken cancellationToken)
        => QuikService.SendAsync<string>(new Message<string>(classCode, "getTradeAccount"), cancellationToken);

        public Task<List<TradesAccounts>> GetTradeAccounts(CancellationToken cancellationToken)
        => QuikService.SendAsync<List<TradesAccounts>>(new Message<string>("", "getTradeAccounts"), cancellationToken);

        public Task<List<AccountPosition>> GetAccountPositions(CancellationToken cancellationToken)
        => QuikService.SendAsync<List<AccountPosition>>(new Message<string>("account_positions", "get_table"), cancellationToken);

        public Task<List<MoneyLimitEx>> GetMoneyLimits(CancellationToken cancellationToken)
        => QuikService.SendAsync<List<MoneyLimitEx>>(new Message<string>("money_limits", "get_table"), cancellationToken);

        public Task<List<DepoLimits>> GetDepoLimits(CancellationToken cancellationToken)
        => QuikService.SendAsync<List<DepoLimits>>(new Message<string>("depo_limits", "get_table"), cancellationToken);

        /// <summary>
        /// Функция для получения информации по фьючерсным лимитам
        /// </summary>
        public Task<FuturesLimits> GetFuturesLimit(ITrader trader, int limitType, string currCode, CancellationToken cancellationToken)
            => QuikService.SendAsync<FuturesLimits>(new MessageS(new string[] { trader.FirmId, trader.AccountID, limitType.ToString(), currCode.ToString() }, "getFuturesLimit"), cancellationToken);
        
        /// <summary>
        ///  функция для получения информации по фьючерсным лимитам всех клиентских счетов
        /// </summary>
        public Task<List<FuturesLimits>> GetFuturesClientLimits(CancellationToken cancellationToken) => QuikService.SendAsync<List<FuturesLimits>>(
                new Message<string>("", "getFuturesClientLimits"), cancellationToken);

        /// <summary>
        /// getFuturesHolding - функция для получения информации по фьючерсной позиции
        /// </summary>
        public Task<FuturesClientHolding> GetFuturesHolding(ITradeSecurity tradeSec, int posType, CancellationToken cancellationToken)
        => QuikService.SendAsync<FuturesClientHolding>(
                new MessageS(new string[] { tradeSec.FirmId, tradeSec.AccountID, tradeSec.SecCode, posType.ToString() }, "getFuturesHolding"), cancellationToken);

        /// <summary>
        /// getFuturesHoldings - функция для получения информации по фьючерсным позициям
        /// </summary>
        public Task<List<FuturesClientHolding>> GetFuturesHoldings(CancellationToken cancellationToken)
        => QuikService.SendAsync<List<FuturesClientHolding>>(new Message<string>("", "getFuturesHoldings"), cancellationToken);

        /// <summary>
        /// getFuturesHoldings - функция для получения информации по фьючерсным позициям
        /// </summary>
        public Task<List<FuturesClientHolding>> GetFuturesHoldingsNotZero(CancellationToken cancellationToken)
        => QuikService.SendAsync<List<FuturesClientHolding>>(new Message<string>("", "getFuturesHoldingsNotZero"), cancellationToken);

        /// <summary>
        /// getFuturesHolding - функция для получения информации по фьючерсной позиции
        /// TABLE getBuySellInfoEx(STRING firm_id, STRING client_code, STRING class_code, STRING sec_code, NUMBER price)
        /// </summary>
        public Task<List<BuySellInfoEx>> GetBuySellInfoEx(ITradeSecurity tradeSec, decimal price, CancellationToken cancellationToken)
        {
            return QuikService.SendAsync<List<BuySellInfoEx>>(new MessageS(new string[] { tradeSec.FirmId, tradeSec.ClientCode, tradeSec.ClassCode, tradeSec.SecCode, price.PriceToString() }, "getBuySellInfoEx"), cancellationToken);
        }

        /// <summary>
        ///  функция для расчета максимально возможного количества лотов в заявке
        ///  При заданном параметре is_market=true, необходимо передать параметр price=0, иначе будет рассчитано максимально возможное количество лотов в заявке по цене price.
        /// </summary>
        public Task<CalcBuySellResult> CalcBuySell(ITradeSecurity tradeSec, decimal price, bool isBuy, bool isMarket, CancellationToken cancellationToken)
        {
            return QuikService.SendAsync<CalcBuySellResult>(
                new MessageS(new string[] { tradeSec.ClassCode, tradeSec.SecCode, tradeSec.ClientCode, tradeSec.AccountID, price.PriceToString(), isBuy.ToString(), isMarket.ToString() }, "calc_buy_sell"), cancellationToken);
        }
    }
}