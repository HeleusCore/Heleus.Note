using System.Linq;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.NoteService;
using Heleus.Transactions;

namespace Heleus.Apps.Note.Page
{
    public class NoteInfoPage : StackPage
    {
        readonly DecryptedDataRecordState _initalState;
        readonly NoteView _noteView;
        readonly SecretKeyView _keyView;

        readonly ButtonRow _download;
        readonly ButtonRow _import;

        public NoteInfoPage(NoteData noteData) : base("NoteInfoPage")
        {
            Subscribe<NewSecretKeyEvent>(NewSecretKey);

            AddTitleRow("Title");

            _initalState = noteData.DecryptetState;

            AddHeaderRow("Note");
            _noteView = new NoteView(noteData);
            AddViewRow(_noteView);

            var download = noteData.EncryptedRecord == null;
            var import = noteData.DecryptetState != DecryptedDataRecordState.Decrypted;

            if (download || import)
            {
                if (download)
                    _download = AddButtonRow("Download", Download);
                if (import)
                    _import = AddButtonRow("Import", Import);
            }

            AddFooterRow();

            AddHeaderRow("SecretKeyInfo");
            _keyView = new SecretKeyView(noteData.EncryptedRecord?.KeyInfo, true);
            AddViewRow(_keyView);
            AddFooterRow();

            AddHeaderRow("TransactionInfo");
            AddViewRow(new DataTransactionView(noteData.TransactionDownload.Transaction));

            AddFooterRow();
        }

        async Task NewSecretKey(NewSecretKeyEvent arg)
        {
            await UpdateNoteData();
        }

        async Task Import(ButtonRow arg)
        {
            var noteData = _noteView.NoteData;
            var serviceNode = noteData.ServiceNode;
            var transaction = noteData.TransactionDownload.Transaction as DataTransaction;

            var account = serviceNode.GetSubmitAccount<SubmitAccount>(transaction.SignKeyIndex, NoteServiceInfo.NoteIndex);
            if (account == null)
                account = serviceNode.GetSubmitAccounts<SubmitAccount>().FirstOrDefault();

            if (account != null)
                await Navigation.PushAsync(new SecretKeysPage(account));
        }

        async Task Download(ButtonRow arg)
        {
            await UpdateNoteData();
        }

        async Task UpdateNoteData()
        {
            var noteData = _noteView.NoteData;
            if (noteData.DecryptetState != DecryptedDataRecordState.Decrypted)
            {
                IsBusy = true;
                await _noteView.UpdateNoteData();
                if (noteData.EncryptedRecord != null)
                    _keyView.Update(noteData.EncryptedRecord.KeyInfo);
                IsBusy = false;
            }

            if (_download != null)
                _download.IsEnabled = noteData.EncryptedRecord == null;
            if (_import != null)
                _import.IsEnabled = noteData.DecryptetState != DecryptedDataRecordState.Decrypted;

            if (_initalState != noteData.DecryptetState)
                await UIApp.PubSub.PublishAsync(new NoteDataChangedEvent(noteData));

        }

        public override async Task InitAsync()
        {
            await UpdateNoteData();
        }
    }
}
