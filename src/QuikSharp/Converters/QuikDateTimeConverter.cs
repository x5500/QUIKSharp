// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using QUIKSharp.DataStructures;
using System;
using System.Text;

namespace QUIKSharp.Converters
{
    public static class QuikDateTimeConverter
    {
        /// <summary>
        /// Конвертируем время [H]Hmmss[.ffff] из квика
        /// </summary>
        /// <param name="time_str">Время в формате Hmmss или HHmmss.ffff</param>
        /// <returns></returns>
        public static TimeSpan HHmmssToTimeSpan(string time_str)
        {
            int div, HH, mm, ss;
            div = time_str.IndexOf('.');
            if (div < 0) div = time_str.Length;
            if (div < 5)
            {
                HH = 0; mm = 0; ss = 0;
            }
            else
            {
                HH = div == 6 ? (time_str[0] - '0') * 10 + (time_str[1] - '0') : time_str[0] - '0';
                mm = (time_str[div - 4] - '0') * 10 + (time_str[div - 3] - '0');
                ss = (time_str[div - 2] - '0') * 10 + (time_str[div - 1] - '0');
            }
            return new TimeSpan(HH, mm, ss);
        }

        /// <summary>
        /// Конвертируем время [H]Hmmss[.ffff] из квика
        /// </summary>
        /// <param name="quik_time_val">Время в формате Hmmss (Int value)</param>
        /// <returns></returns>
        public static TimeSpan HHmmssToTimeSpan(int quik_time_val)
        {
            var HH = quik_time_val / 10000;
            var mm = (quik_time_val % 10000) / 100;
            var ss = quik_time_val / 100;
            return new TimeSpan(HH, mm, ss);
        }


        /// <summary>
        /// Конвертируем время HH:mm:ss из квика
        /// </summary>
        /// <param name="time_str">Время в формате HH:mm:ss</param>
        /// <returns></returns>
        public static TimeSpan TimeStrToTimeSpan(string time_str)
        {
            if (string.IsNullOrEmpty(time_str))
                return TimeSpan.Zero;

            var div = time_str.IndexOf(':');
            if (div < 0)
            { // пхоже, что формат HHmmss[.ffff]
                return HHmmssToTimeSpan(time_str);
            }
            else
            { // Формат HH:mm:ss
                div = time_str.Length;
                var HH = div == 8 ? (time_str[0] - '0') * 10 + (time_str[1] - '0') : time_str[0] - '0';
                var mm = (time_str[div - 5] - '0') * 10 + (time_str[div - 4] - '0');
                var ss = (time_str[div - 2] - '0') * 10 + (time_str[div - 1] - '0');
                return new TimeSpan(HH, mm, ss);
            }
        }

        /// <summary>
        /// Конвертируем дату в формате dd.MM.yyyy или yyyyMMdd из квика
        /// </summary>
        /// <param name="date_str">Дата в формате dd.MM.yyyy или yyyyMMdd</param>
        /// <returns></returns>
        public static DateTime QuikDateStrToDateTime(string date_str)
        {
            int yyyy, MM, dd;
            int div = date_str.IndexOf('.');
            if (div < 4 && date_str.Length == 10)
            { // Формат dd.MM.yyyy
                dd = (date_str[0] - '0') * 10 + (date_str[1] - '0');
                MM = (date_str[3] - '0') * 10 + (date_str[4] - '0');
                yyyy = (date_str[6] - '0') * 1000 + (date_str[7] - '0') * 100 + (date_str[8] - '0') * 10 + (date_str[9] - '0');
            }
            else if ((div < 0 && date_str.Length == 8)||(div == 8))
            { // Формат yyyyMMdd или yyyyMMdd.0000000
                yyyy = (date_str[0] - '0') * 1000 + (date_str[1] - '0') * 100 + (date_str[2] - '0') * 10 + (date_str[3] - '0');
                MM = (date_str[4] - '0') * 10 + (date_str[5] - '0');
                dd = (date_str[6] - '0') * 10 + (date_str[7] - '0');
            }
            else
                throw new ArgumentException($"date_str is in unsupported format: '{date_str}'");

            return new DateTime(yyyy, MM, dd);
        }

        /// <summary>
        /// Конвертируем дату в формате yyyyMMdd  (as int) из квика
        /// </summary>
        /// <param name="date_val">Дата в формате yyyyMMdd (as int)</param>
        /// <returns></returns>
        public static DateTime QuikDateStrToDateTime(int date_val)
        {
            if (date_val < 10101)
                return DateTime.MinValue;

            // Формат yyyyMMdd
            int yyyy = date_val / 10000;
            int MM = (date_val % 10000) / 100;
            int dd = date_val % 100;
            return new DateTime(yyyy, MM, dd);
        }

        /// <summary>
        /// Конвертируем дату и время из квика
        /// </summary>
        /// <param name="date_str">Дата в формате dd.MM.yyyy или yyyyMMdd</param>
        /// <param name="time_str">Время в формате HHmmss или HH:mm:ss или HHmmss.ffff</param>
        /// <returns></returns>
        public static DateTime QuikDateTimeStrToDateTime(string date_str, string time_str)
        {
            var res = QuikDateStrToDateTime(date_str);

            var div = time_str.IndexOf(':');
            if (div < 0)
            { // Формат HHmmss[.ffff]
                res += HHmmssToTimeSpan(time_str);
            }
            else
            { // Формат HH:mm:ss
                res += TimeStrToTimeSpan(time_str);
            }
            return res;
        }

        /// <summary>
        /// Конвертирует TimeSpan в строку в формате HHMMSS
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static string ToHHMMSS(this TimeSpan ts)
        {
            var res = new StringBuilder("000000", 6);
            int hh = ts.Hours;
            int mm = ts.Minutes;
            int ss = ts.Seconds;

            res[0] = (char)('0' + hh / 10);
            res[1] = (char)('0' + hh % 10);
            res[2] = (char)('0' + mm / 10);
            res[3] = (char)('0' + mm % 10);
            res[4] = (char)('0' + ss / 10);
            res[5] = (char)('0' + ss % 10);

            return res.ToString();
        }

        /// <summary>
        /// Конвертирует TimeSpan в строку в формате HH:MM:SS
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static string ToHH_MM_SS(this TimeSpan ts)
        {
            var res = new StringBuilder("00:00:00", 8);
            int hh = ts.Hours;
            int mm = ts.Minutes;
            int ss = ts.Seconds;

            res[0] = (char)('0' + hh / 10);
            res[1] = (char)('0' + hh % 10);
            res[3] = (char)('0' + mm / 10);
            res[4] = (char)('0' + mm % 10);
            res[6] = (char)('0' + ss / 10);
            res[7] = (char)('0' + ss % 10);

            return res.ToString();
        }

        public static string ToYYYYMMDD(this DateTime dateTime)
        {
            var str = new StringBuilder("00000000", 8);
            int dd = dateTime.Day;
            int mm = dateTime.Month;
            int yy = dateTime.Year;

            str[0] = (char)('0' + yy / 1000);
            yy %= 1000;
            str[1] = (char)('0' + yy / 100);
            yy %= 100;
            str[2] = (char)('0' + yy / 10);
            str[3] = (char)('0' + yy % 10);

            str[4] = (char)('0' + mm / 10);
            str[5] = (char)('0' + mm % 10);

            str[6] = (char)('0' + dd / 10);
            str[7] = (char)('0' + dd % 10);

            return str.ToString();
        }

        public static DateTime ToDateTime(this TradeDate trd) => new DateTime(trd.Year, trd.Month, trd.Day);

    }
}