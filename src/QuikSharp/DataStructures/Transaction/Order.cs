// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using QUIKSharp.Converters;
using System;

namespace QUIKSharp.DataStructures.Transaction
{
    /// <summary>
    /// Описание параметров Таблицы заявок
    /// </summary>
    public class Order : IWithLuaTimeStamp, ISecurity
    {
        [JsonProperty("lua_timestamp")]
        public LuaTimeStamp lua_timestamp { get; set; }

        /// <summary>
        /// * Номер заявки в торговой системе
        /// </summary>
        [JsonProperty("order_num")]
        public ulong OrderNum { get; set; }

        /// <summary>
        /// * Поручение/комментарий, обычно: код клиента/номер поручения
        /// </summary>
        [JsonProperty("brokerref")]
        public string Comment { get; set; }

        /// <summary>
        /// * Идентификатор трейдера
        /// </summary>
        [JsonProperty("userid")]
        public string UserId { get; set; }

        /// <summary>
        /// * Идентификатор фирмы
        /// </summary>
        [JsonProperty("firmid")]
        public string FirmId { get; set; }

        /// <summary>
        /// * Торговый счет
        /// </summary>
        [JsonProperty("account")]
        public string Account { get; set; }

        /// <summary>
        /// * Цена
        /// </summary>
        [JsonProperty("price")]
        public decimal Price { get; set; }

        /// <summary>
        ///* Количество в лотах
        /// </summary>
        [JsonProperty("qty")]
        public long Quantity { get; set; }

        /// <summary>
        /// * Остаток
        /// </summary>
        [JsonProperty("balance")]
        public long Balance { get; set; }

        /// <summary>
        /// * Объем в денежных средствах
        /// </summary>
        [JsonProperty("value")]
        public decimal Value { get; set; }

        /// <summary>
        /// * Накопленный купонный доход
        /// </summary>
        [JsonProperty("accruedint")]
        public decimal AccruedInterest { get; set; }

        /// <summary>
        /// * Доходность
        /// </summary>
        [JsonProperty("yield")]
        public decimal Yield { get; set; }

        /// <summary>
        /// * Идентификатор транзакции
        /// </summary>
        [JsonProperty("trans_id")]
        public long TransID { get; set; }

        /// <summary>
        /// * Код клиента
        /// </summary>
        [JsonProperty("client_code")]
        public string ClientCode { get; set; }

        /// <summary>
        /// * Цена выкупа
        /// </summary>
        [JsonProperty("price2")]
        public decimal Price2 { get; set; }

        /// <summary>
        /// * Код расчетов
        /// </summary>
        [JsonProperty("settlecode")]
        public string Settlecode { get; set; }

        /// <summary>
        /// * Идентификатор пользователя
        /// </summary>
        [JsonProperty("uid")]
        public long Uid { get; set; }

        /// <summary>
        /// * Идентификатор пользователя, снявшего заявку
        /// </summary>
        [JsonProperty("canceled_uid")]
        public long CanceledUID { get; set; }

        /// <summary>
        /// * Код биржи в торговой системе
        /// </summary>
        [JsonProperty("exchange_code")]
        public string ExchangeCode { get; set; }

        /// <summary>
        /// * Время активации
        /// </summary>
        [JsonProperty("activation_time")]
        [JsonConverter(typeof(HHMMSS_TimeSpanConverter))]
        public TimeSpan ActivationTime { get; set; }

        /// <summary>
        /// * Номер заявки в торговой системе
        /// </summary>
        [JsonProperty("linkedorder")]
        public ulong Linkedorder { get; set; }

        /// <summary>
        /// Составное поле, дата и время окончания срока действия заявки.
        /// readonly
        /// </summary>
        [JsonIgnore]
        public DateTime ExpiryDateTime { get; private set; }

        /// <summary>
        /// * Время окончания срока действия заявки в формате "ЧЧММСС DESIGNTIMESP=19552". Для GTT-заявок, используется вместе со сроком истечения заявки (Expiry)
        /// </summary>
        [JsonProperty("expiry_time")]
        [JsonConverter(typeof(HHMMSS_TimeSpanConverter))]
        public TimeSpan ExpiryTime
        {
            get => ExpiryDateTime.TimeOfDay;
            set => ExpiryDateTime = ExpiryDateTime.Date + value;
        }

