using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using QUIKSharp.Functions;
using QUIKSharp.DataStructures;
using QUIKSharp.QOrders;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp;
using QUIKSharp.QOrders.Tests.Offline;
using QUIKSharp.TestQuik;

namespace QuikSharp.Tests.QOrders.Testers
{
    public class TestStopOrder : TestOrderBase
    {
        public new QStopOrder Qorder { get => (QStopOrder)_qOrder; protected set => SetQOrder(value); }

        public TaskCompletionSource<bool> tcs_executed { get; protected set; }
        public StopOrder StopOrder { get; protected set; }
        public Order LinkedOrder { get; protected set; }

        public static TestStopOrder New(string SecCode, string ClassCode, TestQuikEmulator emulator)
        {
            var isec = GetUSec(SecCode, ClassCode);
            return new TestStopOrder(isec, emulator);
        }

        protected TestStopOrder(ITradeSecurity isec, TestQuikEmulator emulator) : base (emulator)
        {
            Qorder = new QSimpleStopOrder(isec, Operation.Sell, 122.33m, 120.21m, 100);
        }

        protected TestStopOrder(TestQuikEmulator emulator) : base (emulator)
        { 
        }

        protected override void SetQOrder(QOrder qOrder)
        {
            base.SetQOrder(qOrder);
            Qorder.OnExecuted += Qorder_OnExecuted;
        }

        private void Qorder_OnExecuted(QOrder sender)
        {
            tcs_executed.TrySetResult(true);
            Console.WriteLine("TestStopOrder: OnExecuted()");
        }

        public virtual async Task<TestQuikEmulatorReply> PlaceOrder(QOrdersManager manager)
        {
            InitTCS();

            var cancel = new CancellationTokenSource(timeout);
            var tcs = new TaskCompletionSource<bool>();
            Qorder.OnPlaced += (sender) => tcs.TrySetResult(true);

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

            Assert.IsTrue(tcs2.Task.Status == TaskStatus.RanToCompletion, "Failed to reply on transaction...");

            StopOrder = tcs2.Task.Result.stopOrder;
            TransId = tcs2.Task.Result.TransId;

            Assert.IsTrue(tcs.Task.Status == TaskStatus.RanToCompletion, "Place order failed: No OnPlaced event called!");

            Assert.IsTrue(Qorder.State == QOrderState.Placed, $"qOrder.State ({Qorder.State}) != Placed");

            return tcs2.Task.Result;
        }

        override public void InitTCS()
        {
            base.InitTCS();
            tcs_executed = new System.Threading.Tasks.TaskCompletionSource<bool>();
        }

        public void MakeLinkedOrder()
        {
            LinkedOrder = Emulator.MakeLinkedOrder(StopOrder);
            Assert.IsTrue(LinkedOrder != null, "MakeLinkedOrder failed!");
        }

        public void CheckExecuted(bool fired)
        {
            if (fired)
            {
                Assert.IsTrue(tcs_executed.Task.Status == TaskStatus.RanToCompletion, "Event(true) OnExecuted failed!");
            }
            else
                Assert.IsFalse(tcs_executed.Task.Status == TaskStatus.RanToCompletion, "Event(false) OnExecuted failed!");
        }

        /// <summary>
        /// Устанавливает State ордера и отправляет событие Quik
        /// </summary>
        /// <param name="state"></param>
        public void SetStopOrderState(State state)
        {
            Emulator.SetOrderState(state, StopOrder);
            Emulator.CallStopOrderEvent(StopOrder);
        }
        public void CallStopOrderEvent()
        {
            Emulator.CallStopOrderEvent(StopOrder);
        }

        public async Task<Order> PlaceLinkedOrder()
        {

            LinkedOrder = Emulator.MakeLinkedOrder(StopOrder);
            Emulator.SetOrderState(State.Active, LinkedOrder);
            Emulator.CallOrderEvent(LinkedOrder);

            await CheckEvents_WaitExecutedOnly();

            Assert.IsTrue(Qorder.State == QOrderState.Executed, $"qOrder.State ({Qorder.State}) != Executed");

            return LinkedOrder;
        }

