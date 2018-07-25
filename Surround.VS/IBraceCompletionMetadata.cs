using System;
using System.Collections.Generic;

namespace Surround.VS
{
    // We use this to import data:
    public interface IBraceCompletionMetadata
    {
        IEnumerable<char> OpeningBraces { get; }

        IEnumerable<char> ClosingBraces { get; }

        IEnumerable<string> ContentTypes { get; }
    }

    // We use these to export data:

    public class OpeningBracesAttribute : Attribute
    {
        public OpeningBracesAttribute(params char[] chars)
        {
            OpeningBraces = chars;
        }

        public char[] OpeningBraces { get; }
    }

    public class ClosingBracesAttribute : Attribute
    {
        public ClosingBracesAttribute(params char[] chars)
        {
            ClosingBraces = chars;
        }

        public char[] ClosingBraces { get; }
    }
}
