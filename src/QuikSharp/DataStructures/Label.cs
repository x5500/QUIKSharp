﻿// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using QUIKSharp.Converters;
using System;

namespace QUIKSharp.DataStructures
{
    public enum LabelAlignment
    {
        LEFT,
        RIGHT,
        TOP,
        BOTTOM
    }

    /// <summary>
    /// Формат таблицы с параметрами метки (получаемая при помощи GetLabelParams)
    /// Наименование параметров метки в возвращаемой таблице указаны в нижнем регистре, и все значения имеют тип – STRING.
    /// </summary>
    public class Label
    {
        /// <summary>
        /// * Значение параметра на оси Y, к которому будет привязана метка.
        /// </summary>
        [JsonProperty("yvalue")]
        public decimal YValue { get; set; }

        /// <summary>
        /// Составное поле, дата и время бара на графике (координата по оси Х), к которой будет привязана метка.
        /// </summary>
        [JsonIgnore]
        public DateTime XValue { get; set; }

        /// <summary>
        /// Дата в формате «ГГГГММДД», к которой привязана метка
        /// </summary>
        [JsonProperty("date")]
        [JsonConverter(typeof(YYYYMMDD_DateTimeConverter))]
        public DateTime XDate
        {
            get => this.XValue.Date;
            set => this.XValue = value.Date + XValue.TimeOfDay;
        }

        /// <summary>
        /// Время в формате «ЧЧММСС», к которому будет привязана метка
        /// </summary>
        [JsonProperty("time")]
        [JsonConverter(typeof(HHMMSS_TimeSpanConverter))]
        public TimeSpan XTime
        {
            get => this.XValue.TimeOfDay;
            set => this.XValue = XValue.Date + value;
        }

        /// <summary>
        /// * Подпись метки (если подпись не требуется, то пустая строка).
        /// Хотя бы один из параметров Text или ImagePath должен быть задан.
        /// Если не был задан, то возвращается пустая строка.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// * Путь к картинке, которая будет отображаться в качестве метки (пустая строка, если картинка не требуется).
        /// Используются картинки формата *.bmp, *.jpeg
        /// Хотя бы один из параметров Text или ImagePath должен быть задан.
        /// Если не был задан, то возвращается пустая строка.
        /// </summary>
        [JsonProperty("image_path")]
        public string ImagePath { get; set; }

        /// <summary>
        /// * Расположение картинки относительно точки. Текст распологается аналогично относительно картинки.
        /// (возможно 4 варианта: LEFT, RIGHT, TOP, BOTTOM)   -- по умолчанию LEFT
        /// Если не был задан, то возвращается "LEFT"
        /// </summary>
        [JsonProperty("alignment")]
        public LabelAlignment Alignment { get; set; }

        /// <summary>
        /// * Текст всплывающей подсказки
        /// Если не был задан, то возвращается пустая строка.
        /// </summary>
        [JsonProperty("hint")]
        public string Hint { get; set; }

        /// <summary>
        /// * Красная компонента цвета в формате RGB. Число в интервале [0;255]
        /// По умолчанию 0. Если не был задан, то возвращается "0".
        /// </summary>
        [JsonProperty("r")]
        public int Red { get; set; }

        /// <summary>
        /// * Зеленая компонента цвета в формате RGB. Число в интервале [0;255]
        /// По умолчанию 0. Если не был задан, то возвращается "0".
        /// </summary>
        [JsonProperty("g")]
        public int Green { get; set; }

        /// <summary>
        /// * Синяя компонента цвета в формате RGB. Число в интервале [0;255]
        /// По умолчанию 0. Если не был задан, то возвращается "0".
        /// </summary>
        [JsonProperty("b")]
        public int Blue { get; set; }

        /// <summary>
        /// * Прозрачность метки (картинки) в процентах. Значение должно быть в промежутке [0; 100]
        /// Если не был задан, то возвращается "0".
        /// </summary>
        [JsonProperty("transparency")]
        public int Transparency { get; set; }

        /// <summary>
        /// * Прозрачность фона картинки. Возможные значения: «0» – прозрачность отключена, «1» – прозрачность включена.
        /// По умолчанию = 0. Если == 1, то картинка не рисуется, и у текста исчезает фон. Если == 0, то картинка
        /// рисуется и у текста есть черный фон. если картинка отсутствует и нужен только текст, то делать = 1.
        /// Если не был задан, то возвращается "0".
        /// </summary>
        [JsonProperty("transparent_background")]
        public int TranBackgrnd { get; set; }

        /// <summary>
        /// * Название шрифта (по умолчанию = "Arial")
        /// Если не был задан, то возвращается пустая строка.
        /// </summary>
        [JsonProperty("font_face_name")]
        public string FontName { get; set; }

        /// <summary>
        /// * Размер шрифта (по умолчанию = 12)
        /// Если не был задан, то возвращается "0".
        /// </summary>
        [JsonProperty("font_height")]
        public int FontHeight { get; set; }

        internal string ToMsg()
        {
            var strDate = QuikDateTimeConverter.DateTimeToYYYYMMDD(this.XDate);
            var strTime = QuikDateTimeConverter.TimeSpanToHHMMSS(this.XTime);

            return string.Join("|", new string[]
            {
                this.YValue.ToString(),
                strDate,
                strTime,
                this.Text,
                this.ImagePath,
                this.Alignment.ToString(),
                this.Hint,
                this.Red.ToString(),
                this.Green.ToString(),
                this.Blue.ToString(),
                this.Transparency.ToString(),
                this.TranBackgrnd.ToString(),
                this.FontName,
                this.FontHeight.ToString()
            });
        }
    }
}