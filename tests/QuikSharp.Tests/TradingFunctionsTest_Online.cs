using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QUIKSharp.DataStructures;

namespace QUIKSharp.Tests
{
    [TestFixture]
    public class TradingFunctionsTest_Online
    {
        class GetParamEx2BulkTestClass : IStructClassSecParam
        {
            public string ClassCode { get; set; }
            public string SecCode { get; set; }
            public ParamNames paramName { get; set; }
            public GetParamEx2BulkTestClass(string classCode, string secCode, ParamNames paramName)
            {
                this.ClassCode = classCode;
                this.SecCode = secCode;
                this.paramName = paramName;
            }
        }


        [Test]
        public void GetDepoLimitsTest()
        {
            Quik quik = new Quik();

            // Получаем информацию по всем лимитам со всех доступных счетов.
            List<DepoLimitEx> depoLimits = quik.Trading.GetDepoLimits().Result;
            Console.WriteLine($"Все лимиты со всех доступных счетов {depoLimits.Count}");
            if (depoLimits.Count > 0)
                PrintDepoLimits(depoLimits);

            // Получаем информацию по лимитам инструмента "Сбербанк"
            depoLimits = quik.Trading.GetDepoLimits("SBER").Result;
            Console.WriteLine($"Лимиты инструмента сбербанк {depoLimits.Count}");
            if (depoLimits.Count > 0)
                PrintDepoLimits(depoLimits);

            // Если информация по бумаге есть в таблице, это не значит что открыта позиция. Нужно проверять еще CurrentBalance
            DepoLimitEx depoLimit = depoLimits.SingleOrDefault(_ => _.LimitKind == LimitKind.T2 && _.CurrentBalance > 0);
            if (depoLimit != null)
                Console.WriteLine("Открыта позиция по сбербанку.");

        }

        [Test()]
        public void GetParamEx2BulkTest()
        {
            Char separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
            Quik quik = new Quik();
            var req = new List<GetParamEx2BulkTestClass>();
            req.Add(new GetParamEx2BulkTestClass("TQBR", "VTBR", ParamNames.LAST));
            req.Add(new GetParamEx2BulkTestClass("TQBR", "VTBR", ParamNames.LASTBID));
            req.Add(new GetParamEx2BulkTestClass("TQBR", "VTBR", ParamNames.LASTOFFER));

            var LastClose = new GetParamEx2BulkTestClass("TQBR", "VTBR", ParamNames.LASTCLOSE);
            req.Add(LastClose);

            var res = quik.Trading.GetParamEx2Bulk(req).ConfigureAwait(false).GetAwaiter().GetResult();

            Assert.IsTrue(req.Count == res.Count, "Result Count != Request Count");

            Assert.IsTrue(res[0].Result == 1, "Not found Param?");

            decimal price;
            string value = res[0].ParamValue.Replace('.', separator);
            Assert.IsTrue(decimal.TryParse(value, out price), $"Try parse '{value}' to decimal failed!"); ; ;

            Assert.IsTrue(price != 0);
        }

        private void PrintDepoLimits(List<DepoLimitEx> depoLimits)
        {
            Console.WriteLine($"Количество стро: {depoLimits.Count}");
            foreach (var depo in depoLimits)
            {
                Console.WriteLine($"Код бумаги: {depo.SecCode}");
                Console.WriteLine($"Счет депо: {depo.TrdAccId}");
                Console.WriteLine($"Идентификатор фирмы: {depo.FirmId}");
                Console.WriteLine($"Код клиента: {depo.ClientCode}");
                Console.WriteLine($"Входящий остаток по бумагам: {depo.OpenBalance}");
                Console.WriteLine($"Входящий лимит по бумагам: {depo.OpenLimit}");
                Console.WriteLine($"Текущий остаток по бумагам: {depo.CurrentBalance}");
                Console.WriteLine($"Текущий лимит по бумагам: {depo.CurrentLimit}");
                Console.WriteLine($"Заблокировано на продажу количества лотов: {depo.LockedSell}");
                Console.WriteLine($"Заблокированного на покупку количества лотов: {depo.LockedBuy}");
                Console.WriteLine($"Стоимость ценных бумаг, заблокированных под покупку: {depo.LockedBuyValue}");
                Console.WriteLine($"Стоимость ценных бумаг, заблокированных под продажу: {depo.LockedSellValue}");
                Console.WriteLine($"Цена приобретения: {depo.AweragePositionPrice}");
                Console.WriteLine($"Тип лимита.  = «0» обычные лимиты, <> «0» – технологические лимиты: {depo.LimitKindInt}");
                Console.WriteLine($"Тип лимита бумаги (Т0, Т1 или Т2): {depo.LimitKind}");
                Console.WriteLine("------------------------------------------------------------------------");
            }
        }
    }
}