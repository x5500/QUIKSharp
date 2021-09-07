// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// На основе: http://help.qlua.org/ch4_6_11.htm
    /// Запись, которую можно получить из таблицы "Лимиты по бумагам" (depo_limits)
    /// </summary>
    public class DepoLimits
    {
        /// <summary>
        /// Код бумаги
        /// </summary>
        [JsonProperty("sec_code")]
        public string SecCode { get; set; }

        /// <summary>
        /// Счет депо
        /// </summary>
        [JsonProperty("trdaccid")]
        public string TrdAccId { get; set; }

        /// <summary>
        /// Идентификатор фирмы
        /// </summary>
        [JsonProperty("firmid")]
        public string FirmId { get; set; }

        /// <summary>
        /// Код клиента
        /// </summary>
        [JsonProperty("client_code")]
        public string ClientCode { get; set; }

        /// <summary>
        /// Входящий остаток по бумагам
        /// </summary>
        [JsonProperty("openbal")]
        public long OpenBalance { get; set; }

        /// <summary>
        /// Входящий лимит по бумагам
        /// </summary>
        [JsonProperty("openlimit")]
        public long OpenLimit { get; set; }

        /// <summary>
        /// Текущий остаток по бумагам
        /// </summary>
        [JsonProperty("currentbal")]
        public long CurrentBalance { get; set; }

        /// <summary>
        /// Текущий лимит по бумагам
        /// </summary>
        [JsonProperty("currentlimit")]
        public long CurrentLimit { get; set; }

        /// <summary>
        /// Заблокировано на продажу количества лотов
        /// </summary>
        [JsonProperty("locked_sell")]
        public long LockedSell { get; set; }

        /// <summary>
        /// Заблокированного на покупку количества лотов
        /// </summary>
        [JsonProperty("locked_buy")]
        public long LockedBuy { get; set; }

        /// <summary>
        /// Стоимость ценных бумаг, заблокированных под покупку
        /// </summary>
        [JsonProperty("locked_buy_value")]
        public decimal LockedBuyValue { get; set; }

        /// <summary>
        /// Стоимость ценных бумаг, заблокированных под продажу
        /// </summary>
        [JsonProperty("locked_sell_value")]
        public decimal LockedSellValue { get; set; }

        /// <summary>
        /// Цена приобретения
        /// </summary>
        [JsonProperty("wa_position_price")]
        public decimal AweragePositionPrice { get; set; }

        /// <summary>
        /// Тип лимита. Возможные значения:
        /// «0»,«1»,«2»,«365» – обычные лимиты,
        /// значение меньше «0» – технологические лимиты
        /// </summary>
        [JsonProperty("limit_kind")]
        public int LimitKindInt
        {
            get { return (int)LimitKind; }
            set
            {
                switch (value)
                {
                    case 0:
                        LimitKind = LimitKind.T0;
                        break;

                    case 1:
                        LimitKind = LimitKind.T1;
                        break;

                    case 2:
                        LimitKind = LimitKind.T2;
                        break;

                    case 365:
                        LimitKind = LimitKind.T365;
                        break;

                    default:
                        LimitKind = LimitKind.NotImplemented;
                        break;
                }
            }
        }

        /// <summary>
        /// Тип лимита бумаги (Т0, Т1 или Т2).
        /// </summary>
        [JsonIgnore]
        public LimitKind LimitKind { get; private set; }
    }

    /// <summary>
    /// Тим лимита бумаги.
    /// </summary>
    public enum LimitKind
    {
        /// <summary>
        /// Тип лимита T0
        /// </summary>
        T0 = 0,

        /// <summary>
        /// Тип лимита Т1
        /// </summary>
        T1 = 1,

        /// <summary>
        /// Тип лимита Т2
        /// </summary>
        T2 = 2,

        /// <summary>
        /// Тип лимита Тx (365)
        /// </summary>
        T365 = 365,

        /// <summary>
        /// Не учтенный в данной структуре тип лимита..
        /// </summary>
        NotImplemented = -1
    }
}