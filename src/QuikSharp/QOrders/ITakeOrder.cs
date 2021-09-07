// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace QUIKSharp.QOrders
{
    public interface ITakeOrder
    {
        /// <summary>
        /// Тейк-цена по инстументу, при ее достижении активируется тейк-ордер и начинает отслеживать цену
        /// </summary>
        decimal TakePrice { get; set; }

        /// <summary>
        /// Отступ от масимальной цены, при котором сработает тейк-профит
        /// </summary>
        decimal Offset { get; set; }

        /// <summary>
        /// Защитный спред при выставлении лимитной заявки по тейк-профиту
        /// </summary>
        decimal Spread { get; set; }
    }
}