        /// <summary>
        /// * Дата окончания срока действия заявки
        /// </summary>
        [JsonProperty("expiry")]
        [JsonConverter(typeof(YYYYMMDD_DateTimeConverter))]
        public DateTime ExpiryDate
        {
            get => ExpiryDateTime.Date;
            set => ExpiryDateTime = value.Date + ExpiryDateTime.TimeOfDay;
        }

        /// <summary>
        /// Код бумаги заявки
        /// </summary>
        [JsonProperty("sec_code")]
        public string SecCode { get; set; }

        /// <summary>
        /// Код класса заявки
        /// </summary>
        [JsonProperty("class_code")]
        public string ClassCode { get; set; }

        /// <summary>
        /// * Дата и время
        /// </summary>
        [JsonProperty("datetime")]
        public QuikDateTime Datetime { get; set; }

        /// <summary>
        /// * TABLE Время снятия стоп-заявки
        /// </summary>
        [JsonProperty("withdraw_datetime")]
        public QuikDateTime WithdrawDateTime { get; set; }

        /// <summary>
        /// Идентификатор расчетного счета/кода в клиринговой организации
        /// </summary>
        [JsonProperty("bank_acc_id")]
        public string BankAccID { get; set; }

        /// <summary>
        /// Способ указания объема заявки. Возможные значения: «0» – по количеству, «1» – по объему
        /// </summary>
        [JsonProperty("value_entry_type")]
        public ValueEntryType ValueEntryType { get; set; }

        /// <summary>
        /// Срок РЕПО, в календарных днях
        /// </summary>
        [JsonProperty("repoterm")]
        public decimal Repoterm { get; set; }

        /// <summary>
        /// Сумма РЕПО на текущую дату. Отображается с точностью 2 знака
        /// </summary>
        [JsonProperty("repovalue")]
        public decimal Repovalue { get; set; }

        /// <summary>
        /// Объём сделки выкупа РЕПО. Отображается с точностью 2 знака
        /// </summary>
        [JsonProperty("repo2value")]
        public decimal Repo2Value { get; set; }

        /// <summary>
        /// Остаток суммы РЕПО за вычетом суммы привлеченных или предоставленных по сделке РЕПО денежных средств в неисполненной части заявки, по состоянию на текущую дату. Отображается с точностью 2 знака
        /// </summary>
        [JsonProperty("repo_value_balance")]
        public decimal RepoValueBalance { get; set; }

        /// <summary>
        /// Начальный дисконт, в %
        /// </summary>
        [JsonProperty("start_discount")]
        public decimal StartDiscount { get; set; }

        /// <summary>
        /// Причина отклонения заявки брокером
        /// </summary>
        [JsonProperty("reject_reason")]
        public string RejectReason { get; set; }

        /// <summary>
        /// Битовое поле для получения специфических параметров с западных площадок
        /// </summary>
        [JsonProperty("ext_order_flags")]
        public int ExtOrderFlags { get; set; }

        /// <summary>
        /// Минимально допустимое количество, которое можно указать в заявке по данному инструменту. Если имеет значение «0», значит ограничение по количеству не задано
        /// </summary>
        [JsonProperty("min_qty")]
        public int MinQty { get; set; }

        /// <summary>
        /// Тип исполнения заявки. Возможные значения:
        /// «0» – Значение не указано;
        /// «1» – Немедленно или отклонить;
        /// «2» – Поставить в очередь;
        /// «3» – Снять остаток;
        /// «4» – До снятия;
        /// «5» – До даты;
        /// «6» – В течение сессии;
        /// «7» – Открытие;
        /// «8» – Закрытие;
        /// «9» – Кросс;
        /// «11» – До следующей сессии;
        /// «13» – До отключения;
        /// «15» – До времени;
        /// «16» – Следующий аукцион;
        /// </summary>
        [JsonProperty("exec_type")]
        public OrderExecType ExecType { get; set; }

        /// <summary>
        /// Поле для получения параметров по западным площадкам. Если имеет значение «0», значит значение не задано
        /// </summary>
        [JsonProperty("side_qualifier")]
        public int SideQualifier { get; set; }

