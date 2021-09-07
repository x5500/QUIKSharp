// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// При обработке удаления бумажного лимита функция возвращает таблицу Lua с параметрами
    /// </summary>
    public class DepoLimitDelete
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Код инструмента
        /// </summary>
        [JsonProperty("sec_code")]
        public string SecCode { get; set; }

        /// <summary>
        /// Код торгового счета
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