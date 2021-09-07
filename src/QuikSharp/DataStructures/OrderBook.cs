// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using QUIKSharp.Converters;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// Стакан
    /// </summary>
    public class OrderBook : IWithLuaTimeStamp, ISecurity
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Строка стакана
        /// </summary>
        public class PriceQuantity
        {
            /// <summary>
            /// Цена покупки / продажи
            /// </summary>
            public decimal price { get; set; }

            /// <summary>
            /// Количество в лотах
            /// </summary>
            [JsonConverter(typeof(NUMBER_Converter<long>))]
            public long quantity { get; set; }
        }

        /// <summary>
        /// sec_code  STRING  Код бумаги
        /// </summary>
        [JsonProperty("sec_code")]
        public string SecCode { get; set; }

        /// <summary>
        /// class_code  STRING  Код бумаги
        /// </summary>
        [JsonProperty("class_code")]
        public string ClassCode { get; set; }

        /// <summary>
        /// time in msec from lua epoch
        /// </summary>
        public LuaTimeStamp lua_timestamp { get; set; }

        /// <summary>
        /// Result of getInfoParam("SERVERTIME") right before getQuoteLevel2 call
        /// </summary>
        public string server_time { get; set; }

        /// <summary>
        /// Количество котировок покупки
        /// </summary>
        [JsonConverter(typeof(NUMBER_Converter<long>))]
        public long bid_count { get; set; }

        /// <summary>
        /// Количество котировок продажи
        /// </summary>
        [JsonConverter(typeof(NUMBER_Converter<long>))]
        public long offer_count { get; set; }

        /// <summary>
        /// Котировки спроса (покупки)
        /// </summary>
        public PriceQuantity[] bid { get; set; }

        /// <summary>
        /// Котировки предложений (продажи)
        /// </summary>
        public PriceQuantity[] offer { get; set; }

        // ReSharper restore InconsistentNaming
    }
}