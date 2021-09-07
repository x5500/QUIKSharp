// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// Способ указания объема заявки. Возможные значения:
    /// «0» – по количеству,
    /// «1» – по объему
    /// </summary>
    public enum ValueEntryType
    {
        byQty = 0,
        byVolume = 1,
    }
}