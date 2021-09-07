using NLog;
using QUIKSharp;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.QOrders;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QUIKSharp.Orders;
using QUIKSharp.Functions;
using QUIKSharp.Transport;

namespace Profiler
{

    public class TradeSec : ITradeSecurity
    {
        public string AccountID { get; } = "SPBFUT00C72";

        public string ClientCode { get; } = "XXXX";

        public string FirmId { get; } = "YYYY";

        public string ClassCode { get; } = "SPBFUT";

        public string SecCode { get; } = "VBM1";
    }

    public class Program
    {
        private static void Configure_Nlog(NLog.LogLevel loglevel)
        {
            var nlog_config = new NLog.Config.LoggingConfiguration();
            // Targets where to log to: Console
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole")
            {
                DetectConsoleAvailable = true,
                Layout = ">> [${date:format=HH\\:MM\\:ss}] [${level}] [${message}]"
            };

            nlog_config.AddTarget(logconsole);
            nlog_config.AddRule(loglevel, LogLevel.Fatal, logconsole);

            var tracelog = new NLog.Targets.FileTarget
            {
                FileName = Directory.GetCurrentDirectory() + "//trace.log",
                FileNameKind = NLog.Targets.FilePathKind.Relative,
                Layout = "${date:format=yyyy-MM-dd.HH.mm.ss.ffffff} ${processid}/${threadid} ${level}: ${message}",
                DeleteOldFileOnStartup = true
            };
            var async_tracelog = new NLog.Targets.Wrappers.AsyncTargetWrapper(tracelog, 1000, NLog.Targets.Wrappers.AsyncTargetWrapperOverflowAction.Grow)
            {
                Name = "tracelog"
            };

            nlog_config.AddTarget(async_tracelog);
            nlog_config.AddRule(LogLevel.Trace, LogLevel.Fatal, tracelog);

            NLog.Targets.FileTarget logfile = new NLog.Targets.FileTarget
            {
                FileName = Directory.GetCurrentDirectory() + "//debug.log",
                FileNameKind = NLog.Targets.FilePathKind.Relative,
                Layout = "${date:format=yyyy-MM-dd.HH.mm.ss.ffffff} ${level}: ${message}",
                AutoFlush = true,
                DeleteOldFileOnStartup = false,
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                ArchiveDateFormat = "yyyyMMdd",
                MaxArchiveFiles = 14
            };

            var async_logfile = new NLog.Targets.Wrappers.AsyncTargetWrapper(logfile, 1000, NLog.Targets.Wrappers.AsyncTargetWrapperOverflowAction.Grow)
            {
                Name = "logfile"
            };

            nlog_config.AddTarget(async_logfile);
            nlog_config.AddRule(loglevel, LogLevel.Fatal, logfile);

            // Apply config
            try
            {
                LogManager.ThrowConfigExceptions = true;
                LogManager.Configuration = nlog_config;
                LogManager.ReconfigExistingLoggers();
                LogManager.ThrowConfigExceptions = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void Ping()
        {
            var quikService = QuikService.Create(Quik.DefaultHost, Quik.DefaultPort, Quik.DefaultPort+1);
            var _df = new DebugFunctions(quikService);

            var sw = new Stopwatch();
            Console.WriteLine("Started");
            for (int round = 0; round < 10; round++)
            {
                sw.Reset();
                sw.Start();

                var count = 1000;
                var array = new Task<string>[count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = _df.Ping();
                }
                for (int i = 0; i < array.Length; i++)
                {
                    var pong = array[i].Result;
                    array[i] = null;
                    Trace.Assert(pong == "Pong");
                }

                //for (var i = 0; i < count; i++) {
                //    var pong = qc.Ping().Result;
                //    Trace.Assert(pong == "Pong");
                //}
                sw.Stop();
                Console.WriteLine("MultiPing takes msecs: " + sw.ElapsedMilliseconds);
            }
        }

        public static void EchoTransaction()
        {
            var quikService = QuikService.Create(Quik.DefaultHost, Quik.DefaultPort, Quik.DefaultPort + 1);
            var _df = new DebugFunctions(quikService);

            var sw = new Stopwatch();
            Console.WriteLine("Started");
            for (int round = 0; round < 10; round++)
            {
                sw.Reset();
                sw.Start();

                var count = 1000;
                var t = new Transaction();

                var array = new Task<Transaction>[count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = _df.Echo(t);
                }
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].Wait();
                    array[i] = null;
                }

                sw.Stop();
                Console.WriteLine("MultiPing takes msecs: " + sw.ElapsedMilliseconds);
            }
        }

