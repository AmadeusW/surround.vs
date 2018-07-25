using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surround.VS
{
    [Export(typeof(IBraceCompletionDefaultProvider))]
    [ContentType("CSharp")]
    [OpeningBraces('\'', '"', '(', '[', '{')]
    [ClosingBraces('\'', '"', ')', ']', '}')]
    public class CSharpBraceData : IBraceCompletionDefaultProvider
    { }

    [Export(typeof(IBraceCompletionDefaultProvider))]
    [ContentType("Markdown")]
    [OpeningBraces('\'', '"', '|', '*', '_')]
    [ClosingBraces('\'', '"', '|', '*', '_')]
    public class MarkdownBraceData : IBraceCompletionDefaultProvider
    { }
}
