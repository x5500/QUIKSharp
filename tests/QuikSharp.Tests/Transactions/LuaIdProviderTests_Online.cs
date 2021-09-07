﻿using NUnit.Framework;
using QUIKSharp;
using QUIKSharp.DataStructures.Transaction;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuikSharp.Tests.Transactions
{
    [TestFixture()]
    public class LuaIdProviderTests_Online
    {
        private readonly Quik q = new Quik();

        [Test()]
        public void GetNextIdTest()
        {
            var count = 100;
            var IdProvider = new LuaIdProvider(q);

            var array = new long[count];

            var sw = new Stopwatch();
            sw.Start();

            var prev = IdProvider.GetNextId();
            array[0] = prev;
            for (int i = 1; i < array.Length; i++)
            {
                long transID = IdProvider.GetNextId();
                array[i] = transID;
                Assert.IsTrue(transID > prev);
                Assert.IsTrue(prev + 1 == transID);
                prev = transID;
            }

            sw.Stop();
            Console.WriteLine("LuaIdProvider.GetNextId() x100 takes msecs: " + sw.ElapsedMilliseconds);

            for (int i = 0; i < array.Length - 1; i++)
            {
                if (array[i + 1] > array[i])
                    Assert.IsTrue(array[i + 1] == array[i] + 1, " Id[i+1] != Id[i] + 1");

                for (int j = i + 1; j < array.Length; j++)
                    Assert.IsTrue(array[i] != array[j], "Id[i] not Unique!");
            }
        }

        [Test()]
        public void IdentifyTransactionTest()
        {
            IIdentifyTransaction IdProvider = new LuaIdProvider(q);

            var t = new Transaction();
            Assert.IsNull(t.TRANS_ID);

            long id1 = IdProvider.IdentifyTransaction(t);
            Assert.NotNull(t.TRANS_ID);
            Assert.IsTrue(t.TRANS_ID > 0);

            long id2 = IdProvider.IdentifyTransaction(t);
            Assert.IsTrue(id1 == id2);

            var tr = new TransactionReply
            {
                TransID = t.TRANS_ID.Value
            };

            long id3 = IdProvider.IdentifyTransactionReply(tr);

            Assert.IsTrue(id1 == id3);
        }
    }
}