﻿using Microsoft.VisualStudio.Text.BraceCompletion;
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
    [BracePair('\'', '\'')]
    [BracePair('"', '"')]
    [BracePair('(', ')')]
    [BracePair('[', ']')]
    [BracePair('{', '}')]
    [BracePair('<', '>')]
    public class CSharpBraceData : IBraceCompletionDefaultProvider
    { }

    [Export(typeof(IBraceCompletionDefaultProvider))]
    [ContentType("TypeScript")]
    [BracePair('\'', '\'')]
    [BracePair('"', '"')]
    [BracePair('(', ')')]
    [BracePair('[', ']')]
    [BracePair('{', '}')]
    public class TypeScriptBraceData : IBraceCompletionDefaultProvider
    { }

    [Export(typeof(IBraceCompletionDefaultProvider))]
    [ContentType("code++.Markdown")]
    [BracePair('\'', '\'')]
    [BracePair('"', '"')]
    [BracePair('*', '*')]
    [BracePair('_', '_')]
    [BracePair('|', '|')]
    [BracePair('`', '`')]
    public class MarkdownBraceData : IBraceCompletionDefaultProvider
    { }

    [Export(typeof(IBraceCompletionDefaultProvider))]
    [ContentType("code++.JSON (Javascript Next)")]
    [BracePair('\'', '\'')]
    [BracePair('"', '"')]
    [BracePair('[', ']')]
    public class JsonBraceData : IBraceCompletionDefaultProvider
    { }
}
