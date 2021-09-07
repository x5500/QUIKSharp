// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using NLog;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using QUIKSharp.Functions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
namespace QUIKSharp.TestQuik
{
    public class QuikEmulator : IQuik
    {
        private static readonly Logger logging = LogManager.GetCurrentClassLogger();
        bool IQuik.IsServiceConnected => testService._isConnected;

        public TimeSpan DefaultSendTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ICandleFunctions Candles => throw new NotImplementedException();

        public IClassFunctions Class => throw new NotImplementedException();

        public IDebugFunctions Debug { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IOrderBookFunctions OrderBook { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        ///
        /// </summary>
        private readonly TestOrders testOrders = new TestOrders();

        public IOrderFunctions Orders => testOrders;

        private readonly TestService testService = new TestService();
        public IServiceFunctions Service => testService;

        private readonly TestTrading testTrading = new TestTrading();
        public ITradingFunctions Trading => testTrading;

        private readonly TestTransactions testTransactions = new TestTransactions();
        public ITransactionsFunctions Transactions => testTransactions;

        private readonly TestEvents events = new TestEvents();
        public IQuikEvents Events => events;

        private readonly string timestamp_field = "timestamp";
        public System.Data.DataSet DataSet1 { get; private set; }
        public DateTime PlayerStart { get; set; }
        public DateTime PlayerEnd { get; set; }
        public DateTime LastDateTime { get; private set; }

        private readonly Dictionary<string, int> DtPos = new Dictionary<string, int>();

        private readonly DataTable dtStopOrders;
        private readonly DataTable dtLimitOrders;
        private readonly DataTable dtTransReply;
        private readonly DataTable dtTrades;
        private readonly DataTable dtMarks;

        public readonly Dictionary<string, bool> enableEvents = new Dictionary<string, bool>();

        public QuikEmulator()
        {
            DataSet1 = new DataSet();

            dtLimitOrders = DataSet1.Tables.Add("LimitOrders");
            CreateDataTableforObject(dtLimitOrders, typeof(Order));

            dtStopOrders = DataSet1.Tables.Add("StopOrders");
            CreateDataTableforObject(dtStopOrders, typeof(StopOrder));

            dtTransReply = DataSet1.Tables.Add("TransReply");
            CreateDataTableforObject(dtTransReply, typeof(TransactionReply));

            dtTrades = DataSet1.Tables.Add("Trades");
            CreateDataTableforObject(dtTrades, typeof(Trade));

            dtMarks = DataSet1.Tables.Add("Marks");
            CreateDataTableforObject(dtMarks, typeof(TimeMark));

            foreach (DataTable dt in DataSet1.Tables)
            {
                enableEvents.Add(dt.TableName, true);
            }
        }

        private void CreateDataTableforObject(DataTable dt, Type type)
        {
            dt.Clear();

            if (!dt.Columns.Contains(timestamp_field))
            {
                dt.Columns.Add(timestamp_field, typeof(DateTime));
            }
            DataSetHelper.CreateDataTableforObject(dt, type);
        }

        public string LoadXMLToDataSet()
        {
            var fname = DialogLoadXMLToDataset(DataSet1);
            PlayerStart = DateTime.MinValue;
            PlayerEnd = DateTime.MinValue;
            DtPos.Clear();
            RewindToBegin();
            return fname;
        }

        protected virtual string DialogLoadXMLToDataset(DataSet dataSet1)
        {
            throw new NotImplementedException();
        }

        private void FindFirstDate()
        {
            DateTime min = DateTime.MinValue;
            foreach (DataTable dt in DataSet1.Tables)
            {
                if (dt.Rows.Count > 0)
                {
                    var ts = (DateTime)dt.Rows[0][timestamp_field];
                    if (min == DateTime.MinValue)
                        min = ts;
                    else if (min > ts)
                        min = ts;
                }
            }

            PlayerStart = min;
            LastDateTime = min;
            if (PlayerEnd < PlayerStart)
                PlayerEnd = DateTime.MaxValue;
        }

        private int Find_first_idx(DataTable dt, DateTime date)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var ts = (DateTime)dt.Rows[i][timestamp_field];
                if (ts >= date)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Переставляет указатели эмулятора на начало базы данных
        /// </summary>
        /// <returns></returns>
        public bool RewindToBegin()
        {
            if (PlayerStart == DateTime.MinValue)
                FindFirstDate();

            if (PlayerEnd < PlayerStart)
                PlayerEnd = DateTime.MaxValue;

            testOrders.ClearAll();
            testTrading.ClearAll();

            foreach (DataTable dt in DataSet1.Tables)
            {
                var idx = Find_first_idx(dt, PlayerStart);
                if (idx < 0)
                    DtPos.Remove(dt.TableName);
                else
                    DtPos[dt.TableName] = idx;
            }

            SetLastDate(PlayerStart);

            return true;
        }

        /// <summary>
        /// Переставляет указатели эмулятора на начало (PlayerStart)
        /// </summary>
        /// <returns></returns>
        public bool RewindToStartDate(bool call_events)
        {
            if (PlayerEnd < PlayerStart)
                PlayerEnd = DateTime.MaxValue;

            testOrders.ClearAll();
            testTrading.ClearAll();

            foreach (DataTable dt in DataSet1.Tables)
            {
                var idx = Find_first_idx(dt, DateTime.MinValue);
                if (idx < 0) continue;
                DtPos[dt.TableName] = idx;
            }

            bool next = true;
            while (next)
            {
                next = Step(PlayerStart, false, call_events);
            }
            return true;
        }

        private void SetLastDate(DateTime date)
        {
            LastDateTime = date;
            testService._TradeDate = date;
        }

        internal bool Step(bool StopOnMark)
        {
            return Step(PlayerEnd, StopOnMark, true);
        }

        /// <summary>
        /// Делает шаг в эмуляторе, отсылая следующее событие
        /// возвращает true если возможен следующий шаг
        /// </summary>
        /// <returns></returns>
        internal bool Step(DateTime stop_on_date, bool StopOnMark, bool call_events)
        {
            int idx_min = 0;
            DateTime date_min = DateTime.MinValue;
            DataTable selected_table = null;
            foreach (var kv in DtPos)
            {
                var table = DataSet1.Tables[kv.Key];
                var idx = kv.Value;

                var date = (DateTime)table.Rows[idx][timestamp_field];
                if (date_min == DateTime.MinValue)
                {
                    idx_min = idx;
                    date_min = date;
                    selected_table = table;
                }
                else
                if (date_min > date)
                {
                    idx_min = idx;
                    date_min = date;
                    selected_table = table;
                }
            }

            if ((date_min == DateTime.MinValue) || (date_min > stop_on_date))
                return false;

            SetLastDate(date_min);

            if (selected_table != null)
            {
                var row = selected_table.Rows[idx_min];
                idx_min++;
                if (idx_min < selected_table.Rows.Count)
                {
                    DtPos[selected_table.TableName] = idx_min;
                }
                else
                {
                    DtPos.Remove(selected_table.TableName);
                }

                PushRow(row, call_events);
                if (StopOnMark && row.Table.TableName == "Marks")
                {
                    return false;
                }
            }
            return true;
        }

        private void PushRow(DataRow row, bool call_events)
        {
            if (call_events && enableEvents.TryGetValue(row.Table.TableName, out var enable))
                call_events &= enable;

            switch (row.Table.TableName)
            {
                case "LimitOrders":
                    {
                        var obj = new Order();
                        if (DataSetHelper.FillObjectFromDataRow(row, obj))
                        {
                            testOrders.OnOrder(obj);
                            if (call_events)
                                events.OnOrderCall(obj);
                        }
                        break;
                    }
                case "StopOrders":
                    {
                        var obj = new StopOrder();
                        if (DataSetHelper.FillObjectFromDataRow(row, obj))
                        {
                            testOrders.OnStopOrder(obj);
                            if (call_events)
                                events.OnStopOrderCall(obj);
                        }
                        break;
                    }
                case "TransReply":
                    {
                        var obj = new TransactionReply();
                        if (DataSetHelper.FillObjectFromDataRow(row, obj))
                            if (call_events)
                                events.OnTransReplyCall(obj);
                        break;
                    }
                case "Trades":
                    {
                        var obj = new Trade();
                        if (DataSetHelper.FillObjectFromDataRow(row, obj))
                        {
                            testTrading.OnNewTrade(obj);
                            if (call_events)
                                events.OnTradeCall(obj);
                        }
                        break;
                    }
                case "Marks":
                    {
                        var obj = new TimeMark();
                        if (DataSetHelper.FillObjectFromDataRow(row, obj))
                        {
                            last_label = obj.Text;
                            logging.Info("Mark: " + obj.Text);
                        }
                        break;
                    }
            }
        }

        internal void SetAsStart(DataRow row)
        {
            try
            {
                DateTime ts = (DateTime)row[timestamp_field];
                this.PlayerStart = ts;
            }
            catch (IndexOutOfRangeException) { }
        }

        internal void SetAsEnd(DataRow row)
        {
            try
            {
                DateTime ts = (DateTime)row[timestamp_field];
                this.PlayerEnd = ts;
            }
            catch (IndexOutOfRangeException) { }
        }

        private bool isRunningReconnect = false;

        /// <summary>
        /// Последняя текстовая метка
        /// </summary>
        public string last_label;

        internal async Task Reconnect()
        {
            if (isRunningReconnect) return;
            try
            {
                isRunningReconnect = true;
                testService._isConnected = false;
                events.OnDisconnectedCall();

                await Task.Delay(500);

                events.OnDisconnectedFromQuikCall();

                logging.Info("Emulator: Disconnected...");
                await Task.Delay(1000);
                logging.Info("Emulator: Connected...");

                testService._isConnected = true;
                events.OnConnectedToQuikCall(0);
                await Task.Delay(500);
                events.OnConnectedCall();
            }
            finally
            {
                isRunningReconnect = false;
            }
        }
    }
}

#pragma warning restore CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
