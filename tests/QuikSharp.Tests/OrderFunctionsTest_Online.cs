using System;
using NUnit.Framework;
using QUIKSharp.DataStructures.Transaction;

namespace QUIKSharp.Tests
{
    [TestFixture]
    public class OrderFunctionsTest_Online
    {
        [Test]
        public void GetOrderTest()
        {
            Quik quik = new Quik();

            //Заведомо не существующая заявка.
            long orderId = 123456789;
            Order order = quik.Orders.GetOrder("TQBR", orderId).Result;
            Assert.IsNull(order);

            //Заявка с таким номером должна присутствовать в таблице заявок.
            orderId = 1952337492464566254;//вставьте свой номер
            order = quik.Orders.GetOrder("VBM1", orderId).Result;
            if (order != null)
            {
                Console.WriteLine("Order state: " + order.State);
            }
            else
            {
                Console.WriteLine("Order doesn't exsist.");
            }
        }
    }
}
