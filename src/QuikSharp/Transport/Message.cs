// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace QUIKSharp.Transport
{

    /// <summary>
    /// Default generic implementation
    /// </summary>
    internal class Message<T> : IMessage
    {
        protected static readonly long Epoch = new DateTime(1970, 1, 1, 3, 0, 0, 0).Ticks / 10000L;

        /// <summary>
        /// Unique correlation id to match requests and responses
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public long Id { get; set; } = default;

        /// <summary>
        /// A name of a function to call for requests
        /// </summary>
        [JsonProperty(PropertyName = "cmd")]
        public string Command { get; set; }

        /// <summary>
        /// Timestamp in milliseconds, same as in Lua `socket.gettime() * 1000`
        /// </summary>
        [JsonProperty(PropertyName = "t")]
        public long CreatedTime { get; set; }

        /// <summary>
        /// Some messages are valid only for a short time, e.g. buy/sell orders
        /// </summary>
        [JsonProperty(PropertyName = "v")]
        public DateTime? ValidUntil { get; set; }

        /// <summary>
        /// String message
        /// </summary>
        [JsonProperty(PropertyName = "data")]
        public T Data { get; set; }
        object IMessage.Data
        {
            get => Data;
            set => Data = (T)value;
        }
        public Message(T message, string command, DateTime? validUntil = null)
        {
            Command = command;
            CreatedTime = DateTime.Now.Ticks / 10000L - Epoch;
            ValidUntil = validUntil;
            Data = message;
        }

        public bool IsValid() => !this.ValidUntil.HasValue || (this.ValidUntil > DateTime.UtcNow);
    }

    internal class MessageS : Message<string>
    {
        public MessageS(string[] request, string command, DateTime? validUntil = null) : base(string.Join("|", request), command, validUntil)
        {
        }
    }
}