using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using QUIKSharp.Functions;
using QUIKSharp.DataStructures;
using QUIKSharp.QOrders.Tests.Offline;
using QUIKSharp.QOrders;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.TestQuik;

namespace QuikSharp.Tests.QOrders.Testers
{
    public class BasicLimitOrderTest
    {
        private readonly TestOrderBase test;
        private readonly Order limitOrder;
        public BasicLimitOrderTest(TestOrderBase test, Order limitOrder)
        {
            this.test = test;
            this.limitOrder = limitOrder;
        }

        /// <summary>
        /// 1. Трейд на 20qty через OnTrade event
        /// 2. Трейд на остаток через OnTrade event
        /// Ожидаем: OnPartial, OnFilled
        /// </summary>
        public async Task QLimitTest_FullFill1(TestQuikEmulator emulator)
        {
            // Trade test
            test.InitTCS();
            test.Trade(20);
            emulator.CallTradeEvent(limitOrder, 222.333m, 20);

            await test.CheckEvents_WaitPartialOnly().ConfigureAwait(true);

            // Fullfill all via trade
            test.InitTCS();

            var trade_qty = test.balance_qty;
            test.Trade(trade_qty);

            emulator.CallTradeEvent(limitOrder, 100.123m, trade_qty);

            await test.CheckEvents_WaitPartialAndFilled().ConfigureAwait(true);

            Assert.IsTrue(test.Qorder.State == QOrderState.Filled, $"qOrder.State ({test.Qorder.State}) != Filled");
        }

        /// <summary>
        /// 1.Трейд 20qty через OnOrder
        /// Ожидаем OnPartial
        /// 2. OnTrade в догонку, ожидаем отсутствие событий
        /// 3. Трейд на 11qty через OnTrade
        /// Ожидаем OnPartial
        /// 4. Проторговываем остаток через OnTrade
        /// Ожидаем: OnPartial, OnFilled
        /// </summary>
        public async Task QLimitTest_FullFill2(TestQuikEmulator emulator)
        {
            // partial test via order
            // 1. Трейд 20qty через OnOrder

            long trade_qty = 20;
            test.Trade(trade_qty);

            test.InitTCS();
            emulator.CallOrderEvent(limitOrder, test.balance_qty, State.Active);

            await test.CheckEvents_WaitPartialOnly().ConfigureAwait(true);

            // 2. OnTrade в догонку, ожидаем отсутствие событий
            test.InitTCS();
            emulator.CallTradeEvent(limitOrder, 222.12m, trade_qty);

            await test.CheckEvents_WaitNoAny().ConfigureAwait(true);

            // test partial fill via trade
            // 3. Трейд на 11qty через OnTrade

            trade_qty = 11;
            test.Trade(trade_qty);

            test.InitTCS();
            emulator.CallTradeEvent(limitOrder, 222.12m, trade_qty);

            await test.CheckEvents_WaitPartialOnly().ConfigureAwait(true);

            // Test fulfill via trade
            // Проторговываем остаток через OnTrade

            trade_qty = test.balance_qty;
            test.Trade(trade_qty);

            test.InitTCS();
            emulator.CallTradeEvent(limitOrder, 222.12m, trade_qty);

            await test.CheckEvents_WaitPartialAndFilled().ConfigureAwait(true);

            Assert.IsTrue(test.Qorder.State == QOrderState.Filled, $"qOrder.State ({test.Qorder.State}) != Filled");
        }

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
        public async Task QLimitTest_FullFill3(TestQuikEmulator emulator)
        {
            /// 1.Трейд 20qty через OnOrder
            /// Ожидаем OnPartial
            long trade_qty = 20;
            test.Trade(trade_qty);

            test.InitTCS();
            emulator.CallOrderEvent(limitOrder, test.balance_qty, State.Active);

            await test.CheckEvents_WaitPartialOnly().ConfigureAwait(true);

            // Запаздывающее событие OnTrade вдогонку
            emulator.CallTradeEvent(limitOrder, 222.12m, trade_qty);

            await Task.Delay(10).ConfigureAwait(true);

            // test partial fill via trade
            // 2. Трейд на 13 qty через OnTrade

            trade_qty = 13;
            test.Trade(trade_qty);

            test.InitTCS();
            emulator.CallTradeEvent(limitOrder, 222.12m, trade_qty);

            await test.CheckEvents_WaitPartialOnly().ConfigureAwait(true);

            // 3. Проторговываем остаток через OnOrder
            // Ожидаем: OnPartial, OnFilled

            trade_qty = test.balance_qty;
            test.Trade(trade_qty);
            test.InitTCS();
            emulator.CallOrderEvent(limitOrder, 0, State.Completed);

            await test.CheckEvents_WaitPartialAndFilled().ConfigureAwait(true);

            // Запаздывающее событие OnTrade вдогонку
            test.InitTCS();
            emulator.CallTradeEvent(limitOrder, 222.12m, trade_qty);

            await Task.Delay(100).ConfigureAwait(true);

            /// Ожидаем: отсутствие событий
            await test.CheckEvents_WaitNoAny().ConfigureAwait(true);
            Assert.IsTrue(test.Qorder.State == QOrderState.Filled, $"qOrder.State ({test.Qorder.State}) != Filled");
        }

