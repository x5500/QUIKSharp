using NUnit.Framework;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.QOrders;
using QUIKSharp.TestQuik;

namespace QuikSharp.Tests.QOrders
{
    [TestFixture()]
    public class QTakeOrderTests_Transac
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
            var operation = Operation.Buy;
            decimal tp_price = 10000.0m;
            decimal offset = 2.0m;
            decimal spread = 2.0m;

            var q = new QTakeOrder(emulator, operation, tp_price, offset, spread, qty);
            var t1 = q.PlaceOrderTransaction();
            var t2 = new Transaction
            {
                ACTION = TransactionAction.NEW_STOP_ORDER,
                STOP_ORDER_KIND = StopOrderKind.TAKE_PROFIT_STOP_ORDER,
                ACCOUNT = emulator.AccountID,
                ClassCode = emulator.ClassCode,
                SecCode = emulator.SecCode,
                CLIENT_CODE = emulator.ClientCode,
                QUANTITY = qty,
                OPERATION = operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
                STOPPRICE = tp_price, // -- тэйк-профит
                OFFSET = offset,
                OFFSET_UNITS = OffsetUnits.PRICE_UNITS,
                SPREAD = spread,
                SPREAD_UNITS = OffsetUnits.PRICE_UNITS,
                EXPIRY_DATE = "GTC",
            };

            Assert.IsTrue(TestUtils.CompareIsSameObj(t1, t2));
        }

        [Test()]
        public void KillOrderTransactionTest()
        {
            long OrderNum = 1234567890;

            long qty = 1;
            var operation = Operation.Buy;
            decimal tp_price = 10000.0m;
            decimal offset = 2.0m;
            decimal spread = 2.0m;

            var q = new QTakeOrder(emulator, operation, tp_price, offset, spread, qty)
            {
                OrderNum = OrderNum
            };

            var t1 = q.KillOrderTransaction();
            var t2 = new Transaction
            {
                ACTION = TransactionAction.KILL_STOP_ORDER,
                ClassCode = emulator.ClassCode,
                SecCode = emulator.SecCode,
                ACCOUNT = emulator.AccountID,
                STOP_ORDER_KEY = OrderNum,
            };

            Assert.IsTrue(TestUtils.CompareIsSameObj(t1, t2));
        }
    }
}