        public static void LuaNewTransactionIDTest()
        {
            Quik quik = new Quik();
            LuaIdProvider idProvider1 = new LuaIdProvider(quik);
            LuaBulkIdProvider idProvider2 = new LuaBulkIdProvider(quik, 100);

            for (int i = 0; i < 3; i++)
            {
                bool isServerConnected = quik.Service.IsConnected().Result;
                Console.WriteLine("Соединение с сервером " + (isServerConnected ? "" : "НЕ") + "установлено.");
                if (isServerConnected) break;
                Thread.Sleep(1000);
            }

            var count = 1000;
            var array = new long[count];

            var sw = new Stopwatch();

            sw.Reset();
            Console.WriteLine("Started");
            for (int i = 0; i < array.Length; i++)
            {
                sw.Start();
                long transID = quik.Transactions.LuaNewTransactionID(1).Result;
                sw.Stop();
                array[i] = transID;
            }
            Console.WriteLine("\nLuaNewTransactionID test takes msecs: " + sw.ElapsedMilliseconds);

            sw.Reset();
            Console.WriteLine("Started");
            for (int i = 0; i < array.Length; i++)
            {
                sw.Start();
                //long transID = quik.Transactions.LuaNewTransactionID(step).Result;
                long transID = idProvider1.GetNextId();
                sw.Stop();
                array[i] = transID;
            }
            Console.WriteLine("\nLuaIdProvider test takes msecs: " + sw.ElapsedMilliseconds);

            sw.Reset();
            Console.WriteLine("Started");
            for (int i = 0; i < array.Length; i++)
            {
                sw.Start();
                //long transID = quik.Transactions.LuaNewTransactionID(step).Result;
                long transID = idProvider2.GetNextId();
                sw.Stop();
                array[i] = transID;
            }
            Console.WriteLine("\nLuaBulkIdProvider test takes msecs: " + sw.ElapsedMilliseconds);
        }

