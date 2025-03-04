﻿// Copyright (c) 2021 Alex Mishin, https://github.com/x8800
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using QUIKSharp.DataStructures;

namespace QUIKSharp
{
    public class UnattendedSecurity : Param, ISecurity
    {
    }

    public class UnattendedTradeSecurity : UnattendedSecurity, ITradeSecurity, ISecurity, ITrader
    {
        public string AccountID { get; set; }
        public string ClientCode { get; set; }
        public string FirmId { get; set; }
    }
}