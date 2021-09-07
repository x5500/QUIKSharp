using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using QUIKSharp.Converters;

namespace QUIKSharp.Tests
{
    [TestFixture()]
    public class QuikDateTimeConverterTests
    {
        public QuikDateTimeConverterTests()
        {
        }


        [Test()]
        public void HHmmssToTimeSpanTest()
        {
            var ethalon = new TimeSpan(07, 10, 12);
            var result = QuikDateTimeConverter.HHmmssToTimeSpan("071012");
            Assert.IsTrue(result == ethalon, "Fail hhmmss");

            ethalon = new TimeSpan(02, 11, 02);
            result = QuikDateTimeConverter.HHmmssToTimeSpan("21102");
            Assert.IsTrue(result == ethalon, "fail Hmmss");

            ethalon = new TimeSpan(03, 13, 02);
            result = QuikDateTimeConverter.HHmmssToTimeSpan("31302.0000000");
            Assert.IsTrue(result == ethalon, "fail Hmmss.0000000");

            ethalon = new TimeSpan(0, 0, 0);
            result = QuikDateTimeConverter.HHmmssToTimeSpan("0.0000000");
            Assert.IsTrue(result == ethalon, "fail 0.0000000");
        }

        [Test()]
        public void TimeStrToTimeSpanTest()
        {
            var ethalon = new TimeSpan(07, 10, 12);
            var result = QuikDateTimeConverter.TimeStrToTimeSpan("07:10:12");
            Assert.IsTrue(result == ethalon, "Fail hh:mm:ss");

            ethalon = new TimeSpan(23, 49, 53);
            result = QuikDateTimeConverter.TimeStrToTimeSpan("234953");
            Assert.IsTrue(result == ethalon, "fail HHmmss");

            ethalon = new TimeSpan(03, 13, 02);
            result = QuikDateTimeConverter.TimeStrToTimeSpan("31302.0000000");
            Assert.IsTrue(result == ethalon, "fail Hmmss.0000000");
        }

        [Test()]
        public void QuikDateStrToDateTimeTest()
        {
            var ethalon = new DateTime(2012, 10, 02);
            var result = QuikDateTimeConverter.QuikDateStrToDateTime("20121002");
            Assert.IsTrue(result == ethalon, "Fail yyyyMMdd");

            ethalon = new DateTime(2012, 08, 02);
            result = QuikDateTimeConverter.QuikDateStrToDateTime("02.08.2012");
            Assert.IsTrue(result == ethalon, "Fail dd.MM.yyyy");

        }

        [Test()]
        public void TimeSpanToHHMMSSTest()
        {
            TimeSpan timespan;
            string res;
            {
                timespan = new TimeSpan(1, 2, 3);
                res = QuikDateTimeConverter.TimeSpanToHHMMSS(timespan);
                Assert.IsTrue(res == "010203");
            }
            {
                timespan = new TimeSpan(23, 45, 37);
                res = QuikDateTimeConverter.TimeSpanToHHMMSS(timespan);
                Assert.IsTrue(res == "234537");
            }
            {
                timespan = new TimeSpan();
                res = QuikDateTimeConverter.TimeSpanToHHMMSS(timespan);
                Assert.IsTrue(res == "000000");
            }


            /// Speedtest 
            string var1(TimeSpan ts) => ts.ToString("hhmmss");

            timespan = new TimeSpan(23, 45, 37);

            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 1000000; i++)
            {
                res = QuikDateTimeConverter.TimeSpanToHHMMSS(timespan);
            }
            sw.Stop();
            Console.WriteLine("TimeSpanToHHMMSS x 1 000 000 takes msecs: " + sw.ElapsedMilliseconds);

            sw.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                res = var1(timespan);
            }
            sw.Stop();
            Console.WriteLine("ts.ToString(\"hhmmss\") x 1 000 000 takes msecs: " + sw.ElapsedMilliseconds);
        }

        [Test()]
        public void DateTimeToYYYYMMDDTest()
        {
            // Speedtest
            string var1(DateTime dateTime) => dateTime.ToString("yyyyMMdd");
            string DateTimeToYYYYMMDD(DateTime dateTime)
            {
                var str = new StringBuilder("00000000", 8);
                int dd = dateTime.Day;
                int mm = dateTime.Month;
                int yy = dateTime.Year;

                str[0] = (char)('0' + yy / 1000);
                yy = yy % 1000;
                str[1] = (char)('0' + yy / 100);
                yy = yy % 100;
                str[2] = (char)('0' + yy / 10);
                str[3] = (char)('0' + yy % 10);

                str[4] = (char)('0' + mm / 10);
                str[5] = (char)('0' + mm % 10);

                str[6] = (char)('0' + dd / 10);
                str[7] = (char)('0' + dd % 10);

                return str.ToString();
            }


            DateTime datetime;
            string res;
            {
                datetime = new DateTime(2012, 2, 3);
                res = DateTimeToYYYYMMDD(datetime);
                Assert.IsTrue(res == "20120203");
            }
            {
                datetime = new DateTime(1923, 10, 27);
                res = DateTimeToYYYYMMDD(datetime);
                Assert.IsTrue(res == "19231027");
            }
            {
                datetime = new DateTime();
                res = DateTimeToYYYYMMDD(datetime);
                Assert.IsTrue(res == datetime.ToString("yyyyMMdd"));
            }


            /// Speedtest 
            datetime = new DateTime(2012, 11, 23);

            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 1000000; i++)
            {
                res = DateTimeToYYYYMMDD(datetime);
            }
            sw.Stop();
            Console.WriteLine("DateTimeToYYYYMMDD x 1 000 000 takes msecs: " + sw.ElapsedMilliseconds);

            sw.Restart();
            for (int i = 0; i < 1000000; i++)
            {
                res = var1(datetime);
            }
            sw.Stop();
            Console.WriteLine("dateTime.ToString(\"yyyyMMdd\") x 1 000 000 takes msecs: " + sw.ElapsedMilliseconds);

        }
    }
}