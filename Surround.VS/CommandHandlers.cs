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

        // Brace completion stuff is useless:
        //[Import]
        //IBraceCompletionSessionProvider braceCompletionProvider;
        [Import]
        IMultiSelectionBroker MultiSelectionBroker;
        
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
            var primarySelection = args.TextView.Selection;
            if (primarySelection.IsEmpty)
                return false;
            if (!TypedCharToStarAndEndChar.ContainsKey(args.TypedChar))
                return false;

            var characterPair = TypedCharToStarAndEndChar[args.TypedChar];

            var edit = args.TextView.TextBuffer.CreateEdit();
            MultiSelectionBroker.PerformActionOnAllSelections((transformer) =>
            {
                var selection = transformer.Selection;

                // Perform the edit
                edit.Insert(selection.Start.Position, characterPair.opening);
                edit.Insert(selection.End.Position, characterPair.closing);

                
            });
            edit.Apply();
            var updatedSnapshot = MultiSelectionBroker.CurrentSnapshot;
            MultiSelectionBroker.PerformActionOnAllSelections((transformer) =>
            {
                VirtualSnapshotPoint newAnchorPoint;
                VirtualSnapshotPoint newActivePoint;
                if (transformer.Selection.IsReversed)
                {
                    newActivePoint = new VirtualSnapshotPoint(updatedSnapshot, transformer.Selection.Start.Position - 1);
                    newAnchorPoint = new VirtualSnapshotPoint(updatedSnapshot, transformer.Selection.End.Position + 1);
                }
                else
                {
                    // anchor preceeds active point
                    newAnchorPoint = new VirtualSnapshotPoint(updatedSnapshot, transformer.Selection.Start.Position - 1);
                    newActivePoint = new VirtualSnapshotPoint(updatedSnapshot, transformer.Selection.End.Position + 1);
                }
                transformer.MoveTo(newAnchorPoint, newActivePoint, transformer.Selection.InsertionPoint, PositionAffinity.Predecessor);
            });
            
            return true; // Mark the command as handled, so that we don't overwrite the selection
        }

        public CommandState GetCommandState(TypeCharCommandArgs args) => CommandState.Unspecified;
    }
}
