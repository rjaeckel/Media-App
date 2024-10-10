using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.Storage.Search;
using Windows.Media.Core;
using Windows.System;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.Graphics.Imaging;
using Windows.ApplicationModel.Core;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Media_App
{

    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class PreviewImage : Page
    {
        // hover timer
        private readonly PreviewTimer Preview_Timer = new ();
        // timer for thumbs
        private readonly RenderTimer Render_Timer = new();
        // hide cursor in fs mode
        private readonly DispatcherTimer Hide_Cursor_Timer = new() { Interval = TimeSpan.FromSeconds(3) };
        
        private double scroll_position = 0;

        public PreviewImage()
        {
            this.InitializeComponent();
            /* */
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(AppTitleBar);
            /* */

            Load_Videos(PreviewProgress);
            
            ThePlayer.Visibility = Visibility.Collapsed;
            ThePlayer.RegisterPropertyChangedCallback(MediaPlayerElement.SourceProperty, OnMPESourceChanged);
            ThePlayer.RegisterPropertyChangedCallback(MediaPlayerElement.IsFullWindowProperty, OnMPEFullscreenChanged);

            Hide_Cursor_Timer.Stop();
            Hide_Cursor_Timer.Tick += Hide_MouseCursor;
            Window.Current.CoreWindow.PointerMoved += Mouse_Moved;
        }

        private void OnMPESourceChanged (DependencyObject sender, DependencyProperty dp) {
            if(ThePlayer.Source != null)
            {
                //scroll_position = grid_scroll.VerticalOffset;
                ThePlayer.IsFullWindow = true;
                ThePlayer.MediaPlayer.IsLoopingEnabled = true;
                ThePlayer.Visibility = Visibility.Visible;
                Hide_Cursor_Timer.Start();
                ThePlayer.MediaPlayer.Play();
                ThePlayer.MediaPlayer.PlaybackSession.MediaPlayer.Volume = .1;
            }
        }
        private void OnMPEFullscreenChanged(DependencyObject sender, DependencyProperty dp)
        {
            if(!ThePlayer.IsFullWindow) {
                ThePlayer.Source = null;
                ThePlayer.Visibility = Visibility.Collapsed;
                //grid_scroll.ChangeView(null,scroll_position,null,false);
                Hide_Cursor_Timer.Stop();
            }
        }
        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            ThePlayer.Source = MediaSource.CreateFromStorageFile((StorageFile)((Image)sender).Tag);
        }

        async private void Load_Videos(ProgressBar p)
        {
            //var start = DateTime.UtcNow;
            QueryOptions queryOption = new(CommonFileQuery.OrderByDate, FileExtensions.Video) { FolderDepth = FolderDepth.Deep };
            var files = await KnownFolders.VideosLibrary.CreateFileQueryWithOptions(queryOption).GetFilesAsync();
            p.Maximum = files.Count;
            Render_Timer.p = p;

            //var read = DateTime.UtcNow - start;
            foreach (var file in files)
            {
                var elem = Imaging.Create_Thumbnail_Element(file);

                elem.PointerEntered += Preview_Hover;
                elem.PointerExited += Preview_Out;
                elem.Tapped += Preview_Click;
                
                Border brdr = new()
                {
                    CornerRadius = new CornerRadius(19),
                    Margin = new Thickness(0, 0, 3, 3),
                    Child = elem
                };
                TheGrid.Children.Add(brdr);
                Render_Timer.queue.Enqueue(elem);
            }
            //var list = DateTime.UtcNow - start;
            Render_Timer.Work_parallel();
            
            ApplicationView.GetForCurrentView().Title = $"{TheGrid.Children.Count} Videos"; // ({start} {read} {list})";
        }

        private void Grid_Size_Changed(object sender, SizeChangedEventArgs e)
        {
            // keep items Fitting the whole space
            int items = (int)Math.Floor(e.NewSize.Width / 300); // minimum item width
            TheGrid.ItemWidth = Math.Floor(e.NewSize.Width / items);
            TheGrid.ItemHeight = TheGrid.ItemWidth / 16 * 9;
        }
        
        private void Preview_Hover(object sender, PointerRoutedEventArgs e)
        {
            Preview_Timer.target = (Image)sender;
            //Preview_Timer.offset = 0;
        }

        private void Preview_Out(object sender, PointerRoutedEventArgs e)
        {
            Preview_Timer.target = null;
            Preview_Timer.offset = 0;
        }

        private void Player_Keydown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key== VirtualKey.Escape)
            {
                ThePlayer.IsFullWindow = false;
            }
            if (e.Key == VirtualKey.Space)
            {
                if (ThePlayer.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                    ThePlayer.MediaPlayer.Pause();
                else ThePlayer.MediaPlayer.Play();
            }
        }
        private void Player_Doubletapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ThePlayer.IsFullWindow = false;
        }

        private void Mouse_Moved(object sender, object e)
        {
            if (Window.Current.CoreWindow.PointerCursor == null)
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            if (ThePlayer.IsFullWindow)
                Hide_Cursor_Timer.Start();
        }
        private void Hide_MouseCursor(object sender, object e)
        {
            Window.Current.CoreWindow.PointerCursor = null;
            Hide_Cursor_Timer.Stop();
        }

        internal class FileExtensions
        {
            public static readonly string[] Video = [".mp4", ".wmv", ".flv", ".mkv"];
        }

    }

}



