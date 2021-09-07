// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.Transport;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    public class DebugFunctions : FunctionsBase, IDebugFunctions
    {
        public DebugFunctions(IQuikService quikService) : base(quikService)
        {
        }

        internal class PingRequest : Message<string>
        {
            public PingRequest()
                : base("Ping", "ping", null)
            {
            }
        }

        internal class PingResponse : Message<string>
        {
            public PingResponse()
                : base("Pong", "ping", null)
            {
            }
        }

        public async Task<string> Ping()
        {
            // could have used StringMessage directly. This is an example of how to define DTOs for custom commands
            var response = await QuikService.SendAsync<string>(new PingRequest()).ConfigureAwait(false);
            logger.ConditionalTrace($"Ping response: {response}");
            return response;
        }

        /// <summary>
        /// Could have used StringMessage directly. This is an example of how to define DTOs for custom commands
        /// </summary>
        /// <param name="msg"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Task<T> Echo<T>(T msg) =>
            QuikService.SendAsync<T>(new Message<T>(msg, "echo"));

        /// <summary>
        /// This method returns LuaException and demonstrates how Lua errors are caught
        /// </summary>
        /// <returns></returns>
        public Task<string> DivideStringByZero()
        {
            return QuikService.SendAsync<string>((new Message<string>("", "divide_string_by_zero")));
        }

        /// <summary>
        /// Check if running inside Quik
        /// </summary>
        public async Task<bool> IsQuik()
        {
            var response = await QuikService.SendAsync<string>((new Message<string>("", "is_quik"))).ConfigureAwait(false);
            return response == "1";
        }
    }
}