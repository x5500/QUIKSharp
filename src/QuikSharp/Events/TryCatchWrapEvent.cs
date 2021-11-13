using NLog;
using System;
using System.Reflection;

namespace QUIKSharp.Events
{
    public abstract class TryCatchWrapEvent
    {
        protected static readonly Logger wrap_logger = LogManager.GetCurrentClassLogger();
        protected static void RunTheEvent(Delegate myEventCaller, params object[] args)
        {
            try
            {
                if (myEventCaller == null) return;
                if (args.Length == 0)
                    myEventCaller.DynamicInvoke();
                else if (args.Length == 1)
                    myEventCaller.DynamicInvoke(args[0]);
                else if (args.Length == 2)
                    myEventCaller.DynamicInvoke(args[0], args[1]);
                else if (args.Length == 3)
                    myEventCaller.DynamicInvoke(args[0], args[1], args[2]);
                else if (args.Length == 4)
                    myEventCaller.DynamicInvoke(args[0], args[1], args[2], args[3]);
                else if (args.Length == 5)
                    myEventCaller.DynamicInvoke(args[0], args[1], args[2], args[3], args[4]);
                else if (args.Length == 6)
                    myEventCaller.DynamicInvoke(args[0], args[1], args[2], args[3], args[4], args[5]);
                else if (args.Length == 7)
                    myEventCaller.DynamicInvoke(args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                else if (args.Length == 8)
                    myEventCaller.DynamicInvoke(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]);
                else if (args.Length == 9)
                    myEventCaller.DynamicInvoke(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]);
                else
                    throw new NotImplementedException($"TryCatchWrapEvent: RunTheEvent not implemented for args count = {args.Length}, caller: '{myEventCaller.GetMethodInfo().Name}'");

            }
            catch (TargetInvocationException tie)
            {
                var e = tie.InnerException; // ex now stores the original exception
                LogException(e);
            }
            catch (AggregateException ae)
            {
                wrap_logger.Error(ae, $"RunTheEvent: Unhandled AggregateException in Event['{myEventCaller.GetMethodInfo().Name}'].Invoke():");
                foreach (var e in ae.InnerExceptions)
                    wrap_logger.Error(e, $"Unhandled exception: {e.Message}\n  --- Exception Trace: ---- \n{e.StackTrace}\n--- Exception trace ----");
            }
            catch (Exception e)
            {
                LogException(e);
            }
            void LogException(Exception e)
            {
                wrap_logger.Error(e, $"RunTheEvent: Unhandled Exception in Event['{myEventCaller.GetMethodInfo().Name}'].Invoke():  {e.Message}\n" +
                    $"  --- Exception Trace: ---- \n{e.StackTrace}\n--- Exception trace ----");
            }
        }
    }
}
