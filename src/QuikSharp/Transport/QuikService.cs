// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using HellBrick.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using QUIKSharp.Converters;
using QUIKSharp.DataStructures;
using QUIKSharp.DataStructures.Transaction;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        private readonly AsyncQueue<IMessage> SendQueue = new AsyncQueue<IMessage>();

        /// <summary>
        /// If received message has a correlation id then use its Data to SetResult on TCS and remove the TCS from the dic
        /// </summary>
        private readonly ConcurrentDictionary<long, RequestReplyState<JToken>> Responses = new ConcurrentDictionary<long, RequestReplyState<JToken>>();

        /// <summary>
        /// Get network stats
        /// </summary>
        /// <param name="bytes_sent">Bytes sent as Request</param>
        /// <param name="bytes_recieved">Bytes recieved as Response</param>
        /// <param name="bytes_callback">Bytes recieved as Callback</param>
        /// <param name="request_query_size">Current length of the requests query waiting for response</param>
        public void GetNetStats(out long bytes_sent, out long bytes_recieved, out long bytes_callback, out long request_query_size)
        {
            bytes_sent = bytes_sent_request;
            bytes_recieved = bytes_recieved_response;
            bytes_callback = bytes_recieved_callback;
            request_query_size = Responses.Count;
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
            _requestTask = Task.Factory.StartNew(RequestTaskAction, CancellationToken.None, task_options, TaskScheduler.Default);

            // Response Task
            _responseTask = Task.Factory.StartNew(ResponseTaskAction, CancellationToken.None, task_options, TaskScheduler.Default);

            // Callback Task
            if (_callbackPort != 0)
                _callbackReceiverTask = Task.Factory.StartNew(CallbackTaskAction, CancellationToken.None, task_options, TaskScheduler.Default);
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
            StopAllCancellation.Cancel();

            try
            {
                // here all tasks must exit gracefully
                var isCleanExit = Task.WaitAll(new[] { _requestTask, _responseTask, _callbackReceiverTask }, 5000);
                if (!isCleanExit)
                    logger.Error("All tasks must finish gracefully after cancellation token is cancelled!");
            }
            finally
            {
                // cancel responses to release waiters
                foreach (var responseKey in Responses.Keys.ToList())
                {
                    if (Responses.TryRemove(responseKey, out var responseInfo))
                        responseInfo.TrySetCanceled();
                }
            }
        }

        private async Task RequestTaskAction()
        {
            var cancelToken = StopAllCancellation.Token;
            try
            {
                // Enter the listening loop.
                while (!cancelToken.IsCancellationRequested)
                {
                    await EnsureConnectedClient(cancelToken).ConfigureAwait(false);
                    // here we have a connected TCP client
                    ResetToQueryAllWaitingMessages();

                    try
                    {
                        var stream = new NetworkStream(_responseClient.Client);
                        var writer = new StreamWriter(stream);

                        while (!cancelToken.IsCancellationRequested)
                        {
                            IMessage message = await SendQueue.TakeAsync(cancelToken).ConfigureAwait(false);
                            try
                            {
                                //Trace.WriteLine("Request: " + request);
                                // scenario: Quik is restarted or script is stopped
                                // then writer must throw and we will add a message back
                                // then we will iterate over messages and cancel expired ones
                                if (!message.ValidUntil.HasValue || message.ValidUntil >= DateTime.UtcNow)
                                {
                                    var request = message.ToJson();
                                    await writer.WriteLineAsync(request).ConfigureAwait(false);
                                    Interlocked.Add(ref bytes_sent_request, request.Length);
                                    if (!SendQueue.Any())
                                        await writer.FlushAsync().ConfigureAwait(false);
                                }
                                else
                                {
                                    if (message.Id == 0 && logger.IsDebugEnabled)
                                        logger.Debug("Got Request in SendQueue with id=0. All requests must have correlation id");

                                    if (Responses.TryRemove(message.Id, out var tcs))
                                    {
                                        tcs.SetException(new TimeoutException("ValidUntilUTC is less than current time"));
#if TRACE_QUERY
                                        if (logger.IsTraceEnabled)
                                            logger.Trace($"Removed message(Id:{message.Id}) from Responses queue, reason: expired.");
#endif
                                    }
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                // SendQueue.Take(_cts.Token) was cancelled via the token
                            }
                            catch (IOException e)
                            {
                                logger.Warn(e, $"IOException in _requestTask (Lost connection?): {e.Message}");
                                // this catch is for unexpected and unchecked connection termination
                                // add back, there was an error while writing
                                if (message != null)
                                    SendQueue.Add(message);
                                break;
                            }
                        }
                    }
                    catch (IOException e)
                    {
                        logger.Error(e, $"IOException in _requestTask: {e.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.ConditionalTrace("_requestTask is cancelling");
            }
            catch (Exception e)
            {
                logger.Fatal(e, $"Unhandled exception in _requestTask: {e.Message}");
                StopAllCancellation.Cancel();
                throw new AggregateException("Unhandled exception in _requestTask", e);
            }
            finally
            {
                await Close_RequestReplyClientSocket().ConfigureAwait(false);
            }
        }
        private async Task ResponseTaskAction()
        {
            var cancelToken = StopAllCancellation.Token;
            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    // Поток Response использует тот же сокет, что и поток request
                    await EnsureConnectedClient(cancelToken).ConfigureAwait(false);
                    // here we have a connected TCP client
                    try
                    {
                        var stream = new NetworkStream(_responseClient.Client);
                        var reader = new StreamReader(stream, encoding); //true
                        string response = null;
                        while (!cancelToken.IsCancellationRequested && (response = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                        {
                            // No IO exceptions possible for response, move its processing
                            // to the threadpool and wait for the next mesaage
                            // A new task here gives c.30% boost for full TransactionSpec echo
                            try
                            {
                                //Deserialize into a JObject
                                JToken token = JObject.Parse(response);
                                long message_id = (long)token.SelectToken("id");
                                if (message_id <= 0)
                                    throw new Exception("ResponseTaskAction: Error: message_id less or equal 0.");

                                // it is a response message                                    
                                if (Responses.TryRemove(message_id, out var tcs))
                                {
#if TRACE_QUERY
                                    if (logger.IsTraceEnabled)
                                        logger.Trace($"ResponseTaskAction: Removed message(Id:{message_id}) from Responses queue, Got answer.");
#endif
                                    tcs.SetResult(token);
                                }
                                else
                                {
                                    logger.ConditionalTrace($"No Response is waiting for message with Id:{message_id}");
                                }
                            }
                            catch (Exception e) // deserialization exception is possible
                            {
                                logger.Error(e, $"ResponseTaskAction: Exception: {e.Message}");
                                logger.Error("Response JSON: " + response);
                            }

                            Interlocked.Add(ref bytes_recieved_response, response.Length);
                        }
                    }
                    catch (IOException e)
                    {
                        logger.Error(e, "IOException in ResponseTaskAction: Connection lost?");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.ConditionalTrace("ResponseTaskAction: Response task is cancelling");
            }
            catch (Exception e)
            {
                logger.Fatal(e, $"Unhandled exception in ResponseTaskAction: {e.Message}");
                StopAllCancellation.Cancel();
                throw new AggregateException("Unhandled exception in ResponseTaskAction", e);
            }
            finally
            {
                await Close_RequestReplyClientSocket().ConfigureAwait(false);
            }
        }
        private async Task CallbackTaskAction()
        {
            var cancelToken = StopAllCancellation.Token;
            try
            {
                // reconnection loop
                while (!cancelToken.IsCancellationRequested)
                {
                    await EnsureConnectedClient(cancelToken).ConfigureAwait(false);
                    // now we are connected
                    // here we have a connected TCP client
                    try
                    {
                        var stream = new NetworkStream(_callbackClient.Client);
                        var reader = new StreamReader(stream, encoding); //true
                        string callback = null;
                        while (!cancelToken.IsCancellationRequested && (callback = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                        {
                            try
                            {
                                // it is a callback message
                                ProcessCallbackMessage(callback);
                            }
                            catch (Exception e) // deserialization exception is possible
                            {
                                logger.Error(e, $"Exception in 'CallbackTaskAction': {e.Message}");
                                logger.Error("Recieved JSON: " + callback);
                            }

                            Interlocked.Add(ref bytes_recieved_callback, callback.Length);
                        }
                    }
                    catch (IOException e)
                    {
                        logger.Error(e, "IOExceptioon in CallbackTaskAction: Lost connection?");
                        // handled exception will cause reconnect in the outer loop
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.ConditionalTrace("Callback task is cancelling");
            }
            catch (Exception e)
            {
                logger.Fatal(e, $"Unhandled exception in _callbackReceiverTask: {e.Message}");
                StopAllCancellation.Cancel();
                throw new AggregateException("Unhandled exception in background task", e);
            }
            finally
            {
                await _syncRoot.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_callbackClient != null)
                    {
                        try
                        {
                            _callbackClient.Client?.Shutdown(SocketShutdown.Both);
                        }
                        finally
                        {
                            _callbackClient.Client?.Close();
                            _callbackClient.Close();
                            _callbackClient = null;
                        }
                        logger.ConditionalTrace("Callback channel disconnected");
                    }
                }
                finally
                {
                    _syncRoot.Release();
                }
            }
        }

        // ----------------------- TCP Socket functions -------------------------------------------------------------------------------------------
        private static bool IsConnectedTCP(TcpClient tcp_client) => tcp_client != null && tcp_client.Connected && tcp_client.Client.IsConnectedNow();
        public bool IsServiceConnected() => IsConnectedTCP(_responseClient) && (_callbackPort == 0 || IsConnectedTCP(_callbackClient));
        private async Task Close_RequestReplyClientSocket()
        {
            await _syncRoot.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_responseClient != null)
                {
                    try
                    {
                        _responseClient.Client?.Shutdown(SocketShutdown.Both);
                    }
                    finally
                    {
                        _responseClient.Client?.Close();
                        _responseClient.Close();
                        _responseClient.Dispose();
                        _responseClient = null; // У нас два потока работают с одним сокетом, но только один из них должен его закрыть !
                        logger.ConditionalTrace("Request/Response channel disconnected");
                    };
                }
            }
            catch (Exception e)
            {
                logger.Fatal(e, $"Unhandled exception while close _responseClient socket: {e.Message}");
            }
            finally
            {
                _syncRoot.Release();
            }
        }
        private static void NewTCPSocket(ref TcpClient tcp_client)
        {
            if (tcp_client != null)
            { // cleanup
                try
                {
                    tcp_client.Client?.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    tcp_client.Client?.Close();
                    tcp_client.Close();
                    tcp_client.Dispose();
                }
            }
            tcp_client = new TcpClient
            {
                ExclusiveAddressUse = true,
                NoDelay = true,
                LingerState = new LingerOption(false, 0),
            };
        }
        private async Task EnsureConnectedClient(CancellationToken ct)
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
                        Task.WaitAll(new Task[] { task1, task2 }, cancellationToken: ct);
                    }
                    catch (Exception exc)
                    {
                        logger.Error(exc, $"Exception while trying to connect to {_ipaddress}:[{_responsePort},{_callbackPort}]: {exc.Message}");
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
                    SendQueue.Add(rr.request);
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
            catch (Exception e)
            {
                logger.Error(e, $"Exception in ProcessCallbackMessage wile call for Event/Invoke '{eventName}':  {e.Message}");
            }
        }
        public async Task<TResult> SendAsync<TResult>(IMessage request, int timeout_ms = 0)
        {
            // use DefaultSendTimeout for default calls
            if (timeout_ms == 0)
                timeout_ms = (int)DefaultSendTimeout.TotalMilliseconds;

            if (!_connectedMre.isSet)
            {
                if (timeout_ms > 0)
                {
                    var task = Task.Delay(timeout_ms);
                    if (await Task.WhenAny(_connectedMre.WaitAsync, task).ConfigureAwait(false) == task)
                    {
                        // timeout
                        throw new TimeoutException("Send operation timed out wait for Connection");
                    }
                }
                else
                {
                    await _connectedMre.WaitAsync.ConfigureAwait(false);
                }
                if (StopAllCancellation.IsCancellationRequested)
                    throw new OperationCanceledException("Service stopped!", StopAllCancellation.Token);
            }

            if (request.Id <= 0)
                request.Id = GetNewUniqueId();

            var responseType = typeof(Message<TResult>);
            var rr = new RequestReplyState<JToken>(request, responseType);
            Responses[request.Id] = rr;
#if TRACE_QUERY
            if (logger.IsTraceEnabled)
                logger.Trace($"Added request message(Id:{request.Id}) to Responses queue.");
#endif
            // add to queue after responses dictionary
            SendQueue.Add(request);

            CancellationTokenSource cts = new CancellationTokenSource();
            if (timeout_ms > 0)
            {
                cts.CancelAfter(timeout_ms);
                cts.Token.Register((_rr) =>
                {
                    var rrState = _rr as RequestReplyState<JToken>;
                    if (rrState.SetException(new TimeoutException("Send operation timed out")))
                    {
                        long id = (_rr as RequestReplyState<JToken>).request.Id;
                        Responses.TryRemove(id, out var temp);
#if TRACE_QUERY
                        if (logger.IsTraceEnabled)
                            logger.Trace($"Removed request message(Id:{id}, cmd:{request.Command}) from Responses queue, reason: Send operation timed out.");
#endif
                    }
                }, rr, useSynchronizationContext: false);
            }

            try
            {
                JToken jtoken = await rr.ResultTask.ConfigureAwait(false);
                string command = (string)jtoken.SelectToken("cmd");
                ProcessMessageForLuaError(jtoken, command);
                var response = jtoken.FromJToken(responseType) as IMessage;
                if (response.ValidUntil.HasValue && response.ValidUntil < DateTime.UtcNow)
                    throw new TimeoutException($"Respose message (Id:{response.Id}, cmd:{command}) expired! ValidUntilUTC is less than current time");
                if (string.Compare(request.Command, response.Command, true) != 0)
                    throw new Exception($"SendAsync: Fatal exception: response.Command[{response.Command}] != request.command[{request.Command}]");
                return (TResult)response.Data;
            }
            catch (Exception e) // deserialization exception is possible
            {
                logger.Error(e, $"Exception in 'SendAsync - Process response': {e.Message}");
                throw;
            }
            finally
            {
                if (Responses.TryRemove(request.Id, out var temp))
                {
#if TRACE_QUERY
                    if (logger.IsTraceEnabled)
                        logger.Trace($"Finally removed request message(id:{request.Id}) from Responses queue");
#endif
                }
            }
        }
        private void ProcessMessageForLuaError(JToken jtoken, string cmd)
        {
            var lua_error = (string)jtoken.SelectToken("lua_error");
            if (!string.IsNullOrEmpty(lua_error))
            {
                LuaException exn = string.Compare(cmd, "lua_transaction_error") == 0 ? new TransactionException(lua_error) : new LuaException(lua_error);
                logger.Error($" LUA Error [{cmd}]: {lua_error}");
                // terminate listener task that was processing this task

                long? id = (long?)jtoken.SelectToken("id");
                if (id.HasValue && Responses.TryGetValue(id.Value, out var rRState))
                    rRState.SetException(exn);
                else
                    throw exn;
            }
            if (string.IsNullOrEmpty(cmd))
            {
                throw new ArgumentException("Bad message format: no cmd or lua_error fields");
            }

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
                }
                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // TODO: установить значение NULL для больших полей
                disposedValue = true;
            }
        }

        // // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов
        // ~QuikService()
        // {
        //     // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}