// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace QUIKSharp.QOrders
{
    public interface IStopOrder
    {
        /// <summary>
        /// Стоп-цена по инстументу, при ее достижении срабатывает стоп-ордер
        /// </summary>
        decimal StopPrice { get; set; }

        /// <summary>
        /// Для Стоп-Ордера цена исполнения указывается в Поле Price. Этот параметр просто ссылается на параметр Price
        /// </summary>
        decimal StopDealPrice { get; set; }
    }
}