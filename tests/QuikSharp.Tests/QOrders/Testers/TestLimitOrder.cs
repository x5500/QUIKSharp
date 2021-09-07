using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using QUIKSharp.Functions;
using QUIKSharp.DataStructures;
using QUIKSharp.QOrders;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.QOrders.Tests.Offline;
using QUIKSharp;
using QUIKSharp.TestQuik;

namespace QuikSharp.Tests.QOrders.Testers
{
    internal class TestLimitOrder : TestOrderBase
    {
        internal Order order;

        public static TestLimitOrder New(string SecCode, string ClassCode, TestQuikEmulator emulator)
        {
            var isec = GetUSec(SecCode, ClassCode);
            return new TestLimitOrder(isec, emulator);
        }

        protected TestLimitOrder(ITradeSecurity isec, TestQuikEmulator emulator) : base(emulator)
        {
            Qorder = new QLimitOrder(isec, Operation.Sell, 122.33m, 100);
        }


        internal async Task<TestQuikEmulatorReply> PlaceOrder(QOrdersManager manager)
        {
            InitTCS();

            var cancel = new CancellationTokenSource(timeout);
            var tcs = new TaskCompletionSource<bool>();
            Qorder.OnPlaced += (sender) =>  tcs.TrySetResult(true);

            var tcs2 = new TaskCompletionSource<TestQuikEmulatorReply>();
            
            Emulator.AddAwaiter(Qorder.ClientCode, tcs2);
            var r = manager.PlaceOrderAsync(Qorder, cancel.Token);

            try
            {
                await Task.Run(() => Task.WhenAll(new Task[] { r, tcs.Task, tcs2.Task }), cancel.Token);
            }
            catch (TimeoutException) { };

            Assert.IsTrue(r.Status == TaskStatus.RanToCompletion, "Task PlaceOrderAsync not completed!");
            Assert.IsTrue(r.Result.Result, "PlaceOrderAsync result == false");

            Assert.IsTrue(tcs2.Task.Status == TaskStatus.RanToCompletion, "Place order failed: Failed to reply on transaction!");

            order = tcs2.Task.Result.limitOrder;
            TransId = tcs2.Task.Result.TransId;

            Assert.IsTrue(tcs.Task.Status == TaskStatus.RanToCompletion, "Place order failed: No OnPlaced event called!");

            Assert.IsTrue(Qorder.State == QOrderState.Placed, $"qOrder.State ({Qorder.State}) != Placed");

            Assert.IsTrue(order != null, "Failed to reply on transaction...");


            return tcs2.Task.Result;
        }
    }
}