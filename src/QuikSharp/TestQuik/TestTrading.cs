// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
namespace QUIKSharp.TestQuik
{
    public class TestTrading : ITradingFunctions
    {
        private readonly Dictionary<long, AllTrade> AllTradeTable = new Dictionary<long, AllTrade>();
        private readonly Dictionary<long, Trade> TradeTable = new Dictionary<long, Trade>();

        public TestTrading()
        {
        }

        public void ClearAll()
        {
            AllTradeTable.Clear();
            TradeTable.Clear();
        }
        public void OnNewAllTrade(AllTrade trade)
        {
            AllTradeTable[trade.TradeNum] = trade;
        }
        public void OnNewTrade(Trade trade)
        {
            TradeTable[trade.TradeNum] = trade;
        }

        async Task<bool> ITradingFunctions.CancelParamRequest(IStructClassSecParam classSecParam)
        {
            return true;
        }

        async Task<bool> ITradingFunctions.CancelParamRequest(ISecurity security, ParamNames paramName)
        {
            return true;
        }

        Task<List<bool>> ITradingFunctions.CancelParamRequestBulk(IEnumerable<IStructClassSecParam> classSecParams)
        {
            throw new System.NotImplementedException();
        }

        async Task<List<AllTrade>> ITradingFunctions.GetAllTrades()
        {
            var l = new List<AllTrade>();
            foreach(var kv in AllTradeTable)
            {
                l.Add(kv.Value);
            }
            return l;
        }

        async Task<List<AllTrade>> ITradingFunctions.GetAllTrades(ISecurity security)
        {
            var l = new List<AllTrade>();
            foreach (var kv in AllTradeTable)
            {
                if (string.Compare(kv.Value.SecCode, security.SecCode, true) == 0) 
                    l.Add(kv.Value);
            }
            return l;
        }

        Task<string> ITradingFunctions.GetClientCodeByTrdAcc(ITrader trader)
        {
            throw new System.NotImplementedException();
        }

        async Task<DepoLimit> ITradingFunctions.GetDepo(ITrader trader, string secCode)
        {
            var d = new DepoLimit();
            return d;
        }

        async Task<DepoLimitEx> ITradingFunctions.GetDepoEx(ITrader trader, string secCode, int limitKind)
        {
            var d = new DepoLimitEx
            {
                ClientCode = trader.ClientCode,
                FirmId = trader.FirmId,
                SecCode = secCode,
                TrdAccId = trader.AccountID,
                LimitKindInt = limitKind
            };
            return d;
        }

        async Task<List<DepoLimitEx>> ITradingFunctions.GetDepoLimits()
        {
            var l = new List<DepoLimitEx>();
            return l;
        }

        async Task<List<DepoLimitEx>> ITradingFunctions.GetDepoLimits(string secCode)
        {
            var l = new List<DepoLimitEx>();
            return l;
        }

        Task<MoneyLimit> ITradingFunctions.GetMoney(ITrader trader, string tag, string currCode)
        {
            throw new System.NotImplementedException();
        }

        Task<MoneyLimitEx> ITradingFunctions.GetMoneyEx(ITrader trader, string tag, string currCode, int limitKind)
        {
            throw new System.NotImplementedException();
        }

        Task<List<MoneyLimitEx>> ITradingFunctions.GetMoneyLimits()
        {
            throw new System.NotImplementedException();
        }

        Task<List<OptionBoard>> ITradingFunctions.GetOptionBoard(ISecurity security)
        {
            throw new System.NotImplementedException();
        }

        Task<ParamTable> ITradingFunctions.GetParamEx(IStructClassSecParam classSecParam)
        {
            throw new System.NotImplementedException();
        }

        Task<ParamTable> ITradingFunctions.GetParamEx(ISecurity security, ParamNames paramName)
        {
            throw new System.NotImplementedException();
        }

        Task<ParamTable> ITradingFunctions.GetParamEx2(IStructClassSecParam classSecParam)
        {
            throw new System.NotImplementedException();
        }

        Task<ParamTable> ITradingFunctions.GetParamEx2(ISecurity security, ParamNames paramName)
        {
            throw new System.NotImplementedException();
        }

        Task<List<ParamTable>> ITradingFunctions.GetParamEx2Bulk(IEnumerable<IStructClassSecParam> classSecParams)
        {
            throw new System.NotImplementedException();
        }

        Task<PortfolioInfo> ITradingFunctions.GetPortfolioInfo(ITrader trader)
        {
            throw new System.NotImplementedException();
        }

        Task<PortfolioInfoEx> ITradingFunctions.GetPortfolioInfoEx(ITrader trader, int limitKind)
        {
            throw new System.NotImplementedException();
        }

        async Task<List<Trade>> ITradingFunctions.GetTrades()
        {
            var l = new List<Trade>();
            foreach (var kv in TradeTable)
            {
                l.Add(kv.Value);
            }
            return l;
        }

        async Task<List<Trade>> ITradingFunctions.GetTrades(ISecurity security)
        {
            var l = new List<Trade>();
            foreach (var kv in TradeTable)
            {
                if (string.Compare(kv.Value.SecCode, security.SecCode, true) == 0) 
                    l.Add(kv.Value);
            }
            return l;
        }

        async Task<List<Trade>> ITradingFunctions.GetTrades_by_OdrerNumber(long orderNum)
        {
            var l = new List<Trade>();
            foreach (var kv in TradeTable)
            {
                if (kv.Value.OrderNum == orderNum)
                    l.Add(kv.Value);
            }
            return l;
        }

        Task<string> ITradingFunctions.GetTrdAccByClientCode(ITrader trader)
        {
            throw new System.NotImplementedException();
        }

        Task<bool> ITradingFunctions.IsUcpClient(ITrader trader)
        {
            throw new System.NotImplementedException();
        }

        async Task<bool> ITradingFunctions.ParamRequest(IStructClassSecParam classSecParam)
        {
            return true;
        }

        async Task<bool> ITradingFunctions.ParamRequest(ISecurity security, ParamNames paramName)
        {
            return true;
        }

        Task<List<bool>> ITradingFunctions.ParamRequestBulk(IEnumerable<IStructClassSecParam> classSecParams)
        {
            throw new System.NotImplementedException();
        }
    }
}
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена