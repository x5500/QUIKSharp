﻿// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.Converters;
using QUIKSharp.DataStructures;
using QUIKSharp.Transport;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    /// <summary>
    /// Service functions implementations
    /// </summary>
    public class ServiceFunctions : FunctionsBase, IServiceFunctions
    {
        internal ServiceFunctions(IQuikService quikService) : base(quikService)
        {
        }

        public Task<string> GetWorkingFolder(CancellationToken cancellationToken)
        => QuikService.SendAsync<string>(new Message<string>("", "getWorkingFolder"), cancellationToken);

        public async Task<bool> IsConnected(CancellationToken cancellationToken)
        {
            // Optimizing for acommon case: no async machinery involved.
            var result = await QuikService.SendAsync<string>(new Message<string>("", "isConnected"), cancellationToken).ConfigureAwait(false);
            return (result == "1");
        }

        public Task<string> GetScriptPath(CancellationToken cancellationToken)
        => QuikService.SendAsync<string>(new Message<string>("", "getScriptPath"), cancellationToken);

        public Task<string> GetInfoParam(InfoParams param, CancellationToken cancellationToken)
        => QuikService.SendAsync<string>(new Message<string>(param.ToString(), "getInfoParam"), cancellationToken);

        public Task<string> Message(string message, NotificationType iconType, CancellationToken cancellationToken)
        {
            string cmd;
            switch (iconType)
            {
                case NotificationType.Info:
                    cmd = "message";
                    break;

                case NotificationType.Warning:
                    cmd = "warning_message";
                    break;

                case NotificationType.Error:
                    cmd = "error_message";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(iconType));
            }
            return QuikService.SendAsync<string>(new Message<string>(message, cmd));
        }

        public Task<string> PrintDbgStr(string message, CancellationToken cancellationToken)
        => QuikService.SendAsync<string>(new Message<string>(message, "PrintDbgStr"), cancellationToken);

        public async Task<long> AddLabel(string chartTag, Label label_params, CancellationToken cancellationToken)
        {
            var msg = new MessageS(new string[] { chartTag, label_params.ToMsg() }, "addLabel2");
            var response = await QuikService.SendAsync<string>(msg, cancellationToken).ConfigureAwait(false);
            return Number<long>.FromString(response);
        }

        public Task<bool> SetLabelParams(string chartTag, long labelId, Label label_params, CancellationToken cancellationToken)
        => QuikService.SendAsync<bool>(new MessageS(new string[] { chartTag, labelId.ToString(), label_params.ToMsg() }, "setLabelParams"), cancellationToken);

        public Task<Label> GetLabelParams(string chartTag, long labelId, CancellationToken cancellationToken) => QuikService.SendAsync<Label>(new MessageS(new string[] { chartTag, labelId.ToString() }, "getLabelParams"), cancellationToken);

        public Task<bool> DelLabel(string tag, long labelId, CancellationToken cancellationToken) => QuikService.SendAsync<bool>(new MessageS(new string[] { tag, labelId.ToString() }, "delLabel"), cancellationToken);

        public Task DelAllLabels(string tag, CancellationToken cancellationToken) => QuikService.SendAsync<string>(new Message<string>(tag, "delAllLabels"), cancellationToken);

        public Task PrepareToDisconnect(CancellationToken cancellationToken) => QuikService.SendAsync<string>(new Message<string>("", "prepareToDisconnect"), cancellationToken);

        public async Task<DateTime> GetTradeDate(CancellationToken cancellationToken)
        {
            var r = await QuikService.SendAsync<TradeDate>(new Message<string>("", "getTradeDate"), cancellationToken).ConfigureAwait(false);
            return r.ToDateTime();
        }

        public void GetNetStats(out ServiceNetworkStats networkStats) => QuikService.GetNetStats(out networkStats);
    }
}