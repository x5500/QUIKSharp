// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// На основе: https://euvgub.github.io/qlua_user_guide/ch4_5_16.htm
    /// Запись, которую можно получить из таблицы "Лимиты по бумагам" (depo_limits)
    /// </summary>
    public class BuySellInfoEx : BuySellInfo
    {
        /// <summary>
        /// Эффективный начальный дисконт для длинной позиции. Заполняется для клиентов типа «МД»
        /// </summary>
        [JsonProperty("d_long")]
        public decimal d_long { get; set; }

        /// <summary>
        /// Эффективный минимальный дисконт для длинной позиции. Заполняется для клиентов типа «МД»
        /// </summary>
        [JsonProperty("d_min_long")]
        public decimal d_min_long { get; set; }

        /// <summary>
        /// Эффективный начальный дисконт для короткой позиции. Заполняется для клиентов типа «МД»
        /// </summary>
        [JsonProperty("d_short")]
        public decimal d_short { get; set; }

        /// <summary>
        /// Эффективный минимальный дисконт для короткой позиции. Заполняется для клиентов типа «МД»
        /// </summary>
        [JsonProperty("d_min_short")]
        public decimal d_min_short { get; set; }

        /// <summary>
        /// 	Тип клиента. Возможные значения: «1» – «МЛ»; «2» – «МП»; «3» – «МОП»; «4» – «МД»
        /// </summary>
        [JsonProperty("client_type")]
        public BuySellClientType ClientType { get; set; }

        /// <summary>
        /// Признак того, является ли инструмент разрешенным для покупки на заемные средства.
        /// Возможные значения: «0» – не разрешен; «1» – разрешен;
        /// Заполняется для клиентов типа «МД»
        /// </summary>
        [JsonProperty("is_long_allowed")]
        public NorY IsLongAllowed { get; set; }

        /// <summary>
        /// Признак того, является ли инструмент разрешенным для покупки на заемные средства.
        /// Возможные значения: «0» – не разрешен; «1» – разрешен;
        /// Заполняется для клиентов типа «МД»
        /// </summary>
        [JsonProperty("is_short_allowed")]
        public NorY IsShortAllowed { get; set; }

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

    public enum BuySellClientType
    {
        ML = 1,
        MP = 2,
        MOP = 3,
        MD = 4,
    }
}