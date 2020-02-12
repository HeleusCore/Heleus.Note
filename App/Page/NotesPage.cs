using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Network.Client;
using Heleus.Transactions;

namespace Heleus.Apps.Note.Page
{
    public class NotesPage : StackPage
    {
        public class NoteListView : StackListView<ButtonViewRow<NoteView>, TransactionDownloadData<Transaction>>
        {
            public NoteListView(StackPage page, HeaderRow header) : base(page, header)
            {
            }

            async Task Note(ButtonViewRow<NoteView> button)
            {
                await _page.Navigation.PushAsync(new NoteInfoPage(button.View.NoteData));
            }

            protected override ButtonViewRow<NoteView> AddRow(StackPage page, TransactionDownloadData<Transaction> item)
            {
                var noteData = NoteApp.Current.GetNoteData(item);
                var serviceNode = noteData.ServiceNode;

                var button = page.AddButtonViewRow(new NoteView(noteData), Note);
                button.RowLayout.SetAccentColor(serviceNode.AccentColor);

                return button;
            }

            protected override void UpdateRow(ButtonViewRow<NoteView> row, TransactionDownloadData<Transaction> newItem)
            {
                var noteData = NoteApp.Current.GetNoteData(newItem);
                var serviceNode = noteData.ServiceNode;

                row.View.Update(noteData);
                row.RowLayout.SetAccentColor(serviceNode.AccentColor);
            }
        }

        NoteListView _listView;
        ButtonRow _more;

        public NotesPage() : base("NotesPage")
        {
            Subscribe<ServiceAccountAuthorizedEvent>(AccountAuth);
            Subscribe<ServiceAccountImportEvent>(AccountImport);
            Subscribe<ServiceNodesLoadedEvent>(Loaded);

            Subscribe<NewSecretKeyEvent>(NewSecretKey);
            Subscribe<TransactionDownloadEvent<Transaction>>(DownloadTransactions);
            Subscribe<NoteUploadedEvent>(NoteUploaded);
            Subscribe<NoteDataChangedEvent>(NoteDataChanged);

            SetupPage();
        }

        Task Loaded(ServiceNodesLoadedEvent arg)
        {
            if (ServiceNodeManager.Current.HadUnlockedServiceNode)
            {
                IsBusy = true;
                UIApp.Run(() => NoteApp.Current.DownloadNoteData(false));
            }

            return Task.CompletedTask;
        }

        async Task Add(ButtonRow button)
        {
            await UIApp.Current.ShowPage(typeof(AddNotePage));
        }

        void SetupPage()
        {
            _more = null;
            _listView = null;

            StackLayout.Children.Clear();

            var title = AddTitleRow("Title");

            if (!ServiceNodeManager.Current.HadUnlockedServiceNode)
            {
                AddInfoRow("Info", Tr.Get("App.FullName"));
                AddSubmitRow("Add", Add);
            }
            else
            {
                if(!UIAppSettings.AppReady)
                {
                    AddInfoRow("Info", Tr.Get("App.FullName"));
                    AddSubmitRow("Add", Add);
                }

                _listView = new NoteListView(this, title);

                ToolbarItems.Add(new ExtToolbarItem(Tr.Get("Common.Refresh"), null, async () =>
                {
                    IsBusy = true;
                    await NoteApp.Current.DownloadNoteData(false);
                }));

                AddFooterRow();

                IsBusy = true;
            }
        }

        Task More(ButtonRow button)
        {
            IsBusy = true;
            UIApp.Run(() => NoteApp.Current.DownloadNoteData(true));

            return Task.CompletedTask;
        }

        Task DownloadTransactions(TransactionDownloadEvent<Transaction> downloadEvent)
        {
            AddIndexBefore = false;
            AddIndex = GetRow("Notes");
            var rows = GetHeaderSectionRows("Notes");

            IsBusy = false;

            foreach (var item in downloadEvent.Items)
            {
                if (!item.Result.Ok)
                    UIApp.Toast(Tr.Get("TransactionDownloadResult.DownloadFailed", item.ServiceNode.GetName()));
            }

            var transactions = downloadEvent.GetSortedTransactions(TransactionSortMode.TimestampDescening);
            if (transactions.Count > 0 && _listView != null)
            {
                RemoveView(GetRow("Info"));
                RemoveView(GetRow("Add"));

                if (!UIAppSettings.AppReady)
                {
                    UIAppSettings.AppReady = true;
                    UIApp.Current.SaveSettings();
                }

                _listView.Update(transactions);

                UIApp.Run(UpdateNotes);

                if (downloadEvent.HasMore)
                {
                    if (_more == null)
                        _more = AddButtonRow("MoreButton", More);
                }
                else
                {
                    RemoveView(_more);
                    _more = null;
                }
            }
            else
            {
                if (ServiceNodeManager.Current.HadUnlockedServiceNode && UIAppSettings.AppReady)
                {
                    Toast("NoNotes");
                }
            }

            return Task.CompletedTask;
        }

        async Task UpdateNotes()
        {
            if (_listView != null)
            {
                var rows = _listView.Rows;
                foreach (var row in rows)
                {
                    if (row is ButtonViewRow<NoteView> buttonViewRow)
                    {
                        var view = buttonViewRow.View;
                        if (view != null)
                            await view.UpdateNoteData();
                    }
                }
            }
        }

        Task AccountImport(ServiceAccountImportEvent arg)
        {
            SetupPage();

            return Task.CompletedTask;
        }

        Task AccountAuth(ServiceAccountAuthorizedEvent arg)
        {
            SetupPage();

            return Task.CompletedTask;
        }

        Task NewSecretKey(NewSecretKeyEvent arg)
        {
            UIApp.Run(UpdateNotes);

            return Task.CompletedTask;
        }

        Task NoteDataChanged(NoteDataChangedEvent arg)
        {
            UIApp.Run(UpdateNotes);

            return Task.CompletedTask;
        }

        Task NoteUploaded(NoteUploadedEvent uploadedEvent)
        {
            if(uploadedEvent.Response.TransactionResult == TransactionResultTypes.Ok)
            {
                IsBusy = true;
                UIApp.Run(() => NoteApp.Current.DownloadNoteData(false));
            }

            return Task.CompletedTask;
        }
    }
}
