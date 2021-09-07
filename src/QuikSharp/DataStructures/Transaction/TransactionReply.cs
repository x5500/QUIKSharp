﻿// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace QUIKSharp.DataStructures.Transaction
{
    /// <summary>
    /// Результат OnTransReply
    /// </summary>
    public class TransactionReply : IWithLuaTimeStamp, ISecurity
    {
        /// <summary>
        /// Пользовательский идентификатор транзакции
        /// </summary>
        [JsonProperty("trans_id")]
        public long TransID { get; set; }

        /// <summary>
        /// Статус
        /// «0» - транзакция отправлена серверу,
        /// «1» - транзакция получена на сервер QUIK от клиента,
        /// «2» - ошибка при передаче транзакции в торговую систему, поскольку отсутствует подключение шлюза Московской Биржи, повторно транзакция не отправляется,
        /// «3» - транзакция выполнена,
        /// «4» - транзакция не выполнена торговой системой, код ошибки торговой системы будет указан в поле «DESCRIPTION»,
        /// «5» - транзакция не прошла проверку сервера QUIK по каким-либо критериям. Например, проверку на наличие прав у пользователя на отправку транзакции данного типа,
        /// «6» - транзакция не прошла проверку лимитов сервера QUIK,
        /// «10» - транзакция не поддерживается торговой системой. К примеру, попытка отправить «ACTION = MOVE_ORDERS» на Московской Бирже,
        /// «11» - транзакция не прошла проверку правильности электронной подписи. К примеру, если ключи, зарегистрированные на сервере, не соответствуют подписи отправленной транзакции.
        /// «12» - не удалось дождаться ответа на транзакцию, т.к. истек таймаут ожидания. Может возникнуть при подаче транзакций из QPILE.
        /// «13» - транзакция отвергнута, т.к. ее выполнение могло привести к кросс-сделке (т.е. сделке с тем же самым клиентским счетом).
        /// «14» – транзакция не прошла контроль дополнительных ограничений
        /// «15» – транзакция принята после нарушения дополнительных ограничений
        /// «16» – транзакция отменена пользователем в ходе проверки дополнительных ограничений
        /// </summary>
        [JsonProperty("status")]
        public TransactionReplyStatus Status { get; set; }

        /// <summary>
        /// Сообщение
        /// </summary>
        [JsonProperty("result_msg")]
        public string ResultMsg { get; set; }

        /// <summary>
        ///Дата и время (в QLUA представлено как число)
        /// </summary>
        [JsonProperty("date_time")]
        public QuikDateTime Time { get; set; }

        /// <summary>
        /// Идентификатор пользователя у брокера. Для каждого брокера он свой и меняться не должен.
        /// </summary>
        [JsonProperty("uid")]
        public long Uid { get; set; }

        /// <summary>
        /// Флаги транзакции (временно не используется)
        /// </summary>
        [JsonProperty("flags")]
        public long Flags { get; set; }

        /// <summary>
        /// Идентификатор транзакции на сервере
        /// </summary>
        [JsonProperty("server_trans_id")]
        public long ServerTransID { get; set; }

        /// <summary>
        /// Номер заявки
        /// </summary>
        [JsonProperty("order_num")]
        public long? OrderNum { get; set; }

        /// <summary>
        /// Цена
        /// </summary>
        [JsonProperty("price")]
        public decimal? Price { get; set; }

        /// <summary>
        /// Количество
        /// </summary>
        [JsonProperty("quantity")]
        public long? Quantity { get; set; }

        /// <summary>
        /// Неисполненнный Остаток
        /// для транзакции на снятие неисполненной (в т.ч. частично) заявки.
        /// </summary>
        [JsonProperty("balance")]
        public long? Balance { get; set; }

        /// <summary>
        /// Идентификатор фирмы
        /// </summary>
        [JsonProperty("firm_id")]
        public string FirmID { get; set; }

        /// <summary>
        /// Торговый счет
        /// </summary>
        [JsonProperty("account")]
        public string Account { get; set; }

        /// <summary>
        /// Код клиента
        /// </summary>
        [JsonProperty("client_code")]
        public string ClientCode { get; set; }

        /// <summary>
        /// Комментарий, обычно: код клиента // номер поручения
        /// </summary>
        [JsonProperty("brokerref")]
        public string Brokerref { get; set; }

        /// <summary>
        /// Код класса
        /// </summary>
        [JsonProperty("class_code")]
        public string ClassCode { get; set; }

        /// <summary>
        /// Код бумаги
        /// </summary>
        [JsonProperty("sec_code")]
        public string SecCode { get; set; }

        /// <summary>
        /// Биржевой номер заявки
        /// </summary>
        [JsonProperty("exchange_code")]
        public string ExchangeCode { get; set; }

        /// <summary>
        /// Числовой код ошибки. Значение равно «0», если транзакция выполнена успешно
        /// </summary>
        [JsonProperty("error_code")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// Источник сообщения. Возможные значения:
        /// «1» – Торговая система;
        /// «2» – Сервер QUIK;
        /// «3» – Библиотека расчёта лимитов;
        /// «4» – Шлюз торговой системы
        /// </summary>
        [JsonProperty("error_source")]
        public int ErrorSource { get; set; }

        /// <summary>
        /// Номер первой заявки, которая выставлялась при автоматической замене кода клиента. Используется, если на сервере QUIK настроена замена кода клиента для кросс-сделки
        /// </summary>
        [JsonProperty("first_ordernum")]
        public long FirstOrderNum { get; set; }

        /// <summary>
        /// Дата и время получения шлюзом ответа на транзакцию
        /// </summary>
        [JsonProperty("gate_reply_time")]
        public QuikDateTime GateReplyTime { get; set; }

        [JsonProperty("lua_timestamp")]
        public LuaTimeStamp lua_timestamp { get; set; }
    }
}