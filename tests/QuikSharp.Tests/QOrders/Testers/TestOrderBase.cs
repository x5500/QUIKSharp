using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using QUIKSharp.Functions;
using QUIKSharp.DataStructures;
using QuikSharp.Tests.QOrders;
using QUIKSharp.TestQuik;

namespace QUIKSharp.QOrders.Tests.Offline
{
    public abstract class TestOrderBase
    {
        protected TestQuikEmulator Emulator { get; private set; }

        protected QOrder _qOrder;
        public QOrder Qorder { get => _qOrder; protected set => SetQOrder(value); }

        public static TimeSpan timeout = new TimeSpan(0, 0, 30);
        public long traded_qty = 0;
        public long balance_qty = 0;
        public long partialFill = 0;

        public TaskCompletionSource<bool> tcs_filled;
        public TaskCompletionSource<bool> tcs_killed;
        public TaskCompletionSource<bool> tcs_partial;

        public long count_filled = 0;
        public long count_killed = 0;
        public long count_placed = 0;

        private static long uuid = 12345;
        internal long TransId;
        protected static ITradeSecurity GetUSec(string SecCode, string ClassCode)
        {
            var id = Interlocked.Increment(ref uuid);
            var uid = "TEST_" + id.ToString();
            var usec = new UnattendedTradeSecurity()
            {
                ClassCode = ClassCode,
                SecCode = SecCode,
                AccountID = uid,
                ClientCode = uid,
                FirmId = "TEST",
            };
            return usec;
        }

        protected TestOrderBase(TestQuikEmulator emulator) 
        {
            this.Emulator = emulator;
        }

        protected virtual void SetQOrder(QOrder qOrder)
        {
            this._qOrder = qOrder;
            this._qOrder.OnPlaced += Order_OnPlaced;
            this._qOrder.OnPartial += Order_OnPartial;
            this._qOrder.OnKilled += Order_OnKilled;
            this._qOrder.OnFilled += Order_OnFilled;

            balance_qty = Qorder.Qty;
            partialFill = 0;
            traded_qty = 0;
        }

        public void CheckFilled(bool fired)
        {
            if (fired)
            {
                Assert.IsTrue(tcs_filled.Task.Status == TaskStatus.RanToCompletion, "Event(true) OnFilled failed!");
                Assert.IsTrue(partialFill == traded_qty, $"OnFilled failed: partialFill ({partialFill} != traded_qty({traded_qty})!");
                Assert.IsTrue(Qorder.QtyLeft == balance_qty, $"OnFilled check failed: order.QtyLeft ({Qorder.QtyLeft} != balance_qty({balance_qty})!");
                Assert.IsTrue(count_filled == 1, $"OnFilled fired {count_filled} times!");
            }
            else
                Assert.IsFalse(tcs_filled.Task.Status == TaskStatus.RanToCompletion, "Event(false) OnFilled failed!");
        }

        public void CheckKilled(bool fired)
        {
            if (fired)
            {
                Assert.IsTrue(tcs_killed.Task.Status == TaskStatus.RanToCompletion, "Event(true) OnKilled failed!");
                Assert.IsTrue(partialFill == traded_qty, $"OnKilled failed: partialFill({partialFill}) != traded_qty({traded_qty})!");
                Assert.IsTrue(Qorder.QtyLeft == balance_qty, $"OnKilled check failed: order.QtyLeft ({Qorder.QtyLeft} != balance_qty({balance_qty})!");
                Assert.IsTrue(count_killed == 1, $"OnKilled fired {count_killed} times!");
            }
            else
                Assert.IsFalse(tcs_killed.Task.Status == TaskStatus.RanToCompletion, "Event(false) OnKilled failed!");
        }

        public void CheckPartial(bool fired, bool check_volume)
        {
            if (fired)
                Assert.IsTrue(tcs_partial.Task.Status == TaskStatus.RanToCompletion, "Event(true) OnPartial failed!");
            else
                Assert.IsFalse(tcs_partial.Task.Status == TaskStatus.RanToCompletion, "Event(false) OnPartial failed!");

            if (check_volume)
            {
                Assert.IsTrue(partialFill == traded_qty, $"Partial fill failed: partialFill ({partialFill}) != traded_qty({traded_qty})!");
                Assert.IsTrue(Qorder.QtyLeft == balance_qty, $"Partial check failed: order.QtyLeft ({Qorder.QtyLeft} != balance_qty({balance_qty})!");
            }
        }

