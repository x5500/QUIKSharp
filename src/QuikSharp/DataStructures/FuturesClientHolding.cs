// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using Newtonsoft.Json;
namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// Описание параметров Таблицы позиций по клиентским счетам (фьючерсы):
    /// </summary>
    public class FuturesClientHolding : IWithLuaTimeStamp
    {
        /// <summary>
        /// Идентификатор фирмы
        /// </summary>
        [JsonProperty("firmid")]
        public string FirmId { get; set; }

        /// <summary>
        /// Торговый счет
        /// </summary>
        [JsonProperty("trdaccid")]
        public string TrdAccId { get; set; }

        /// <summary>
        /// Код фьючерсного контракта
        /// </summary>
        [JsonProperty("sec_code")]
        public string SecCode { get; set; }

        /// <summary>
        /// Тип лимита. Возможные значения:
        /// «Основной счет»;
        /// «Клиентские и дополнительные счета»;
        /// «Все счета торг. членов»;
        /// </summary>
        [JsonProperty("type")]
        public FuturesHoldingLimitType type { get; set; }

        /// <summary>
        /// Входящие длинные позиции
        /// </summary>
        [JsonProperty("startbuy")]
        public long startBuy { get; set; }

        /// <summary>
        /// Входящие короткие позиции
        /// </summary>
        [JsonProperty("startsell")]
        public long startSell { get; set; }

        /// <summary>
        /// Входящие чистые позиции
        /// </summary>
        [JsonProperty("startnet")]
        public long startNet { get; set; }

        /// <summary>
        /// Текущие длинные позиции
        /// </summary>
        [JsonProperty("todaybuy")]
        public long todayBuy { get; set; }

        /// <summary>
        /// Текущие короткие позиции
        /// </summary>
        [JsonProperty("todaysell")]
        public long todaySell { get; set; }

        /// <summary>
        /// Текущие чистые позиции
        /// </summary>
        [JsonProperty("totalnet")]
        public long totalNet { get; set; }

        /// <summary>
        /// Активные на покупку
        /// </summary>
        [JsonProperty("openbuys")]
        public long openBuys { get; set; }

        /// <summary>
        /// Активные на продажу
        /// </summary>
        [JsonProperty("opensells")]
        public long openSells { get; set; }

        /// <summary>
        /// Оценка текущих чистых позиций
        /// </summary>
        [JsonProperty("cbplused")]
        public decimal cbPlUsed { get; set; }

        /// <summary>
        /// Плановые чистые позиции
        /// </summary>
        [JsonProperty("cbplplanned")]
        public decimal cbpPPlanned { get; set; }

        /// <summary>
        /// Вариационная маржа
        /// </summary>
        [JsonProperty("varmargin")]
        public decimal varMargin { get; set; }

        /// <summary>
        /// Эффективная цена позиций
        /// </summary>
        [JsonProperty("avrposnprice")]
        public decimal avrPosnPrice { get; set; }

        /// <summary>
        /// Стоимость позиций
        /// </summary>
        [JsonProperty("positionvalue")]
        public decimal positionValue { get; set; }

        /// <summary>
        /// Реально начисленная в ходе клиринга вариационная маржа.
        /// Отображается с точностью до 2 двух знаков.
        /// При этом, в поле «varmargin» транслируется вариационная маржа, рассчитанная с учетом установленных границ изменения цены
        /// </summary>
        [JsonProperty("real_varmargin ")]
        public decimal realVarMargin { get; set; }

        /// <summary>
        /// Суммарная вариационная маржа по итогам основного клиринга начисленная по всем позициям.
        /// Отображается с точностью до 2 двух знаков
        /// </summary>
        [JsonProperty("total_varmargin ")]
        public decimal totalVarMargin { get; set; }

        /// <summary>
        /// Актуальный статус торговой сессии. Возможные значения:
        /// «0» – не определено;
        /// «1» – основная сессия;
        /// «2» – начался промклиринг;
        /// «3» – завершился промклиринг;
        /// «4» – начался основной клиринг;
        /// «5» – основной клиринг: новая сессия назначена;
        /// «6» – завершился основной клиринг;
        /// «7» – завершилась вечерняя сессия
        /// </summary>
        [JsonProperty("session_status ")]
        public FuturesSessionStatus SessionStatus { get; set; }

        [JsonProperty("lua_timestamp")]
        public LuaTimeStamp lua_timestamp { get; set; }
    }

    /// <summary>
    /// Актуальный статус торговой сессии. Возможные значения:
    /// «0» – не определено;
    /// «1» – основная сессия;
    /// «2» – начался промклиринг;
    /// «3» – завершился промклиринг;
    /// «4» – начался основной клиринг;
    /// «5» – основной клиринг: новая сессия назначена;
    /// «6» – завершился основной клиринг;
    /// «7» – завершилась вечерняя сессия
    /// </summary>
    public enum FuturesSessionStatus
    {
        NotDefined,
        MainSession = 1,
        PromCleaningStarted = 2,
        PromCleaningEnded = 3,
        MainCleaninStarted = 4,
        MainCleaningNewSession = 5,
        MainCleaningEnded = 6,
        EveningSessionEnded = 7,
    }
}