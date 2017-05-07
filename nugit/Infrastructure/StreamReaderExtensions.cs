using System.Collections.Generic;
using System.IO;

namespace nugit.Infrastructure
{

    public static class StreamReaderExtensions
    {

        public static IEnumerable<string> ReadAllLines(this StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null) yield return line;
        }

    }

}
