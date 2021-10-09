// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json.Linq;
using NLog;
using QUIKSharp.Converters;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Transport
{
    public abstract class RequestReplyStateBase
    {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Запрос
        /// </summary>
        internal IMessage request;
        /// <summary>
        /// Тип обьекта ответа на запрос (Response)
        /// </summary>
        protected Type responseType;
        protected CancellationToken task_cancel;
        protected CancellationToken service_stop;
        /// <summary>
        /// используется для вычисления времени выполнения запроса
        /// </summary>
        private long execution_ticks;
        public static bool EnablePerfomanceLog { get; set; } = logger.IsTraceEnabled;
        /// <summary>
        /// Log to Trace only about cases exceed this threshold in ms.
        /// </summary>
        public static double PerfomanceLogThreshholdMS { get; set; } = 50.0;

        protected CancellationTokenSource _cts { get; }
        internal RequestReplyStateBase(IMessage request, Type responseType, CancellationToken task_cancel, CancellationToken service_stop)
        {
            this.request = request;
            this.responseType = responseType;
            this.task_cancel = task_cancel;
            this.service_stop = service_stop;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(task_cancel, service_stop);
            _cts.Token.Register(OnInternalCancellation, useSynchronizationContext: false);
            if (EnablePerfomanceLog)
                execution_ticks = DateTime.Now.Ticks;
        }
        internal void SetException(Exception e)
        {
            if (!this.TrySetException(e)) return;
            if (EnablePerfomanceLog)
                PerfomanceLog();
        }
        protected abstract bool TrySetResult(object data);
        protected abstract bool TrySetException(Exception e);
        public abstract TaskStatus TaskStatus { get; }
        public void CancelAfter(TimeSpan delay)
        {
            _cts.CancelAfter(delay);
        }
        protected void OnInternalCancellation()
        { //         => this.SetException(new OperationCanceledException((CancellationToken)cancellationToken));
            if (task_cancel.IsCancellationRequested)
                this.SetException(new OperationCanceledException("Operation cancelled.", task_cancel));
            if (service_stop.IsCancellationRequested)
                this.SetException(new OperationCanceledException("Service stopped!", service_stop));
            else
                this.SetException(new TimeoutException("Send operation timed out"));
        }
        public bool SetResult(JToken jtoken)
        {
            try
            {
                var cmd = (string)jtoken.SelectToken("cmd");
                ProcessMessageForLuaError(jtoken, cmd);
                var response = jtoken.FromJToken(responseType) as IMessage;
                if (response.ValidUntil.HasValue && response.ValidUntil < DateTime.UtcNow)
                    this.SetException(new TimeoutException($"Respose message (Id:{response.Id}, cmd:{cmd}) expired! ValidUntilUTC is less than current time"));
                else
                if (string.Compare(request.Command, response.Command, true) != 0)
                    this.SetException(new Exception($"SendAsync: Fatal exception: response.Command[{response.Command}] != request.command[{request.Command}]"));
                else
                {
                    if (!this.TrySetResult(response.Data))
                        return false;
                    if (EnablePerfomanceLog)
                        PerfomanceLog();
                    return true;                    
                }
            }
            catch (Exception e) // deserialization exception is possible
            {
                logger.Error(e, $"Exception in 'SendAsync, processing response': {e.Message}");
                this.SetException(e);
            }
            return false;
        }
        protected void PerfomanceLog()
        {
            execution_ticks = DateTime.Now.Ticks - execution_ticks;
            TimeSpan ts = new TimeSpan(execution_ticks);
            double ms = ts.TotalMilliseconds;

            if (ms > PerfomanceLogThreshholdMS)
            {
                var result = TaskStatus.ToString();
                var ms_str = ms.ToString("F3", CultureInfo.InvariantCulture);
                logger.Trace($"Request/Response for cmd: '{request.Command}' -> TaskResult:'{result}' tooks: {ms_str} ms.");
            }
        }
        public void Dispose()
        {
            if (_cts != null)
                _cts.Dispose();
            request = null;
        }
        /// <summary>
        /// Checks for 'lua_error' in recieved message, throws exception
        /// </summary>
        /// <param name="jtoken"></param>
        /// <param name="cmd"></param>
        public static void ProcessMessageForLuaError(JToken jtoken, string cmd)
        {
            var lua_error = (string)jtoken.SelectToken("lua_error");
            if (!string.IsNullOrEmpty(lua_error))
                if (string.Compare(cmd, "lua_transaction_error") == 0)
                    throw new TransactionException(lua_error);
                else
                    throw new LuaException(lua_error);
            if (string.IsNullOrEmpty(cmd))
                throw new ArgumentException("Bad message format: no cmd or lua_error fields");
        }
    }
}