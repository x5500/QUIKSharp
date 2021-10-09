// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json.Linq;
using NLog;
using QUIKSharp.Converters;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp.Transport
{

    /// <summary>
    /// QuikService - обрабатывает отправку и прием сообщений с QUIK
    /// </summary>
    public sealed class QuikService : IDisposable, IQuikService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Encoding encoding;
        private static readonly object StaticSync = new object();
        // Current correlation id. Use Interlocked.Increment to get a new id.
        private static long _correlationId;
        private static readonly Dictionary<int, QuikService> Services = new Dictionary<int, QuikService>();
        static QuikService()
        {
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;

            encoding = null;
            try
            {
                // Get a UTF-32 encoding by codepage.
                encoding = Encoding.GetEncoding(1251);
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
            if (encoding == null)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                encoding = Encoding.GetEncoding(1251);
            }
        }

        /// <summary>
        /// For each port only one instance of QuikService
        /// </summary>
        public static QuikService Create(IPAddress host, int port, int callback_port)
        {
            bool created = false;
            QuikService service = null;
            lock (StaticSync)
            {
                if (Services.ContainsKey(port))
                {
                    service = Services[port];
                    service.Start();
                }
                else
                {
                    service = new QuikService(host, port, callback_port);
                    Services.Add(port, service);
                    created = true;
                }
            }
            if (created)
                logger.ConditionalTrace($"QuikService created for {host}:{port}");
            return service;
        }

        // Generate a new unique ID for current session
        internal static long GetNewUniqueId()
        {
            // 2^31 = 2147483648
            // with 1 000 000 messages per second it will take more than
            // 35 hours to overflow => safe for use as TRANS_ID in SendTransaction
            // very weird stuff: Уникальный идентификационный номер заявки, значение от 1 до 2 294 967 294
            var newId = Interlocked.Increment(ref _correlationId);
            if (newId <= 0)
            {
                lock (StaticSync)
                {
                    if (_correlationId <= 0)
                        _correlationId = 1;
                }
                newId = Interlocked.Increment(ref _correlationId);
            }
            return newId;
        }

        /// <summary>
        /// Устанавливает стартовое значение для CorrelactionId.
        /// </summary>
        /// <param name="startCorrelationId">Стартовое значение.</param>
        internal static void InitializeCorrelationId(int startCorrelationId) => _correlationId = startCorrelationId;

        public static void EnablePerfomanceLog(bool Enable)
        {
            RequestReplyState<JToken>.EnablePerfomanceLog = Enable;
        }

        /// <summary>
        /// Создает экземпляр QuikService
        /// </summary>
        /// <param name="host"></param>
        /// <param name="RequestResponsePort"></param>
        /// <param name="callbackPort"></param>
        public QuikService(IPAddress host, int RequestResponsePort, int callbackPort)
        {
            _ipaddress = host;
            _responsePort = RequestResponsePort;
            _callbackPort = callbackPort;

            logger.ConditionalTrace($"QuikService created for {host}:{_responsePort},{_callbackPort}");

            Start();
            Events = new QuikEvents();
        }

        /// <summary>
        /// Default timeout to use for send operations if no specific timeout supplied.
        /// </summary>
        public TimeSpan DefaultSendTimeout { get; set; } = new TimeSpan(0, 0, 0, 20, 0); // 20 sec.
        /// <summary>
        /// Read/Write Socket operations timeout, in msec.
        /// </summary>
        public int SocketOperationTimeout { get; set; } = 10000; // 10 sec.

        public QuikEvents Events { get; private set; }

        private readonly AsyncManualResetEvent _connectedMre = new AsyncManualResetEvent();

        private readonly IPAddress _ipaddress;
        private readonly int _responsePort;
        private readonly int _callbackPort;
        private TcpClient _responseClient;
        private TcpClient _callbackClient;

        private readonly SemaphoreSlim _syncRoot = new SemaphoreSlim(1, 1);

        private bool IsStarted = false;
        private Task _requestTask;
        private Task _responseTask;
        private Task _callbackReceiverTask;

        private CancellationTokenSource StopAllCancellation;

        /// <summary>
        /// Network usage stats
        /// </summary>
        private long bytes_sent_request = 0;
        private long bytes_recieved_response = 0;
        private long bytes_recieved_callback = 0;

        /// <summary>
        /// IQuickCalls functions enqueue a message and return a task from TCS
        /// </summary>
        private readonly ConcurrentQueue<IMessage> SendQueue = new ConcurrentQueue<IMessage>();
        private readonly ManualResetEventSlim SendQueue_Avail = new ManualResetEventSlim(false);
        /// <summary>
        /// If received message has a correlation id then use its Data to SetResult on TCS and remove the TCS from the dic
        /// </summary>
        private readonly ConcurrentDictionary<long, RequestReplyState<JToken>> Responses = new ConcurrentDictionary<long, RequestReplyState<JToken>>();
        /// <summary>
        /// Get network stats
        /// </summary>
        public void GetNetStats(out ServiceNetworkStats networkStats)
        {
            networkStats.bytes_sent = bytes_sent_request;
            networkStats.bytes_recieved = bytes_recieved_response;
            networkStats.bytes_callback = bytes_recieved_callback;
            networkStats.requests_query_size = Responses.Count;
            networkStats.send_query_size = SendQueue.Count;
        }
        // ----------------------------------- Async Workers ------------------------------------------------------------------------------
        /// <summary>
        /// Start Service
        /// </summary>
        /// <exception cref="ApplicationException">Response message id does not exists in results dictionary</exception>
        public void Start()
        {
            if (IsStarted) return;
            IsStarted = true;
            StopAllCancellation = new CancellationTokenSource();

            // NB we use the token for signalling, could use a simple TCS
            var task_options = TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.PreferFairness;
            // Request Task
            _requestTask = Task.Factory.StartNew(() => Socket_Main_Loop(SendTaskLoop),  CancellationToken.None, task_options, TaskScheduler.Default);
            // Response Task
            _responseTask = Task.Factory.StartNew(() => Socket_Main_Loop(ResponseTaskLoop),  CancellationToken.None, task_options, TaskScheduler.Default);
            // Callback Task
            if (_callbackPort != 0)
                _callbackReceiverTask = Task.Factory.StartNew(() => Socket_Main_Loop(CallbackTaskLoop), CancellationToken.None, task_options, TaskScheduler.Default);
            else
                _callbackReceiverTask = Task.CompletedTask;
        }
        /// <summary>
        /// Stop service
        /// </summary>
        public void Stop()
        {
            if (!IsStarted) return;
            IsStarted = false;

            // cancel all service tasks
            // cancel responses to release waiters            
            StopAllCancellation.Cancel();

            ReleaseSocket(ref _responseClient);
            ReleaseSocket(ref _callbackClient);

            // here all tasks must exit gracefully
            var timeout = SocketOperationTimeout + 5000;
            var isCleanExit = Task.WaitAll(new[] { _requestTask, _responseTask, _callbackReceiverTask }, timeout);
            if (!isCleanExit)
                logger.Error("All tasks must finish gracefully after cancellation token is cancelled!");
        }

        private delegate void InnerSocketLoop(CancellationToken cancelToken);        
        private void Socket_Main_Loop(InnerSocketLoop taskAction)
        {
            var cancelToken = StopAllCancellation.Token;
            try
            {
                // Enter the listening loop.
                while (!cancelToken.IsCancellationRequested)
                {
                    EnsureConnectedClient(cancelToken);
                    if (cancelToken.IsCancellationRequested) break;
                    // here we have a connected TCP client
                    taskAction(cancelToken);
                }
            }
            catch (OperationCanceledException)
            {
                logger.ConditionalTrace("RequestTaskAction is cancelling");
            }
            catch (Exception e)
            {
                var name = taskAction.GetMethodInfo().Name;
                logger.Fatal(e, $"Unhandled exception in taskAction '{name}' : {e.Message}");
                StopAllCancellation.Cancel();
                throw new AggregateException($"Unhandled exception in taskAction '{name}'", e);
            }
        }
        private static bool ProcessIOException(IOException ioe, string func_name)
        {
            var ex = (ioe.InnerException != null) ? ioe.InnerException : ioe;
            var ext = ex.GetType();
            if (typeof(SocketException).IsAssignableFrom(ext))
            {
                var se = (SocketException)ex;
                switch (se.SocketErrorCode)
                {
                    case SocketError.ConnectionReset:
                    case SocketError.ConnectionAborted:
                        break;
                    case SocketError.TimedOut:
                        return true;
                    default:
                        logger.Error(se, $"SocketException in {func_name}, SocketError: '{se.SocketErrorCode}'");
                        break;
                }
            }
            else
            {
                bool disposed = typeof(ObjectDisposedException).IsAssignableFrom(ext);
                if (!disposed)
                    logger.Error(ex, $"IOException in {func_name}: '{ex.Message}'. Connection lost?");
            }
            return false;
        }
        private void SendTaskLoop(CancellationToken cancelToken)
        {
            ResetToQueryAllWaitingMessages();

            var stream = new NetworkStream(_responseClient.Client);
            stream.WriteTimeout = SocketOperationTimeout;
            var writer = new StreamWriter(stream);
            while (!cancelToken.IsCancellationRequested)
            {
                IMessage message;
                if (!SendQueue.TryDequeue(out message))
                {
                    SendQueue_Avail.Reset();
                    SendQueue_Avail.Wait(cancelToken);
                    continue;
                }
                try
                {
                    if (message.Id == 0)
                    {
                        if (logger.IsDebugEnabled)
                            logger.Debug("SendTaskLoop: Got Request in SendQueue with id=0. All requests must have correlation id");
                        continue;
                    }

                    // Request(task) can be cancelled => not waiting for response.
                    if (!Responses.TryGetValue(message.Id, out var tcs))
                        continue;

                    // Trace.WriteLine("Request: " + request);
                    // scenario: Quik is restarted or script is stopped
                    // then writer must throw and we will add a message back
                    // then we will iterate over messages and cancel expired ones
                    if (message.ValidUntil.HasValue && message.ValidUntil < DateTime.UtcNow)
                    {
                        tcs.SetException(new TimeoutException("SendTaskLoop: ValidUntilUTC is less than current time"));
                        continue;
                    }

                    var request = message.ToJson();
                    writer.WriteLine(request);
                    if (!SendQueue.Any()) writer.Flush();
                    Interlocked.Add(ref bytes_sent_request, request.Length);
                }
                catch (IOException ioe)
                {
                    if (message != null)
                        AddToQueue(message);

                    ProcessIOException(ioe, "SendTaskLoop");
                    break;
                }
            }
        }
        private void ResponseTaskLoop(CancellationToken cancelToken)
        {
            // here we have a connected TCP client
            var stream = new NetworkStream(_responseClient.Client);
            stream.ReadTimeout = SocketOperationTimeout;
            var reader = new StreamReader(stream, encoding); //true
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    string response;
                    response = reader.ReadLine();
                    if (response == null) break;
                    // No IO exceptions possible for response, move its processing
                    // to the threadpool and wait for the next mesaage
                    // A new task here gives c.30% boost for full TransactionSpec echo
                    try
                    {
                        //Deserialize into a JObject
                        JToken token = JObject.Parse(response);
                        long message_id = (long)token.SelectToken("id");
                        if (message_id <= 0)
                            throw new Exception("ResponseTaskLoop: Error: message_id less or equal 0.");

                        // it is a response message                                    
                        if (Responses.TryRemove(message_id, out var tcs))
                            tcs.SetResult(token);
                        else
                        if (logger.IsTraceEnabled)
                            logger.Trace($"No Response is waiting for message with Id:{message_id}");
                    }
                    catch (Exception e) // deserialization exception is possible
                    {
                        logger.Error(e, $"ResponseTaskLoop: Exception: {e.Message}");
                        logger.Error("Response JSON: " + response);
                    }
                    Interlocked.Add(ref bytes_recieved_response, response.Length);
                }
                catch (IOException ioe)
                {
                    if (ProcessIOException(ioe, "ResponseTaskLoop"))
                        continue;
                    else
                        break;
                }
            }
        }
        private void CallbackTaskLoop(CancellationToken cancelToken)
        {
            // here we have a connected TCP client
            var stream = new NetworkStream(_callbackClient.Client);
            stream.ReadTimeout = SocketOperationTimeout;
            var reader = new StreamReader(stream, encoding); //true
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    string callback;
                    callback = reader.ReadLine();
                    if (callback == null) break;
                    try
                    {
                        ProcessCallbackMessage(callback);
                    }
                    catch (Exception e) // deserialization exception is possible
                    {
                        logger.Error(e, $"Exception in 'CallbackTaskLoop': {e.Message}");
                        logger.Error("Recieved JSON: " + callback);
                    }
                    Interlocked.Add(ref bytes_recieved_callback, callback.Length);
                }
                catch (IOException ioe)
                {
                    if (ProcessIOException(ioe, "CallbackTaskLoop"))
                        continue;
                    else
                        break;
                }
            }
        }
        // ----------------------- TCP Socket functions -------------------------------------------------------------------------------------------
        private static bool IsConnectedTCP(TcpClient tcp_client) => (tcp_client != null) && tcp_client.Connected && tcp_client.Client.Connected && tcp_client.Client.IsConnectedNow();
        public bool IsServiceConnected() => IsConnectedTCP(_responseClient) && (_callbackPort == 0 || IsConnectedTCP(_callbackClient));
        private void ReleaseSocket(ref TcpClient tcpClient)
        {
            _syncRoot.Wait();
            try
            {
                ReleaseSocketNoLock(ref tcpClient);
            }
            finally
            {
                _syncRoot.Release();
            }
        }
        private static void ReleaseSocketNoLock(ref TcpClient tcpClient)
        {
            if (tcpClient != null)
            { // cleanup
                try
                {
                    if (tcpClient.Connected || (tcpClient.Client?.Connected ?? false))
                        tcpClient.Client?.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    tcpClient.Client?.Close();
                    tcpClient.Close();
                    tcpClient.Dispose();
                }
                tcpClient = null;
            }
        }
        private static void NewTCPSocket(ref TcpClient tcpClient)
        {
            ReleaseSocketNoLock(ref tcpClient);
            tcpClient = new TcpClient
            {
                ExclusiveAddressUse = true,
                NoDelay = true,
                LingerState = new LingerOption(false, 0),
            };
        }
        private void EnsureConnectedClient(CancellationToken ct)
        {
            if (IsServiceConnected()) return;
            bool call_events = false;
            try
            {
                _syncRoot.Wait(ct);
                if (_connectedMre.isSet && !IsServiceConnected())
                {
                    _connectedMre.Reset();
                    Events.OnDisconnectedFromQuikCall();
                }
                while (!IsServiceConnected())
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        if (!IsConnectedTCP(_responseClient))
                        {
                            NewTCPSocket(ref _responseClient);
                            logger.ConditionalTrace("Connecting on request/response channel... ");
                            _responseClient.Connect(_ipaddress, _responsePort);
                            logger.ConditionalTrace($"Request/response channel connected to '{_ipaddress}:{_responsePort}");
                        }

                        if (_callbackPort != 0 && !IsConnectedTCP(_callbackClient))
                        {
                            NewTCPSocket(ref _callbackClient);
                            logger.ConditionalTrace("Connecting on request/response channel... ");
                            _callbackClient.Connect(_ipaddress, _callbackPort);
                            logger.ConditionalTrace($"Request/response channel connected to '{_ipaddress}:{_callbackPort}");
                        }
                    }
                    catch (SocketException ex)
                    {
                        switch (ex.SocketErrorCode)
                        {
                            case SocketError.ConnectionRefused:
                                break;
                            default:
                                logger.Error(ex, $"SocketException while trying to connect to {_ipaddress}:[{_responsePort},{_callbackPort}]: {ex.Message}");
                            break;
                        }
                        Task.Delay(100, ct).Wait(ct);
                    }
                    catch (Exception exc)
                    {
                        var ex = (exc.InnerException != null) ? exc.InnerException : exc;
                        logger.Error(ex, $"Exception while trying to connect to {_ipaddress}:[{_responsePort},{_callbackPort}]: {ex.Message}");
                        Task.Delay(100, ct).Wait(ct);
                    }
                }
                if (_connectedMre.isWaiting)
                {
                    _connectedMre.Set();
                    // Оповещаем клиента что произошло подключение к Quik'у
                    call_events = true;
                }
            }
            finally
            {
                _syncRoot.Release();
            }
            if (call_events)
                _ = Task.Run(InvokeOnConnectedEvent, ct);
        }
        private void InvokeOnConnectedEvent()
        {
            try
            {
                Events.OnConnectedToQuikCall(_responsePort);
            }
            catch (TargetInvocationException tie)
            {
                var e = tie.InnerException; // ex now stores the original exception
                logger.Error(e, $"ProcessCallbackMessage: Exception in Event['ConnectedToQuik'].Invoke():  {e.Message}");
            }
            catch (Exception e)
            {
                logger.Error(e, $"ProcessCallbackMessage: Exception in Event['ConnectedToQuik'].Invoke():  {e.Message}");
            }
        }
        private async Task EnsureConnectedClientAsync(CancellationToken ct)
        {
            await _syncRoot.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_connectedMre.isSet && !IsServiceConnected())
                {
                    _connectedMre.Reset();
                    Events.OnDisconnectedFromQuikCall();
                }

                while (!IsServiceConnected())
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        Task task1 = Task.CompletedTask;
                        if (!IsConnectedTCP(_responseClient))
                        {
                            NewTCPSocket(ref _responseClient);
                            logger.ConditionalTrace("Connecting on request/response channel... ");
                            task1 = _responseClient.ConnectAsync(_ipaddress, _responsePort)
                                .ContinueWith((t1, args) =>
                                {
                                    var _args = (Tuple<IPAddress, int>)args;
                                    logger.ConditionalTrace($"Request/response channel connected to '{_args.Item1}:{_args.Item2}");
                                }, new Tuple<IPAddress, int>(_ipaddress, _responsePort), ct, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
                        }

                        Task task2 = Task.CompletedTask;
                        if (_callbackPort != 0 && !IsConnectedTCP(_callbackClient))
                        {
                            NewTCPSocket(ref _callbackClient);
                            logger.ConditionalTrace("Connecting on callback channel... ");
                            task2 = _callbackClient.ConnectAsync(_ipaddress, _callbackPort)
                                .ContinueWith((t2, args) =>
                                {
                                    var _args = (Tuple<IPAddress, int>)args;
                                    logger.ConditionalTrace($"Callback channel connected to '{_args.Item1}:{_args.Item2}'");
                                }, new Tuple<IPAddress, int>(_ipaddress, _callbackPort), ct, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
                        }
                        Task[] tasks = { task1, task2 };
                        Task.WaitAll(tasks, cancellationToken: ct);
                    }
                    catch (SocketException ex) 
                    {
                        logger.Error(ex, $"SocketException while trying to connect to {_ipaddress}:[{_responsePort},{_callbackPort}]: {ex.Message}");
                        await Task.Delay(100, ct).ConfigureAwait(false);
                    }
                    catch (Exception exc)
                    {
                        var ex = (exc.InnerException != null) ? exc.InnerException : exc;
                        if (!typeof(TaskCanceledException).IsAssignableFrom(ex.GetType()))
                            logger.Error(ex, $"Exception while trying to connect to {_ipaddress}:[{_responsePort},{_callbackPort}]: {ex.Message}");
                        await Task.Delay(100, ct).ConfigureAwait(false);
                    }
                }

                if (_connectedMre.isWaiting)
                {
                    _connectedMre.Set();
                    // Оповещаем клиента что произошло подключение к Quik'у
                    Events.OnConnectedToQuikCall(_responsePort);
                }
            }
            finally
            {
                _syncRoot.Release();
            }
        }
        // --------------------- Query, Request/Reply processing ------------------------------------------------------------------------------------
        /// <summary>
        /// Повторно отправляем запросы, которых нет в очереди на отправку
        /// (В случае разрыва соединиения)
        /// </summary>
        private void ResetToQueryAllWaitingMessages()
        {
            var awaiting_list = Responses.Keys.Except(SendQueue.Select(m => m.Id));
            foreach (var key in awaiting_list)
                if (Responses.TryGetValue(key, out var rr))
                {
                    if (rr.request.ValidUntil.HasValue || rr.request.ValidUntil <= DateTime.UtcNow)
                        continue;
                    AddToQueue(rr.request);
                }
        }
        private void ProcessCallbackMessage(string callback)
        {
            //Deserialize into a JObject
            JToken jtoken = JObject.Parse(callback);
            string command = (string)jtoken.SelectToken("cmd");
            ProcessMessageForLuaError(jtoken, command);

            var parsed = Enum.TryParse<EventNames>(command, true, out var eventName);
            if (!parsed)
                throw new InvalidOperationException("ProcessCallbackMessage: Unknown command in a message: " + command);
            try
            {
                switch (eventName)
                {
                    case EventNames.OnAccountBalance:
                        Events.OnAccountBalanceCall(jtoken.FromJTokenMessage<AccountBalance>());
                        break;
                    case EventNames.OnAccountPosition:
                        Events.OnAccountPositionCall(jtoken.FromJTokenMessage<AccountPosition>());
                        break;
                    case EventNames.OnAllTrade:
                        Events.OnAllTradeCall(jtoken.FromJTokenMessage<AllTrade>());
                        break;
                    case EventNames.OnCleanUp:
                        Events.OnCleanUpCall();
                        return;
                    case EventNames.OnClose:
                        Events.OnCloseCall();
                        return;
                    case EventNames.OnConnected:
                        Events.OnConnectedCall();
                        return;
                    case EventNames.OnDisconnected:
                        Events.OnDisconnectedCall();
                        return;
                    case EventNames.OnInit:
                        // Этот callback никогда не будет вызван так как на момент получения вызова OnInit в lua скрипте
                        // соединение с библиотекой QuikSharp не будет еще установлено. То есть этот callback не имеет смысла.
                        return;
                    case EventNames.OnStop:
                        Events.OnStopCall(int.Parse(jtoken.FromJTokenMessage<string>()));
                        break;
                    case EventNames.OnDepoLimit:
                        Events.OnDepoLimitCall(jtoken.FromJTokenMessage<DepoLimitEx>());
                        break;
                    case EventNames.OnDepoLimitDelete:
                        Events.OnDepoLimitDeleteCall(jtoken.FromJTokenMessage<DepoLimitDelete>());
                        break;
                    case EventNames.OnFirm:
                        Events.OnFirmCall(jtoken.FromJTokenMessage<Firm>());
                        break;
                    case EventNames.OnFuturesClientHolding:
                        Events.OnFuturesClientHoldingCall(jtoken.FromJTokenMessage<FuturesClientHolding>());
                        break;
                    case EventNames.OnFuturesLimitChange:
                        Events.OnFuturesLimitChangeCall(jtoken.FromJTokenMessage<FuturesLimits>());
                        break;
                    case EventNames.OnFuturesLimitDelete:
                        Events.OnFuturesLimitDeleteCall(jtoken.FromJTokenMessage<FuturesLimitDelete>());
                        break;
                    case EventNames.OnMoneyLimit:
                        Events.OnMoneyLimitCall(jtoken.FromJTokenMessage<MoneyLimitEx>());
                        break;
                    case EventNames.OnMoneyLimitDelete:
                        Events.OnMoneyLimitDeleteCall(jtoken.FromJTokenMessage<MoneyLimitDelete>());
                        break;
                    case EventNames.OnNegDeal:
                    // Функция вызывается терминалом QUIK при получении внебиржевой заявки или при изменении параметров существующей внебиржевой заявки.
                    case EventNames.OnNegTrade:
                        // Функция вызывается терминалом QUIK при получении сделки для исполнения или при изменении параметров существующей сделки для исполнения.
                        // Не реализовано
                        return;
                    case EventNames.OnOrder:
                        Events.OnOrderCall(jtoken.FromJTokenMessage<Order>());
                        break;
                    case EventNames.OnParam:
                        Events.OnParamCall(jtoken.FromJTokenMessage<Param>());
                        break;
                    case EventNames.OnQuote:
                        Events.OnQuoteCall(jtoken.FromJTokenMessage<OrderBook>());
                        break;
                    case EventNames.OnStopOrder:
                        Events.OnStopOrderCall(jtoken.FromJTokenMessage<StopOrder>());
                        break;
                    case EventNames.OnTrade:
                        Events.OnTradeCall(jtoken.FromJTokenMessage<Trade>());
                        break;
                    case EventNames.OnTransReply:
                        Events.OnTransReplyCall(jtoken.FromJTokenMessage<TransactionReply>());
                        break;
                    case EventNames.NewCandle:
                        Events.OnNewCandleEvent(jtoken.FromJTokenMessage<Candle>());
                        break;
                    case EventNames.lua_error:
                        // an error from an event not request (from req is caught is response loop)
                        logger.Error(jtoken.FromJTokenMessage<string>());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("eventName", eventName, "Invalid event name or such event was not implemented");
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                throw;
            }
            catch (TargetInvocationException tie)
            {
                var e = tie.InnerException; // ex now stores the original exception
                logger.Error(e, $"ProcessCallbackMessage: Exception in Event['{eventName}'].Invoke():  {e.Message}");
            }
            catch (Exception e)
            {
                logger.Error(e, $"ProcessCallbackMessage: Exception in Event['{eventName}'].Invoke():  {e.Message}");
            }
        }
        public Task<TResult> SendAsync<TResult>(IMessage request) => SendAsync<TResult>(request, CancellationToken.None);
        public async Task<TResult> SendAsync<TResult>(IMessage request, CancellationToken task_cancel)
        {
            var service_stop = StopAllCancellation.Token;
            CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(task_cancel, service_stop);
            if (DefaultSendTimeout.Milliseconds > 0)
                _cts.CancelAfter(DefaultSendTimeout);

            if (request.Id <= 0)
                request.Id = GetNewUniqueId();

            var responseType = typeof(Message<TResult>);
            var rr = new RequestReplyState<JToken>(request, responseType, _cts.Token);
            Responses[request.Id] = rr;
            // add to queue after responses dictionary
            AddToQueue(request);
            try
            {

                JToken jtoken = await rr.ResultTask.ConfigureAwait(false);
                var command = (string)jtoken.SelectToken("cmd");
                ProcessMessageForLuaError(jtoken, command);
                var response = jtoken.FromJToken(responseType) as IMessage;
                if (response.ValidUntil.HasValue && response.ValidUntil < DateTime.UtcNow)
                    throw new TimeoutException($"Respose message (Id:{response.Id}, cmd:{command}) expired! ValidUntilUTC is less than current time");
                if (string.Compare(request.Command, response.Command, true) != 0)
                    throw new Exception($"SendAsync: Fatal exception: response.Command[{response.Command}] != request.command[{request.Command}]");
                return (TResult)response.Data;
            }
            catch (OperationCanceledException e)
            {
                if (task_cancel.IsCancellationRequested)
                    throw new OperationCanceledException("Operation cancelled.", e);
                if (service_stop.IsCancellationRequested)
                    throw new OperationCanceledException("Service stopped!", e);
                else
                    throw new TimeoutException("Send operation timed out", e);
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (LuaException)
            {
                throw;
            }
            catch (Exception e) // deserialization exception is possible
            {
                logger.Error(e, $"Exception in 'SendAsync, processing response': {e.Message}");
                throw;
            }
            finally
            {
                Responses.TryRemove(request.Id, out var temp);
                if (_cts != null)
                    _cts.Dispose();
            }
        }
        private void AddToQueue(IMessage request)
        {
            SendQueue.Enqueue(request);
            SendQueue_Avail.Set();
        }
        private static void ProcessMessageForLuaError(JToken jtoken, string cmd)
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
        // ---------------------------------------------------------------------------------------------------
        private bool disposedValue;
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты)
                    Stop();
                    _requestTask = null;
                    _responseTask = null;
                    _callbackReceiverTask = null;
                    SendQueue_Avail.Dispose();
                }
                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // TODO: установить значение NULL для больших полей
                disposedValue = true;
            }
        }
        void IDisposable.Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}