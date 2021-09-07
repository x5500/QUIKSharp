// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using QUIKSharp.Converters;

namespace QUIKSharp.DataStructures
{

    /// <summary>
    /// Таблица с параметрами для функции getParamEx
    /// </summary>
    public class ParamTable : IWithLuaTimeStamp
    {
        /// <summary>
        /// Тип данных параметра, используемый в Таблице текущих значений параметров. Возможные значения:
        /// «1» - DOUBLE ,
        /// «2» - LONG,
        /// «3» - CHAR,
        /// «4» - перечислимый тип,
        /// «5» - время,
        /// «6» - дата
        /// </summary>
        [JsonProperty("param_type")]
        public ParamTableType ParamType { get; set; }

        /// <summary>
        /// Значение параметра. Для param_type = 3 значение параметра равно «0», в остальных случаях – числовое представление.
        /// Для перечислимых типов значение равно порядковому значению перечисления
        /// </summary>
        [JsonProperty("param_value")]
        public string ParamValue { get; set; }

        /// <summary>
        /// Строковое значение параметра, аналогичное его представлению в таблице.
        /// В строковом представлении учитываются разделители разрядов, разделители целой и дробной части.
        /// Для перечислимых типов выводятся соответствующие им строковые значения
        /// </summary>
        [JsonProperty("param_image")]
        public string ParamImage { get; set; }

        /// <summary>
        /// Результат выполнения операции. Возможные значения:
        /// «0» – ошибка;
        /// «1» – параметр найден;
        /// </summary>
        [JsonProperty("result")]
        public int Result { get; set; }

        [JsonProperty("lua_timestamp")]
        public LuaTimeStamp lua_timestamp { get; set; }

        [JsonIgnore]
        public object Value
        {
            get
            {
                if (Result == 0) // «0» – ошибка;
                    return null;

                switch (ParamType)
                {
                    case ParamTableType.DOUBLE:
                        return Converters.Number<double>.FromString(ParamValue);
                    case ParamTableType.LONG:
                        return Converters.Number<long>.FromString(ParamValue);
                    case ParamTableType.CHAR:
                        return ParamImage;
                    case ParamTableType.ENUM:
                        return Converters.Number<long>.FromString(ParamValue);
                    case ParamTableType.TIME:
                        return QuikDateTimeConverter.TimeStrToTimeSpan(ParamValue);
                    case ParamTableType.DATE:
                        return QuikDateTimeConverter.QuikDateStrToDateTime(ParamValue);
                };
                return null;
            }
        }
    }
}