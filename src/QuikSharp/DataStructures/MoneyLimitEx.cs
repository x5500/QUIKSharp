// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// Лимиты по денежным средствам
    /// </summary>
    public class MoneyLimitEx
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Код валюты
        /// </summary>
        [JsonProperty("currcode")]
        public string CurrCode { get; set; }

        /// <summary>
        /// Тэг расчетов
        /// </summary>
        [JsonProperty("tag")]
        public string Tag { get; set; }

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
        /// Входящий остаток по деньгам
        /// </summary>
        [JsonProperty("openbal")]
        public decimal OpenBal { get; set; }

        /// <summary>
        /// Входящий лимит по деньгам
        /// </summary>
        [JsonProperty("openlimit")]
        public decimal OpenLimit { get; set; }

        /// <summary>
        /// Текущий остаток по деньгам
        /// </summary>
        [JsonProperty("currentbal")]
        public decimal CurrentBal { get; set; }

        /// <summary>
        /// Текущий лимит по деньгам
        /// </summary>
        [JsonProperty("currentlimit")]
        public decimal CurrentLimit { get; set; }

        /// <summary>
        /// Заблокированное количество
        /// </summary>
        [JsonProperty("locked")]
        public decimal Locked { get; set; }

        /// <summary>
        /// Стоимость активов в заявках на покупку немаржинальных бумаг
        /// </summary>
        [JsonProperty("locked_value_coef")]
        public decimal LockedValueCoef { get; set; }

        /// <summary>
        /// Стоимость активов в заявках на покупку маржинальных бумаг
        /// </summary>
        [JsonProperty("locked_margin_value")]
        public decimal LockedMarginValue { get; set; }

        /// <summary>
        /// Плечо
        /// </summary>
        [JsonProperty("leverage")]
        public decimal Leverage { get; set; }

        /// <summary>
        /// Средневзвешенная цена приобретения позиции
        /// </summary>
        [JsonProperty("wa_position_price")]
        public decimal WaPositionPrice { get; set; }

        /// <summary>
        /// Гарантийное обеспечение заявок
        /// </summary>
        [JsonProperty("orders_collateral")]
        public decimal OrdersCollateral { get; set; }

        /// <summary>
        /// Гарантийное обеспечение позиций
        /// </summary>
        [JsonProperty("positions_collateral")]
        public decimal PositionsCollateral { get; set; }

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

        // ReSharper restore InconsistentNaming
    }
}