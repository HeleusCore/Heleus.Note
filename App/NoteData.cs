using System;
using Heleus.Network.Client;
using Heleus.NoteService;
using Heleus.Transactions;

namespace Heleus.Apps.Shared
{
    public class NoteData : DecryptedRecordData<NoteRecord>
    {
        public string NoteText => Record?.Note;

        public NoteData(TransactionDownloadData<Transaction> transaction, ServiceNode serviceNode) : base(transaction, serviceNode, NoteServiceInfo.NoteIndex, NoteServiceInfo.NoteFileName, DecryptedRecordDataSource.Attachement)
        {
        }
    }
}