        /// <summary>
        /// Поле для получения параметров по западным площадкам. Если имеет значение «0», значит значение не задано
        /// </summary>
        [JsonProperty("acnt_type")]
        public int AcntType { get; set; }

        /// <summary>
        /// Роль в исполнении заявки. Возможные значения:
        /// «0» – Не определено;
        /// «1» – Agent;
        /// «2» – Principal;
        /// «3» – Riskless principal;
        /// «4» – CFG give up;
        /// «5» – Cross as agent;
        /// «6» – Matched Principal;
        /// «7» – Proprietary;
        /// «8» – Individual;
        /// «9» – Agent for other member;
        /// «10» – Mixed;
        /// «11» – Market maker;
        /// </summary>
        [JsonProperty("capacity")]
        public int Capacity { get; set; }

        /// <summary>
        /// Поле для получения параметров по западным площадкам. Если имеет значение «0», значит значение не задано
        /// </summary>
        [JsonProperty("passive_only_order")]
        public int PassiveOnlyOrder { get; set; }

        /// <summary>
        /// Видимое количество. Параметр айсберг-заявок, для обычных заявок выводится значение: «0»
        /// </summary>
        [JsonProperty("visible")]
        public int Visible { get; set; }

        /// <summary>
        /// Средняя цена приобретения. Актуально, когда заявка выполнилась частями
        /// </summary>
        [JsonProperty("awg_price")]
        public decimal AwgPrice { get; set; }

        /// <summary>
        /// Номер ревизии заявки. Используется, если заявка была заменена с сохранением номера
        /// </summary>
        [JsonProperty("revision_number")]
        public int RevisionNumber { get; set; }

        /// <summary>
        /// Валюта цены заявки
        /// </summary>
        [JsonProperty("price_currency")]
        public string PriceCurrency { get; set; }

        /// <summary>
        /// Расширенный статус заявки. Возможные значения:
        /// «0» (по умолчанию) – не определено;
        /// «1» – заявка активна;
        /// «2» – заявка частично исполнена;
        /// «3» – заявка исполнена;
        /// «4» – заявка отменена;
        /// «5» – заявка заменена;
        /// «6» – заявка в состоянии отмены;
        /// «7» – заявка отвергнута;
        /// «8» – приостановлено исполнение заявки;
        /// «9» – заявка в состоянии регистрации;
        /// «10» – заявка снята по времени действия;
        /// «11» – заявка в состоянии замены
        /// </summary>
        [JsonProperty("ext_order_status")]
        public ExtOrderStatus ExtOrderStatus { get; set; }

        /// <summary>
        /// UID пользователя-менеджера, подтвердившего заявку при работе в режиме с подтверждениями
        /// </summary>
        [JsonProperty("accepted_uid")]
        public long AcceptedUID { get; set; }

        /// <summary>
        /// Исполненный объем заявки в валюте цены для частично или полностью исполненных заявок
        /// </summary>
        [JsonProperty("filled_value")]
        public int FilledValue { get; set; }

        /// <summary>
        /// Внешняя ссылка, используется для обратной связи с внешними системами
        /// </summary>
        [JsonProperty("extref")]
        public string ExtRef { get; set; }

        /// <summary>
        /// Валюта расчетов по заявке
        /// </summary>
        [JsonProperty("settle_currency")]
        public string SettleCurrency { get; set; }

        /// <summary>
        /// UID пользователя, от имени которого выставлена заявка
        /// </summary>
        [JsonProperty("on_behalf_of_uid")]
        public long OnBehalfOfUID { get; set; }

        /// <summary>
        /// Квалификатор клиента, от имени которого выставлена заявка. Возможные значения:
        /// «0» – не определено;
        /// «1» – Natural Person;
        /// «3» – Legal Entity
        /// </summary>
        [JsonProperty("client_qualifier")]
        public int ClientQualifier { get; set; }

        /// <summary>
        /// Краткий идентификатор клиента, от имени которого выставлена заявка
        /// </summary>
        [JsonProperty("client_short_code")]
        public int ClientShortCode { get; set; }

        /// <summary>
        /// Квалификатор принявшего решение о выставлении заявки. Возможные значения:
        /// «0» – не определено;
        /// «1» – Natural Person;
        /// «2» – Algorithm
        /// </summary>
        [JsonProperty("investment_decision_maker_qualifier")]
        public int InvestmentDecisionMakerQualifier { get; set; }

