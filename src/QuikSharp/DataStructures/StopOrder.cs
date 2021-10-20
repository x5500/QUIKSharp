// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using QUIKSharp.Converters;
using System;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// Стоп-заявка
    /// На основе http://help.qlua.org/ch4_6_6.htm
    /// </summary>
    public class StopOrder : ISecurity
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty("lua_timestamp")]
        public long LuaTimeStamp { get; set; }

        /// <summary>
        /// * Регистрационный номер стоп-заявки на сервере QUIK
        /// </summary>
        [JsonProperty("order_num")]
        public ulong OrderNum { get; set; }

        /// <summary>
        /// * Набор битовых флагов.
        /// </summary>
        [JsonProperty("flags")]
        public StopOrderFlags Flags { get; set; }

        /// <summary>
        /// * Поручение/комментарий, обычно: код клиента/номер поручения
        /// </summary>
        [JsonProperty("brokerref")]
        public string Comment { get; set; }

        /// <summary>
        /// * Торговый счет
        /// </summary>
        [JsonProperty("account")]
        public string Account { get; set; }

        /// <summary>
        /// * Направленность стоп-цены.
        /// </summary>
        [JsonProperty("condition")]
        public StopOrderCondition Condition { get; set; }

        /// <summary>
        /// * Стоп-цена
        /// </summary>
        [JsonProperty("condition_price")]
        public decimal ConditionPrice { get; set; }

        /// <summary>
        /// * Цена
        /// </summary>
        [JsonProperty("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// * Количество в лотах
        /// </summary>
        [JsonProperty("qty")]
        public long Quantity { get; set; }

        /// <summary>
        /// * Номер заявки в торговой системе, зарегистрированной по наступлению условия стоп-цены.
        /// </summary>
        [JsonProperty("linkedorder")]
        public ulong LinkedOrder { get; set; }

        /// <summary>
        ///  * NUMBER  Дата окончания срока действия заявки
        /// </summary>
        [JsonProperty("expiry")]
        [JsonConverter(typeof(YYYYMMDD_DateTimeConverter))]
        public DateTime Expiry;

        /// <summary>
        /// * Идентификатор транзакции.
        /// </summary>
        [JsonProperty("trans_id")]
        public long TransID { get; set; }

        /// <summary>
        /// * Код клиента
        /// </summary>
        [JsonProperty("client_code")]
        public string ClientCode { get; set; }

        /// <summary>
        /// * NUMBER  Связанная заявка
        /// </summary>
        [JsonProperty("co_order_num")]
        public ulong co_order_num { get; set; }

        /// <summary>
        /// * NUMBER  Цена связанной заявки
        /// </summary>
        [JsonProperty("co_order_price")]
        public decimal co_order_price { get; set; }

        /// <summary>
        /// * Вид стоп заявки.
        /// «1» – стоп-лимит;
        /// «2» – условие по другому инструменту;
        /// «3» – со связанной заявкой;
        /// «6» – тейк-профит;
        /// «7» – стоп-лимит по исполнению активной заявки;
        /// «8» – тейк-профит по исполнению активной заявки;
        /// «9» - тейк-профит и стоп-лимит
        /// </summary>
        [JsonProperty("stop_order_type")]
        public StopOrderType StopOrderType { get; set; }

        /// <summary>
        /// * NUMBER  Сделка условия
        /// </summary>
        [JsonProperty("alltrade_num")]
        public long AlltradeNum;

        /// <summary>
        /// * stopflags  NUMBER  Набор битовых флагов
        /// </summary>
        [JsonProperty("stopflags")]
        public StopBehaviorFlags StopFlags { get; set; }

        /// <summary>
        /// * Отступ от min/max
        /// </summary>
        [JsonProperty("offset")]
        public decimal Offset { get; set; }

        /// <summary>
        /// * Защитный спред
        /// </summary>
        [JsonProperty("spread")]
        public decimal Spread { get; set; }

        /// <summary>
        /// * Остаток
        /// </summary>
        [JsonProperty("balance")]
        public long Balance { get; set; }

        /// <summary>
        /// * Идентификатор пользователя
        /// </summary>
        [JsonProperty("uid")]
        public long Uid { get; set; }

        /// <summary>
        /// * Исполненное количество
        /// </summary>
        [JsonProperty("filled_qty")]
        public long FilledQuantity { get; set; }

        /// <summary>
        /// * TABLE Время снятия заявки
        /// </summary>
        [JsonProperty("withdraw_time")]
        public string WithdrawTime;

        /// <summary>
        /// * Стоп-лимит цена (для заявок типа «Тэйк-профит и стоп-лимит»)
        /// </summary>
        [JsonProperty("condition_price2")]
        public decimal ConditionPrice2 { get; set; }

        /// <summary>
        /// * NUMBER  Время начала периода действия заявки типа «Тэйк-профит и стоп-лимит»
        /// </summary>
        [JsonProperty("active_from_time")]
        [JsonConverter(typeof(HHMMSS_TimeSpanConverter))]
        public TimeSpan ActiveFromTime { get; set; }

        /// <summary>
        /// * NUMBER  Время окончания периода действия заявки типа «Тэйк-профит и стоп-лимит»
        /// </summary>
        [JsonProperty("active_to_time")]
        [JsonConverter(typeof(HHMMSS_TimeSpanConverter))]
        public TimeSpan ActiveToTime { get; set; }

        /// <summary>
        /// * Код бумаги заявки
        /// </summary>
        [JsonProperty("sec_code")]
        public string SecCode { get; set; }

        /// <summary>
        /// * Код класса заявки
        /// </summary>
        [JsonProperty("class_code")]
        public string ClassCode { get; set; }

        /// <summary>
        /// * STRING  Код инструмента стоп-цены
        /// </summary>
        [JsonProperty("condition_sec_code")]
        public string ConditionSecCode { get; set; }

        /// <summary>
        /// * STRING  Код класса стоп-цены
        /// </summary>
        [JsonProperty("condition_class_code")]
        public string ConditionClassCode { get; set; }

        /// <summary>
        /// * STRING идентификатор пользователя отменившего заявку
        /// </summary>
        [JsonProperty("canceled_uid")]
        public int CanceledUID { get; set; }

        /// <summary>
        /// * TABLE Время выставления стоп-заявки
        /// </summary>
        [JsonProperty("order_date_time")]
        public QuikDateTime OrderDateTime;

        /// <summary>
        /// * TABLE Время снятия стоп-заявки
        /// </summary>
        [JsonProperty("withdraw_datetime")]
        public QuikDateTime WithdrawDateTime { get; set; }

        /// <summary>
        /// * TABLE Дата и время активации стоп-заявки
        /// </summary>
        [JsonProperty("activation_date_time")]
        public QuikDateTime ActivationDateTime;

        /// <summary>
        /// Единицы измерения отступа
        /// </summary>
        [JsonIgnore]
        public OffsetUnits OffsetUnit
        {
            get => StopFlags.HasFlag(StopBehaviorFlags.PercentageOffset) ? OffsetUnits.PERCENTS : OffsetUnits.PRICE_UNITS;
            set => StopFlags = (value == OffsetUnits.PERCENTS) ? (StopFlags | StopBehaviorFlags.PercentageOffset) : (StopFlags & ~StopBehaviorFlags.PercentageOffset);
        }

        /// <summary>
        /// Единицы измерения защитного спреда
        /// </summary>
        [JsonIgnore]
        public OffsetUnits SpreadUnit
        {
            get => StopFlags.HasFlag(StopBehaviorFlags.PercentageSpread) ? OffsetUnits.PERCENTS : OffsetUnits.PRICE_UNITS;
            set => StopFlags = (value == OffsetUnits.PERCENTS) ? (StopFlags | StopBehaviorFlags.PercentageSpread) : (StopFlags & ~StopBehaviorFlags.PercentageSpread);
        }

        /// <summary>
        /// Заявка на продажу, иначе – на покупку.
        /// </summary>
        [JsonIgnore]
        public Operation Operation
        {
            get => Flags.HasFlag(StopOrderFlags.Sell) ? Operation.Sell : Operation.Buy;
            set => Flags = (value == Operation.Sell) ? (Flags | StopOrderFlags.Sell) : (Flags & ~StopOrderFlags.Sell);
        }

        /// <summary>
        /// Состояние стоп-заявки.
        /// </summary>
        [JsonIgnore]
        public State State => Flags.HasFlag(StopOrderFlags.Active) ? State.Active : Flags.HasFlag(StopOrderFlags.Canceled) ? State.Canceled : State.Completed;

        /// <summary>
        /// Стоп-заявка ожидает активации.
        /// </summary>
        [JsonIgnore]
        public bool IsWaitingActivation => Flags.HasFlag(StopOrderFlags.WaitingActivation);

        /// <summary>
        /// Заявка на продажу, иначе – на покупку.
        /// </summary>
        [JsonIgnore]
        public bool IsSell => Flags.HasFlag(StopOrderFlags.Sell);
    }

    [Flags]
    public enum StopOrderFlags
    {
        None = 0,

        /// <summary>
        /// бит 0 (0x1)  Заявка активна, иначе не активна
        /// </summary>
        Active = 0x1,

        /// <summary>
        /// бит 1 (0x2)  Заявка снята. Если не установлен и значение бита 0 равно 0, то заявка исполнена
        /// </summary>
        Canceled = 0x2,

        /// <summary>
        /// бит 2 (0x4)  Заявка на продажу, иначе – на покупку
        /// </summary>
        Sell = 0x4,

        /// <summary>
        ///  бит 3 (0x8)  Лимитированная заявка
        /// </summary>
        Limit = 0x8,

        /// <summary>
        /// Бит 4
        /// </summary>
        Bit4 = 0x10,

        /// <summary>
        /// бит 5 (0x20)  Стоп-заявка ожидает активации
        /// </summary>
        WaitingActivation = 0x20,

        /// <summary>
        /// бит 6 (0x40)  Стоп-заявка с другого сервера
        /// </summary>
        Foreign = 0x40,

        /// <summary>
        /// бит 8 (0x100)  Устанавливается в случае стоп-заявки типа тейк-профита по заявке,
        /// в случае когда исходная заявка частично исполнена и по выставленной тейк-профит заявке на исполненную часть заявки выполнилось условие активации
        /// </summary>
        PartialTake = 0x100,

        /// <summary>
        /// бит 9 (0x200)  Стоп-заявка активирована вручную
        /// </summary>
        ManualActivation = 0x200,

        /// <summary>
        /// бит 10 (0x400)  Стоп-заявка сработала, но была отвергнута торговой системой
        /// </summary>
        RejectedOnActivation = 0x400,

        /// <summary>
        /// бит 11 (0x800)  Стоп-заявка сработала, но не прошла контроль лимитов
        /// </summary>
        RejectedOnLimits = 0x800,

        /// <summary>
        /// бит 12 (0x1000)  Стоп-заявка снята, так как снята связанная заявка
        /// </summary>
        WithdrawOnLinked = 0x1000,

        /// <summary>
        /// бит 13 (0x2000)  Стоп-заявка снята, так как связанная заявка исполнена
        /// </summary>
        WithdrawOnExecuted = 0x2000,

        /// <summary>
        /// бит 15 (0x8000)  Идет расчет минимума-максимума
        /// </summary>
        CalculationUnderway = 0x8000,
    }

    [Flags]
    public enum StopBehaviorFlags
    {
        None = 0,

        /// <summary>
        /// бит 0 (0x1)  Использовать остаток основной заявки
        /// </summary>
        UseRemains = 0x1,

        /// <summary>
        /// бит 1 (0x2)  При частичном исполнении заявки снять стоп-заявку
        /// </summary>
        KillIfPartlyFilled = 0x2,

        /// <summary>
        /// бит 2 (0x4)  Активировать стоп-заявку при частичном исполнении связанной заявки
        /// </summary>
        ActivateOnPartial = 0x4,

        /// <summary>
        /// бит 3 (0x8)  Отступ задан в процентах, иначе – в пунктах цены
        /// </summary>
        PercentageOffset = 0x8,

        /// <summary>
        /// бит 4 (0x10)  Защитный спред задан в процентах, иначе – в пунктах цены
        /// </summary>
        PercentageSpread = 0x10,

        /// <summary>
        /// бит 5 (0x20)  Срок действия стоп-заявки ограничен сегодняшним днем
        /// </summary>
        ExpireEndOfDay = 0x20,

        /// <summary>
        /// бит 6 (0x40)  Установлен интервал времени действия стоп-заявки
        /// </summary>
        UseActionInterval = 0x40,

        /// <summary>
        /// бит 7 (0x80)  Выполнение тейк-профита по рыночной цене
        /// </summary>
        MarketTakeProfit = 0x80,

        /// <summary>
        /// бит 8 (0x100)  Выполнение стоп-заявки по рыночной цене
        /// </summary>
        MarketStop = 0x100,
    };

    public enum StopOrderType
    {
        NotImplemented,

        /// <summary>
        /// «1» – стоп-лимит
        /// </summary>
        SimpleStopOrder = 1,

        ///«2» – условие по другому инструменту,
        AnotherInstCondition = 2,

        //«3» – со связанной заявкой,
        WithLinkedOrder = 3,

        /// <summary>
        ///«6» – тейк-профит
        /// </summary>
        TakeProfit = 6,

        //«7» – стоп-лимит по исполнению активной заявки,
        StopLimitOnActiveOrderExecution = 7,

        //«8» –  тейк-профит по исполнению активной заявки,
        TakeProfitOnActiveOrderExecution = 8,

        /// <summary>
        /// «9» - тэйк-профит и стоп-лимит
        /// </summary>
        TakeProfitStopLimit = 9,

        TPSLOnActiveOrderExecution = 10,
    }

    /// <summary>
    /// Направленность стоп-цены. Возможные значения.
    /// </summary>
    public enum StopOrderCondition
    {
        NotImplemented,

        /// <summary>
        /// «4» – меньше или равно
        /// </summary>
        LessOrEqual = 4,

        /// <summary>
        /// «5» – больше или равно
        /// </summary>
        MoreOrEqual = 5,
    }

    public enum Operation
    {
        Buy,
        Sell
    }
}