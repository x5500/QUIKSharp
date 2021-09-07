using NUnit.Framework;
using QuikSharp.Tests.Helpers;
using QUIKSharp;
using QUIKSharp.QOrders;
using System.Threading;
using System.Threading.Tasks;

namespace QuikSharp.Tests.QOrders
{
    [TestFixture()]
    public class QOrdersManagerTests_Online
    {
        readonly Quik q = new Quik();
        TestQ ins;

        [OneTimeSetUp]
        public void BeforeTestSuit()
        {
            ins = new TestQ(q);
            Assert.IsTrue(ins.ClientCode != string.Empty && ins.AccountID != string.Empty);
        }

        [Test()]
        public void RequestPlaceOrderTest()
        {
            var om = new QOrdersManager(q, 20000);
            var order = new QTakeOrder(ins, QUIKSharp.DataStructures.Operation.Sell, 5000m, 2m, 2m, 1);

            var cancel = new CancellationTokenSource(40000);
            var tcs = new TaskCompletionSource<bool>();
            order.OnPlaced += (sender) =>
            {
                tcs.TrySetResult(true);
            };

            om.RequestPlaceOrder(order);
            tcs.Task.Wait(cancel.Token);

            Assert.IsTrue(tcs.Task.Result, "Place order failed!");

        }

        [Test()]
        public void RequestKillOrderTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void RequestMoveOrderTest()
        {
            Assert.Fail();
        }
    }
}