        /// <summary>
        /// 1.Трейд 20qty через OnOrder
        /// Ожидаем OnPartial
        /// 2. OnTrade в догонку, ожидаем отсутствие событий
        /// 3. Трейд на 23qty через OnTrade
        /// Ожидаем OnPartial (23)
        /// 4. OnOrder событие со снятием ордера и дополнительно проторгованным обьемом
        /// Ожидаем: OnPartial, OnKilled
        /// </summary>
        public async Task  QLimitTest_CancelPartial1(TestQuikEmulator emulator)
        {
            // partial test via order
            long trade_qty = 20;
            test.Trade(trade_qty);

            test.InitTCS();
            emulator.CallOrderEvent(limitOrder, test.balance_qty, State.Active);

            await test.CheckEvents_WaitPartialOnly().ConfigureAwait(true);

            // 2. OnTrade в догонку, ожидаем отсутствие событий
            test.InitTCS();
            emulator.CallTradeEvent(limitOrder, 222.12m, trade_qty);

            await Task.Delay(100).ConfigureAwait(true);

            await test.CheckEvents_WaitNoAny().ConfigureAwait(true);

            // test partial fill via trade
            // 3. Трейд на 23qty через OnTrade

            trade_qty = 23;
            test.Trade(trade_qty);
            test.InitTCS();
            emulator.CallTradeEvent(limitOrder, 222.12m, trade_qty);

            await test.CheckEvents_WaitPartialOnly().ConfigureAwait(true);

            // 4. Test cancel via OrderEvent w Traded Qty

            trade_qty = 33;
            test.Trade(trade_qty);
            test.InitTCS();
            emulator.CallOrderEvent(limitOrder, test.balance_qty, State.Canceled);

            await test.CheckEvents_WaitPartialAndKilled().ConfigureAwait(true);

            // 5. OnTrade в догонку, ожидаем отсутствие событий
            test.InitTCS();
            emulator.CallTradeEvent(limitOrder, 222.12m, trade_qty);

            await test.CheckEvents_WaitNoAny().ConfigureAwait(true);

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");

        }

        /// <summary>
        /// 1.Трейд 20qty
        /// Ожидаем OnPartial
        /// 2. OnOrder событие со снятием ордера
        /// Ожидаем: OnKilled
        /// </summary>
        public async Task QLimitTest_CancelPartial2(TestQuikEmulator emulator)
        {
            // partial test via order
            long trade_qty = 20;
            test.Trade(trade_qty);

            test.InitTCS();
            emulator.CallTradeEvent(limitOrder, 222.12m, trade_qty);
            emulator.CallOrderEvent(limitOrder, test.balance_qty, State.Active);
            await test.CheckEvents_WaitPartialOnly().ConfigureAwait(true);

            // 2. Ожидаем отсутствие событий
            test.InitTCS();
            await Task.Delay(100).ConfigureAwait(true);
            await test.CheckEvents_WaitNoAny().ConfigureAwait(true);

            emulator.CallOrderEvent(limitOrder, test.balance_qty, State.Canceled);

            await test.CheckEvents_WaitKilledOnly().ConfigureAwait(true);

            // 5. Ожидаем отсутствие событий
            test.InitTCS();
            await test.CheckEvents_WaitNoAny().ConfigureAwait(true);

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");

        }

        /// <summary>
        /// Снимаем не исполненный ордер
        /// Ожидаем: OnKilled
        /// </summary>
        public async Task QLimitTest_CancelEmpty(TestQuikEmulator emulator)
        {
            // Test cancel via OrderEvent
            test.InitTCS();
            emulator.CallOrderEvent(limitOrder, test.balance_qty, State.Canceled);

            await test.CheckEvents_WaitKilledOnly().ConfigureAwait(true);

            Assert.IsTrue(test.Qorder.State == QOrderState.Killed, $"qOrder.State ({test.Qorder.State}) != Killed");

        }
    }
}