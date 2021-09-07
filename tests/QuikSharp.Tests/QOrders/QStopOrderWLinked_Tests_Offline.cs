using NUnit.Framework;
using QUIKSharp.QOrders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QUIKSharp.Functions;
using QUIKSharp.DataStructures;
using Newtonsoft.Json;
using QuikSharp.Tests.QOrders.Testers;
using QUIKSharp.TestQuik;

namespace QuikSharp.Tests.QOrders
{
    [TestFixture()]
    public class QStopOrderWLinked_Tests_Offline
    {

        TestQuikEmulator emulator;
        QOrdersManager om;

        [OneTimeSetUp]
        public void BeforeTestSuit()
        {
            emulator = new TestQuikEmulator();
            om = new QOrdersManager(emulator, 1);
            TestStopOrderwLinked.timeout = new TimeSpan(0, 0, 20);
        }

        [Test()]
        public async Task PlaceOrder()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "PlaceOrder", emulator);

            await test.PlaceOrder(om);
        }

        [Test()]
        public async Task CoOrder_Cancelled()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "CoOrder_Cancelled", emulator);
            emulator.delay_lo = new TimeSpan(10);
            emulator.delay_so = new TimeSpan(10);
            emulator.delay_tr = new TimeSpan(500);

            await test.PlaceOrder(om);

            // -------------------------------- ------------------------------------------------
            // TEST co_order cancelled!
            test.CoOrder.Flags = OrderTradeFlags.Canceled | OrderTradeFlags.IsSell | OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice;
            emulator.CallOrderEvent(test.CoOrder);

            test.StopOrder.Flags = StopOrderFlags.Canceled | StopOrderFlags.Sell |StopOrderFlags.Limit | StopOrderFlags.Bit4;
            test.StopOrder.StopFlags = StopBehaviorFlags.ExpireEndOfDay;
            emulator.CallStopOrderEvent(test.StopOrder);

            await Task.Delay(10);
            await test.CheckEvents_WaitKilledOnly();

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");

        }

        [Test()]
        public async Task CoOrder_Cancelled2()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "CoOrder_Cancelled2", emulator);

            await test.PlaceOrder(om);

            // -------------------------------- ------------------------------------------------
            // TEST co_order cancelled!
            // Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnStopOrder: OrderNum: 206921562,
            // TransId: 18811538, Qty: 50 | 50 Status: Canceled, Flags: Canceled, Sell, Limit, Bit4, StopFlags: ExpireEndOfDay, CoOrder: 25887487399
            test.StopOrder.Balance = test.StopOrder.Quantity;
            test.StopOrder.Flags = StopOrderFlags.Canceled | StopOrderFlags.Sell | StopOrderFlags.Limit | StopOrderFlags.Bit4;
            test.StopOrder.StopFlags = StopBehaviorFlags.ExpireEndOfDay;
            emulator.CallStopOrderEvent(test.StopOrder);
            //
            // Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 25887487399,
            // TransId: 18811538, Qty: 50 | 50 Status: Canceled, Flags: Canceled, IsSell, IsLimit, AllowDiffPrice, ExtFlags: 0, LinkedOrder: 0

            test.CoOrder.Flags = OrderTradeFlags.Canceled | OrderTradeFlags.IsSell | OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice;
            test.CoOrder.Balance = test.CoOrder.Quantity;
            emulator.CallOrderEvent(test.CoOrder);

            await test.CheckEvents_WaitKilledOnly();

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");

        }

        [Test()]
        public async Task CoOrder_Cancelled_by_linked_order()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "CoOrder_Cancelled_by_linked_order", emulator);

            await test.PlaceOrder(om);

            // -------------------------------- ------------------------------------------------
            // TEST co_order cancelled!
            // Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 25887487399,
            // TransId: 18811538, Qty: 50 | 50 Status: Canceled, Flags: Canceled, IsSell, IsLimit, AllowDiffPrice, ExtFlags: 0, LinkedOrder: 0

            test.CoOrder.Flags = OrderTradeFlags.Canceled | OrderTradeFlags.IsSell | OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice;
            test.CoOrder.Balance = test.CoOrder.Quantity;
            emulator.CallOrderEvent(test.CoOrder);

            await test.CheckEvents_WaitKilledOnly();

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");

        }

        [Test()]
        public async Task CoOrder_RejectedOnLimits()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "CoOrder_RejectedOnLimits", emulator);

            await test.PlaceOrder(om);

            // -------------------------------- ------------------------------------------------
            // Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnStopOrder: OrderNum: 206921920,
            // TransId: 18811569, Qty: 50 | 50 Status: Completed, Flags: Sell, Limit, Bit4, RejectedOnLimits, StopFlags: ExpireEndOfDay, CoOrder: 25887583348
            test.StopOrder.Balance = test.StopOrder.Quantity;
            test.StopOrder.Flags = StopOrderFlags.Sell | StopOrderFlags.Limit | StopOrderFlags.Bit4 | StopOrderFlags.RejectedOnLimits;
            test.StopOrder.StopFlags = StopBehaviorFlags.ExpireEndOfDay;
            emulator.CallStopOrderEvent(test.StopOrder);

            // Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 25887583348,
            // TransId: 18811569, Qty: 50 | 50 Status: Canceled, Flags: Canceled, IsSell, IsLimit, AllowDiffPrice, ExtFlags: 0, LinkedOrder: 0
            test.CoOrder.Flags = OrderTradeFlags.Canceled | OrderTradeFlags.IsSell | OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice;
            test.CoOrder.Balance = test.CoOrder.Quantity;
            emulator.CallOrderEvent(test.CoOrder);

            await test.CheckEvents_WaitKilledOnly();

            Assert.IsTrue(test.Qorder.State == QOrderState.ErrorRejected, $"qOrder.State ({test.Qorder.State}) != ErrorRejected");

        }


        [Test()]
        public async Task CoOrder_FullFill2()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "CoOrder_FullFill2", emulator);

            await test.PlaceOrder(om);
            // -------------------------------- ------------------------------------------------
            test.InitTCS();
            test.StopOrder.Flags = StopOrderFlags.Limit | StopOrderFlags.Bit4;
            emulator.CallStopOrderEvent(test.StopOrder);

            var tester = new BasicLimitOrderTest(test, test.CoOrder);
            await tester.QLimitTest_FullFill2(emulator);
        }

        [Test()]
        public async Task CoOrder_Partial2()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "CoOrder_Partial2", emulator);

            await test.PlaceOrder(om);
            // -------------------------------- ------------------------------------------------
            test.InitTCS();
            test.StopOrder.Flags = StopOrderFlags.Limit | StopOrderFlags.Bit4;
            emulator.CallStopOrderEvent(test.StopOrder);

            var tester = new BasicLimitOrderTest(test, test.CoOrder);
            await tester.QLimitTest_FullFill3(emulator);
        }

        [Test()]
        public async Task CoOrder_FullFilled()
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


        [Test()]
        public async Task CoOrder_CancelPartial()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "CoOrder_CancelPartial", emulator);

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

        [Test()]
        public async Task Linked_CancelEmpty()
        {
            /*
             * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 1952337518234203614,
             * TransId: 61, Qty:1|1 Status: Active, Flags: Active, IsLimit, AllowDiffPrice, ExtFlags: 0, LinkedOrder: 0 
             * 
             * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnStopOrder: OrderNum: 1506030,
             * TransId: 61, Qty:1|1 Status: Active, Flags: Active, Limit, Bit4, StopFlags: ExpireEndOfDay, CoOrder: 1952337518234203614
             * 
             * 
             * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnStopOrder: OrderNum: 1506030,
             * TransId: 61, Qty:1|0 Status: Completed, Flags: Limit, Bit4, StopFlags: ExpireEndOfDay, CoOrder: 1952337518234203614
             * 
             * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 1952337518234203614,
             * TransId: 61, Qty:1|1 Status: Canceled, Flags: Canceled, IsLimit, AllowDiffPrice, ExtFlags: 0, LinkedOrder: 0 
             * 
             * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 1952337518234203867,
             * TransId: 61, Qty:1|1 Status: Active, Flags: Active, IsLimit, AllowDiffPrice, LinkedOrder, ExtFlags: 0, LinkedOrder: 1506030 
             * 
             * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnStopOrder: OrderNum: 1506030,
             * TransId: 61, Qty:1|0 Status: Completed, Flags: Limit, Bit4, StopFlags: ExpireEndOfDay, CoOrder: 1952337518234203614
             *
             * Info: TestQOrders.TestQuik.QuikEmulator >> Mark: Стоп заявка со связанной лимитной заявкой сработала по стоп заявке, была выставлена лимитная сервером квик, но она не сработала.
             * 
             * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 1952337518234203867,
             * TransId: 61, Qty:1|1 Status: Canceled, Flags: Canceled, IsLimit, AllowDiffPrice, LinkedOrder, ExtFlags: 0, LinkedOrder: 1506030 
             */

            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "Linked_CancelEmpty", emulator);

            await test.PlaceOrder(om);

            // -------------------------------- ------------------------------------------------
            test.InitTCS();
            test.StopOrder.Flags = StopOrderFlags.Limit | StopOrderFlags.Bit4;
            test.StopOrder.StopFlags = StopBehaviorFlags.ExpireEndOfDay;
            test.StopOrder.Balance = 0;
            emulator.CallStopOrderEvent(test.StopOrder);

            test.CoOrder.Flags = OrderTradeFlags.IsLimit | OrderTradeFlags.Canceled | OrderTradeFlags.AllowDiffPrice;
            emulator.CallOrderEvent(test.CoOrder);

            test.MakeLinkedOrder();
            test.LinkedOrder.Flags = OrderTradeFlags.Active | OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice | OrderTradeFlags.LinkedOrder;
            emulator.CallOrderEvent(test.LinkedOrder);

            emulator.SetOrderState(State.Completed, test.StopOrder);
            test.CallStopOrderEvent();

            await test.CheckEvents_WaitExecutedOnly();
            // Linked order placed...

            Assert.IsTrue(test.Qorder.State == QOrderState.Executed, $"qOrder.State ({test.Qorder.State}) != Executed");

            test.InitTCS();

            // Test cancel via OrderEvent
            // Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 1952337518234203867,
            // TransId: 61, Qty:1|1 Status: Canceled, Flags: Canceled, IsLimit, AllowDiffPrice, LinkedOrder, ExtFlags: 0, LinkedOrder: 1506030 

            test.LinkedOrder.Flags = OrderTradeFlags.Canceled | OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice | OrderTradeFlags.LinkedOrder;
            emulator.CallOrderEvent(test.LinkedOrder);

            await test.CheckEvents_WaitKilledOnly();

            test.InitTCS();
            await test.CheckEvents_WaitNoAny();

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");
        }

        [Test()]
        public async Task Linked_Partial1()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "Linked_Partial1", emulator);

            await test.PlaceOrder(om);

            test.InitTCS();
            // -------------------------------- ------------------------------------------------
            test.InitTCS();
            test.StopOrder.Flags = StopOrderFlags.Limit | StopOrderFlags.Bit4;
            test.StopOrder.StopFlags = StopBehaviorFlags.ExpireEndOfDay;
            test.StopOrder.Balance = 0;
            emulator.CallStopOrderEvent(test.StopOrder);

            test.CoOrder.Flags = OrderTradeFlags.IsLimit | OrderTradeFlags.Canceled | OrderTradeFlags.AllowDiffPrice;
            emulator.CallOrderEvent(test.CoOrder);

            test.MakeLinkedOrder();
            test.LinkedOrder.Flags = OrderTradeFlags.Active | OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice | OrderTradeFlags.LinkedOrder;
            emulator.CallOrderEvent(test.LinkedOrder);

            emulator.SetOrderState(State.Completed, test.StopOrder);
            test.CallStopOrderEvent();

            await test.CheckEvents_WaitExecutedOnly();
            // Linked order placed...

            test.InitTCS();
            var tester = new BasicLimitOrderTest(test, test.LinkedOrder);
            await tester.QLimitTest_FullFill2(emulator);

            test.InitTCS();
            await test.CheckEvents_WaitNoAny();

        }

        [Test()]
        public async Task Linked_Partial2()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "Linked_Partial2", emulator);

            await test.PlaceOrder(om);

            test.InitTCS();
            // -------------------------------- ------------------------------------------------
            test.InitTCS();
            test.StopOrder.Flags = StopOrderFlags.Limit | StopOrderFlags.Bit4;
            test.StopOrder.StopFlags = StopBehaviorFlags.ExpireEndOfDay;
            test.StopOrder.Balance = 0;
            emulator.CallStopOrderEvent(test.StopOrder);

            test.CoOrder.Flags = OrderTradeFlags.IsLimit | OrderTradeFlags.Canceled | OrderTradeFlags.AllowDiffPrice;
            emulator.CallOrderEvent(test.CoOrder);

            test.MakeLinkedOrder();
            test.LinkedOrder.Flags = OrderTradeFlags.Active | OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice | OrderTradeFlags.LinkedOrder;
            emulator.CallOrderEvent(test.LinkedOrder);

            emulator.SetOrderState(State.Completed, test.StopOrder);
            test.CallStopOrderEvent();

            await test.CheckEvents_WaitExecutedOnly();
            // Linked order placed...

            test.InitTCS();

            var tester = new BasicLimitOrderTest(test, test.LinkedOrder);
            await tester.QLimitTest_FullFill3(emulator);
        }

        [Test()]
        public async Task Linked_CancelPartial()
        {
            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "Linked_CancelPartial", emulator);

            await test.PlaceOrder(om);

            test.InitTCS();
            // -------------------------------- ------------------------------------------------
            // Стоп заявка со связанной лимитной заявкой сработала по стоп заявке
            test.MakeLinkedOrder();
            test.LinkedOrder.Flags = OrderTradeFlags.Active | OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice;
            test.LinkedOrder.Linkedorder = 0;
            test.LinkedOrder.Balance = test.balance_qty;

            long trade_qty = 11;
            test.Trade(trade_qty);
            test.LinkedOrder.Balance = test.balance_qty;
            emulator.CallTradeEvent(test.LinkedOrder, 222.333m, trade_qty);

            emulator.CallOrderEvent(test.LinkedOrder);

            test.LinkedOrder.Linkedorder = test.StopOrder.OrderNum;
            test.LinkedOrder.Flags |= OrderTradeFlags.LinkedOrder;
            emulator.CallOrderEvent(test.LinkedOrder);

            test.CoOrder.Flags = OrderTradeFlags.IsLimit | OrderTradeFlags.Canceled | OrderTradeFlags.AllowDiffPrice;
            emulator.CallOrderEvent(test.CoOrder);

            test.StopOrder.Balance = 0;
            test.StopOrder.Flags = StopOrderFlags.Limit | StopOrderFlags.Bit4;
            test.StopOrder.StopFlags = StopBehaviorFlags.ExpireEndOfDay;
            test.CallStopOrderEvent();

            await test.CheckEvents_WaitPartialAndExecuted();

            // Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 1952337518234203867, TransId: 61, Qty:1|1 Status: Canceled,
            // Flags: Canceled, IsLimit, AllowDiffPrice, LinkedOrder, ExtFlags: 0, LinkedOrder: 1506030 

            test.InitTCS();
            emulator.SetOrderState(State.Canceled, test.LinkedOrder);
            emulator.CallOrderEvent(test.LinkedOrder);

            await test.CheckEvents_WaitKilledOnly();

            test.InitTCS();
            await test.CheckEvents_WaitNoAny();

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");

        }


        /// <summary>
        /// Стоп заявка со связанной исполнена по стоп-заявке
        /// Ожидаем: OnExecuted, OnPartial, OnFilled
        /// </summary>
        [Test()]
        public async Task Linked_FullFiled()
        {
            /*
            * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnTrade: OrderNum: 1952337518234216762, TransId: 73, Kind: 1, Flags: IsSell, IsMarketMakerOrSent, TradeNum: 1952337518233937484,
            * Price: 4890, Qty: 2 OrderQty: 0
            * 
            * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 1952337518234216757, TransId: 73, Qty:2|2 Status: Canceled, Flags: Canceled, IsSell, IsLimit, AllowDiffPrice, ExtFlags: 0, LinkedOrder: 0 
            * 
            * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnOrder: OrderNum: 1952337518234216762, TransId: 73, Qty:2|0 Status: Completed, Flags: IsSell, IsLimit, AllowDiffPrice, LinkedOrder, ExtFlags: 0, LinkedOrder: 1506075 
            * 
            * Debug: QUIKSharp.QOrders.QOrdersManager >> Events_OnStopOrder: OrderNum: 1506075, TransId: 73, Qty:2|0 Status: Completed, Flags: Sell, Limit, Bit4, StopFlags: ExpireEndOfDay, CoOrder: 1952337518234216757
            */

            var test = TestStopOrderwLinked.New("TestStopOrderwLinked", "Linked_FullFiled", emulator);

            await test.PlaceOrder(om);
            // -------------------------------- ------------------------------------------------
            // Стоп заявка со связанной лимитной заявкой сработала по стоп заявке
            test.MakeLinkedOrder();
            test.LinkedOrder.Flags = OrderTradeFlags.IsLimit | OrderTradeFlags.AllowDiffPrice;

            // Trade test
            test.InitTCS();
            test.Trade(20);
            emulator.CallTradeEvent(test.LinkedOrder, 222.333m, 20);

            test.Trade(test.balance_qty);
            emulator.CallTradeEvent(test.LinkedOrder, 222.333m, test.balance_qty);

            emulator.SetOrderState(State.Canceled, test.CoOrder);
            emulator.CallOrderEvent(test.CoOrder);

            test.LinkedOrder.Balance = 0;
            emulator.SetOrderState(State.Completed, test.LinkedOrder);
            emulator.CallOrderEvent(test.LinkedOrder);

            test.StopOrder.Balance = 0;
            emulator.SetOrderState(State.Completed, test.StopOrder);
            test.CallStopOrderEvent();

            await test.CheckEvents_WaitPartialAndFilled();

            Assert.IsTrue(test.Qorder.State == QOrderState.Filled, $"qOrder.State ({test.Qorder.State}) != Filled");

        }

    }
}