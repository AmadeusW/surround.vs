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
        //[Import]
        //IBraceCompletionSessionProvider braceCompletionProvider;
        
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

            // Preserve the selection
            var growingSelectionStart = selection.Start.Position.Snapshot.CreateTrackingPoint(selection.Start.Position.Position, PointTrackingMode.Negative);
            var growingSelectionEnd = selection.Start.Position.Snapshot.CreateTrackingPoint(selection.End.Position.Position, PointTrackingMode.Positive);
            var selectionReversedProperty = selection.IsReversed;

            var characterPair = TypedCharToStarAndEndChar[args.TypedChar];
            var edit = args.TextView.TextBuffer.CreateEdit();
            edit.Insert(selection.Start.Position, characterPair.opening);
            edit.Insert(selection.End.Position, characterPair.closing);
            edit.Apply();

            // Restore selection
            var newSnapshot = selection.Start.Position.Snapshot;
            selection.Select(new SnapshotSpan(growingSelectionStart.GetPoint(newSnapshot), growingSelectionEnd.GetPoint(newSnapshot)), selectionReversedProperty);
            return true; // we don't want to type and replace the selection
        }

        public CommandState GetCommandState(TypeCharCommandArgs args) => CommandState.Unspecified;
    }
}
