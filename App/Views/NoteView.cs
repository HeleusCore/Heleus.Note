using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Xamarin.Forms;

namespace Heleus.Apps.Note.Page
{
    public class NoteView : StackLayout
    {
        readonly ExtLabel _note;
        readonly ExtLabel _footer;

        public NoteData NoteData { get; private set; }

        static string _defaultText;

        public NoteView(NoteData noteData)
        {
            if (_defaultText == null)
                _defaultText = Tr.Get("NoteView.Default");

            _note = new ExtLabel { Text = null, FontStyle = Theme.RowFont, ColorStyle = Theme.TextColor, Margin = new Thickness(0, 0, 0, 5), InputTransparent = true };
            _footer = new ExtLabel { Text = null, FontStyle = Theme.DetailFont, ColorStyle = Theme.TextColor, InputTransparent = true };

            InputTransparent = true;
            Spacing = 0;
            SizeChanged += ViewSizeChanged;

            Children.Add(_note);
            Children.Add(_footer);

            Update(noteData);
        }

        public void Update(NoteData noteData)
        {
            NoteData = noteData;
            _footer.Text = Time.DateTimeString(noteData.TransactionDownload.Transaction.Timestamp);
            Update();
        }

        public async Task UpdateNoteData()
        {
            if (NoteData.DecryptetState != DecryptedDataRecordState.Decrypted)
                await NoteData.Decrypt();

            Update();
        }

        void Update()
        {
            if (NoteData.DecryptetState == DecryptedDataRecordState.Decrypted)
            {
                if (_note.Text != NoteData.NoteText)
                    _note.Text = NoteData.NoteText;
                if (_note.ColorStyle != Theme.TextColor)
                    _note.ColorStyle = Theme.TextColor;
            }
            else if (NoteData.DecryptetState == DecryptedDataRecordState.DecryptionError)
            {
                _note.ColorStyle = Theme.WarningColor;
                _note.Text = Tr.Get("NoteView.Error");
            }
            else if (NoteData.DecryptetState == DecryptedDataRecordState.SecretKeyMissing)
            {
                _note.ColorStyle = Theme.WarningColor;
                _note.Text = Tr.Get("NoteView.SecretMissing", NoteData.EncryptedRecord.KeyInfo.SecretId);
            }
            else if (NoteData.DecryptetState == DecryptedDataRecordState.DownloadFailed)
            {
                _note.ColorStyle = Theme.WarningColor;
                _note.Text = Tr.Get("NoteView.AttachementMissing");
            }
            else
            {
                if (_note.ColorStyle != Theme.TextColor)
                    _note.ColorStyle = Theme.TextColor;
                if (_note.Text != _defaultText)
                    _note.Text = _defaultText;
            }
        }

        void ViewSizeChanged(object sender, EventArgs e)
        {
            var w = (int)Width;
            if (Width <= 0)
                return;

            if (w != (int)_note.WidthRequest)
                _note.WidthRequest = w;
            if (w != (int)_footer.WidthRequest)
                _footer.WidthRequest = w;
        }
    }
}
