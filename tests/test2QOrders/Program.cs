using System;
using System.Threading.Tasks;
using QUIKSharp.QOrders;
using QUIKSharp.TestQuik;
using QUIKSharp.DataStructures;
using QuikSharp.Tests.QOrders.Testers;
using NLog;

namespace test2QOrders
{
    public static class Assert
    {
        public static void IsTrue(bool cond, string text = "")
        {
            if (cond)
                throw new Exception(text);
        }
    }
    public static class Program
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static TestQuikEmulator emulator;
        static QOrdersManager om;

        static void Main(string[] args)
        {
            Configure_Nlog(LogLevel.Trace);
            Log.Info("Program started.");

            QUIKSharp.QOrders.Tests.Offline.TestOrderBase.timeout = new TimeSpan(0, 0, 200);

            emulator = new TestQuikEmulator();
            om = new QOrdersManager(emulator, 1);

            Task.Run(() => CoOrder_CancelPartial())               
                .ContinueWith((t) => Console.WriteLine("Finished"));

            //Task.Run(QSimpleStopOrder_CancelLinkedEmpty);


            Console.ReadKey();
        }


        static public async Task CoOrder_FullFilled()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "CoOrder_FullFilled", emulator);

            await test.PlaceOrder(om);
            // -------------------------------- ------------------------------------------------
            // Trade test
            test.InitTCS();
            test.Trade(20);
            emulator.CallTradeEvent(test.CoOrder, 222.333m, 20);

            await test.CheckEvents_WaitPartialOnly();

            // Fullfill all via trade
            test.InitTCS();

            var trade_qty = test.balance_qty;
            test.Trade(trade_qty);
            emulator.CallTradeEvent(test.CoOrder, 100.123m, trade_qty);

            test.StopOrder.Flags = StopOrderFlags.Limit | StopOrderFlags.Bit4 | StopOrderFlags.WithdrawOnExecuted;
            emulator.CallStopOrderEvent(test.StopOrder);

            await test.CheckEvents_WaitPartialAndFilled();

            Assert.IsTrue(test.Qorder.State == QOrderState.Filled, $"qOrder.State ({test.Qorder.State}) != Filled");
        }

        static public async Task CoOrder_Cancelled()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "CoOrder_Cancelled", emulator);

            await test.PlaceOrder(om);

            // -------------------------------- ------------------------------------------------
            // TEST co_order cancelled!
            test.CoOrder.Flags = OrderTradeFlags.Canceled | OrderTradeFlags.IsSell | OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice;
            emulator.CallOrderEvent(test.CoOrder);

            test.StopOrder.Flags = StopOrderFlags.Canceled | StopOrderFlags.Sell | StopOrderFlags.Limit | StopOrderFlags.Bit4;
            test.StopOrder.StopFlags = StopBehaviorFlags.ExpireEndOfDay;
            emulator.CallStopOrderEvent(test.StopOrder);

            await Task.Delay(10);
            await test.CheckEvents_WaitKilledOnly();

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");

        }

        static public async Task CoOrder_CancelPartial()
        {
            emulator.delay_tr = new TimeSpan(100);
            emulator.delay_lo = new TimeSpan(10);
            emulator.delay_so = new TimeSpan(20);

            var test = TestStopOrderwLinked.New("XXX", "CancelPartial", emulator);
            await test.PlaceOrder(om);
            // -------------------------------- ------------------------------------------------
            test.InitTCS();

            // partial test via order
            long trade_qty = 20;
            test.Trade(trade_qty);

            emulator.CallOrderEvent(test.CoOrder, test.balance_qty, State.Active);

            await test.CheckEvents_WaitPartialOnly();

            // 2. OnTrade в догонку, ожидаем отсутствие событий
            test.InitTCS();
            emulator.CallTradeEvent(test.CoOrder, 222.12m, trade_qty);

            await Task.Delay(100);

            await test.CheckEvents_WaitNoAny();

            // test partial fill via trade
            // 3. Трейд на 23qty через OnTrade

            trade_qty = 23;
            test.Trade(trade_qty);
            test.InitTCS();
            emulator.CallTradeEvent(test.CoOrder, 222.12m, trade_qty);

            await test.CheckEvents_WaitPartialOnly();

            // 4. Test cancel via OrderEvent w Traded Qty

            trade_qty = 33;
            test.Trade(trade_qty);
            test.InitTCS();
            emulator.CallOrderEvent(test.CoOrder, test.balance_qty, State.Canceled);

            await test.CheckEvents_WaitPartialOnly();

            test.InitTCS();
            // 5. OnTrade в догонку, ожидаем отсутствие событий
            test.InitTCS();
            emulator.CallTradeEvent(test.CoOrder, 222.12m, trade_qty);

            await test.CheckEvents_WaitNoAny();

            test.InitTCS();

            // Событие об отмене стоп заявки
            // Ожидаем Killed
            test.StopOrder.Balance = test.balance_qty;
            test.SetStopOrderState(State.Canceled);
            test.CallStopOrderEvent();

            await test.CheckEvents_WaitKilledOnly();

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");
        }

        private static void Configure_Nlog(NLog.LogLevel loglevel)
        {
            var nlog_config = new NLog.Config.LoggingConfiguration();
            // Targets where to log to: Console
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole")
            {
                DetectConsoleAvailable = true,
                Layout = ">>[${level}] [${message}]"
            };

            nlog_config.AddTarget(logconsole);
            nlog_config.AddRule(loglevel, LogLevel.Fatal, logconsole);

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
    }
}