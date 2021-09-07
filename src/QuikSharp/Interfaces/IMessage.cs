// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using System;

namespace QUIKSharp
{
    public interface IMessage
    {
        /// <summary>
        /// Unique correlation id to match requests and responses
        /// </summary>
        long Id { get; set; }

        /// <summary>
        /// A name of a function to call for requests
        /// </summary>
        string Command { get; set; }

        /// <summary>
        /// Timestamp in milliseconds, same as in Lua `socket.gettime() * 1000`
        /// </summary>
        long CreatedTime { get; set; }

        /// <summary>
        /// Some messages are valid only for a short time, e.g. buy/sell orders
        /// </summary>
        DateTime? ValidUntil { get; set; }

        object Data { get; set; }
    }
}