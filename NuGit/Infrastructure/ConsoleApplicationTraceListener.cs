using System.Diagnostics;

namespace NuGit.Infrastructure
{

    /// <summary>
    /// A trace listener suitable for use as a console program's output mechanism
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="Trace.WriteLine(string)"/> is output to stdout as-is.
    /// </para>
    /// <para>
    /// <see cref="Trace.TraceInformation(string)"/> is output to stdout as-is, if not <see cref="Quiet"/>.
    /// </para>
    /// <para>
    /// <see cref="Trace.TraceWarning(string)"/> and <see cref="Trace.TraceError(string)"/> are output to stdout in the
    /// format <c>[&lt;severity&gt;] &lt;message&gt;</c>.
    /// </para>
    /// </remarks>
    ///
    public class ConsoleApplicationTraceListener
        : ConsoleTraceListener
    {

        public ConsoleApplicationTraceListener()
            : base()
        {
            Quiet = false;
        }


        /// <summary>
        /// Mute unimportant output?
        /// </summary>
        ///
        public bool Quiet
        {
            get;
            set;
        }


        /// <inheritdoc/>
        public override void TraceEvent(
            TraceEventCache eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string format,
            params object[] args)
        {
            TraceEvent(eventCache, source, eventType, id, StringExtensions.FormatInvariant(format, args));
        }


        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Globalization",
            "CA1303:Do not pass literals as localized parameters",
            MessageId = "System.Diagnostics.TraceListener.WriteLine(System.String)",
            Justification = "Don't care")]
        public override void TraceEvent(
            TraceEventCache eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string message)
        { 
            message = message ?? "";

            if (eventType <= TraceEventType.Warning)
            {
                WriteLine("[" + eventType.ToString() + "] " + message);
            }
            else
            {
                if (!Quiet)
                {
                    WriteLine(message);
                }
            }
        }

    }

}
