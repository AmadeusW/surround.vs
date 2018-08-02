using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor;
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

        [ImportMany]
        private IEnumerable<Lazy<IBraceCompletionDefaultProvider, IBraceCompletionMetadata>> BraceCompletionProviders;

        Dictionary<string, Dictionary<char, char>> ContentTypeToBracePairs = new Dictionary<string, Dictionary<char, char>>();
        Dictionary<string, Dictionary<char, char>> _rawContentTypeAndBracePairs = null;
        Dictionary<string, Dictionary<char, char>> RawContentTypeAndBracePairs
        {
            get
            {
                if (_rawContentTypeAndBracePairs == null)
                {
                    _rawContentTypeAndBracePairs = new Dictionary<string, Dictionary<char, char>>();
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
                            _rawContentTypeAndBracePairs[contentType] = bracePairs;
                        }
                    }
                }
                return _rawContentTypeAndBracePairs;
            }
        }

        public CommandState GetCommandState(TypeCharCommandArgs args) => CommandState.Unspecified;

        public bool ExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext)
        {
            var primarySelection = args.TextView.Selection;
            if (primarySelection.IsEmpty)
                return false;

            var multiSelectionBroker = args.TextView.GetMultiSelectionBroker();
            // If user typed opening brace, find matching closing brace. Otherwise, don't act.
            var bracePairs = GetBracePairs(args.SubjectBuffer.ContentType);
            string opening = default(string);
            string closing = default(string);
            if (bracePairs?.ContainsKey(args.TypedChar) == true)
            {
                // We act only when user typed opening brace
                // For implmementation that works with either brace, see tag v0.2
                opening = args.TypedChar.ToString();
                closing = bracePairs[args.TypedChar].ToString();
            }
            else
            {
                return false;
            }

            // Add braces
            var edit = args.TextView.TextBuffer.CreateEdit();
            multiSelectionBroker.PerformActionOnAllSelections((transformer) =>
            {
                var selection = transformer.Selection;
                edit.Insert(selection.Start.Position, opening);
                edit.Insert(selection.End.Position, closing);
            });
            edit.Apply();

            // Expand the selection so that the operation can be repeated
            var updatedSnapshot = multiSelectionBroker.CurrentSnapshot;
            multiSelectionBroker.PerformActionOnAllSelections((transformer) =>
            {
                VirtualSnapshotPoint newAnchorPoint;
                VirtualSnapshotPoint newActivePoint;
                if (transformer.Selection.IsReversed)
                {
                    // Selection does not expand left when adding characters at its beginning. We need to explicitly expand it.
                    newActivePoint = new VirtualSnapshotPoint(updatedSnapshot, transformer.Selection.Start.Position - 1);
                    newAnchorPoint = new VirtualSnapshotPoint(updatedSnapshot, transformer.Selection.End.Position);
                }
                else
                {
                    // Selection does not expand left when adding characters at its beginning. We need to explicitly expand it.
                    newAnchorPoint = new VirtualSnapshotPoint(updatedSnapshot, transformer.Selection.Start.Position - 1);
                    newActivePoint = new VirtualSnapshotPoint(updatedSnapshot, transformer.Selection.End.Position);
                }
                transformer.MoveTo(newAnchorPoint, newActivePoint, transformer.Selection.InsertionPoint, PositionAffinity.Predecessor);
            });

            // Mark command as handled, so that the editor doesn't type and replace the selection
            return true;
        }

        private Dictionary<char, char> GetBracePairs(IContentType contentType)
        {
            if (!ContentTypeToBracePairs.ContainsKey(contentType.TypeName))
            {
                var applicableContentTypes = RawContentTypeAndBracePairs.Keys.Where(ct => contentType.IsOfType(ct));
                if (!applicableContentTypes.Any())
                {
                    ContentTypeToBracePairs[contentType.TypeName] = null;
                    return null;
                }

                var map = new Dictionary<char, char>();
                foreach (var applicableContentType in applicableContentTypes)
                {
                    var relevant = RawContentTypeAndBracePairs[applicableContentType];
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
