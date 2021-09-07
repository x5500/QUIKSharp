using NUnit.Framework;
using QUIKSharp;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Functions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuikSharp.Tests.Transactions
{
    [TestFixture()]
    public class TransactionsFunctionsTests_Online
    {
        private readonly Quik q = new Quik();
        private string ClientCode = string.Empty;
        private string AccountID = string.Empty;
        private const string CLASSCODE = "TQBR";
        private const string SECCODE = "VTBR";
        public TransactionsFunctionsTests_Online()
        {
            q.Transactions.IdProvider = new LuaBulkIdProvider(q, 100);
        }

        [OneTimeSetUp]
        public void BeforeTestSuit()
        {
            if (ClientCode != string.Empty && AccountID != string.Empty)
                return;

            var depo_limits = q.Class.GetDepoLimits().ConfigureAwait(false).GetAwaiter().GetResult();

            var fst = depo_limits.Where(row => row.TrdAccId != string.Empty).FirstOrDefault();
            AccountID = fst?.TrdAccId;
            ClientCode = fst?.ClientCode;

            Assert.IsTrue(ClientCode != string.Empty && AccountID != string.Empty);
        }


        [Test()]
        public void SendWaitTransactionAsync_Test_Quik_Exception()
        {
            var t = new Transaction()
            {
                ACTION = TransactionAction.NEW_STOP_ORDER,
                ACCOUNT = "",
                ClassCode = "",
                SecCode = "",
                STOPPRICE = 1.0m,
                PRICE = 1.0m,
                QUANTITY = 1,
                STOP_ORDER_KIND = StopOrderKind.SIMPLE_STOP_ORDER,
                OPERATION = TransactionOperation.B,
                CLIENT_CODE = "",
            };

            var timeoutCancel = new CancellationTokenSource(1000);
            var r = q.Transactions.SendWaitTransactionAsync(t, timeoutCancel.Token).ConfigureAwait(false).GetAwaiter().GetResult();
            Console.WriteLine($"Status: {r.Status} ");
            if (r.transReply != null)
                Console.WriteLine($"ResultMsg: {r.transReply.ResultMsg} ErrCode: {r.transReply.ErrorCode} ErrSource: {r.transReply.ErrorSource}");

            Assert.IsTrue(r.Status == TransactionStatus.TransactionException);
        }

        [Test()]
        public void SendWaitTransactionAsync_Test_Timeout()
        {
            var t = new Transaction()
            {
                ACTION = TransactionAction.KILL_ORDER,
                ACCOUNT = AccountID,
                CLIENT_CODE = ClientCode,
                ClassCode = CLASSCODE,
                SecCode = SECCODE,
                STOPPRICE = 1.0m,
                PRICE = 1.0m,
                QUANTITY = 1,
                ORDER_KEY = 11111,
            };

            var timeoutCancel = new CancellationTokenSource(1);
            var r = q.Transactions.SendWaitTransactionAsync(t, timeoutCancel.Token).ConfigureAwait(false).GetAwaiter().GetResult();
            Console.WriteLine($"Status: {r.Status} ");
            if (r.transReply != null)
                Console.WriteLine($"ResultMsg: {r.transReply.ResultMsg} ErrCode: {r.transReply.ErrorCode} ErrSource: {r.transReply.ErrorSource}");
            Assert.IsTrue(r.Status == TransactionStatus.TimeoutWaitReply);
        }

        [Test()]
        public async Task SendWaitTransactionAsync_Test_Success()
        {
            IIdentifyTransaction idp = q.Transactions.IdProvider;

            var t = new Transaction()
            {
                ACTION = TransactionAction.NEW_STOP_ORDER,
                ACCOUNT = AccountID,
                ClassCode = CLASSCODE,
                SecCode = SECCODE,
                CLIENT_CODE = ClientCode,
                STOPPRICE = 1.0m,
                PRICE = 1.0m,
                QUANTITY = 1,
                STOP_ORDER_KIND = StopOrderKind.SIMPLE_STOP_ORDER,
                OPERATION = TransactionOperation.S,
            };

            bool any_event = false;
            bool got_event = false;

            void Events_OnTransReply(TransactionReply transReply)
            {
                any_event = true;

                var id = idp.IdentifyTransaction(t);
                if (idp.IdentifyTransactionReply(transReply) == id)
                    got_event = true;
            }

            try
            {
                q.Events.OnTransReply += Events_OnTransReply;

                var timeoutCancel = new CancellationTokenSource(1000);
                var r = await q.Transactions.SendWaitTransactionAsync(t, timeoutCancel.Token);

                Console.WriteLine($"Status: {r.Status} ");
                if (r.transReply != null)
                    Console.WriteLine($"ResultMsg: {r.transReply.ResultMsg} ErrCode: {r.transReply.ErrorCode} ErrSource: {r.transReply.ErrorSource}");

                Assert.IsTrue(any_event, "Can't test: No any Events_OnTransReply was invoked!");

                Assert.IsFalse(got_event && r.Status == TransactionStatus.TimeoutWaitReply, "SendWaitTransactionAsync failed to catch OnTransReply() event ..");

                Assert.IsTrue(r.Status == TransactionStatus.Success, r.transReply?.ResultMsg ?? r.Status.ToString());
            }
            finally
            {
                q.Events.OnTransReply -= Events_OnTransReply;
            }
        }

        [Test()]
        public void SendTransactionAsyncTest_Success()
        {
            var t = new Transaction()
            {
                ACTION = TransactionAction.KILL_ORDER,
                ACCOUNT = AccountID,
                CLIENT_CODE = ClientCode,
                ClassCode = CLASSCODE,
                SecCode = SECCODE,
                STOPPRICE = 1.0m,
                PRICE = 1.0m,
                QUANTITY = 1,
                ORDER_KEY = 11111,
            };

            var r = q.Transactions.SendTransactionAsync(t).ConfigureAwait(false).GetAwaiter().GetResult();
            Console.WriteLine($"Result: {r.Result}, Id:{r.TransId}, ResultMsg: {r.ResultMsg}");

            Assert.IsTrue(r.Result == TransactionStatus.Success);
        }


        [Test()]
        public void SendTransactionAsync_Test_Transaction_Exception()
        {
            var t = new Transaction()
            {
                ACTION = TransactionAction.NEW_STOP_ORDER,
                ACCOUNT = "",
                ClassCode = "",
                SecCode = "",
                STOPPRICE = 1.0m,
                PRICE = 1.0m,
                QUANTITY = 1,
                STOP_ORDER_KIND = StopOrderKind.SIMPLE_STOP_ORDER,
                OPERATION = TransactionOperation.B,
                CLIENT_CODE = "",
            };

            var r = q.Transactions.SendTransactionAsync(t).ConfigureAwait(false).GetAwaiter().GetResult();
            Console.WriteLine($"Result: {r.Result}, Id:{r.TransId}, ResultMsg: {r.ResultMsg}");

            Assert.IsTrue(r.Result == TransactionStatus.TransactionException);
        }

    }
}