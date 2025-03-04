﻿namespace QUIKSharp
{
    public struct ServiceNetworkStats
    {
        /// Bytes sent as Request
        public long bytes_sent;
        /// Bytes recieved as Response
        public long bytes_recieved;
        /// Bytes recieved as Callback
        public long bytes_callback;
        /// Current length of the requests query waiting for response
        public long requests_query_size;
        /// Current length of the requests query waiting been send by network
        public long send_query_size;
        /// <summary>
        /// Counts current running callback tasks
        /// </summary>
        public long callback_wait_query;
    }
}
