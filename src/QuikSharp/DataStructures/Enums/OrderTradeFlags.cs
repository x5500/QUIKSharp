// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.
using System;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    ///
    /// </summary>
    [Flags]
    public enum OrderTradeFlags
    {
        None = 0,

        /// <summary>
        /// Заявка активна, иначе – не активна
        /// </summary>
        Active = 0x1,

        /// <summary>
        /// Заявка снята. Если флаг не установлен и значение бита «0» равно «0», то заявка исполнена
        /// </summary>
        Canceled = 0x2,

        /// <summary>
        /// Заявка на продажу, иначе – на покупку
        /// </summary>
        IsSell = 0x4,

        /// <summary>
        /// Заявка лимитированная, иначе – рыночная
        /// </summary>
        IsLimit = 0x8,

        /// <summary>
        /// Исполнить заявку по разным ценам
        /// </summary>
        AllowDiffPrice = 0x10,

        /// <summary>
        /// Исполнить заявку немедленно или снять (FILL OR KILL)
        /// </summary>
        FillOrKill = 0x20,

        /// <summary>
        /// Заявка маркет-мейкера. Для адресных заявок – заявка отправлена контрагенту
        /// </summary>
        IsMarketMakerOrSent = 0x40,

        /// <summary>
        /// Скрытая заявка
        /// </summary>
        IsReceived = 0x80,

        /// <summary>
        /// Снять остаток
        /// </summary>
        IsKillBalance = 0x100,

        /// <summary>
        /// Айсберг-заявка
        /// </summary>
        Iceberg = 0x200,

        /// <summary>
        /// Заявка отклонена торговой системой
        /// </summary>
        Rejected = 0x400,

        /// <summary>
        /// Поле «linkedorder» заполняется номером стоп-заявки
        /// </summary>
        LinkedOrder = 0x100000,
    }
}