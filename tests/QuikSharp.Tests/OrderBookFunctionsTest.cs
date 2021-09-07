using System;
using NUnit.Framework;
using QuikSharp.Tests.Helpers;

namespace QUIKSharp.Tests
{

    //struct Sec : ISecurity { }

    [TestFixture]
    public class OrderBookFunctionsTest 
    {
        private bool _isQuik;
        private Quik _q = new Quik();
        private Sec sec = new Sec { ClassCode = "SPBFUT", SecCode = "RIH5" };
        public OrderBookFunctionsTest() { _isQuik = _q.Debug.IsQuik().Result; }

        [Test]
        public void Subscribe_Level_II_Quotes() {
            Console.WriteLine("Subscribe_Level_II_Quotes: "
                + String.Join(",", _q.OrderBook.Subscribe(sec).Result));
        }

        [Test]
        public void Unsubscribe_Level_II_Quotes() {
            Console.WriteLine("Unsubscribe_Level_II_Quotes: "
                + String.Join(",", _q.OrderBook.Unsubscribe(sec).Result));
        }

        [Test]
        public void IsSubscribed_Level_II_Quotes() {
            Console.WriteLine("IsSubscribed_Level_II_Quotes: "
                + String.Join(",", _q.OrderBook.IsSubscribed(sec).Result));
        }


    }
}
