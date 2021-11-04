// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using NLog;
using QUIKSharp.DataStructures;
using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
namespace QUIKSharp.TestQuik
{
    public class TestService : IServiceFunctions
    {
        private static readonly Logger logging = LogManager.GetCurrentClassLogger();
        internal bool _isConnected;
        internal DateTime _TradeDate;

        public void GetNetStats(out ServiceNetworkStats networkStats)
        {
            throw new NotImplementedException();
        }

        Task<long> IServiceFunctions.AddLabel(string chartTag, Label label_params, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IServiceFunctions.DelAllLabels(string tag, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<bool> IServiceFunctions.DelLabel(string tag, long labelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<string> IServiceFunctions.GetInfoParam(InfoParams param, CancellationToken task_cancel)
        {
            throw new NotImplementedException();
        }

        Task<Label> IServiceFunctions.GetLabelParams(string chartTag, long labelId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<string> IServiceFunctions.GetScriptPath(CancellationToken task_cancel)
        {
            throw new NotImplementedException();
        }

        async Task<DateTime> IServiceFunctions.GetTradeDate(CancellationToken cancellationToken)
        {
            return _TradeDate;
        }

        Task<string> IServiceFunctions.GetWorkingFolder(CancellationToken task_cancel)
        {
            throw new NotImplementedException();
        }

        async Task<bool> IServiceFunctions.IsConnected(CancellationToken task_cancel)
        {
            return _isConnected;
        }

        async Task<string> IServiceFunctions.Message(string message, NotificationType iconType, CancellationToken task_cancel)
        {
            logging.Info($"({iconType}) Message: {message}");
            return "";
        }

        Task IServiceFunctions.PrepareToDisconnect(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        async Task<string> IServiceFunctions.PrintDbgStr(string message, CancellationToken task_cancel)
        {
            logging.Debug($"PrintDbgStr: {message}");
            return "";
        }

        Task<bool> IServiceFunctions.SetLabelParams(string chartTag, long labelId, Label label_params, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
#pragma warning restore CS1998 // В асинхронном методе отсутствуют операторы await, будет выполнен синхронный метод
#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена