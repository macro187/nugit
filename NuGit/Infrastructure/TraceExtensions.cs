using System;
using System.Diagnostics;

namespace NuGit
{

    public static class TraceExtensions
    {

        /// <summary>
        /// Print a heading to trace output
        /// </summary>
        ///
        public static void TraceHeading(string heading)
        {
            if (heading == null) throw new ArgumentNullException("heading");
            Trace.WriteLine("");
            Trace.WriteLine("==> " + heading);
        }

    }

}