        public override async Task CheckEvents_WaitNoAny()
        {
            await base.CheckEvents_WaitNoAny();
            CheckExecuted(false);
        }

        protected override async Task WaitAnyTask()
        {
            var cancel = new CancellationTokenSource(timeout);
            try
            {   // Wait any
                await Task.Run(() => Task.WhenAny(new Task[] { tcs_partial.Task, tcs_filled.Task, tcs_killed.Task, tcs_executed.Task, Task.Delay(timeout) }), cancel.Token);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            await Task.Delay(100);

            Assert.IsFalse(cancel.IsCancellationRequested, "IsCancellationRequested");
        }

        public override async Task CheckEvents_WaitKilledOnly()
        {
            await base.CheckEvents_WaitKilledOnly();
            CheckExecuted(false);
        }

        public override async Task CheckEvents_WaitFilledOnly()
        {
            await base.CheckEvents_WaitFilledOnly();
            CheckExecuted(false);
        }

        public virtual async Task CheckEvents_WaitExecutedOnly()
        {
            await WaitAnyTask();

            CheckPartial(false, true);
            CheckFilled(false);
            CheckKilled(false);
            CheckExecuted(true);
        }

        public override async Task CheckEvents_WaitPartialOnly()
        {
            await base.CheckEvents_WaitPartialOnly();
            CheckExecuted(false);
        }

        public override async Task CheckEvents_WaitPartialAndFilled()
        {
            CancellationTokenSource cancel = new CancellationTokenSource(timeout);
            try
            {
                await Task.Run(() => Task.WhenAny(new Task[] { tcs_killed.Task, tcs_executed.Task, 
                    Task.WhenAll(new Task[] { tcs_partial.Task, tcs_filled.Task } ) }), cancel.Token);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            Assert.IsFalse(cancel.IsCancellationRequested, "IsCancellationRequested");

            await Task.Delay(100);

            CheckPartial(true, true);
            CheckFilled(true);
            CheckKilled(false);
            CheckExecuted(false);
        }

        public virtual async Task CheckEvents_WaitPartialAndExecuted()
        {
            var cancel = new CancellationTokenSource(timeout);
            try
            {
                await Task.Run(() => Task.WhenAny(new Task[] { tcs_killed.Task, tcs_filled.Task,
                    Task.WhenAll(new Task[] { tcs_partial.Task, tcs_executed.Task } ) }), cancel.Token);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            Assert.IsFalse(cancel.IsCancellationRequested, "IsCancellationRequested");

            await Task.Delay(100);

            CheckPartial(true, true);
            CheckExecuted(true);
            CheckFilled(false);
            CheckKilled(false);
        }

        public virtual async Task CheckEvents_WaitPartialAndExecutedAndFilled()
        {
            var cancel = new CancellationTokenSource(timeout);
            try
            {
                await Task.Run(() => Task.WhenAny(new Task[] { tcs_killed.Task,
                    Task.WhenAll(new Task[] { tcs_partial.Task, tcs_executed.Task, tcs_filled.Task } ) }), cancel.Token);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            Assert.IsFalse(cancel.IsCancellationRequested, "IsCancellationRequested");

            await Task.Delay(100);

            CheckPartial(true, true);
            CheckFilled(true);
            CheckExecuted(true);
            CheckKilled(false);
        }

        public override async Task CheckEvents_WaitPartialAndKilled()
        {
            CancellationTokenSource cancel = new CancellationTokenSource(timeout);
            try
            {
                await Task.Run(() => Task.WhenAny(new Task[] { tcs_filled.Task, tcs_executed.Task,
                    Task.WhenAll(new Task[] { tcs_partial.Task, tcs_killed.Task } ) }), cancel.Token);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            Assert.IsFalse(cancel.IsCancellationRequested, "IsCancellationRequested");

            await Task.Delay(100);

            CheckPartial(true, true);
            CheckKilled(true);
            CheckFilled(false);
            CheckExecuted(false);
        }
    }
}