// Copyright (c) 2021 alex.mishin@me.com77
// Based on QUIKSharp, Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using NLog;
using System;

namespace QUIKSharp.Functions
{
    public abstract class FunctionsBase
        : IDisposable
    {
        protected static readonly Logger logger = LogManager.GetCurrentClassLogger();

        internal IQuikService QuikService { get; private set; }
        protected bool disposedValue { get; private set; } = false;

        internal FunctionsBase(IQuikService quikService)
        {
            QuikService = quikService;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // освободить управляемое состояние (управляемые объекты)
                    if (QuikService != null)
                    {
                        QuikService = null;
                    }
                }
                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}