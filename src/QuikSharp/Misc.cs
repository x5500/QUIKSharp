// Copyright (c) 2014-2020 QUIKSharp Authors https://github.com/finsight/QUIKSharp/blob/master/AUTHORS.md. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

using NLog;
using NLog.LayoutRenderers;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace QUIKSharp
{
    public static class QuikSharpUtils
    {
        /// <summary>
        /// The connection state of a socket is reflected in the Connected property,
        /// but this property is only updated with the last send or receive action.
        /// To determine the connection state before send or receive the one and only way
        /// is polling the state directly from the socket itself. The following
        /// extension class does this.
        /// </summary>
        public static bool IsConnectedNow(this Socket s)
        {
            var part1 = s.Poll(1000, SelectMode.SelectRead);
            var part2 = (s.Available == 0);
            if ((part1 && part2) || !s.Connected) return false;
            return true;
        }

		/// <summary>
		/// renders exception starting from new line
		/// with short type exception name followed by message
		/// and stacktrace (optionally)
		/// if exception is logged more than once (catched, logged and re-thrown as inner), stack trace is not written
		/// </summary>
		[LayoutRenderer("IndentException")]
		public class IndentExceptionLayoutRenderer : LayoutRenderer
		{
			/// <summary>
			/// indent before exception type (default is tab)
			/// </summary>
			public string Indent { get; set; }
			/// <summary>
			/// indent between each stack trace line (default is two tab characters)
			/// </summary>
			public string StackTraceIndent { get; set; }
			/// <summary>
			/// is written before exception type name (default [)
			/// </summary>
			public string BeforeType { get; set; }
			/// <summary>
			/// is written after exception type name (default ])
			/// </summary>
			public string AfterType { get; set; }
			/// <summary>
			/// separator between exception type and message
			/// </summary>
			public string Separator { get; set; }
			/// <summary>
			/// log stack trace or not (for console logger e.g.)
			/// </summary>
			public bool LogStack { get; set; }

			/// <summary>
			/// holds logged already exceptions just to skip surplus stack logging
			/// </summary>
			static ConcurrentQueue<Exception> _loggedErrors = new ConcurrentQueue<Exception>();

			public IndentExceptionLayoutRenderer()
			{
				Indent = "\t";
				StackTraceIndent = "\t\t";
				BeforeType = "[";
				AfterType = "]";
				LogStack = true;
				Separator = " ";
			}

			protected override void Append(StringBuilder builder, LogEventInfo logEvent)
			{
				var e = logEvent.Exception;
				while (e != null)
				{
					builder.AppendFormat("{1}{2}{0}{3}{4}", e.GetType().Name, Indent, BeforeType, AfterType, Separator);
					builder.Append(e.Message);

					if (LogStack)
					{
						var stackTraceWasLogged = _loggedErrors.Contains(e);
						if (!stackTraceWasLogged)
						{
							builder.AppendLine();
							_loggedErrors.Enqueue(e);
							builder.AppendFormat("{0}", e.StackTrace.Replace("   ", StackTraceIndent));
						}

						if (_loggedErrors.Count > 50)
						{
							_loggedErrors.TryDequeue(out Exception ex1);
							_loggedErrors.TryDequeue(out Exception ex2);
						}
					}

					e = e.InnerException;
					if (e != null)
						builder.AppendLine();
				}
			}            
        }
	}
}