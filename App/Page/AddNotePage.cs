using System;
using System.Text;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Transactions;

namespace Heleus.Apps.Note.Page
{
    public class AddNotePage : StackPage
    {
        SubmitAccountButtonRow _submitAccount;

        async Task Submit(ButtonRow button)
        {
            var editor = GetRow<EditorRow>("Text");
            var submit = GetRow<ButtonRow>("Submit");
            var text = editor?.Edit?.Text;

            if(!string.IsNullOrWhiteSpace(text))
            {
                if (await ConfirmAsync("ConfirmSubmit"))
                {
                    IsBusy = true;
                    submit.IsEnabled = false;
                    editor.Edit.IsEnabled = false;

                    UIApp.Run(() => NoteApp.Current.UploadNote(_submitAccount.SubmitAccount, text));
                }
            }
        }

        async Task NoteUploaded(NoteUploadedEvent uploadedEvent)
        {
            var editor = GetRow<EditorRow>("Text");
            var submit = GetRow<ButtonRow>("Submit");

            submit.IsEnabled = true;
            editor.Edit.IsEnabled = true;
            IsBusy = false;

            if (uploadedEvent.Response.TransactionResult == TransactionResultTypes.Ok)
            {
                await MessageAsync("Success");
                editor.Edit.Text = string.Empty;
            }
            else
            {
                await ErrorTextAsync(uploadedEvent.Response.GetErrorMessage());
            }
        }

        async Task SelectSubmitAccount(SubmitAccountButtonRow<SubmitAccount> arg)
        {
            await Navigation.PushAsync(new SubmitAccountsPage(ServiceNodeManager.Current.GetSubmitAccounts<SubmitAccount>(), (submitAccount) =>
            {
                _submitAccount.SubmitAccount = submitAccount;
                NoteApp.Current.SetLastUsedSubmitAccount(submitAccount);
            }));
        }

        void Edit_TextChanged(object sender, Xamarin.Forms.TextChangedEventArgs e)
        {
            var submit = GetRow<ButtonRow>("Submit");
            if(submit != null)
                submit.IsEnabled = !string.IsNullOrWhiteSpace(e.NewTextValue);
        }

        public AddNotePage() : base("AddNotePage")
        {
            Subscribe<ServiceAccountAuthorizedEvent>(AccountAuth);
            Subscribe<ServiceAccountImportEvent>(AccountImport);
            Subscribe<ServiceNodesLoadedEvent>(Loaded);
            Subscribe<NoteUploadedEvent>(NoteUploaded);

            SetupPage();
        }

        Task Loaded(ServiceNodesLoadedEvent arg)
        {
            if(_submitAccount != null && _submitAccount.SubmitAccount == null)
                _submitAccount.SubmitAccount = NoteApp.Current.GetLastUsedSubmitAccount<SubmitAccount>();

            return Task.CompletedTask;
        }

        void SetupPage()
        {
            AddTitleRow("Title");

            if (!ServiceNodeManager.Current.HadUnlockedServiceNode)
            {
                AddInfoRow("NotesPage.Info", Tr.Get("App.FullName"));

                ServiceNodesPage.AddAuthorizeSection(ServiceNodeManager.Current.NewDefaultServiceNode, this, false);
            }
            else
            {
                var editor = AddEditorRow("", "Text");
                editor.Edit.TextChanged += Edit_TextChanged;
                FocusElement = editor.Edit;


                var submit = AddSubmitRow("Submit", Submit, true);
                submit.IsEnabled = false;

                AddHeaderRow("Common.SubmitAccount");
                _submitAccount = AddRow(new SubmitAccountButtonRow(NoteApp.Current.GetLastUsedSubmitAccount<SubmitAccount>(), this, SelectSubmitAccount));
                AddInfoRow("Common.SubmitAccountInfo");
                AddFooterRow();
            }
        }

        void ClearSections()
        {
            var editor = GetRow<EditorRow>("Text");
            if (editor != null)
                editor.Edit.TextChanged -= Edit_TextChanged;

            StackLayout.Children.Clear();
        }

        Task AccountImport(ServiceAccountImportEvent arg)
        {
            ClearSections();
            SetupPage();

            return Task.CompletedTask;
        }

        Task AccountAuth(ServiceAccountAuthorizedEvent arg)
        {
            ClearSections();
            SetupPage();

            return Task.CompletedTask;
        }
    }
}
