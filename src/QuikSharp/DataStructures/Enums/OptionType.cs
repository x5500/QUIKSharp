using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QUIKSharp.DataStructures
{ 
    public enum OptionType
    {
        /// <summary>
        /// Undefined (error)
        /// </summary>
        Undef = 0,

        /// <summary>
        /// PUT (Short)
        /// </summary>
        Put = 1,

        /// <summary>
        /// Call (Long)
        /// </summary>
        Call = 2,
    }
}