        public virtual void InitTCS()
        {
            tcs_filled = new System.Threading.Tasks.TaskCompletionSource<bool>();
            tcs_killed = new System.Threading.Tasks.TaskCompletionSource<bool>();
            tcs_partial = new System.Threading.Tasks.TaskCompletionSource<bool>();

            count_filled = 0;
            count_killed = 0;
        }

        public void Trade(long qty)
        {
            traded_qty += qty;
            balance_qty -= qty;
        }

        private void Order_OnPlaced(QOrder sender)
        {
            count_placed++;
            Console.WriteLine($"Order_OnPlaced: OnPlaced({count_placed})");
            Assert.IsTrue(count_placed <= 1, "OnPlaced called more than one time!!!");
        }

        private void Order_OnFilled(QOrder sender)
        {
            tcs_filled.TrySetResult(true);
            count_filled++;
            Console.WriteLine($"Order_OnFilled: OnFilled({count_filled})");
            Assert.IsTrue(count_filled <= 1, "OnFilled called more than one time!!!");
        }

        private void Order_OnKilled(QOrder sender)
        {
            tcs_killed.TrySetResult(true);
            count_killed++;
            Console.WriteLine($"Order_OnKilled: OnKilled({count_killed})");
            Assert.IsTrue(count_killed <= 1, "OnKilled called more than one time!!!");
        }

        private void Order_OnPartial(QOrder sender, long last_filled_qty)
        {
            partialFill += last_filled_qty;
            tcs_partial.TrySetResult(true);
            Console.WriteLine($"Order_OnPartial: OnPartial({last_filled_qty})");
        }


        protected virtual async Task WaitAnyTask()
        {
            var cancel = new CancellationTokenSource(timeout);
            try
            {   // Wait any
                await Task.Run(() => Task.WhenAny(new Task[] { tcs_partial.Task, tcs_filled.Task, tcs_killed.Task }), cancel.Token);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            await Task.Delay(100);

            Assert.IsFalse(cancel.IsCancellationRequested, "IsCancellationRequested");
        }

        public virtual async Task CheckEvents_WaitKilledOnly()
        {
            await WaitAnyTask();

            CheckPartial(false, true);
            CheckFilled(false);
            CheckKilled(true);
        }
        public virtual async Task CheckEvents_WaitNoAny()
        {
            await Task.Delay(100);

            CheckPartial(false, false);
            CheckFilled(false);
            CheckKilled(false);
        }
        public virtual async Task CheckEvents_WaitPartialAndFilled()
        {
            CancellationTokenSource cancel = new CancellationTokenSource(timeout);
            try
            {
                await Task.Run(() => Task.WhenAny(new Task[] { Task.WhenAll(new Task[] { tcs_partial.Task, tcs_filled.Task }), tcs_killed.Task }), cancel.Token);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            Assert.IsFalse(cancel.IsCancellationRequested, "IsCancellationRequested");

            CheckPartial(true, true);
            CheckFilled(true);
            CheckKilled(false);
        }

        public virtual async Task CheckEvents_WaitPartialAndKilled()
        {
            CancellationTokenSource cancel = new CancellationTokenSource(timeout);
            try
            {
                await Task.Run(() => Task.WhenAny(new Task[] { Task.WhenAll(new Task[] { tcs_partial.Task, tcs_killed.Task }), tcs_filled.Task }), cancel.Token);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            Assert.IsFalse(cancel.IsCancellationRequested, "IsCancellationRequested");

            CheckPartial(true, true);
            CheckKilled(true);
            CheckFilled(false);
        }

        public virtual async Task CheckEvents_WaitPartialOnly()
        {
            await WaitAnyTask();

            CheckPartial(true, true);
            CheckFilled(false);
            CheckKilled(false);
        }
        public virtual async Task CheckEvents_WaitFilledOnly()
        {
            await WaitAnyTask();

            CheckPartial(false, true);
            CheckFilled(true);
            CheckKilled(false);
        }
    }
}