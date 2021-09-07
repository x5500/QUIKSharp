// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QUIKSharp
{
    public interface ICandleFunctions
    {
        Task<List<Candle>> GetAllCandles(string graphicTag);

        Task<List<Candle>> GetAllCandles(ISecurity sec, CandleInterval interval);

        Task<List<Candle>> GetCandles(string graphicTag, long line, long first, long count);

        Task<List<Candle>> GetLastCandles(ISecurity sec, CandleInterval interval, long count);

        Task<long> GetNumCandles(string graphicTag);

        Task<bool> IsSubscribed(ISecurity sec, CandleInterval interval);

        Task Subscribe(ISecurity sec, CandleInterval interval);

        Task Unsubscribe(ISecurity sec, CandleInterval interval);
    }
}