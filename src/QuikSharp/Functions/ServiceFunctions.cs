// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
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

        public Task<string> GetWorkingFolder()
        => QuikService.SendAsync<string>(new Message<string>("", "getWorkingFolder"));

        public async Task<bool> IsConnected(int timeout = Timeout.Infinite)
        {
            var response = await QuikService.SendAsync<string>(new Message<string>("", "isConnected"), timeout).ConfigureAwait(false);
            return response == "1";
        }

        public Task<string> GetScriptPath()
        => QuikService.SendAsync<string>(new Message<string>("", "getScriptPath"));

        public Task<string> GetInfoParam(InfoParams param)
        => QuikService.SendAsync<string>(new Message<string>(param.ToString(), "getInfoParam"));

        public Task<string> Message(string message, NotificationType iconType = NotificationType.Info)
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

        public Task<string> PrintDbgStr(string message)
        => QuikService.SendAsync<string>(new Message<string>(message, "PrintDbgStr"));

        public async Task<long> AddLabel(double price, string curDate, string curTime, string hint, string path, string tag, string alignment, double backgnd)
        {
            var response = await QuikService.SendAsync<string>(new MessageS(new string[] { price.ToString(), curDate, curTime, hint, path, tag, alignment, backgnd.ToString() }, "addLabel")).ConfigureAwait(false);
            return Number<long>.FromString(response);
        }

        public async Task<long> AddLabel(string chartTag, decimal yValue, string strDate, string strTime, string text, string imagePath,
            string alignment, string hint, int r, int g, int b, int transparency, int tranBackgrnd, string fontName, int fontHeight)
        {
            var msg = new MessageS(new string[] { chartTag, yValue.ToString(), strDate, strTime, text, imagePath, alignment, hint,
                                            r.ToString(), g.ToString(), b.ToString(), transparency.ToString(), tranBackgrnd.ToString(),
                                            fontName, fontHeight.ToString()}, "addLabel2");
            var response = await QuikService.SendAsync<string>(msg).ConfigureAwait(false);
            return Number<long>.FromString(response);
        }

        public async Task<long> AddLabel(string chartTag, Label label_params)
        {
            var msg = new MessageS(new string[] { chartTag, label_params.ToMsg() }, "addLabel2");
            var response = await QuikService.SendAsync<string>(msg).ConfigureAwait(false);
            return Number<long>.FromString(response);
        }

        public Task<bool> SetLabelParams(string chartTag, long labelId, decimal yValue, string strDate, string strTime, string text, string imagePath,
            string alignment, string hint, int r, int g, int b, int transparency, int tranBackgrnd, string fontName, int fontHeight)
        => QuikService.SendAsync<bool>(new MessageS(new string[] { chartTag, labelId.ToString(), yValue.ToString(), strDate, strTime, text, imagePath, alignment, hint, r.ToString(), g.ToString(), b.ToString(), transparency.ToString(), tranBackgrnd.ToString(), fontName, fontHeight.ToString() }, "setLabelParams"));

        public Task<bool> SetLabelParams(string chartTag, long labelId, Label label_params)
        => QuikService.SendAsync<bool>(new MessageS(new string[] { chartTag, labelId.ToString(), label_params.ToMsg() }, "setLabelParams"));

        public Task<Label> GetLabelParams(string chartTag, long labelId) => QuikService.SendAsync<Label>(new MessageS(new string[] { chartTag, labelId.ToString() }, "getLabelParams"));

        public Task<bool> DelLabel(string tag, long labelId) => QuikService.SendAsync<bool>(new MessageS(new string[] { tag, labelId.ToString() }, "delLabel"));

        public Task DelAllLabels(string tag) => QuikService.SendAsync<string>(new Message<string>(tag, "delAllLabels"));

        public Task PrepareToDisconnect() => QuikService.SendAsync<string>(new Message<string>("", "prepareToDisconnect"));

        public async Task<DateTime> GetTradeDate()
        {
            var r = await QuikService.SendAsync<TradeDate>(new Message<string>("", "getTradeDate")).ConfigureAwait(false);
            return r.ToDateTime();
        }

        public void GetNetStats(out long bytes_sent, out long bytes_recieved, out long bytes_callback, out long request_query_size)
            => QuikService.GetNetStats(out bytes_sent, out bytes_recieved, out bytes_callback, out request_query_size);
    }
}