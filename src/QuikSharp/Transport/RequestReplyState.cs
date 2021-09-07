// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using NLog;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace QUIKSharp.Transport
{
    /// <summary>
    /// QuikService RequestReplyState state
    /// </summary>
    public sealed class RequestReplyState<T>
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static bool EnablePerfomanceLog { get; set; } = logger.IsTraceEnabled;
        /// <summary>
        /// Log to Trace only about cases exceed this threshold in ms.
        /// </summary>
        public static double PerfomanceLogThreshholdMS { get; set; } = 50.0;
        /// <summary>
        /// используется для вычисления времени выполнения запроса
        /// </summary>
        private long execution_ticks;
        /// <summary>
        /// Здесь сохраняем полученный ответ от Квика
        /// и тем самым информируем заказчика
        /// </summary>
        private readonly TaskCompletionSource<T> tcs;
        /// <summary>
        /// Задача TaskCompletionSource <typeparamref name="T"/> для ожидания результата
        /// </summary>
        internal Task<T> ResultTask => tcs.Task;
        /// <summary>
        /// Запрос
        /// </summary>
        internal IMessage request;
        /// <summary>
        /// Тип обьекта ответа на запрос (Response)
        /// </summary>
        internal Type objectType;
        internal RequestReplyState(IMessage request, Type objectType)
        {
            this.request = request;
            this.objectType = objectType;
            tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (EnablePerfomanceLog)
                execution_ticks = DateTime.Now.Ticks;
        }
        internal bool SetException(Exception exn)
        {
            if (!tcs.TrySetException(exn))
                return false;
            if (EnablePerfomanceLog)
                PerfomanceLog();
            return true;
        }
        internal bool TrySetCanceled()
        {
            if (!tcs.TrySetCanceled())
                return false;
            if (EnablePerfomanceLog)
                PerfomanceLog();
            return true;
        }
        internal bool SetResult(T message)
        {
            if (!tcs.TrySetResult(message))
                return false;
            if (EnablePerfomanceLog)
                PerfomanceLog();
            return true;
        }
        private void PerfomanceLog()
        {
            execution_ticks = DateTime.Now.Ticks - execution_ticks;
            TimeSpan ts = new TimeSpan(execution_ticks);
            double ms = ts.TotalMilliseconds;

            if (ms > PerfomanceLogThreshholdMS)
            {
                var result = tcs.Task.Status.ToString();
                var ms_str = ms.ToString("F3", CultureInfo.InvariantCulture);
                logger.Trace($"Request/Response for cmd: '{request.Command}' -> '{result}' tooks: {ms_str} ms.");
            }
        }
    }
}