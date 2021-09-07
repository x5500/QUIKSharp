// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using NLog;
using System;
using System.ComponentModel;
using System.Globalization;

namespace QUIKSharp.Converters
{
    public static class Number<T>
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public delegate T converterType(string value);

        private static readonly char[] separators = { ',', '.' };
        private static readonly Char separator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator[0];
        public static readonly TypeConverter TypeConverter = TypeDescriptor.GetConverter(typeof(T));
        public static readonly  converterType FromString;
        static Number()
        {
            Type t = typeof(T);
            var underlying = Nullable.GetUnderlyingType(t);
            if (underlying != null) t = underlying;

            if (t.IsValueType)
            {
                if ((t == typeof(decimal)) || (t == typeof(float)) || (t == typeof(double)))
                    FromString = value => ConvertToRealType(value);
                else
                    FromString = value => ConvertToIntegralType(value);
            }
            else if (t.IsEnum)
                FromString = value => ConvertToIntegralType(value);
            else
                FromString = value => ConvertOther(value);
        }
        private static T ConvertOther(string value) => (T)TypeConverter.ConvertFromInvariantString(value);
        private static T ConvertToRealType(string value)
        {
            if (TryConvertBase64Encoded(value, out var result)) return result;
            return (T)TypeConverter.ConvertFromInvariantString(value.Replace(separators[0], separator));
        }
        private static T ConvertToIntegralType(string value)
        {
            if (TryConvertBase64Encoded(value, out var result))
                return result;

            var div = value.IndexOfAny(separators);
            if (div > 0)
                value = value.Remove(div);

            return (T)TypeConverter.ConvertFromInvariantString(value);
        }
        public static bool TryConvertBase64Encoded(string value, out T result)
        {

            try
            {
                if (value.StartsWith("D="))
                {
                    var base64 = value.Substring(2);
                    byte[] bytes = System.Convert.FromBase64String(base64);
                    var value_as_double = BitConverter.ToDouble(bytes, 0);
                    //result = (T)typeConverter_double.ConvertTo(value_as_double, typeof(T));
                    result = (T)Convert.ChangeType(value_as_double, typeof(T));
                    return true;
                }
                if (value.StartsWith("L="))
                {
                    var base64 = value.Substring(2);
                    byte[] bytes = System.Convert.FromBase64String(base64);
                    var value_as_long = BitConverter.ToInt64(bytes, 0);
                    //result = (T)typeConverter.ConvertFrom(value_as_long);
                    result = (T)Convert.ChangeType(value_as_long, typeof(T));
                    return true;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, $"Exception: '{e.Message}' while Decode NUMBER from tBASE64Encoded '{value}'");
                result = default;
                return true;
            }
            result = default;
            return false;
        }

    }
}
