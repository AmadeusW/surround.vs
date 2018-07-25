using System;
using System.Collections.Generic;

namespace Surround.VS
{
    public interface IBraceCompletionMetadata
    {
        IEnumerable<char> OpeningBraces { get; }

        IEnumerable<char> ClosingBraces { get; }

        IEnumerable<string> ContentTypes { get; }
    }
}
