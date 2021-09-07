using NUnit.Framework;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.QOrders;
using QUIKSharp.TestQuik;

namespace QuikSharp.Tests.QOrders
{
    [TestFixture()]
    public class QLimitOrderTests_Transac
    {
        readonly TestQuikEmulator emulator = new TestQuikEmulator();

        [OneTimeSetUp]
        public void BeforeTestSuit()
        {
        }

        [Test()]
        public void PlaceOrderTransactionTest()
        {
            long qty = 1;
            decimal price = 1.0m;
            var operation = Operation.Buy;

            var q = new QLimitOrder(emulator, operation, price, qty, ExecutionCondition.PUT_IN_QUEUE);
            var t1 = q.PlaceOrderTransaction();
            var t2 = new Transaction
            {
                ACTION = TransactionAction.NEW_ORDER,
                ACCOUNT = emulator.AccountID,
                ClassCode = emulator.ClassCode,
                SecCode = emulator.SecCode,
                CLIENT_CODE = emulator.ClientCode,
                QUANTITY = qty,
                OPERATION = TransactionOperation.B,
                PRICE = price,
                TYPE = TransactionType.L,
                EXECUTION_CONDITION = ExecutionCondition.PUT_IN_QUEUE,
                EXPIRY_DATE = "GTC",
            };

            Assert.IsTrue(TestUtils.CompareIsSameObj(t1, t2));
        }

        [Test()]
        public void MoveOrderTransactionTest()
        {
            long qty = 1;
            decimal price = 1.0m;
            var operation = Operation.Buy;
            long OrderNum = 1234567890;
            var mode = TransactionMode.NewQty;

            var q = new QLimitOrder(emulator, operation, price, qty, ExecutionCondition.PUT_IN_QUEUE)
            {
                OrderNum = OrderNum
            };

            var t1 = q.MoveOrderTransaction();
            var t2 = new Transaction
            {
                ACTION = TransactionAction.MOVE_ORDERS,
                MODE = mode,
                ClassCode = emulator.ClassCode,
                SecCode = emulator.SecCode,
                ACCOUNT = emulator.AccountID,
                FIRST_ORDER_NUMBER = OrderNum,
                FIRST_ORDER_NEW_PRICE = price,
                FIRST_ORDER_NEW_QUANTITY = qty
            };

            Assert.IsTrue(TestUtils.CompareIsSameObj(t1, t2));
        }

        [Test()]
        public void KillOrderTransactionTest()
        {
            long qty = 1;
            decimal price = 1.0m;
            var operation = Operation.Buy;
            long OrderNum = 1234567890;

            var q = new QLimitOrder(emulator, operation, price, qty, ExecutionCondition.PUT_IN_QUEUE)
            {
                OrderNum = OrderNum
            };

            var t1 = q.KillOrderTransaction();
            var t2 = new Transaction
            {
                ACTION = TransactionAction.KILL_ORDER,
                ClassCode = emulator.ClassCode,
                SecCode = emulator.SecCode,
                ACCOUNT = emulator.AccountID,
                ORDER_KEY = OrderNum,
            };

            Assert.IsTrue(TestUtils.CompareIsSameObj(t1, t2));
        }
    }
}