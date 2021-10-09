// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QUIKSharp
{
    public interface ICandleFunctions
    {
        Task<List<Candle>> GetAllCandles(string graphicTag, CancellationToken cancellationToken);

        Task<List<Candle>> GetAllCandles(ISecurity sec, CandleInterval interval, CancellationToken cancellationToken);

        Task<List<Candle>> GetCandles(string graphicTag, long line, long first, long count, CancellationToken cancellationToken);

        Task<List<Candle>> GetLastCandles(ISecurity sec, CandleInterval interval, long count, CancellationToken cancellationToken);

        Task<long> GetNumCandles(string graphicTag, CancellationToken cancellationToken);

        Task<bool> IsSubscribed(ISecurity sec, CandleInterval interval, CancellationToken cancellationToken);

        Task Subscribe(ISecurity sec, CandleInterval interval, CancellationToken cancellationToken);

        Task Unsubscribe(ISecurity sec, CandleInterval interval, CancellationToken cancellationToken);
    }
}