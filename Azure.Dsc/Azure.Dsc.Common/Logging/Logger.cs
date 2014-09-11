using System;
using System.Diagnostics;

namespace Azure.Dsc.Common.Logging
{
    public static class Logger
    {
        public static void Log(Guid id, Exception ex)
        {
            if (ex.InnerException != null)
            {
                Log(id, ex.InnerException);
            }

            Trace.TraceError(@"[{0}] {1}", id, ex.Message);
        }
    }
}
