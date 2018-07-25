using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surround.VS
{
    [Export(typeof(ICommandHandler))]
    [ContentType("text")]
    [Name("Surround selection command handler")]
    class CommandHandlers : ICommandHandler<TypeCharCommandArgs>
    {
        const bool useTwoUndos = false; // based on PR feedback

        [ImportMany]
        private IEnumerable<Lazy<IBraceCompletionDefaultProvider, IBraceCompletionMetadata>> BraceCompletionProviders;

        Dictionary<string, Dictionary<char, char>> _contentTypeToBracePairs = null;
        Dictionary<string, Dictionary<char, char>> ContentTypeToBracePairs = new Dictionary<string, Dictionary<char, char>>();
        Dictionary<string, Dictionary<char, char>> _allContentTypeAndBracePairs = null;
        Dictionary<string, Dictionary<char, char>> AllContentTypeAndBracePairs
        {
            get
            {
                if (_contentTypeToBracePairs == null)
                {
                    _contentTypeToBracePairs = new Dictionary<string, Dictionary<char, char>>();
                    foreach (var braceCompletionProvider in BraceCompletionProviders)
                    {
                        foreach (var contentType in braceCompletionProvider.Metadata.ContentTypes)
                        {
                            var bracePairs = new Dictionary<char, char>();
                            var pairs = braceCompletionProvider.Metadata.OpeningBraces.Zip(braceCompletionProvider.Metadata.ClosingBraces, (a, b) => (a, b));
                            foreach (var pair in pairs)
                            {
                                bracePairs[pair.Item1] = pair.Item2;
                            }
                            _contentTypeToBracePairs[contentType] = bracePairs;
                        }
                    }
                }
                return _contentTypeToBracePairs;
            }
        }

        // Check if there is a selection
        // Optionally, Check if selection is on the word boundary
        // If there is, use brace completion to insert matching character
        public string DisplayName => throw new NotImplementedException();

        Dictionary<char, char> BracePairs = new Dictionary<char, char>()
        {
            { '\'', '\''},
            { '"', '"'},
            { '`', '`'},
            { '(', ')'},
            { '<', '>'},
            { '{', '}'},
            { '[', ']'},
            { '_', '_'},
            { '*', '*'},
        };

        Dictionary<char, (string opening, string closing)> TypedCharToStarAndEndChar = new Dictionary<char, (string, string)>();

        public CommandHandlers()
        {
            foreach (var pair in BracePairs)
            {
                TypedCharToStarAndEndChar[pair.Key] = (pair.Key.ToString(), pair.Value.ToString());
                TypedCharToStarAndEndChar[pair.Value] = (pair.Key.ToString(), pair.Value.ToString());
            }
        }

        public bool ExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext)
        {
            var selection = args.TextView.Selection;
            if (selection.IsEmpty)
                return false;
            if (selection.Start != args.TextView.Caret.Position.VirtualBufferPosition
                && selection.End != args.TextView.Caret.Position.VirtualBufferPosition)
                return false;
            if (!TypedCharToStarAndEndChar.ContainsKey(args.TypedChar))
                return false;
            // add specific args.SubjectBuffer.ContentType into ContentTypeToBracePairs
            if (!ContentTypeToBracePairs.ContainsKey(args.SubjectBuffer.ContentType.TypeName))
            {
                var applicableContentTypes = AllContentTypeAndBracePairs.Keys.Where(ct => args.SubjectBuffer.ContentType.IsOfType(ct));
                if (!applicableContentTypes.Any())
                {
                    ContentTypeToBracePairs[args.SubjectBuffer.ContentType.TypeName] = new Dictionary<char, char>();
                    return false;
                }

                var map = new Dictionary<char, char>();
                foreach (var applicableContentType in applicableContentTypes)
                {
                    var relevant = AllContentTypeAndBracePairs[applicableContentType];
                    foreach (var pair in relevant)
                    {
                        map[pair.Key] = pair.Value;
                    }
                }
                ContentTypeToBracePairs[args.SubjectBuffer.ContentType.TypeName] = map;
            }
            var bracePairs = ContentTypeToBracePairs[args.SubjectBuffer.ContentType.TypeName];
            if (bracePairs.ContainsKey(args.TypedChar))
            {
                var match = bracePairs[args.TypedChar];
            }


            // Preserve the selection
            var growingSelectionStart = selection.Start.Position.Snapshot.CreateTrackingPoint(selection.Start.Position.Position, PointTrackingMode.Negative);
            var growingSelectionEnd = selection.Start.Position.Snapshot.CreateTrackingPoint(selection.End.Position.Position, PointTrackingMode.Positive);
            var selectionReversed = selection.IsReversed;

            var characterPair = TypedCharToStarAndEndChar[args.TypedChar];

            ITextEdit edit;
            if (useTwoUndos)
            {
                // Allow user to undo the matching character.
                // To do this, insert character at caret location first, then insert matching character
                if (selection.Start == args.TextView.Caret.Position.VirtualBufferPosition)
                {
                    // First edit: opening character at caret location
                    edit = args.TextView.TextBuffer.CreateEdit();
                    edit.Insert(selection.Start.Position, characterPair.opening);
                    edit.Apply();

                    // Second edit: closing character
                    edit = args.TextView.TextBuffer.CreateEdit();
                    edit.Insert(selection.End.Position, characterPair.closing);
                    edit.Apply();
                }
                else
                {
                    // First edit: closing character at caret location
                    edit = args.TextView.TextBuffer.CreateEdit();
                    edit.Insert(selection.End.Position, characterPair.closing);
                    edit.Apply();

                    // Second edit: opening character
                    edit = args.TextView.TextBuffer.CreateEdit();
                    edit.Insert(selection.Start.Position, characterPair.opening);
                    edit.Apply();
                }
            }
            else
            {
                // Single undo operation
                edit = args.TextView.TextBuffer.CreateEdit();
                edit.Insert(selection.Start.Position, characterPair.opening);
                edit.Insert(selection.End.Position, characterPair.closing);
                edit.Apply();
            }

            // Restore selection
            var newSnapshot = selection.Start.Position.Snapshot;
            selection.Select(new SnapshotSpan(growingSelectionStart.GetPoint(newSnapshot), growingSelectionEnd.GetPoint(newSnapshot)), selectionReversed);

            // Move the caret so that this operation can be repeated
            args.TextView.Caret.MoveTo(selectionReversed ? growingSelectionStart.GetPoint(newSnapshot) : growingSelectionEnd.GetPoint(newSnapshot));
            return true; // we don't want to type and replace the selection
        }

        public CommandState GetCommandState(TypeCharCommandArgs args) => CommandState.Unspecified;
    }
}
