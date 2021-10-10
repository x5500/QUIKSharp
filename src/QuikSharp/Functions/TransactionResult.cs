// Copyright (c) 2021 alex.mishin@me.com77
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures.Transaction;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Functions
{
    public enum TransactionStatus
    {
        Success = 0,
        LuaException,
        TransactionException,
        QuikError,
        TimeoutWaitReply,
        SendRecieveTimeout,
        FailedToSend,
        NoConnection,
    }
    public struct TransactionResult
    {
        public long TransId;
        public TransactionStatus Result;
        public string ResultMsg;
    }

    public struct TransactionWaitResult
    {
        public long TransId;
        public long OrderNum;
        public TransactionReply transReply;
        public TransactionStatus Status;
        public string ResultMsg;
    }
}