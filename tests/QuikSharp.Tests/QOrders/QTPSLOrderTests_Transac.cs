using NUnit.Framework;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.QOrders;
using QUIKSharp.TestQuik;


#pragma warning disable IDE0044 // Добавить модификатор только для чтения
namespace QuikSharp.Tests.QOrders
{
    [TestFixture()]
    public class QTPSLOrderTests_Transac
    {
        readonly TestQuikEmulator emulator = new TestQuikEmulator();
        long qty = 1;
        decimal tp_price = 10000.0m;
        decimal sl_price = 1.0m;
        decimal deal_sl_price = 10000.0m;
        decimal offset = 2.0m;
        decimal spread = 2.0m;
        long OrderNum = 1234567890;
        Operation operation = Operation.Buy;

        [Test()]
        public void PlaceOrderTransactionTest()
        {
            var q = new QTPSLOrder(emulator, operation, tp_price, sl_price, deal_sl_price, offset, spread, qty);
            var t1 = q.PlaceOrderTransaction();
            var t2 = new Transaction
            {
                ACTION = TransactionAction.NEW_STOP_ORDER,
                STOP_ORDER_KIND = StopOrderKind.TAKE_PROFIT_AND_STOP_LIMIT_ORDER,
                ACCOUNT = emulator.AccountID,
                ClassCode = emulator.ClassCode,
                SecCode = emulator.SecCode,
                CLIENT_CODE = emulator.ClientCode,
                QUANTITY = qty,
                OPERATION = operation == Operation.Buy ? TransactionOperation.B : TransactionOperation.S,
                STOPPRICE = tp_price,  // -- тэйк-профит
                STOPPRICE2 = sl_price, // -- стоп-лимит
                PRICE = deal_sl_price,  // -- Цена заявки, за единицу инструмента.
                OFFSET = offset,
                OFFSET_UNITS = OffsetUnits.PRICE_UNITS,
                MARKET_STOP_LIMIT = YesOrNo.NO,
                SPREAD = spread,
                SPREAD_UNITS = OffsetUnits.PRICE_UNITS,
                MARKET_TAKE_PROFIT = YesOrNo.NO,
                EXPIRY_DATE = "GTC",
                IS_ACTIVE_IN_TIME = YesOrNo.NO,
            };

            Assert.IsTrue(TestUtils.CompareIsSameObj(t1, t2));
        }

        [Test()]
        public void KillOrderTransactionTest()
        {
            var q = new QTPSLOrder(emulator, operation, tp_price, sl_price, deal_sl_price, offset, spread, qty)
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
#pragma warning restore IDE0044 // Добавить модификатор только для чтения
