using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using QUIKSharp.Converters;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Functions;

namespace QUIKSharp.Tests
{
    public class TypeWithNumberSerializedAsString {
        public int NormalInt { get; set; }
        [JsonConverter(typeof(NUMBER_Converter<int?>))]
        public int? AsString { get; set; }
    }
    public class TypeWithNumberDeSerializedAsString {
        public int NormalInt { get; set; }
        public string AsString { get; set; }
    }
    public class TypeWithTimeSpanSerializedAsString
    {
        [JsonConverter(typeof(HHMMSS_TimeSpanConverter))]
        public TimeSpan? AsString { get; set; }
    }
    public class TypeWithDateTimeDeSerializedAsString {
        public string AsString { get; set; }
    }
    public class TypeWithDateSerializedAsString
    {
        [JsonConverter(typeof(YYYYMMDD_DateTimeConverter))]
        public DateTime? AsString { get; set; }
    }

    [TestFixture]
    public class TransactionSpecTest
    {
        /// <summary>
        /// Make sure that Buy or Sell is never set by default
        /// That would be a very stupid mistake, but such king of mistakes are the most
        /// dangerous because one never believes he will make them
        /// </summary>
        [Test]
        public void UndefinedEnumCannotBeSerialized() {
            var op = (TransactionOperation)10;
            var json = op.ToJson();
            Assert.AreEqual(json, "null");
            Console.WriteLine(json);
            op = (TransactionOperation)1;
            json = op.ToJson();
            Console.WriteLine(json);
            Assert.AreEqual(json, "B".ToJson());

            var act = (TransactionAction)0;
            json = act.ToJson();
            Console.WriteLine(json);
            Assert.AreEqual(json, "null");

            var yesNo = (YesOrNo)0;
            json = yesNo.ToJson();
            Console.WriteLine(json);
            Assert.AreEqual(json, "null");

            var yesNoDef = (YesOrNoDefault)0;
            json = yesNoDef.ToJson();
            Console.WriteLine(json);
            Assert.AreEqual(json, "NO".ToJson());
        }


        [Test]
        public void CouldSerializeNumberPropertyAsString() {
            var t = new TypeWithNumberSerializedAsString
            {
                NormalInt = 123,
                AsString = 456
            };
            var j = t.ToJson();
            Console.WriteLine(j);
            var t2 = j.FromJson<TypeWithNumberDeSerializedAsString>();
            Assert.AreEqual("456", t2.AsString);
            var t1 = j.FromJson<TypeWithNumberSerializedAsString>();
            Assert.AreEqual(456, t1.AsString);
        }


        [Test]
        public void CouldSerializeDateTimePropertyAsString() {
            var t = new TypeWithTimeSpanSerializedAsString
            {
                AsString = new TimeSpan(1, 21, 55)
            };
            var j = t.ToJson();
            Console.WriteLine(j);
            var t2 = j.FromJson<TypeWithDateTimeDeSerializedAsString>();
            Assert.AreEqual(t.AsString.Value.ToString("hhmmss"), t2.AsString);
            var t1 = j.FromJson<TypeWithTimeSpanSerializedAsString>();
            Assert.AreEqual(t.AsString, t1.AsString);
        }

        [Test]
        public void CouldDeSerializeHHMMSSTimeProperty()
        {
            var ethalon = new TimeSpan(7, 10, 11);
            string j = "{\"AsString\":\"71011.0000\"}";
            var t2 = j.FromJson<TypeWithTimeSpanSerializedAsString>();
            Assert.AreEqual(t2.AsString.Value, ethalon);
        }

        [Test]
        public void CouldSerializeDatePropertyAsString()
        {
            var t = new TypeWithDateSerializedAsString();
            var now = DateTime.Now;
            t.AsString = new DateTime(now.Year, now.Month, now.Day);
            var j = t.ToJson();
            Console.WriteLine(j);

            var t2 = j.FromJson<TypeWithDateTimeDeSerializedAsString>();
            Assert.AreEqual(t.AsString.Value.ToString("yyyyMMdd"), t2.AsString);

            var t1 = j.FromJson<TypeWithDateSerializedAsString>();
            Assert.AreEqual(t.AsString, t1.AsString);
        }


        [Test]
        public void CouldSerializeEmptyTransactionSpec() {
            var t = new Transaction();
            var j = t.ToJson();
            Console.WriteLine(j);
            var t2 = j.FromJson<Transaction>();
            Assert.AreEqual(t.ToJson(), t2.ToJson());
        }


        [Test]
        public void CouldSerializeEmptyTransactionSpecMulti() {
            var t = new Transaction();
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 100000; i++) {
                var j = t.ToJson();
                var t2 = j.FromJson<Transaction>();
            }
            sw.Stop();
            Console.WriteLine("Multiserialization takes msecs: " + sw.ElapsedMilliseconds);
        }

        [Test]
        public void TransactionPriceWithoutTrailoringZeros()
        {
            // Проверка, что в цене отбрасываются незначащие нули, т.к. в противном случае возвращается ошибка:
            // ошибка отправки транзакции Неправильно указана цена: "81890,000000"
            // Сообщение об ошибке: Число не может содержать знак разделителя дробной части

            Transaction t1 = new Transaction { PRICE = 1.00000m };
            Transaction t2 = new Transaction { PRICE = 1.01000m };
            string json1 = t1.ToJson();
            string json2 = t2.ToJson();

            Assert.IsTrue(json1.Contains("\"PRICE\":\"1\""));
            Assert.IsTrue(json2.Contains("\"PRICE\":\"1,01\"") || json2.Contains("\"PRICE\":\"1.01\""));
        }
    }
}