        public static void ConnectionTest()
        {
            void OnConnectedToQuik(int port)
            {
                Console.WriteLine("EVENT: Соединение с терминалом Quik установлено. Port:" + port.ToString());
            }

            void OnDisconnectedQuik()
            {
                Console.WriteLine("EVENT: Соединение с терминалом Quik разорвано.");
            }

            Quik quik = new Quik();
            quik.Events.OnConnectedToQuik += OnConnectedToQuik;
            quik.Events.OnDisconnectedFromQuik += OnDisconnectedQuik;

            for (int i = 0; i < 100; i++)
            {
                if (quik.IsServiceConnected)
                {
                    try
                    {
                        bool isServerConnected = quik.Service.IsConnected(1000).Result;
                        Console.WriteLine("Соединение с сервером " + (isServerConnected ? "" : "НЕ ") + "установлено.");
                    }
                    catch (System.TimeoutException e)
                    {
                        Console.WriteLine("TimeoutException: " + e.Message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                Thread.Sleep(2000);
            }
        }

        public static async void TransactionTest()
        {
            void OnConnectedToQuik(int port)
            {
                Console.WriteLine("EVENT: Соединение с терминалом Quik установлено. Port:" + port.ToString());
            }

            void OnDisconnectedQuik()
            {
                Console.WriteLine("EVENT: Соединение с терминалом Quik разорвано.");
            }

            Quik quik = new Quik();
            quik.Events.OnConnectedToQuik += OnConnectedToQuik;
            quik.Events.OnDisconnectedFromQuik += OnDisconnectedQuik;

            bool isServiceConnected = quik.IsServiceConnected;
            Console.WriteLine("Соединение с терминалом QUIK " + (isServiceConnected ? "" : "НЕ ") + "установлено.");

            var tradesec = new TradeSec();
            if (isServiceConnected)
            {
                try
                {
                    bool isServerConnected = quik.Service.IsConnected(1000).Result;
                    Console.WriteLine("Соединение с сервером " + (isServerConnected ? "" : "НЕ ") + "установлено.");
                    if (!isServerConnected) return;

                    //var tm = new TransactionManager(quik, luaId);
                    var qo = new QuikOrders(quik);

                    var r = await qo.SendStopOrder(tradesec,Operation.Buy, 4590m, 4540m, 1);
                    Console.WriteLine(string.Concat("Result OrderNum: ", r.OrderNum, " err_code: ", r.Result, " ", r.ResultMsg));

                    if (r.OrderNum != null && r.OrderNum.Value > 0)
                    {
                        var rm = await qo.KillStopOrder(tradesec, r.OrderNum.Value);
                        Console.WriteLine(string.Concat("KillOrder Result: ", rm.Result, " ", rm.ResultMsg));
                    }

                    //r = await qo.SendStopOrder(SecCode, ClassCode, AccountID, QuikSharp.DataStructures.Operation.Buy, 4590m, 4540m, 1);
                    //Console.WriteLine(string.Concat("Result OrderNum: ", r.OrderNum, " err_code: ", r.result, " ", r.ResultMsg));

                    var stopOrders = await quik.Orders.GetStopOrders();
                    if (stopOrders.Count > 0)
                    {
                        var order1 = stopOrders.Find(o => (o.SecCode == tradesec.SecCode) && o.Flags.HasFlag(QUIKSharp.DataStructures.StopOrderFlags.Active));
                        if (order1 != null)
                        {
                            var r2 = await qo.CreateStopOrder(order1);
                            Console.WriteLine(string.Concat("Result CreateStopOrder: ", r2.OrderNum, " err_code: ", r2.Result, " ", r2.ResultMsg));
                        }
                    }
                    {
                        var rm = await qo.KillAllStopOrders(tradesec);
                        Console.WriteLine(string.Concat("KillAllStopOrders Result: ", rm.Result, " ", rm.ResultMsg));
                    }

                    // -- Limit orders --
                    r = await qo.SendLimitOrder(tradesec, QUIKSharp.DataStructures.Operation.Buy, 4540m, 1);
                    Console.WriteLine(string.Concat("Result OrderNum: ", r.OrderNum, " err_code: ", r.Result, " ", r.ResultMsg));

                    if (r.OrderNum.HasValue && r.OrderNum.Value > 0)
                    {
                        var rm = await qo.Move_Order(tradesec, r.OrderNum.Value, 4235, 1);
                        Console.WriteLine(string.Concat("Move Result OrderNum: ", rm.OrderNum, " err_code: ", rm.Result, " ", rm.ResultMsg));
                        if (rm.OrderNum.HasValue)
                            r = rm;
                    }

                    var limitOrders = await quik.Orders.GetOrders();
                    if (limitOrders.Count > 0)
                    {
                        var order1 = limitOrders.Find(o => (o.SecCode == tradesec.SecCode) && o.Flags.HasFlag(OrderTradeFlags.Active));
                        if (order1 != null)
                        {
                            var r2 = await qo.CreateOrder(order1);
                            Console.WriteLine(string.Concat("Result CreateOrder: ", r2.OrderNum, " err_code: ", r2.Result, " ", r2.ResultMsg));

                            if (r2.OrderNum.Value > 0)
                            {
                                var r3 = await qo.KillOrder(tradesec, r.OrderNum.Value);
                                Console.WriteLine(string.Concat("Kill Result OrderNum: ", r3.OrderNum, " err_code: ", r3.Result, " ", r3.ResultMsg));
                            }
                        }
                    }

                    if (r.OrderNum.HasValue && r.OrderNum.Value > 0)
                    {
                        Thread.Sleep(200);
                        var r2 = quik.Orders.GetOrder_by_transID(tradesec, r.TransID.Value).Result;
                        if (r2 != null)
                            Console.WriteLine(string.Concat("GetOrder_by_transID: ", r.TransID, " OrderNum: ", r2.OrderNum, " ", r2.Comment));
                        else
                            Console.WriteLine("GetOrder_by_transID failed!");

                        var r3 = quik.Orders.GetOrder(tradesec.ClassCode, r.OrderNum.Value).Result;
                        if (r3 != null)
                            Console.WriteLine(string.Concat("GetOrder: by OrderNum: ", r.OrderNum, " ", r2.Comment));
                        else
                            Console.WriteLine("GetOrder: by OrderNum failed!");
                    }

                    if (r.OrderNum.HasValue && r.OrderNum.Value > 0)
                    {
                        var r3 = await qo.KillOrder(tradesec, r.OrderNum.Value);
                        Console.WriteLine(string.Concat("Kill Result OrderNum: ", r3.OrderNum, " err_code: ", r3.Result, " ", r3.ResultMsg));
                    }

                    {
                        var rm = await qo.KillAllFuturesOrders(tradesec, "VTBR");
                        Console.WriteLine(string.Concat("KillAllFuturesOrders Result: ", rm.Result, " ", rm.ResultMsg));
                    }

                    {
                        var rm = await qo.KillAllOrders(tradesec);
                        Console.WriteLine(string.Concat("KillAllOrders Result: ", rm.Result, " ", rm.ResultMsg));
                    }
                }
                catch (System.TimeoutException e)
                {
                    Console.WriteLine("TimeoutException: " + e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public static async void OrdersTest()
        {
            void OnConnectedToQuik(int port)
            {
                Console.WriteLine("EVENT: Соединение с терминалом Quik установлено. Port:" + port.ToString());
            }

            void OnDisconnectedQuik()
            {
                Console.WriteLine("EVENT: Соединение с терминалом Quik разорвано.");
            }

            Quik quik = new Quik
            {
                DefaultSendTimeout = new TimeSpan(0, 0, 0, 10, 0)
            };

            LuaIdProvider luaId = new LuaIdProvider(quik);
            quik.Events.OnConnectedToQuik += OnConnectedToQuik;
            quik.Events.OnDisconnectedFromQuik += OnDisconnectedQuik;

            bool isServiceConnected = quik.IsServiceConnected;
            Console.WriteLine("Соединение с терминалом QUIK " + (isServiceConnected ? "" : "НЕ ") + "установлено.");


            var tradesec = new TradeSec();            
            if (isServiceConnected)
            {
                try
                {
                    bool isServerConnected = quik.Service.IsConnected(1000).Result;
                    Console.WriteLine("Соединение с сервером " + (isServerConnected ? "" : "НЕ ") + "установлено.");
                    //if (!isServerConnected) return;

                    //var tm = new TransactionManager(quik, luaId);
                    var qo = new QuikOrders(quik);
                    var r = await qo.SendStopOrder(tradesec, QUIKSharp.DataStructures.Operation.Buy, 4590m, 4540m, 1).ConfigureAwait(false);
                    Console.WriteLine(string.Concat("Result OrderNum: ", r.OrderNum, " err_code: ", r.Result, " ", r.ResultMsg));

                    if (r.OrderNum != null && r.OrderNum.Value > 0)
                    {
                        var rm = await qo.Move_Order(tradesec, r.OrderNum.Value, 4235m, 1).ConfigureAwait(false);
                        Console.WriteLine(string.Concat("Move Result OrderNum: ", rm.OrderNum, " err_code: ", rm.Result, " ", rm.ResultMsg));
                        if (rm.OrderNum.HasValue)
                            r = rm;
                    }
                    if (r.OrderNum != null && r.OrderNum.Value > 0)
                    {
                        Thread.Sleep(200);
                        var r2 = quik.Orders.GetOrder_by_transID(tradesec, r.TransID.Value).Result;
                        if (r2 != null)
                            Console.WriteLine(string.Concat("GetOrder_by_transID: ", r.TransID, " OrderNum: ", r2.OrderNum, " ", r2.Comment));
                        else
                            Console.WriteLine("GetOrder_by_transID failed!");

                        var r3 = quik.Orders.GetOrder(tradesec.ClassCode, r.OrderNum.Value).Result;
                        if (r3 != null)
                            Console.WriteLine(string.Concat("GetOrder: by OrderNum: ", r.OrderNum, " ", r2.Comment));
                        else
                            Console.WriteLine("GetOrder: by OrderNum failed!");
                    }

                    if (r.OrderNum != null && r.OrderNum.Value > 0)
                    {
                        var r3 = await qo.KillOrder(tradesec, r.OrderNum.Value).ConfigureAwait(false);
                        Console.WriteLine(string.Concat("Kill Result OrderNum: ", r3.OrderNum, " err_code: ", r3.Result, " ", r3.ResultMsg));
                    }
                }
                catch (System.TimeoutException e)
                {
                    Console.WriteLine("TimeoutException: " + e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        private class TestQ : ITradeSecurity
        {
            public string Tag;
            public string ClientCode { get; } = string.Empty;
            public string FirmId { get; }  = string.Empty;
            public string AccountID { get; } = string.Empty;
            public string ClassCode { get; } = "SPBFUT";
            public string SecCode { get; } = "VBM1";

            public TestQ(Quik q, string ClassCode = null, string SecCode = null, string AccountID = null)
            {
                var money_limits = q.Class.GetMoneyLimits().ConfigureAwait(false).GetAwaiter().GetResult();
                var depo_limits = q.Class.GetDepoLimits().ConfigureAwait(false).GetAwaiter().GetResult();

                var fst = money_limits.FirstOrDefault();
                ClientCode = fst?.ClientCode;
                FirmId = fst?.FirmId;
                Tag = fst?.Tag;

                if (string.IsNullOrEmpty(AccountID))
                {
                    this.AccountID = depo_limits.Where(row => row.TrdAccId != string.Empty).FirstOrDefault()?.TrdAccId;
                }
                else
                {
                    this.AccountID = AccountID;
                }
                if (!string.IsNullOrEmpty(ClassCode))
                {
                    this.ClassCode = ClassCode;
                }
                if (!string.IsNullOrEmpty(SecCode))
                {
                    this.SecCode = SecCode;
                }
            }
        }

        public static void OrderManagerTest()
        {
            void OnConnectedToQuik(int port)
            {
                Console.WriteLine("EVENT: Соединение с терминалом Quik установлено. Port:" + port.ToString());
            }

            void OnDisconnectedQuik()
            {
                Console.WriteLine("EVENT: Соединение с терминалом Quik разорвано.");
            }

            Quik quik = new Quik
            {
                DefaultSendTimeout = new TimeSpan(0, 0, 0, 10, 0)
            };

            quik.Events.OnConnectedToQuik += OnConnectedToQuik;
            quik.Events.OnDisconnectedFromQuik += OnDisconnectedQuik;

            bool isServiceConnected = quik.IsServiceConnected;
            Console.WriteLine("Соединение с терминалом QUIK " + (isServiceConnected ? "" : "НЕ ") + "установлено.");

            string SecCode = "VBM1";
            string ClassCode = "SPBFUT";
            string AccountID = "SPBFUT00C72";

            if (isServiceConnected)
            {
                try
                {
                    bool isServerConnected = quik.Service.IsConnected(1000).Result;
                    Console.WriteLine("Соединение с сервером " + (isServerConnected ? "" : "НЕ ") + "установлено.");
                    //if (!isServerConnected) return;

                    var ins = new TestQ(quik, ClassCode, SecCode, AccountID);
                    var om = new QOrdersManager(quik, 20000);

                    /// --- print result ---
                    /// 
                    Console.WriteLine($"LimitOrders count:{om.limit_orders.Count} StopOrders count: {om.stop_orders.Count} Trades count: {om.limit_trades.Count}\n");
                    foreach (var kv in om.limit_orders)
                    {
                        var order = kv.Value;
                        Console.WriteLine($"LIMIT Order {order.Operation} [{order.Qty}]{order.QtyLeft}+{order.QtyTraded} on {order.Price}. OrderNum: {order.OrderNum} LinkedWith: {order.LinkedOrder?.OrderNum} LinkedRole: {order.LinkedRole}");
                    }
                    foreach (var kv in om.stop_orders)
                    {
                        var order = kv.Value;
                        Console.WriteLine($"STOP Order {order.Operation} [{order.Qty}]{order.QtyLeft}+{order.QtyTraded} on {order.Price}. OrderNum: {order.OrderNum} CoOrder: {order.CoOrderNum} LinkedWith: {order.ChildLimitOrder?.OrderNum}");
                    }

                    Task.Delay(300000).Wait();
                }
                catch (TaskCanceledException) { }
                catch (OperationCanceledException) { }
                catch (System.TimeoutException e)
                {
                    Console.WriteLine("TimeoutException: " + e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public static void OrderManagerTest2()
        {
            void OnConnectedToQuik(int port)
            {
                Console.WriteLine("EVENT: Соединение с терминалом Quik установлено. Port:" + port.ToString());
            }

            void OnDisconnectedQuik()
            {
                Console.WriteLine("EVENT: Соединение с терминалом Quik разорвано.");
            }

            Quik quik = new Quik
            {
                DefaultSendTimeout = new TimeSpan(0, 0, 0, 10, 0)
            };

            quik.Events.OnConnectedToQuik += OnConnectedToQuik;
            quik.Events.OnDisconnectedFromQuik += OnDisconnectedQuik;

            bool isServiceConnected = quik.IsServiceConnected;
            Console.WriteLine("Соединение с терминалом QUIK " + (isServiceConnected ? "" : "НЕ ") + "установлено.");

            string SecCode = "VBM1";
            string ClassCode = "SPBFUT";
            string AccountID = "SPBFUT00C72";

            if (isServiceConnected)
            {
                try
                {
                    bool isServerConnected = quik.Service.IsConnected(1000).Result;
                    Console.WriteLine("Соединение с сервером " + (isServerConnected ? "" : "НЕ ") + "установлено.");
                    //if (!isServerConnected) return;

                    var ins = new TestQ(quik, ClassCode, SecCode, AccountID);
                    var om = new QOrdersManager(quik, 20000);
                    var orders = new QOrder[1];
                    try
                    {
                        var cancel = new CancellationTokenSource(40000);
                        var tcs = new TaskCompletionSource<bool>();
                        for (int i = 0; i < orders.Length; i++)
                        {
                            //var order = new QStopOrderWLinked(ins, QuikSharp.DataStructures.Operation.Sell, 4988, 4975, 4972, 1);
                            //                            var order = new QStopOrderWLinked(ins, QuikSharp.DataStructures.Operation.Buy, 4952, 4970, 4972, 1);
                            /*                            {
                                                            var order = new QLimitOrder(ins, Operation.Buy, 4930, 1);
                                                            order.OnPlaced += (sender) =>
                                                            {
                                                                Console.WriteLine($"Order {sender.Operation} {sender.Qty} on {sender.Price} placed OrderNum: {sender.OrderNum} ");
                                                                var tpsl = new QTPSLOrder(ins, Operation.Sell, 4970, 4925, null, 0, null, 1);                                    
                                                                tpsl.CoOrderNum = sender.OrderNum.Value;

                                                                tpsl.OnPlaced += (sender2) =>
                                                                {
                                                                    var asSL = (IStopOrder)sender2;
                                                                    var asTP = (ITakeOrder)sender2;
                                                                    Console.WriteLine($"Order {sender2.Operation} {sender2.Qty} on StopPrice: {asSL.StopPrice} TP: {asTP.TakePrice} placed OrderNum: {sender2.OrderNum}");
                                                                    tcs.TrySetResult(true);
                                                                };
                                                                om.RequestPlaceOrder(tpsl);
                                                            };

                                                            om.RequestPlaceOrder(order);
                                                            orders[i] = order;
                                                        }
                            */

                            var order = new QSimpleStopOrder(ins, Operation.Sell, 4953, 4956, 1);
                            order.OnPlaced += (sender) =>
                            {
                                Console.WriteLine($"Order {sender.Operation} {sender.Qty} on {sender.Price} placed OrderNum: {sender.OrderNum} ");
                            };
                            //om.RequestPlaceOrder(order);
                            //orders[i] = order;
                        }

                        tcs.Task.Wait(cancel.Token);
                    }
                    catch (TaskCanceledException) { }
                    catch (OperationCanceledException) { }

                    Task.Delay(5000).Wait();

                    // ---- kill orders ----
/*                    try
                    {
                        var cancel = new CancellationTokenSource(40000);
                        var tcs = new TaskCompletionSource<bool>();
                        foreach (var order in orders)
                        {
                            order.OnKilled += (sender) =>
                            {
                                Console.WriteLine($"Order {order.Operation} {order.Qty} on {order.Price} KILLED OrderNum: {order.OrderNum} ");
                                tcs.TrySetResult(true);
                            };
                            om.RequestKillOrder(order);
                        }
                        tcs.Task.Wait(cancel.Token);
                    }
                    catch (TaskCanceledException) { }
*/

                    /// --- print result ---
                    foreach (var kv in om.limit_orders)
                    {
                        var order = kv.Value;
                        Console.WriteLine($"LIMIT Order {order.Operation} [{order.Qty}]{order.QtyLeft}+{order.QtyTraded} on {order.Price}. OrderNum: {order.OrderNum} LinkedWith: {order.LinkedOrder?.OrderNum} LinkedRole: {order.LinkedRole}");
                    }
                    foreach (var kv in om.stop_orders)
                    {
                        var order = kv.Value;
                        Console.WriteLine($"STOP Order {order.Operation} [{order.Qty}]{order.QtyLeft}+{order.QtyTraded} on {order.Price}. OrderNum: {order.OrderNum} CoOrder: {order.CoOrderNum} LinkedWith: {order.ChildLimitOrder?.OrderNum}");
                    }

                    Task.Delay(300000).Wait();
                }
                catch (System.TimeoutException e)
                {
                    Console.WriteLine("TimeoutException: " + e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [STAThread]
        public static void Main()
        {
            Configure_Nlog(LogLevel.Debug);
            Log.Info("Program started.");

            //OrderManagerTest();

            //LuaNewTransactionIDTest();

            //TransactionTest();
            //OrdersTest();
            //ConnectionTest();
            Ping();
            //GetOrderTest();
            EchoTransaction();
            Console.WriteLine("Finished");
            Console.ReadKey();
        }
    }
}