// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace QUIKSharp.DataStructures
{
    public class BuySellInfo
    {
        /// <summary>
        /// Признак маржинальности инструмента. Возможные значения: «0» – не маржинальная; «1» – маржинальная.
        /// </summary>
        [JsonProperty("is_margin_sec")]
        public NorY IsMargin { get; set; }

        /// <summary>
        /// Принадлежность инструмента к списку инструментов, принимаемых в обеспечение. Возможные значения: «0» – не принимается в обеспечение; «1» – принимается в обеспечение.
        /// </summary>
        [JsonProperty("is_asset_sec")]
        public NorY IsAsset { get; set; }

        /// <summary>
        /// 	Текущая позиция по инструменту, в лотах
        /// </summary>
        [JsonProperty("balance")]
        public decimal Balance { get; set; }

        /// <summary>
        /// Оценка количества лотов, доступных на покупку по указанной цене *
        /// </summary>
        [JsonProperty("can_buy")]
        public long CanBuyLots { get; set; }

        /// <summary>
        /// 	Оценка количества лотов, доступных на продажу по указанной цене *
        /// </summary>
        [JsonProperty("can_sell")]
        public long CanSellLots { get; set; }

        /// <summary>
        /// Денежная оценка позиции по инструменту по ценам спроса/предложения
        /// </summary>
        [JsonProperty("position_valuation")]
        public decimal PositionValue { get; set; }

        /// <summary>
        /// Оценка стоимости позиции по цене последней сделки
        /// </summary>
        [JsonProperty("value")]
        public decimal ValueByLastPrice { get; set; }

        /// <summary>
        /// Оценка стоимости позиции клиента, рассчитанная по цене закрытия предыдущей торговой сессии
        /// </summary>
        [JsonProperty("open_value")]
        public decimal open_value { get; set; }

        /// <summary>
        /// Предельный размер позиции по данному инструменту, принимаемый в обеспечение длинных позиций
        /// </summary>
        [JsonProperty("lim_long")]
        public decimal lim_long { get; set; }

        /// <summary>
        /// Коэффициент дисконтирования, применяемый для длинных позиций по данному инструменту
        /// </summary>
        [JsonProperty("long_coef")]
        public decimal long_coef { get; set; }

        /// <summary>
        /// Предельный размер короткой позиции по данному инструменту
        /// </summary>
        [JsonProperty("lim_short")]
        public decimal lim_short { get; set; }

        /// <summary>
        /// 	Коэффициент дисконтирования, применяемый для коротких позиций по данному инструменту
        /// </summary>
        [JsonProperty("short_coef")]
        public decimal short_coef { get; set; }

        /// <summary>
        /// 	Оценка стоимости позиции по цене последней сделки, с учетом дисконтирующих коэффициентов
        /// </summary>
        [JsonProperty("value_coef")]
        public decimal value_coef { get; set; }

        /// <summary>
        /// Оценка стоимости позиции клиента, рассчитанная по цене закрытия предыдущей торговой сессии с учетом дисконтирующих коэффициентов
        /// </summary>
        [JsonProperty("open_value_coef")]
        public decimal open_value_coef { get; set; }

        /// <summary>
        /// Процентное отношение стоимости позиции по данному инструменту к стоимости всех активов клиента, рассчитанное по текущим ценам
        /// </summary>
        [JsonProperty("share")]
        public decimal share { get; set; }

        /// <summary>
        /// Средневзвешенная стоимость коротких позиций по инструментам
        /// </summary>
        [JsonProperty("short_wa_price")]
        public decimal short_wa_price { get; set; }

        /// <summary>
        /// Средневзвешенная стоимость длинных позиций по инструментам
        /// </summary>
        [JsonProperty("long_wa_price")]
        public decimal long_wa_price { get; set; }

        /// <summary>
        /// Разница между средневзвешенной ценой приобретения инструментов и их рыночной оценки
        /// </summary>
        [JsonProperty("profit_loss")]
        public decimal profit_loss { get; set; }

        /// <summary>
        /// Коэффициент корреляции между инструментами
        /// </summary>
        [JsonProperty("spread_hc")]
        public decimal spread_hc { get; set; }

        /// <summary>
        /// Максимально возможное количество инструментов в заявке на покупку этого инструмента на этом классе на собственные средства клиента, исходя из цены лучшего предложения
        /// </summary>
        [JsonProperty("can_buy_own")]
        public long can_buy_own { get; set; }

        /// <summary>
        /// Максимально возможное количество инструментов в заявке на продажу этого инструмента на этом классе из собственных активов клиента, исходя из цены лучшего спроса
        /// </summary>
        [JsonProperty("can_sell_own")]
        public long can_sell_own { get; set; }

    }
}
