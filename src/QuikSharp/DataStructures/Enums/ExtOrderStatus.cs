// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace QUIKSharp.DataStructures.Transaction
{
    /// <summary>
    /// Расширенный статус заявки. Возможные значения:
    /// </summary>
    public enum ExtOrderStatus
    {
        /// <summary>
        /// «0» (по умолчанию) – не определено;
        /// </summary>
        None = 0,

        /// <summary>
        /// «1» – заявка активна;
        /// </summary>
        Active = 1,

        /// <summary>
        /// «2» – заявка частично исполнена;
        /// </summary>
        PartialFill = 2,

        /// <summary>
        /// «3» – заявка исполнена;
        /// </summary>
        Filled = 3,

        /// <summary>
        /// «4» – заявка отменена;
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// «5» – заявка заменена;
        /// </summary>
        Replaced = 5,

        /// <summary>
        /// «6» – заявка в состоянии отмены;
        /// </summary>
        Cancelling = 6,

        /// <summary>
        /// «7» – заявка отвергнута;
        /// </summary>
        Rejected = 7,

        /// <summary>
        /// «8» – приостановлено исполнение заявки;
        /// </summary>
        Suspended = 8,

        /// <summary>
        /// «9» – заявка в состоянии регистрации;
        /// </summary>
        Registering = 9,

        /// <summary>
        /// «10» – заявка снята по времени действия;
        /// </summary>
        Expired = 10,

        /// <summary>
        /// «11» – заявка в состоянии замены
        /// </summary>
        Replacing = 11,
    }
}