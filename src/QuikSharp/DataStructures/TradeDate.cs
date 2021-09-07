// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// При получении описания новой фирмы от сервера функция возвращает таблицу Lua с параметрами
    /// </summary>
    public class TradeDate
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        ///  STRING Торговая дата в виде строки «ДД.ММ.ГГГГ»
        /// </summary>
        [JsonProperty("date")]
        //[JsonConverter(typeof(YYYYMMDD_DateTimeConverter))]
        public string DateStr { get; set; }

        /// <summary>
        ///  NUMBER Год
        /// </summary>
        [JsonProperty("year")]
        public int Year { get; set; }

        /// <summary>
        /// NUMBER Месяц
        /// </summary>
        [JsonProperty("month")]
        public int Month { get; set; }

        /// <summary>
        /// NUMBER День
        /// </summary>
        [JsonProperty("day")]
        public int Day { get; set; }

        // ReSharper restore InconsistentNaming

        public DateTime ToDateTime()
        {
            return new DateTime(this.Year, this.Month, this.Day);
        }
    }
}