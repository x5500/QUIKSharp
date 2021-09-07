// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// При обработке изменения денежного лимита функция getMoney возвращает таблицу Lua с параметрами:
    /// </summary>
    public class MoneyLimit
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Входящий лимит по денежным средствам
        /// </summary>
        [JsonProperty("money_open_limit")]
        public decimal MoneyOpenLimit { get; set; }

        /// <summary>
        /// Стоимость немаржинальных бумаг в заявках на покупку
        /// </summary>
        [JsonProperty("money_limit_locked_nonmarginal_value")]
        public decimal MoneyLimitLockedNonmarginalValue { get; set; }

        /// <summary>
        /// Заблокированное в заявках на покупку количество денежных средств
        /// </summary>
        [JsonProperty("money_limit_locked")]
        public decimal MoneyLimitLocked { get; set; }

        /// <summary>
        /// Входящий остаток по денежным средствам
        /// </summary>
        [JsonProperty("money_open_balance")]
        public decimal MoneyOpenBalance { get; set; }

        /// <summary>
        /// Текущий лимит по денежным средствам
        /// </summary>
        [JsonProperty("money_current_limit")]
        public decimal MoneyCurrentLimit { get; set; }

        /// <summary>
        /// Текущий остаток по денежным средствам
        /// </summary>
        [JsonProperty("money_current_balance")]
        public decimal MoneyCurrentBalance { get; set; }

        /// <summary>
        /// Доступное количество денежных средств
        /// </summary>
        [JsonProperty("money_limit_available")]
        public decimal MoneyLimitAvailable { get; set; }

        // ReSharper restore InconsistentNaming
    }
}