        /// <summary>
        /// Краткий идентификатор принявшего решение о выставлении заявки
        /// </summary>
        [JsonProperty("investment_decision_maker_short_code")]
        public int InvestmentDecisionMakerShortCode { get; set; }

        /// <summary>
        /// Квалификатор трейдера, исполнившего заявку. Возможные значения:
        /// «0» – не определено;
        /// «1» – Natural Person;
        /// «2» – Algorithm
        /// </summary>
        [JsonProperty("executing_trader_qualifier")]
        public int ExecutingTraderQualifier { get; set; }

        /// <summary>
        /// Краткий идентификатор трейдера, исполнившего заявку
        /// </summary>
        [JsonProperty("executing_trader_short_code")]
        public int ExecutingTraderShortCode { get; set; }

        /// <summary>
        /// Набор битовых флагов
        /// http://help.qlua.org/ch9_1.htm
        /// </summary>
        [JsonProperty("flags")]
        public OrderTradeFlags Flags { get; set; }

        /// <summary>
        /// Тип операции - Buy или Sell
        /// </summary>
        [JsonIgnore]
        public Operation Operation
        {
            get => Flags.HasFlag(OrderTradeFlags.IsSell) ? Operation.Sell : Operation.Buy;
            set => Flags = (value == Operation.Sell) ? (Flags | OrderTradeFlags.IsSell) : (Flags & ~OrderTradeFlags.IsSell);
        }

        /// <summary>
        /// Состояние заявки.
        /// </summary>
        [JsonIgnore]
        public State State
        {
            get => Flags.HasFlag(OrderTradeFlags.Active) ? State.Active
                        : Flags.HasFlag(OrderTradeFlags.Canceled) ? State.Canceled
                        : Flags.HasFlag(OrderTradeFlags.Rejected) ? State.Rejected : State.Completed;
            set
            {
                const OrderTradeFlags clearmask = ~(OrderTradeFlags.Active | OrderTradeFlags.Canceled | OrderTradeFlags.Rejected);
                switch (value)
                {
                    case State.Active:
                        Flags = (Flags & clearmask) | OrderTradeFlags.Active;
                        break;
                    case State.Canceled:
                        Flags = (Flags & clearmask) | OrderTradeFlags.Canceled;
                        break;
                    case State.Rejected:
                        Flags = (Flags & clearmask) | OrderTradeFlags.Rejected;
                        break;
                    case State.Completed:
                        Flags &= clearmask;
                        break;
                }
            }
        }
        /// <summary>
        /// Тип операции - Buy или Sell
        /// </summary>
        [JsonIgnore]
        public bool IsSell
        {
            get => Flags.HasFlag(OrderTradeFlags.IsSell);
            set => Flags = value ? (Flags | OrderTradeFlags.IsSell) : (Flags & ~OrderTradeFlags.IsSell);
        }

        /// <summary>
        /// Признак лимитного ордера
        /// </summary>
        [JsonIgnore]
        public bool IsLimit
        {
            get => Flags.HasFlag(OrderTradeFlags.IsLimit);
            set => Flags = value ? (Flags | OrderTradeFlags.IsLimit) : (Flags & ~OrderTradeFlags.IsLimit);
        }

        /// <summary>
        /// Признак состояние заявки Active
        /// </summary>
        [JsonIgnore]
        public bool IsActive => Flags.HasFlag(OrderTradeFlags.Active);

        /// <summary>
        /// Признак состояние заявки Canceled
        /// </summary>
        [JsonIgnore]
        public bool IsCancelled => !IsActive && Flags.HasFlag(OrderTradeFlags.Canceled);

        /// <summary>
        /// Признак состояние заявки Rejected
        /// </summary>
        [JsonIgnore]
        public bool IsRejected => !IsActive && Flags.HasFlag(OrderTradeFlags.Rejected);

        /// <summary>
        /// Признак состояние заявки Completed
        /// </summary>
        [JsonIgnore]
        public bool IsCompleted => (Flags & (OrderTradeFlags.Active | OrderTradeFlags.Canceled | OrderTradeFlags.Rejected)) == OrderTradeFlags.None;

    }
}