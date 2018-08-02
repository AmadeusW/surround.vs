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
        public string DisplayName => "Surround selection command handler";
        const bool useTwoUndos = false; // Prototype based on PR feedback

        [ImportMany]
        private IEnumerable<Lazy<IBraceCompletionDefaultProvider, IBraceCompletionMetadata>> BraceCompletionProviders;

        Dictionary<string, Dictionary<char, char>> ContentTypeToBracePairs = new Dictionary<string, Dictionary<char, char>>();
        Dictionary<string, Dictionary<char, char>> _allContentTypeAndBracePairs = null;
        Dictionary<string, Dictionary<char, char>> AllContentTypeAndBracePairs
        {
            get
            {
                if (_allContentTypeAndBracePairs == null)
                {
                    _allContentTypeAndBracePairs = new Dictionary<string, Dictionary<char, char>>();
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
                            _allContentTypeAndBracePairs[contentType] = bracePairs;
                        }
                    }
                }
                return _allContentTypeAndBracePairs;
            }
        }

        public CommandState GetCommandState(TypeCharCommandArgs args) => CommandState.Unspecified;

        public bool ExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext)
        {
            var contentType = args.TextView.TextBuffer.ContentType;
            var selection = args.TextView.Selection;
            if (selection.IsEmpty)
                return false;
            // Act only if caret is in the middle of selection
            if (selection.Start != args.TextView.Caret.Position.VirtualBufferPosition
                && selection.End != args.TextView.Caret.Position.VirtualBufferPosition)
                return false;

            var bracePairs = GetBracePairs(args.SubjectBuffer.ContentType);
            char opening = default(char);
            char closing = default(char);
            if (bracePairs?.ContainsKey(args.TypedChar) == true)
            {
                // We act only when user typed opening brace
                // For implmementation that works with either brace, see tag v0.2
                opening = args.TypedChar;
                closing = bracePairs[args.TypedChar];
            }
            else
            {
                return false;
            }

            // Preserve the selection
            var growingSelectionStart = selection.Start.Position.Snapshot.CreateTrackingPoint(selection.Start.Position.Position, PointTrackingMode.Negative);
            var growingSelectionEnd = selection.Start.Position.Snapshot.CreateTrackingPoint(selection.End.Position.Position, PointTrackingMode.Positive);
            var selectionReversed = selection.IsReversed;

            ITextEdit edit;
            if (useTwoUndos)
            {
                // Allow user to undo the matching character.
                // To do this, insert character at caret location first, then insert matching character
                if (selection.Start == args.TextView.Caret.Position.VirtualBufferPosition)
                {
                    // First edit: opening character at caret location
                    edit = args.TextView.TextBuffer.CreateEdit();
                    edit.Insert(selection.Start.Position, opening.ToString());
                    edit.Apply();

                    // Second edit: closing character
                    edit = args.TextView.TextBuffer.CreateEdit();
                    edit.Insert(selection.End.Position, closing.ToString());
                    edit.Apply();
                }
                else
                {
                    // First edit: closing character at caret location
                    edit = args.TextView.TextBuffer.CreateEdit();
                    edit.Insert(selection.End.Position, closing.ToString());
                    edit.Apply();

                    // Second edit: opening character
                    edit = args.TextView.TextBuffer.CreateEdit();
                    edit.Insert(selection.Start.Position, opening.ToString());
                    edit.Apply();
                }
            }
            else
            {
                // Single undo operation
                edit = args.TextView.TextBuffer.CreateEdit();
                edit.Insert(selection.Start.Position, opening.ToString());
                edit.Insert(selection.End.Position, closing.ToString());
                edit.Apply();
            }

            // Restore selection
            var newSnapshot = selection.Start.Position.Snapshot;
            selection.Select(new SnapshotSpan(growingSelectionStart.GetPoint(newSnapshot), growingSelectionEnd.GetPoint(newSnapshot)), selectionReversed);

            // Move the caret so that this operation can be repeated
            args.TextView.Caret.MoveTo(selectionReversed ? growingSelectionStart.GetPoint(newSnapshot) : growingSelectionEnd.GetPoint(newSnapshot));
            return true; // we don't want to type and replace the selection
        }

        private Dictionary<char, char> GetBracePairs(IContentType contentType)
        {
            if (!ContentTypeToBracePairs.ContainsKey(contentType.TypeName))
            {
                var applicableContentTypes = AllContentTypeAndBracePairs.Keys.Where(ct => contentType.IsOfType(ct));
                if (!applicableContentTypes.Any())
                {
                    ContentTypeToBracePairs[contentType.TypeName] = null;
                    return null;
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
                ContentTypeToBracePairs[contentType.TypeName] = map;
            }
            return ContentTypeToBracePairs[contentType.TypeName];
        }
    }
}
