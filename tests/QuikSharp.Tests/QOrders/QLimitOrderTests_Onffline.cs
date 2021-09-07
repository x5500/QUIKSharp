using NUnit.Framework;
using QUIKSharp.QOrders;
using System;
using System.Threading.Tasks;
using QuikSharp.Tests.QOrders.Testers;
using QUIKSharp.TestQuik;

namespace QuikSharp.Tests.QOrders
{
    [TestFixture()]
    public class QLimitOrderTests_Onffline
    {
        TestQuikEmulator emulator;
        QOrdersManager om;
        TimeSpan timeout = new TimeSpan(0, 0, 2);

        [OneTimeSetUp]
        public void BeforeTestSuit()
        {
            emulator = new TestQuikEmulator();
            om = new QOrdersManager(emulator, 1);
        }

        [Test()]
        public async Task QLimitTest_PlaceOrder()
        {
            var test = TestLimitOrder.New("QLimitTest", "PlaceOrder", emulator);
            await test.PlaceOrder(om);
            // -------------------------------------------------------------------
        }

        [Test()]
        /// <summary>
        /// 1. Трейд на 20qty через OnTrade event
        /// 2. Трейд на остаток через OnTrade event
        /// Ожидаем: OnPartial, OnFilled
        /// </summary>
        public async Task QLimitTest_FullFill1()
        {
            var test = TestLimitOrder.New("QLimitTest", "FullFill1", emulator);
            await test.PlaceOrder(om);
            // -------------------------------------------------------------------
            var tester = new Testers.BasicLimitOrderTest(test, test.order);
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
        public async Task QLimitTest_FullFill2()
        {
            var test = TestLimitOrder.New("QLimitTest", "FullFill2", emulator);
            await test.PlaceOrder(om);
            // -------------------------------------------------------------------
            var tester = new Testers.BasicLimitOrderTest(test, test.order);
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
        public async Task QLimitTest__FullFill3()
        {
            var test = TestLimitOrder.New("QLimitTest", "FullFill3", emulator);
            await test.PlaceOrder(om);
            // -------------------------------------------------------------------
            var tester = new Testers.BasicLimitOrderTest(test, test.order);
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
        public async Task QLimitTest_CancelPartial1()
        {
            var test = TestLimitOrder.New("QLimitTest", "CancelPartial1", emulator);
            await test.PlaceOrder(om);
            // -------------------------------------------------------------------
            var tester = new Testers.BasicLimitOrderTest(test, test.order);
            await tester.QLimitTest_CancelPartial1(emulator);
        }

        [Test()]
        /// <summary>
        /// 1.Трейд 20qty
        /// Ожидаем OnPartial
        /// 2. OnOrder событие со снятием ордера
        /// Ожидаем: OnKilled
        /// </summary>
        public async Task QLimitTest_CancelPartial2()
        {
            var test = TestLimitOrder.New("QLimitTest", "CancelPartial2", emulator);
            await test.PlaceOrder(om);
            // -------------------------------------------------------------------
            var tester = new Testers.BasicLimitOrderTest(test, test.order);
            await tester.QLimitTest_CancelPartial2(emulator);
        }

        [Test()]
        /// <summary>
        /// Снимаем не исполненный даже частично ордер
        /// Ожидаем: OnKilled
        /// </summary>
        public async Task QLimitTest_CancelEmpty()
        {
            var test = TestLimitOrder.New("QLimitTest", "CancelEmpty", emulator);

            await test.PlaceOrder(om);
            // -------------------------------------------------------------------
            var tester = new Testers.BasicLimitOrderTest(test, test.order);
            await tester.QLimitTest_CancelEmpty(emulator);
        }

    }
}