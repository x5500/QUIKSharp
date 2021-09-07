// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace QUIKSharp
{
    /// <summary>
    /// Обьекты размещаются в хранилище по ключам, основанным на Хэш коде объекта, передаваемого как key.
    /// </summary>
    public interface IPersistentStorage
    {
        /// <summary>
        ///
        /// </summary>
        void Set<T>(object key, T value);

        /// <summary>
        ///
        /// </summary>
        T Get<T>(object key);

        /// <summary>
        ///
        /// </summary>
        bool Contains(object key);

        /// <summary>
        ///
        /// </summary>
        bool Remove(object key);
    }
}