using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
namespace Hung.AI.Editor
{
    public sealed class AINoteView : StickyNote
    {
        public AINoteView(AIGraphNote note, Action<AINoteView> onChanged) : base(note.Position.position)
        {
            NoteId = note.Id;
            title = note.Title;
            contents = note.Contents;
            SetPosition(note.Position);
            AddToClassList("ai-note");
            RegisterCallback<StickyNoteChangeEvent>(_ => onChanged?.Invoke(this));
        }

        public string NoteId { get; }
    }
}
