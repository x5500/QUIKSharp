// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using System;

namespace QUIKSharp.DataStructures
{
    public class LuaTimeStamp
    {
        private readonly long timestamp;

        public LuaTimeStamp(long timestamp) => this.timestamp = timestamp;

        public static implicit operator LuaTimeStamp(long lua_timestamp) =>
            new LuaTimeStamp(lua_timestamp);

        public static DateTime DateTime(LuaTimeStamp lts) =>
            new DateTime(1970, 1, 1).AddMilliseconds(lts.timestamp).ToLocalTime();

        public static implicit operator DateTime(LuaTimeStamp lts) =>
            new DateTime(1970, 1, 1).AddMilliseconds(lts.timestamp).ToLocalTime();

        public static implicit operator long(LuaTimeStamp lts) => lts.timestamp;

        public static implicit operator double(LuaTimeStamp lts) => lts.timestamp;

        public override bool Equals(object obj)
        {
            return Equals(obj);
        }

        public override int GetHashCode()
        {
            return timestamp.GetHashCode();
        }

        public override string ToString()
        {
            return timestamp.ToString();
        }
    }
}