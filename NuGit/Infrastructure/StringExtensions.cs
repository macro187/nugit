using System;
using System.Globalization;

namespace NuGit.Infrastructure
{

    public static class StringExtensions
    {
        
        /// <summary>
        /// <see cref="string.Format(string, object[])"/> using <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        ///
        public static string FormatInvariant(string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }


        /// <summary>
        /// Convert any line endings in a string to <see cref="Environment.NewLine"/>
        /// </summary>
        ///
        public static string NormaliseLineEndings(string value)
        {
            if (value == null) throw new ArgumentNullException("value");
            return value.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
        }

    }

}
