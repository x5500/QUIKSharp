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
        /// ������ �������, ����� � �� �������
        /// </summary>
        Active = 0x1,

        /// <summary>
        /// ������ �����. ���� ���� �� ���������� � �������� ���� �0� ����� �0�, �� ������ ���������
        /// </summary>
        Canceled = 0x2,

        /// <summary>
        /// ������ �� �������, ����� � �� �������
        /// </summary>
        IsSell = 0x4,

        /// <summary>
        /// ������ ��������������, ����� � ��������
        /// </summary>
        IsLimit = 0x8,

        /// <summary>
        /// ��������� ������ �� ������ �����
        /// </summary>
        AllowDiffPrice = 0x10,

        /// <summary>
        /// ��������� ������ ���������� ��� ����� (FILL OR KILL)
        /// </summary>
        FillOrKill = 0x20,

        /// <summary>
        /// ������ ������-�������. ��� �������� ������ � ������ ���������� �����������
        /// </summary>
        IsMarketMakerOrSent = 0x40,

        /// <summary>
        /// ������� ������
        /// </summary>
        IsReceived = 0x80,

        /// <summary>
        /// ����� �������
        /// </summary>
        IsKillBalance = 0x100,

        /// <summary>
        /// �������-������
        /// </summary>
        Iceberg = 0x200,

        /// <summary>
        /// ������ ��������� �������� ��������
        /// </summary>
        Rejected = 0x400,

        /// <summary>
        /// ���� �linkedorder� ����������� ������� ����-������
        /// </summary>
        LinkedOrder = 0x100000,
    }
}