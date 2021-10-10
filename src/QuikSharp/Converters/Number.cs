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
        private static readonly Type myType = typeof(T);
        public delegate T converterType(string value);

        private static readonly char[] separators = { ',', '.' };
        private static readonly Char separator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator[0];
        public static readonly TypeConverter TypeConverter = TypeDescriptor.GetConverter(typeof(T));
        public static readonly  converterType FromString;
        static Number()
        {
            var underlying = Nullable.GetUnderlyingType(myType);
            if (underlying != null) myType = underlying;

            if (myType.IsValueType)
            {
                if ((myType == typeof(decimal)) || (myType == typeof(float)) || (myType == typeof(double)))
                    FromString = value => ConvertToRealType(value);
                else
                    FromString = value => ConvertToIntegralType(value);
            }
            else if (myType.IsEnum)
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
                    Span<byte> src_bytes = System.Text.Encoding.UTF8.GetBytes(value);
                    byte[] dst_bytes = new byte[8];
                    System.Buffers.Text.Base64.DecodeFromUtf8(src_bytes.Slice(2,12), dst_bytes, out _, out _);
                    var decoded = BitConverter.ToDouble(dst_bytes, 0);
                    result = (T)Convert.ChangeType(decoded, myType);
                    return true;
                }
                if (value.StartsWith("L="))
                {
                    Span<byte> src_bytes = System.Text.Encoding.UTF8.GetBytes(value);
                    byte[] dst_bytes = new byte[8];
                    System.Buffers.Text.Base64.DecodeFromUtf8(src_bytes.Slice(2, 12), dst_bytes, out _, out _);
                    var decoded = BitConverter.ToDouble(dst_bytes, 0);
                    result = (T)Convert.ChangeType(decoded, myType);
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
    public static class Price
    {
        public static string PriceToString(this decimal price) => price.ToString("G29");
        public static string ToString(decimal price) => price.ToString("G29");
    }
}
