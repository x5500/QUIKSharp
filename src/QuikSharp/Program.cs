﻿// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using NLog;
using QUIKSharp.Converters;
using QUIKSharp.DataStructures;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace QUIKSharp
{
#if NETSTANDARD
    /*    internal class ConsoleTraceListener : TraceListener
        {
            public override void Write(string message)
            {
                Console.Write(message);
            }

            public override void WriteLine(string message)
            {
                Console.WriteLine(message);
            }
        }*/
#endif
    // Шаманство с обработкой закрытия может быть нужно, если кровь из носа следует
    // почистить за собой перед выходом, например снять все заявки или сохранить
    // необработанные данные. Взято из:
    // http://stackoverflow.com/questions/474679/capture-console-exit-c-sharp?lq=1
    // http://stackoverflow.com/questions/1119841/net-console-application-exit-event

    internal static class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static Quik _quik;

        private static bool _exitSystem;

        #region Trap application termination

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);

        private static EventHandler _handler;

        private enum CtrlType
        {
            // ReSharper disable InconsistentNaming
            // ReSharper disable UnusedMember.Local
            CTRL_C_EVENT = 0,

            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        internal static void ManualExitHandler(object sender, EventArgs e)
        {
            Handler(CtrlType.CTRL_CLOSE_EVENT);
        }

        private static bool Handler(CtrlType sig)
        {
            logger.Trace("Exiting system due to manual close, external CTRL-C, or process kill, or shutdown");
            //do your cleanup here
            Cleanup();
            //allow main to run off
            _exitSystem = true;
            //shutdown right away so there are no lingering threads
            Environment.Exit(-1);
            return true;
        }

        #endregion Trap application termination

        private static void Main()
        {
            LogManager.Configuration = new NLog.Config.LoggingConfiguration();
            // Targets where to log to: Console
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole")
            {
                DetectConsoleAvailable = true
            };
            LogManager.Configuration.AddTarget(logconsole);
            LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            // Apply config
            LogManager.ReconfigExistingLoggers();
            logger.Info("Program started");

            // Do not spam console when used as a dependency and use Trace
            //Trace.Listeners.Clear();
            //Trace.Listeners.Add(new ConsoleTraceListener());
            //// Some biolerplate to react to close window event, CTRL-C, kill, etc
            //_handler += Handler;
            //SetConsoleCtrlHandler(_handler, true);

            //ServiceManager.StartServices();

            //_quik = new Quik();
            ////Console.WriteLine("Running in Quik? : " + _quik.Debug.IsQuik().Result);

            //_quik.Events.OnAllTrade += Events_OnAllTrade;
            //_quik.Events.OnQuote += Events_OnQuote;
            //_quik.Events.OnOrder += Events_OnOrder;
            //_quik.Events.OnStop += Events_OnStop;
            //_quik.Events.OnClose += Events_OnClose;

            // ************************************************************************************

            string securityCode = "VBM1";

            // Do not spam console when used as a dependency and use Trace

            // Some biolerplate to react to close window event, CTRL-C, kill, etc
            _handler += Handler;
            SetConsoleCtrlHandler(_handler, true);

            _quik = new Quik();
            //Console.WriteLine("Running in Quik? : " + _quik.Debug.IsQuik().Result);

            // _quik.Events.OnAllTrade += Events_OnAllTrade;
            _quik.Events.OnStop += Events_OnStop;
            _quik.Events.OnClose += Events_OnClose;
            var time = _quik.Service.GetInfoParam(InfoParams.SERVERTIME, CancellationToken.None).Result;

            if (DateTime.TryParse(time, out var serverTime))
                Console.WriteLine(serverTime);

            #region Example

            decimal bestBidPrice = 0;
            decimal bestOfferPrice = 0;

            _quik.Events.OnQuote += (orderBook) =>
            {
                if (orderBook.SecCode == securityCode)
                {
                    bestBidPrice = orderBook.bid.OrderByDescending(o => o.price).First().price;
                    bestOfferPrice = orderBook.offer.OrderByDescending(o => o.price).Last().price;
                }
            };

            var n = 0;
            var sw = new Stopwatch();
            sw.Start();
            while (n <= 10)
            {
                Console.Write("Best bid: " + bestBidPrice);
                Console.WriteLine(" Best offer: " + bestOfferPrice);

                // this line caused threading issues with CJSON, when two threads were calling ToJson at the same time
                time = _quik.Service.GetInfoParam(InfoParams.SERVERTIME, CancellationToken.None).Result;

                Thread.Sleep(50);

                n++;
            }

            sw.Stop();
            Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds);
            Console.WriteLine("Per call: " + (sw.ElapsedMilliseconds / n));
            Console.WriteLine("While Exit");

            #endregion Example

            var sec = new Param
            {
                ClassCode = "SPBOPT",
                SecCode = "SFZ1"
            };
            var ob = _quik.Trading.GetOptionBoard(sec, CancellationToken.None).Result;
            foreach(var op in ob)
            {
                Console.WriteLine(op.ToJson());
            }
            // hold the console so it doesn’t run off the end
            while (!_exitSystem)
            {
                Thread.Sleep(100);
            }
        }

        private static void Events_OnClose()
        {
            Console.WriteLine("Events_OnQuote: ");
        }

        private static void Events_OnStop(int signal)
        {
            Console.WriteLine("Events_OnStop: " + signal);
        }

        private static void Cleanup()
        {
            Console.WriteLine("Bye!");
        }
    }
}