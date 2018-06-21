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

            // Preserve the selection
            var growingSelectionStart = selection.Start.Position.Snapshot.CreateTrackingPoint(selection.Start.Position.Position, PointTrackingMode.Negative);
            var growingSelectionEnd = selection.Start.Position.Snapshot.CreateTrackingPoint(selection.End.Position.Position, PointTrackingMode.Positive);
            var selectionReversed = selection.IsReversed;

            var characterPair = TypedCharToStarAndEndChar[args.TypedChar];

            // Allow user to undo the matching character.
            // To do this, insert character at caret location first, then insert matching character
            ITextEdit edit;
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
