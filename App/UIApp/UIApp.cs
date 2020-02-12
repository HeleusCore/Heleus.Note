using System;
using Xamarin.Forms;
using Heleus.Base;
using SkiaSharp;
using Heleus.Apps.Note.Page;
using Heleus.NoteService;
using Heleus.ProfileService;
using Heleus.Network.Client;
#if !(GTK || CLI)
using SkiaSharp.Views.Forms;
#endif

namespace Heleus.Apps.Shared
{
    partial class UIApp : Application
	{
		public static void NewContentPage(ExtContentPage contentPage)
		{
			if (IsGTK)
				return;
#if ASSHOLE
            var background = new Label
            {
                BackgroundColor = Color.Red,
                FontSize = 20,
                TextColor = Color.White,
                Text = "DEBUG",
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center
            };

            AbsoluteLayout.SetLayoutFlags(background, AbsoluteLayoutFlags.WidthProportional);
            AbsoluteLayout.SetLayoutBounds(background, new Rectangle(0, IsIOS ? 100 : 0, 1, 30));

            background.InputTransparent = true;

            contentPage.RootLayout.Children.Insert(0, background);
#endif
			if (!(contentPage is UWPMenuPage || contentPage is DesktopMenuPage))
				contentPage.EnableSkiaBackground();
		}

		public static void UpdateBackgroundCanvas(SKCanvas canvas, int width, int height)
		{
			try
			{
#if !(GTK || CLI)
				var colors = new SKColor[] { Theme.PrimaryColor.Color.ToSKColor(), Theme.SecondaryColor.Color.ToSKColor() };
				var positions = new float[] { 0.0f, 1.0f };

				var gradient = SKShader.CreateLinearGradient(new SKPoint(0, height / 2), new SKPoint(width, height / 2), colors, positions, SKShaderTileMode.Mirror);
				var paint = new SKPaint { Shader = gradient, IsAntialias = true };

				canvas.DrawPaint(paint);
#endif
			}
			catch (Exception ex)
			{
				Log.IgnoreException(ex);
			}
		}

        public static bool UIAppUsesPushNotifications = false;

        void Init()
        {
            SchemeAction.SchemeParser = (host, segments) =>
            {
                var action = string.Empty;
                var startIndex = 0;

                if (host == "heleuscore.com" && segments[1] == "note/")
                {
                    if (segments[2] == "request/")
                    {
                        action = SchemeAction.GetString(segments, 3);
                        startIndex = 4;
                    }
                }

                return new Tuple<string, int>(action, startIndex);
            };

            var sem = new ServiceNodeManager(NoteServiceInfo.ChainId, NoteServiceInfo.EndPoint, NoteServiceInfo.Version, NoteServiceInfo.Name, _currentSettings, _currentSettings, PubSub, TransactionDownloadManagerFlags.DecrytpedTransactionStorage);
            NoteApp.Current.Init();
            _ = new ProfileManager(new ClientBase(sem.HasDebugEndPoint ? sem.DefaultEndPoint : ProfileServiceInfo.EndPoint, ProfileServiceInfo.ChainId), sem.CacheStorage, PubSub);

            if (IsAndroid || IsUWP || IsDesktop)
            {
                var masterDetail = new ExtMasterDetailPage();
                var navigation = new ExtNavigationPage(new NotesPage());
                MenuPage menu = null;

                if (IsAndroid)
                    menu = new AndroidMenuPage(masterDetail, navigation);
                else if (IsUWP)
                    menu = new UWPMenuPage(masterDetail, navigation);
                else if (IsDesktop)
                    menu = new DesktopMenuPage(masterDetail, navigation);

                menu.AddPage(typeof(NotesPage), "NotesPage.Title", Icons.StickyNote);
                menu.AddPage(typeof(AddNotePage), "AddNotePage.Title", Icons.Plus);
                menu.AddPage(typeof(SettingsPage), "SettingsPage.Title", Icons.Slider);

                masterDetail.Master = menu;
                masterDetail.Detail = navigation;

                MainPage = MainMasterDetailPage = masterDetail;
            }
            else if (IsIOS)
            {
                var tabbed = new ExtTabbedPage();

                tabbed.AddPage(typeof(NotesPage), "NotesPage.Title", "icons/stickynote.png");
                tabbed.AddPage(typeof(AddNotePage), "AddNotePage.Title", "icons/plus.png");
                tabbed.AddPage(typeof(SettingsPage), "SettingsPage.Title", "icons/sliders.png");

                MainPage = MainTabbedPage = tabbed;
            }
        }

        void Start()
        {

        }

        void Resume()
        {

        }

        void Sleep()
        {

        }

        void RestoreSettings(ChunkReader reader)
        {
			
        }

        void StoreSettings(ChunkWriter writer)
        {
			
        }

        public void Activated()
        {

        }

        public void Deactivated()
        {

        }
    }
}
