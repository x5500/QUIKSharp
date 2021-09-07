// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// Тип исполнения заявки. Возможные значения:
    /// </summary>
    public enum OrderExecType
    {
        /// <summary>
        /// «0» – Значение не указано;
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// «1» – Немедленно или отклонить (FOK);
        /// </summary>
        FillOrKill = 1,

        /// <summary>
        /// «2» – Поставить в очередь;
        /// </summary>
        PlaceInQuery = 2,

        /// <summary>
        /// «3» – Снять остаток;
        /// </summary>
        ImmediateOrCancel = 3,

        /// <summary>
        /// «4» – До снятия;
        /// </summary>
        GoodTillCancelled = 4,

        /// <summary>
        /// «5» – До даты;
        /// </summary>
        TillDate = 5,

        /// <summary>
        /// «6» – В течение сессии;
        /// </summary>
        WhileThisSession = 6,

        /// <summary>
        /// «7» – Открытие;
        /// </summary>
        Opening = 7,

        /// <summary>
        /// «8» – Закрытие;
        /// </summary>
        Closing = 8,

        /// <summary>
        /// «9» – Кросс;
        /// </summary>
        Cross = 9,

        /// <summary>
        /// «11» – До следующей сессии;
        /// </summary>
        UntillNextSession = 11,

        /// <summary>
        /// «13» – До отключения;
        /// </summary>
        UntillDisconnect = 13,

        /// <summary>
        /// «15» – До времени;
        /// </summary>
        UntillTime = 15,

        /// <summary>
        /// «16» – Следующий аукцион;
        /// </summary>
        NextAution = 16,

        /// <summary>
        /// «17» – ExecType17;
        /// </summary>
        ExecType17 = 17,

    }
}