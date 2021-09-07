using NUnit.Framework;
using QUIKSharp.QOrders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QUIKSharp.Functions;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.DataStructures;
using Newtonsoft.Json;
using QuikSharp.Tests.QOrders.Testers;
using QUIKSharp.TestQuik;

namespace QuikSharp.Tests.QOrders
{
    [TestFixture()]
    public partial class QStopOrderTests_Onffline
    {
        TestQuikEmulator emulator;
        QOrdersManager om;

        [OneTimeSetUp]
        public void BeforeTestSuit()
        {
            emulator = new TestQuikEmulator();
            om = new QOrdersManager(emulator, 1);
            TestStopOrder.timeout = new TimeSpan(0, 0, 2);
        }

        [Test()]
        /// <summary>
        /// Снимаем не исполненный ордер
        /// Ожидаем: OnKilled
        /// </summary>
        public async Task QSimpleStopOrder_CancelEmpty()
        {
            var test = TestStopOrder.New("QSimpleStopOrder", "CancelEmpty", emulator);

            await test.PlaceOrder(om);

            // Test cancel via OrderEvent
            test.InitTCS();

            test.SetStopOrderState(State.Canceled);

            await test.CheckEvents_WaitKilledOnly();

            test.InitTCS();
            await test.CheckEvents_WaitNoAny();

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");
        }


        [Test()]
        /// <summary>
        /// Срабатывает стоп-заявка, выставляется linked ордер
        /// Ожидаем: OnExecuted
        /// Снимаем не исполненный Linked ордер
        /// Ожидаем: OnKilled
        /// </summary>
        public async Task QSimpleStopOrder_CancelLinkedEmpty()
        {
            var test = TestStopOrder.New("QSimpleStopOrder", "CancelEmpty", emulator);
            await test.PlaceOrder(om);

            // -------------------------------------------------------------------
            // Test cancel via OrderEvent
            test.InitTCS();

            test.MakeLinkedOrder();
            emulator.SetOrderState(State.Active, test.LinkedOrder);
            emulator.CallOrderEvent(test.LinkedOrder);

            await Task.Delay(10);

            await test.CheckEvents_WaitExecutedOnly();

            test.InitTCS();

            emulator.SetOrderState(State.Canceled, test.LinkedOrder);
            emulator.CallOrderEvent(test.LinkedOrder);

            await test.CheckEvents_WaitKilledOnly();

            test.InitTCS();
            await test.CheckEvents_WaitNoAny();

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");
        }


        [Test()]
        /// <summary>
        /// Срабатывает стоп-заявка, выставляется linked ордер
        /// Ожидаем: OnExecuted
        /// Run: QLimitTest_FullFill
        /// </summary>
        public async Task QSimpleStopOrder_QLimitTest_FullFill1()
        {
            var test = TestStopOrder.New("QSimpleStopOrder", "CancelEmpty", emulator);
            await test.PlaceOrder(om);

            // -------------------------------------------------------------------
            await test.PlaceLinkedOrder();

            var tester = new BasicLimitOrderTest(test, test.LinkedOrder);
            await tester.QLimitTest_FullFill1(emulator);
        }

        [Test()]
        /// <summary>
        /// 1.Трейд 20qty через OnOrder
        /// Ожидаем OnPartial
        /// 2. OnTrade в догонку, ожидаем отсутствие событий
        /// 3. Трейд на 11qty через OnTrade
        /// Ожидаем OnPartial
        /// 4. Проторговываем остаток через OnTrade
        /// Ожидаем: OnPartial, OnFilled
        /// </summary>
        public async Task QSimpleStopOrder_QLimitTest_FullFill2()
        {
            var test = TestStopOrder.New("QSimpleStopOrder", "CancelEmpty", emulator);
            await test.PlaceOrder(om);

            // -------------------------------------------------------------------
            await test.PlaceLinkedOrder();

            var tester = new BasicLimitOrderTest(test, test.LinkedOrder);
            await tester.QLimitTest_FullFill2(emulator);
        }

        [Test()]
        /// <summary>
        /// 1.Трейд 20qty через OnOrder
        /// Ожидаем OnPartial
        /// 2. Запаздывающее событие OnTrade вдогонку
        /// 3. Трейд на  13 qty через OnTrade
        /// Ожидаем OnPartial (13)
        /// 4. Проторговываем остаток через OnOrder
        /// Ожидаем: OnPartial, OnFilled
        /// Запаздывающее событие OnTrade вдогонку
        /// Ожидаем: отсутствие событий
        /// </summary>
        public async Task QSimpleStopOrder_QLimitTest_FullFill3()
        {
            var test = TestStopOrder.New("QSimpleStopOrder", "CancelEmpty", emulator);
            await test.PlaceOrder(om);

            // -------------------------------------------------------------------
            await test.PlaceLinkedOrder();

            var tester = new BasicLimitOrderTest(test, test.LinkedOrder);
            await tester.QLimitTest_FullFill3(emulator);
        }


        [Test()]
        /// <summary>
        /// 1.Трейд 20qty через OnOrder
        /// Ожидаем OnPartial
        /// 2. OnTrade в догонку, ожидаем отсутствие событий
        /// 3. Трейд на 23qty через OnTrade
        /// Ожидаем OnPartial (23)
        /// 4. OnOrder событие со снятием ордера и дополнительно проторгованным обьемом
        /// Ожидаем: OnPartial, OnKilled
        /// </summary>
        public async Task QSimpleStopOrder_QLimitTest_CancelPartial1()
        {
            var test = TestStopOrder.New("QSimpleStopOrder", "CancelEmpty", emulator);
            await test.PlaceOrder(om);

            // -------------------------------------------------------------------
            await test.PlaceLinkedOrder();

            var tester = new BasicLimitOrderTest(test, test.LinkedOrder);
            await tester.QLimitTest_CancelPartial1(emulator);
        }

        [Test()]
        /// <summary>
        /// 1.Трейд 20qty
        /// Ожидаем OnPartial
        /// 2. OnOrder событие со снятием ордера
        /// Ожидаем: OnKilled
        /// </summary>
        public async Task QSimpleStopOrder_QLimitTest_CancelPartial2()
        {
            var test = TestStopOrder.New("QSimpleStopOrder", "CancelEmpty", emulator);
            await test.PlaceOrder(om);

            // -------------------------------------------------------------------
            await test.PlaceLinkedOrder();

            var tester = new BasicLimitOrderTest(test, test.LinkedOrder);
            await tester.QLimitTest_CancelPartial2(emulator);
        }

        [Test()]
        /// <summary>
        /// Снимаем не исполненный даже частично ордер
        /// Ожидаем: OnKilled
        /// </summary>
        public async Task QSimpleStopOrder_QLimitTest_CancelEmpty()
        {
            var test = TestStopOrder.New("QSimpleStopOrder", "CancelEmpty", emulator);
            await test.PlaceOrder(om);

            // -------------------------------------------------------------------
            await test.PlaceLinkedOrder();

            var tester = new BasicLimitOrderTest(test, test.LinkedOrder);
            await tester.QLimitTest_CancelEmpty(emulator);
        }
    }
}