// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Transport;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    /// <summary>
    /// Функции взаимодействия скрипта Lua и Рабочего места QUIK
    /// </summary>
    public class TradingFunctions : FunctionsBase, ITradingFunctions
    {
        internal TradingFunctions(IQuikService quikService) : base(quikService)
        {
        }

        public Task<DepoLimit> GetDepo(ITrader trader, string secCode, CancellationToken cancellationToken)
            => QuikService.SendAsync<DepoLimit>(
                new MessageS(new string[] { trader.ClientCode, trader.FirmId, secCode, trader.AccountID }, "getDepo"), cancellationToken);

        public Task<DepoLimitEx> GetDepoEx(ITrader trader, string secCode, int limitKind, CancellationToken cancellationToken)
            => QuikService.SendAsync<DepoLimitEx>(
                new MessageS(new string[] { trader.FirmId, trader.ClientCode, secCode, trader.AccountID, limitKind.ToString() }, "getDepoEx"), cancellationToken);

        /// <summary>
        /// Возвращает список всех записей из таблицы 'Лимиты по бумагам'.
        /// </summary>
        public Task<List<DepoLimitEx>> GetDepoLimits(CancellationToken cancellationToken) => QuikService.SendAsync<List<DepoLimitEx>>(new Message<string>("", "get_depo_limits"), cancellationToken);

        /// <summary>
        /// Возвращает список записей из таблицы 'Лимиты по бумагам', отфильтрованных по коду инструмента.
        /// </summary>
        public Task<List<DepoLimitEx>> GetDepoLimits(string secCode, CancellationToken cancellationToken) => QuikService.SendAsync<List<DepoLimitEx>>(new Message<string>(secCode, "get_depo_limits"), cancellationToken);

        /// <summary>
        /// Функция для получения информации по денежным лимитам.
        /// </summary>
        public Task<MoneyLimit> GetMoney(ITrader trader, string tag, string currCode, CancellationToken cancellationToken) => QuikService.SendAsync<MoneyLimit>(
                new MessageS(new string[] { trader.ClientCode, trader.FirmId, tag, currCode }, "getMoney"), cancellationToken);

        /// <summary>
        /// Функция для получения информации по денежным лимитам указанного типа.
        /// </summary>
        public Task<MoneyLimitEx> GetMoneyEx(ITrader trader, string tag, string currCode, int limitKind, CancellationToken cancellationToken)
        => QuikService.SendAsync<MoneyLimitEx>(new MessageS(new string[] { trader.FirmId, trader.ClientCode, tag, currCode, limitKind.ToString() }, "getMoneyEx"), cancellationToken);

        /// <summary>
        ///  функция для получения информации по денежным лимитам всех торговых счетов (кроме фьючерсных) и валют.
        ///  Лучшее место для получения связки clientCode + firmid
        /// </summary>
        public Task<List<MoneyLimitEx>> GetMoneyLimits(CancellationToken cancellationToken)
        => QuikService.SendAsync<List<MoneyLimitEx>>(new Message<string>("", "getMoneyLimits"), cancellationToken);

        /// <summary>
        /// Функция заказывает получение параметров Таблицы текущих торгов
        /// </summary>
        /// <returns></returns>
        public Task<bool> ParamRequest(IStructClassSecParam secParam, CancellationToken cancellationToken)
        => QuikService.SendAsync<bool>(new MessageS(new string[] { secParam.ClassCode, secParam.SecCode, secParam.paramName.ToString() }, "paramRequest"), cancellationToken);

        public Task<bool> ParamRequest(ISecurity security, ParamNames paramName, CancellationToken cancellationToken)
        => QuikService.SendAsync<bool>(new MessageS(new string[] { security.ClassCode, security.SecCode, paramName.ToString() }, "paramRequest"), cancellationToken);

        /// <summary>
        /// Функция отменяет заказ на получение параметров Таблицы текущих торгов
        /// </summary>
        /// <param name="secParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<bool> CancelParamRequest(IStructClassSecParam secParam, CancellationToken cancellationToken)
        => QuikService.SendAsync<bool>(new MessageS(new string[] { secParam.ClassCode, secParam.SecCode, secParam.paramName.ToString() }, "cancelParamRequest"), cancellationToken);

        public Task<bool> CancelParamRequest(ISecurity security, ParamNames paramName, CancellationToken cancellationToken)
        => QuikService.SendAsync<bool>(new MessageS(new string[] { security.ClassCode, security.SecCode, paramName.ToString() }, "cancelParamRequest"), cancellationToken);

        /// <summary>
        /// Функция для получения значений Таблицы текущих значений параметров
        /// </summary>
        /// <returns></returns>
        public Task<ParamTable> GetParamEx(IStructClassSecParam secParam, CancellationToken cancellationToken)
        => QuikService.SendAsync<ParamTable>(new MessageS(new string[] { secParam.ClassCode, secParam.SecCode, secParam.paramName.ToString() }, "getParamEx"), cancellationToken);

        public Task<ParamTable> GetParamEx(ISecurity security, ParamNames paramName, CancellationToken cancellationToken)
        => QuikService.SendAsync<ParamTable>(new MessageS(new string[] { security.ClassCode, security.SecCode, paramName.ToString() }, "getParamEx"), cancellationToken);

        /// <summary>
        /// Функция для получения всех значений Таблицы текущих значений параметров
        /// </summary>
        /// <returns></returns>
        public Task<ParamTable> GetParamEx2(IStructClassSecParam secParam, CancellationToken cancellationToken)
        => QuikService.SendAsync<ParamTable>(new MessageS(new string[] { secParam.ClassCode, secParam.SecCode, secParam.paramName.ToString() }, "getParamEx2"), cancellationToken);

        public Task<ParamTable> GetParamEx2(ISecurity security, ParamNames paramName, CancellationToken cancellationToken)
        => QuikService.SendAsync<ParamTable>(new MessageS(new string[] { security.ClassCode, security.SecCode, paramName.ToString() }, "getParamEx2"), cancellationToken);

        public Task<List<OptionBoard>> GetOptionBoard(ISecurity security, CancellationToken cancellationToken)
        => QuikService.SendAsync<List<OptionBoard>>(new MessageS(new string[] { security.ClassCode, security.SecCode }, "getOptionBoard"), cancellationToken);

        public Task<List<Trade>> GetTrades(CancellationToken cancellationToken)
        => QuikService.SendAsync<List<Trade>>(new Message<string>("", "get_trades"), cancellationToken);

        public Task<List<Trade>> GetTrades(ISecurity security, CancellationToken cancellationToken)
        => QuikService.SendAsync<List<Trade>>(new MessageS(new string[] { security.ClassCode, security.SecCode }, "get_trades"), cancellationToken);

        public Task<List<Trade>> GetTrades_by_OdrerNumber(long orderNum, CancellationToken cancellationToken)
        => QuikService.SendAsync<List<Trade>>(new Message<string>(orderNum.ToString(), "get_Trades_by_OrderNumber"), cancellationToken);

        public Task<PortfolioInfo> GetPortfolioInfo(ITrader trader, CancellationToken cancellationToken)
        => QuikService.SendAsync<PortfolioInfo>(new MessageS(new string[] { trader.FirmId, trader.ClientCode }, "getPortfolioInfo"), cancellationToken);

        public Task<PortfolioInfoEx> GetPortfolioInfoEx(ITrader trader, int limitKind, CancellationToken cancellationToken)
        => QuikService.SendAsync<PortfolioInfoEx>(new MessageS(new string[] { trader.FirmId, trader.ClientCode, limitKind.ToString() }, "getPortfolioInfoEx"), cancellationToken);

        public Task<string> GetTrdAccByClientCode(ITrader trader, CancellationToken cancellationToken)
        => QuikService.SendAsync<string>(new MessageS(new string[] { trader.FirmId, trader.ClientCode }, "GetTrdAccByClientCode"), cancellationToken);

        public Task<string> GetClientCodeByTrdAcc(ITrader trader, CancellationToken cancellationToken)
        => QuikService.SendAsync<string>(new MessageS(new string[] { trader.FirmId, trader.AccountID }, "GetClientCodeByTrdAcc"), cancellationToken);

        public Task<bool> IsUcpClient(ITrader trader, CancellationToken cancellationToken)
        => QuikService.SendAsync<bool>(new MessageS(new string[] { trader.FirmId, trader.ClientCode }, "IsUcpClient"), cancellationToken);

        public Task<List<AllTrade>> GetAllTrades(CancellationToken cancellationToken)
        => QuikService.SendAsync<List<AllTrade>>(new Message<string>("", "get_all_trades"), cancellationToken);

        public Task<List<AllTrade>> GetAllTrades(ISecurity security, CancellationToken cancellationToken)
        => QuikService.SendAsync<List<AllTrade>>(new MessageS(new string[] { security.ClassCode, security.SecCode }, "get_all_trades"), cancellationToken);

        /// <summary>
        /// Функция принимает список в формате class_code|sec_code|param_name и возвращает результаты вызова
        /// функции getParamEx2 для каждой строки запроса в виде списка в таком же порядке, как в запросе
        /// </summary>
        public Task<List<ParamTable>> GetParamEx2Bulk(IEnumerable<IStructClassSecParam> classSecParams, CancellationToken cancellationToken)
        {
            var msg = new List<string>();
            foreach (var csp in classSecParams)
            {
                msg.Add(string.Concat(csp.ClassCode, "|", csp.SecCode, "|", csp.paramName));
            }

            return QuikService.SendAsync<List<ParamTable>>(new Message<List<string>>(msg, "getParamEx2Bulk"), cancellationToken);
        }

        Task<List<bool>> ITradingFunctions.ParamRequestBulk(IEnumerable<IStructClassSecParam> classSecParams, CancellationToken cancellationToken)
        {
            var msg = new List<string>();
            foreach (var csp in classSecParams)
            {
                msg.Add(string.Concat(csp.ClassCode, "|", csp.SecCode, "|", csp.paramName));
            }

            return QuikService.SendAsync<List<bool>>(new Message<List<string>>(msg, "paramRequestBulk"), cancellationToken);
        }

        Task<List<bool>> ITradingFunctions.CancelParamRequestBulk(IEnumerable<IStructClassSecParam> classSecParams, CancellationToken cancellationToken)
        {
            var msg = new List<string>();
            foreach (var csp in classSecParams)
            {
                msg.Add(string.Concat(csp.ClassCode, "|", csp.SecCode, "|", csp.paramName));
            }

            return QuikService.SendAsync<List<bool>>(new Message<List<string>>(msg, "cancelParamRequestBulk"), cancellationToken);
        }
    }
}