using NUnit.Framework;
using QUIKSharp.Converters;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Functions;
using QUIKSharp.Transport;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace QUIKSharp.Tests
{
    [TestFixture]
    public class TransactionSpecTest_Online
    {
        readonly Quik _q = new Quik();
        readonly DebugFunctions _df = new DebugFunctions(QuikService.Create(Quik.DefaultHost, Quik.DefaultPort, Quik.DefaultPort + 1));

        /// <summary>
        /// Very important than this works!
        /// (both with nulls and with ignored nulls
        /// </summary>
        [Test]
        public void CouldEchoTransactionSpec()
        {
            var t = new Transaction();
            var echoed = _df.Echo(t).Result;
            Console.WriteLine(t.ToJson());
            Console.WriteLine(echoed.ToJson());
            Assert.AreEqual(t.ToJson(), echoed.ToJson());
        }


        [Test]
        public void CouldSendEmptyTransactionSpec()
        {
            var t = new Transaction();
            var result = _q.Transactions.SendTransaction(t).Result;

            Console.WriteLine("Sent Id: " + t.TRANS_ID);
            Console.WriteLine("Result Id: " + result);
            Assert.IsTrue(result < 0);
            Console.WriteLine("Error: " + t.ErrorMessage);
        }

        [Test]
        public void MultiEchoTransactionSpec()
        {

            var sw = new Stopwatch();
            Console.WriteLine("Started");
            for (int round = 0; round < 10; round++)
            {
                sw.Reset();
                sw.Start();

                var count = 1000;
                var t = new Transaction();

                var array = new Task<Transaction>[count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = _df.Echo(t);
                }
                for (int i = 0; i < array.Length; i++)
                {
                    var res = array[i].Result;
                    array[i] = null;
                }

                sw.Stop();
                Console.WriteLine("MultiPing takes msecs: " + sw.ElapsedMilliseconds);
            }
        }
    }
}