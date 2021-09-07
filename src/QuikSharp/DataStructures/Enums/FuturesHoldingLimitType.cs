using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QUIKSharp.DataStructures
{
    /// <summary>
    /// Тип лимита.Возможные значения:
    /// «0» – не определён;
    /// «1» – основной счет;
    /// «2» – клиентские и дополнительные счета;
    /// «4» – все счета торг.членов
    /// </summary>
    public enum FuturesHoldingLimitType
    {
        Undefined = 0,
        Primar = 1,
        Additional = 2,
        Other = 4,
    }
}
