using System;
using System.Collections.Generic;

namespace nugit.Infrastructure
{

    public static class ReadmeParser
    {

        /// <summary>
        /// Select only certain sections from a Markdown readme file
        /// </summary>
        ///
        public static IEnumerable<string> SelectSections(IEnumerable<string> lines, params string[] sections)
        {
            if (lines == null) throw new ArgumentNullException("lines");
            if (sections == null) throw new ArgumentNullException("sections");
            return SelectSectionsImpl(lines, new HashSet<string>(sections, StringComparer.OrdinalIgnoreCase));
        }


        static IEnumerable<string> SelectSectionsImpl(IEnumerable<string> lines, ISet<string> sections)
        {
            string lastLine = null;
            bool selected = false;
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length > 0 && trimmedLine == new string('=', trimmedLine.Length))
                {
                    if (lastLine == null) continue;
                    selected = sections.Contains(lastLine);
                    if (selected)
                    {
                        yield return lastLine;
                        yield return line;
                    }
                    lastLine = null;
                    continue;
                }

                if (lastLine != null && selected) yield return lastLine;

                lastLine = line;
            }

            if (lastLine != null && selected) yield return lastLine;
        }

    }

}
