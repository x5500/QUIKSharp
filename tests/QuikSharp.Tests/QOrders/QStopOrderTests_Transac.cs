using NUnit.Framework;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.QOrders;
using QUIKSharp.TestQuik;

namespace QuikSharp.Tests.QOrders
{
    [TestFixture()]
    public class QStopOrderTests_Transac
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
            decimal sl_price = 1.0m;
            decimal deal_sl_price = 10000.0m;

            var q = new QSimpleStopOrder(emulator, operation, sl_price, deal_sl_price, qty);
            var t1 = q.PlaceOrderTransaction();
            var t2 = new Transaction
            {
                ACTION = TransactionAction.NEW_STOP_ORDER,
                STOP_ORDER_KIND = StopOrderKind.SIMPLE_STOP_ORDER,
                ACCOUNT = emulator.AccountID,
                ClassCode = emulator.ClassCode,
                SecCode = emulator.SecCode,
                CLIENT_CODE = emulator.ClientCode,
                QUANTITY = qty,
                OPERATION = operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
                STOPPRICE = sl_price,
                PRICE = deal_sl_price,
                EXPIRY_DATE = "GTC",
            };

            Assert.IsTrue(TestUtils.CompareIsSameObj(t1, t2));
        }

        [Test()]
        public void KillOrderTransactionTest()
        {
            long qty = 1;
            var operation = Operation.Buy;
            decimal sl_price = 1.0m;
            decimal deal_sl_price = 10000.0m;
            long OrderNum = 1234567890;


            var q = new QSimpleStopOrder(emulator, operation, sl_price, deal_sl_price, qty)
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