// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using System.Threading.Tasks;

namespace QUIKSharp
{
    public class LuaBulkIdProvider : LuaIdProvider, IIdentifyTransaction
    {
        protected object rwLock = new object();

        private long nextId = 0;
        private long lastId = 0;
        private readonly long bulk_size;

        /// <summary>
        /// Для идентифцикации транзакий запрашивает у QUIK LUA блок идентификаторов размером bulk_size
        /// Далее, выдает при вызове функции GetNextId() по одному идентификатору, если блок заканчивается, то запрашивает слледующий.
        /// </summary>
        /// <param name="quik"></param>
        /// <param name="bulk_size"></param>
        public LuaBulkIdProvider(Quik quik, long bulk_size = 1000) : base(quik)
        {
            bulk_size = bulk_size > 1 ? bulk_size : 1;
            this.bulk_size = bulk_size < short.MaxValue ? bulk_size : short.MaxValue;
        }

        private Task Current_RequestNewBlockTask = null;

        override public long GetNextId()
        {
            long new_id;
            bool background_request_new_block;

            lock (rwLock)
            {
                if (nextId < lastId)
                {
                    new_id = nextId;
                    nextId++;
                }
                else
                {
                    // Нужно заказать новый блок сейчас
                    // и взять из него new_id
                    new_id = quik.Transactions.LuaNewTransactionID(bulk_size).Result;
                    nextId = new_id + 1;
                    lastId = new_id + bulk_size;
                }
                // если мы забрали последний из свободных
                background_request_new_block = nextId == lastId;
            }

            // Если нужно создать фоновую задачу на заказ нового блока
            if (background_request_new_block)
            {
                if (Current_RequestNewBlockTask == null || Current_RequestNewBlockTask.IsCompleted)
                { // create new
                    Current_RequestNewBlockTask = Task.Run(() =>
                   {
                       lock (rwLock)
                       {
                           if (nextId >= lastId)
                           {
                               var new_block_start = quik.Transactions.LuaNewTransactionID(bulk_size).Result;
                               nextId = new_block_start;
                               lastId = new_block_start + bulk_size;
                           }
                       }
                   });
                }
            }

            return new_id;
        }
    }
}