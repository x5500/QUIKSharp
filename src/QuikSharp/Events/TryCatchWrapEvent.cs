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
                myEventCaller?.DynamicInvoke(args);
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
