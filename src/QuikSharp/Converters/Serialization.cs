// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NLog;
using QUIKSharp.Transport;
using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace QUIKSharp.Converters
{
    /// <summary>
    /// Extensions for JSON.NET
    /// </summary>
    public static class JsonExtensions
    {
        private static readonly JsonSerializer _serializer;

        [ThreadStatic]
        private static StringBuilder _stringBuilder;

        static JsonExtensions()
        {
            _serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore,
            };
            _serializer.Converters.Add(new NUMBER_Converter<decimal>());
            _serializer.Converters.Add(new NUMBER_Converter<double>());
            _serializer.Converters.Add(new NUMBER_Converter<ulong>());
            _serializer.Converters.Add(new NUMBER_Converter<long>());
            _serializer.Converters.Add(new NUMBER_Converter<int>());
            _serializer.Converters.Add(new SafeEnumConverter());
            _serializer.Converters.Add(new HHMMSS_TimeSpanConverter());
            _serializer.Converters.Add(new YYYYMMDD_DateTimeConverter());
        }

        /// <summary>
        ///
        /// </summary>
        public static T FromJson<T>(this string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                // reader will get buffer from array pool
                reader.ArrayPool = JsonArrayPool.Instance;
                var value = _serializer.Deserialize<T>(reader);
                return value;
            }
        }

        internal static object FromJToken(this JToken jtoken, Type objectType)
        {
            using (var reader = new JTokenReader(jtoken))
            {
                var value = _serializer.Deserialize(reader, objectType);
                return value;
            }
        }

        internal static T FromJToken<T>(this JToken jtoken)
             where T : class, IMessage
        {
            using (var reader = new JTokenReader(jtoken))
            {
                var value = _serializer.Deserialize<T>(reader);
                return value;
            }
        }

        internal static T FromJTokenMessage<T>(this JToken jtoken)
        {
            using (var reader = new JTokenReader(jtoken))
            {
                var value = _serializer.Deserialize<Message<T>>(reader);
                if (value == null)
                    throw new JsonSerializationException("Fail to deserialize Message<T> from JToken: result == null");

                if (typeof(IWithLuaTimeStamp).IsAssignableFrom(typeof(T)))
                {
                    ((IWithLuaTimeStamp)value.Data).lua_timestamp = value.CreatedTime;
                }
                return value.Data;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static string ToJson<T>(this T obj)
        {
            if (_stringBuilder == null)
            {
                _stringBuilder = new StringBuilder();
            }

            using (var writer = new JsonTextWriter(new StringWriter(_stringBuilder)))
            {
                try
                {
                    // reader will get buffer from array pool
                    writer.ArrayPool = JsonArrayPool.Instance;
                    _serializer.Serialize(writer, obj);
                    return _stringBuilder.ToString();
                }
                finally
                {
                    _stringBuilder.Clear();
                }
            }
        }

        /// <summary>
        /// Returns indented JSON
        /// </summary>
        public static string ToJsonFormatted<T>(this T obj)
        {
            var message = JsonConvert.SerializeObject(obj, Formatting.Indented,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None, // Objects
                    Formatting = Formatting.Indented,
                    // NB this is important for correctness and performance
                    // Transaction could have many null properties
                    NullValueHandling = NullValueHandling.Ignore
                });
            return message;
        }
    }

    /// <summary>
    /// Limits enum serialization only to defined values
    /// </summary>
    public class SafeEnumConverter : StringEnumConverter
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public override bool CanConvert(Type objectType)
        {
            if (objectType.IsEnum) return true;
            var underlying = Nullable.GetUnderlyingType(objectType);
            return (underlying != null) && underlying.IsEnum;
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.String:
                    {
                        var int_val = Number<int>.FromString((string)reader.Value);
                        return Enum.ToObject(objectType, int_val);
                    }
                case JsonToken.Integer:
                case JsonToken.Float:
                    {
                        var int_val = Convert.ToInt32(reader.Value);
                        return Enum.ToObject(objectType, int_val);
                    }
                default:
                    throw new Exception($"SafeEnumConverter: Unexpected token. Expected Token: String or Integer,Float, got '{reader.TokenType}'.");
            }
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!Enum.IsDefined(value.GetType(), value))
            {
                logger.Error($"SafeEnumConverter: Try Serialize Enum({value.GetType().Name}) with value not defined in Enum: '{value}'. Null value will be written.");
                value = null;
            }
            base.WriteJson(writer, value, serializer);
        }
    }

    public class NUMBER_Converter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (objectType.Equals(typeof(T))) return true;
            var underlying = Nullable.GetUnderlyingType(objectType);
            return (underlying != null) && underlying.Equals(typeof(T));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken.FromObject(value.ToString()).WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.String:
                    {
                        string encodedData = reader.Value.ToString();
                        var obj = Number<T>.FromString(encodedData);
                        return obj;
                    }
                case JsonToken.Integer:
                case JsonToken.Float:
                    {
                        if (reader.ValueType.Equals(typeof(T)))
                            return reader.Value;
                        var obj = Convert.ChangeType(reader.Value, typeof(T));
                        return obj;
                    }
                default:
                    throw new Exception($"Unexpected token parsing NUMBER. Expected String, got '{reader.TokenType.ToString()}'.");
            }
        }
    }

    /// <summary>
    /// Serialize Decimal to string without trailing zeros
    /// </summary>
    public class DecimalG29ToStringConverter : NUMBER_Converter<decimal>
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => JToken.FromObject(((decimal)value).ToString("G29")).WriteTo(writer);
    }

    /// <summary>
    /// Convert TimeSpan to HHMMSS
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class HHMMSS_TimeSpanConverter : NUMBER_Converter<int>
    {
        public override bool CanConvert(Type objectType) => (objectType.Equals(typeof(TimeSpan)) || objectType.Equals(typeof(TimeSpan?)));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            int as_int = (int)base.ReadJson(reader, objectType, existingValue, serializer);
            if (as_int <= 0) return null;
            var result = QuikDateTimeConverter.HHmmssToTimeSpan(as_int);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken.FromObject(QuikDateTimeConverter.ToHHMMSS((TimeSpan)value)).WriteTo(writer);
        }
    }

    /// <summary>
    /// Convert text string YYYYMMDD to DateTime
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class YYYYMMDD_DateTimeConverter : NUMBER_Converter<int>
    {
        public override bool CanConvert(Type objectType) => (objectType.Equals(typeof(DateTime)) || objectType.Equals(typeof(DateTime?)));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            int as_int = (int)base.ReadJson(reader, objectType, existingValue, serializer);
            if (as_int < 10101) return null;
            var result = QuikDateTimeConverter.QuikDateStrToDateTime(as_int);
            return result;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => JToken.FromObject(QuikDateTimeConverter.ToYYYYMMDD((DateTime)value)).WriteTo(writer);
    }
    public class JsonArrayPool : IArrayPool<char>
    {
        public static readonly JsonArrayPool Instance = new JsonArrayPool();

        public char[] Rent(int minimumLength)
        {
            // get char array from System.Buffers shared pool
            return ArrayPool<char>.Shared.Rent(minimumLength);
        }

        public void Return(char[] array)
        {
            // return char array to System.Buffers shared pool
            ArrayPool<char>.Shared.Return(array);
        }
    }
}