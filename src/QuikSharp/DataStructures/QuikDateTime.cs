// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using System;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// Формат даты и времени, используемый таблицах.
    /// Для корректного отображения даты и времени все параметры должны быть заданы.
    /// </summary>
    public class QuikDateTime : IComparable<QuikDateTime>, IComparable<DateTime>, IEquatable<QuikDateTime>, IEquatable<DateTime>
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Микросекунды игнорируются в текущей версии.
        /// </summary>
        public int mcs { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int ms { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int sec { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int min { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int hour { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int day { get; set; }

        /// <summary>
        /// Monday is 1
        /// </summary>
        public int week_day { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int month { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int year { get; set; }

        // ReSharper restore InconsistentNaming
        /// <summary>
        ///
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public QuikDateTime(DateTime dt)
        {
            year = dt.Year;
            month = dt.Month;
            day = dt.Day;
            hour = dt.Hour;
            min = dt.Minute;
            sec = dt.Second;
            ms = dt.Millisecond;
            mcs = 0;
            week_day = (int)dt.DayOfWeek;
        }

        public static DateTime DateTime(QuikDateTime qdt) => new DateTime(qdt.year, qdt.month, qdt.day, qdt.hour, qdt.min, qdt.sec, qdt.ms);
        public static implicit operator QuikDateTime(DateTime dt) => new QuikDateTime(dt);
        public static implicit operator DateTime(QuikDateTime qdt) => new DateTime(qdt.year, qdt.month, qdt.day, qdt.hour, qdt.min, qdt.sec, qdt.ms);
        public static implicit operator QuikDateTime(double dd) => new QuikDateTime(new DateTime().AddSeconds(dd));
        public DateTime ToDateTime(IFormatProvider formatProvider) => new DateTime(this.year, this.month, this.day, this.hour, this.min, this.sec, this.ms);
        public DateTime ToDateTime() => new DateTime(this.year, this.month, this.day, this.hour, this.min, this.sec, this.ms);
        public int CompareTo(QuikDateTime qdt2)
        {
            if (year < qdt2.year) return -1;
            if (year > qdt2.year) return 1;

            if (month < qdt2.month) return -1;
            if (month > qdt2.month) return 1;

            if (day < qdt2.day) return -1;
            if (day > qdt2.day) return 1;

            if (hour < qdt2.hour) return -1;
            if (hour > qdt2.hour) return 1;

            if (min < qdt2.min) return -1;
            if (min > qdt2.min) return 1;

            if (sec < qdt2.sec) return -1;
            if (sec > qdt2.sec) return 1;

            if (ms < qdt2.ms) return -1;
            if (ms > qdt2.ms) return 1;

            if (mcs < qdt2.mcs) return -1;
            if (mcs > qdt2.mcs) return 1;

            return 0;
        }

        public int CompareTo(DateTime other)
            => DateTime(this).CompareTo(other);

        public bool Equals(QuikDateTime other)
            => this.CompareTo(other) == 0;

        public bool Equals(DateTime other)
            => DateTime(this).Equals(other);

        public override string ToString()
            => DateTime(this).ToString();

        public string ToString(string format, IFormatProvider formatProvider)
            => DateTime(this).ToString(format, formatProvider);

        public string ToString(IFormatProvider provider)
            => DateTime(this).ToString(provider);

        static public TypeCode GetTypeCode() => TypeCode.DateTime;

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(DateTime))
            {
                return DateTime(this);
            }
            throw new NotImplementedException();
        }
    }
}