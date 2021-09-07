using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using QUIKSharp.Functions;
using QUIKSharp.DataStructures;
using QUIKSharp.QOrders;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp;
using QUIKSharp.TestQuik;

namespace QuikSharp.Tests.QOrders.Testers
{
    public class TestStopOrderwLinked : TestStopOrder
    {
        public new QStopOrderWLinked Qorder { get => (QStopOrderWLinked)_qOrder; protected set => SetQOrder(value); }
        public Order CoOrder { get; protected set; }

        new public static TestStopOrderwLinked New(string SecCode, string ClassCode, TestQuikEmulator emulator)
        {
            var isec = GetUSec(SecCode, ClassCode);
            return new TestStopOrderwLinked(isec, emulator);
        }

        protected TestStopOrderwLinked(ITradeSecurity isec, TestQuikEmulator emulator) : base(emulator) 
        {
            Qorder = new QStopOrderWLinked(isec, Operation.Sell, 122.33m, 22.33m, 44.55m, 111);
        }

        override public async Task<TestQuikEmulatorReply> PlaceOrder(QOrdersManager manager)
        {
            InitTCS();
            var r = await base.PlaceOrder(manager);
            CoOrder = r.limitOrder;

            Assert.IsTrue(CoOrder != null, "Fatal!: No CoOrder for TestStopOrderwLinked");
            Assert.IsTrue(Qorder.CoOrderNum != 0, "Fatal!: No CoOrderNum is set for QStopOrderWLinked");
            //Assert.IsTrue(Qorder.CoOrder != null, "Fatal!: No CoOrder is set for QStopOrderWLinked");

            return r;
        }

        /// <summary>
        /// Устанавливает State ордера и отправляет событие Quik
        /// </summary>
        /// <param name="state"></param>
        public void SetCoOrderState(State state)
        {
            Emulator.SetOrderState(state, CoOrder);
            Emulator.CallOrderEvent(CoOrder);
        }
    }
}