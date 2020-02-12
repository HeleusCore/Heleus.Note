using Heleus.Network.Client;

namespace Heleus.Apps.Shared
{
    public class NoteUploadedEvent
    {
        public readonly HeleusClientResponse Response;

        public NoteUploadedEvent(HeleusClientResponse response)
        {
            Response = response;
        }
    }
}
