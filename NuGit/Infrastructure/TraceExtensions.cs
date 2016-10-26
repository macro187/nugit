using System;
using System.Diagnostics;

namespace NuGit.Infrastructure
{

    public static class TraceExtensions
    {

        public static IDisposable Step(string description)
        {
            if (description == null) throw new ArgumentNullException("description");
            depth++;
            justFinished = false;
            Trace.WriteLine("");
            Trace.WriteLine(Arrow() + description);
            return new StepFinisher(description);
        }

        
        static int depth = 0;


        static bool justFinished = false;


        static string Arrow()
        {
            return new string('=', depth) + "> ";
        }


        class StepFinisher
            : IDisposable
        {
            public StepFinisher(string description)
            {
                this.description = description;
                disposed = false;
            }

            string description;

            bool disposed;

            [System.Diagnostics.CodeAnalysis.SuppressMessage(
                "Microsoft.Globalization",
                "CA1308:NormalizeStringsToUppercase",
                Justification = "This program assumes english")]
            public void Dispose()
            {
                if (disposed) return;
                int p = Math.Min(1, description.Length);
                if (justFinished)
                {
                    Trace.WriteLine("");
                }
                Trace.WriteLine(
                    Arrow() +
                    "Finished " +
                    description.Substring(0,p).ToLowerInvariant() + description.Substring(p));
                justFinished = true;
                depth--;
            }
        }

    }

}
