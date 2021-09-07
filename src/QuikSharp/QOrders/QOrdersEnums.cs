// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace QUIKSharp.QOrders
{
    public enum QOrderState
    {
        ErrorRejected = -1,

        /// <summary>
        /// Например, только что созданный ордер
        /// </summary>
        None = 0,

        /// <summary>
        /// WAITPLACEMENT -> request -> Placed -> Executed -> Filled
        /// </summary>
        WaitPlacement,

        /// <summary>
        /// WaitPlacement -> REQUEST -> Placed -> Executed -> Filled
        /// </summary>
        RequestedPlacement,

        /// <summary>
        /// WaitPlacement -> request -> PLACED -> Executed -> Filled
        /// </summary>
        Placed,

        /// <summary>
        /// Одрер исполнен (если это стоп ордер, то по его исполнению размещены лимитные ордера, которые могут быть не сразу исполнены)
        /// WaitPlacement -> request -> placed -> EXECUTED -> Filled
        /// </summary>
        Executed,

        /// <summary>
        /// Ордер исполнен полностью. Если это стоп-ордер, то размещенные им лимитные ордера исполнены полностью.
        /// WaitPlacement -> request -> placed -> Executed -> FILLED
        /// </summary>
        Filled,

        /// <summary>
        /// Ордер снят
        /// </summary>
        Killed,

        /// <summary>
        /// WAITMOVE -> RequestedMove -> Placed
        /// </summary>
        WaitMove,

        /// <summary>
        /// WaitMove -> REQUESTEDMOVE -> Placed
        /// </summary>
        RequestedMove,
    }

    public enum QOrderKillState
    {
        NoKill,

        /// <summary>
        /// NoKill -> WAITKILL / WaitOrderNum -> RequestedKill -> killed
        /// </summary>
        WaitKill,

        /// <summary>
        /// NoKill -> Waitkill / WaitOrderNum -> REQUESTEDKILL -> killed
        /// </summary>
        RequestedKill,

        /// <summary>
        /// NoKill -> Waitkill / WaitOrderNum -> RequestedKill -> KILLED
        /// </summary>
        Killed,

        ErrorRejected,
    }

    public enum QOrderLinkedRole
    {
        /// <summary>
        /// Это отдельный лимитный-ордер, нет связанного с ним стоп-ордера
        /// </summary>
        StandAlone,

        /// <summary>
        /// Есть стоп-ордер, который зависит от этого лимитного ордера
        /// </summary>
        MasterOrder,

        /// <summary>
        /// Этот лимитный ордер размещен автоматичекски при размещении стоп-ордера и зависит от него
        /// </summary>
        ControlledByStopOrder,

        /// <summary>
        /// Этот лимитный ордер был размещен при исполнении стоп-ордера
        /// </summary>
        PlacedByStopOrder,
    }
}