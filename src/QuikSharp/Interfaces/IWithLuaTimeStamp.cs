// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using QUIKSharp.DataStructures;

namespace QUIKSharp
{
    /// <summary>
    ///
    /// </summary>
    public interface IWithLuaTimeStamp
    {
        // TODO change to TimeStamp without refactoring and add cast to DateTime
        // then replace all assignments.
        /// <summary>
        /// Lua timestamp
        /// </summary>
        [JsonProperty("lua_timestamp")]
        LuaTimeStamp lua_timestamp { get; set; }
    }
}