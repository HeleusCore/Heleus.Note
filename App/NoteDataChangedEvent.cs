using Heleus.Apps.Shared;

namespace Heleus.Apps.Note.Page
{
    public class NoteDataChangedEvent
    {
        public readonly NoteData NoteData;

        public NoteDataChangedEvent(NoteData noteData)
        {
            NoteData = noteData;
        }
    }
}
