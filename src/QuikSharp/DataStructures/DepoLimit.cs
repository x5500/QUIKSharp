// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// При обработке изменения бумажного лимита функция возвращает таблицу Lua с параметрами
    /// </summary>
    public class DepoLimit
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Стоимость ценных бумаг, заблокированных на покупку
        /// </summary>
        [JsonProperty("depo_limit_locked_buy_value")]
        public decimal LockedBuyValue { get; set; }

        /// <summary>
        /// Текущий остаток по бумагам
        /// </summary>
        [JsonProperty("depo_current_balance")]
        public decimal DepoCurrentBalance { get; set; }

        /// <summary>
        /// Количество лотов ценных бумаг, заблокированных на покупку
        /// </summary>
        [JsonProperty("depo_limit_locked_buy")]
        public long LockedBuy { get; set; }

        /// <summary>
        /// Заблокированное Количество лотов ценных бумаг
        /// </summary>
        [JsonProperty("depo_limit_locked")]
        public long LockedLimit { get; set; }

        /// <summary>
        /// Доступное количество ценных бумаг
        /// </summary>
        [JsonProperty("depo_limit_available")]
        public long AvailableLimit { get; set; }

        /// <summary>
        /// Текущий лимит по бумагам
        /// </summary>
        [JsonProperty("depo_current_limit")]
        public long CurrentLimit { get; set; }

        /// <summary>
        /// Входящий остаток по бумагам
        /// </summary>
        [JsonProperty("depo_open_balance")]
        public decimal DepoOpenBalance { get; set; }

        /// <summary>
        /// Входящий остаток по бумагам
        /// </summary>
        [JsonProperty("depo_open_limit")]
        public long DepoOpenLimit { get; set; }

        // ReSharper restore InconsistentNaming
    }
}