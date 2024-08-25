using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.Media.Editing;
using Windows.System.Threading;
using Windows.Storage.Search;
using System.Collections.Concurrent;
using Windows.Media.Core;
using System.Windows.Input;
using Windows.System;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.WindowManagement;
using Windows.UI.ViewManagement;
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
        private PreviewTimer Preview_Timer = new PreviewTimer();
        private double scroll_position = 0;

        private int num_cpus = Environment.ProcessorCount;
        // hackish way to handle memory problems and speed, need some Queue 
        private RenderTimer Render_Timer = new RenderTimer();
        private DispatcherTimer cursor_timer = new DispatcherTimer();

        public PreviewImage()
        {
            this.InitializeComponent();
            ThePlayer.Visibility = Visibility.Collapsed;
            ThePlayer.RegisterPropertyChangedCallback(MediaPlayerElement.SourceProperty, OnMPESourceChanged);
            ThePlayer.RegisterPropertyChangedCallback(MediaPlayerElement.IsFullWindowProperty, OnMPEFullscreenChanged);

            query_videos();

            cursor_timer.Interval = TimeSpan.FromSeconds(3);
            cursor_timer.Tick += hide_mouse;
            Window.Current.CoreWindow.PointerMoved += mouse_moved;
        }

        async private void OnMPESourceChanged (DependencyObject sender, DependencyProperty dp) {
            if(ThePlayer.Source != null)
            {
                scroll_position = grid_scroll.VerticalOffset;
                ThePlayer.IsFullWindow = true;
                ThePlayer.MediaPlayer.IsLoopingEnabled = true;
                ThePlayer.Visibility = Visibility.Visible;
                cursor_timer.Start();
                ThePlayer.MediaPlayer.Play();
                ThePlayer.MediaPlayer.PlaybackSession.MediaPlayer.Volume = .1;
            }
        }
        async private void OnMPEFullscreenChanged(DependencyObject sender, DependencyProperty dp)
        {
            if(!ThePlayer.IsFullWindow) {
                ThePlayer.Source = null;
                ThePlayer.Visibility = Visibility.Collapsed;
                grid_scroll.ChangeView(null,scroll_position,null,false);
                cursor_timer.Stop();
            }
        }
        async private void Preview_click(object sender, RoutedEventArgs e)
        {
            ThePlayer.Source = MediaSource.CreateFromStorageFile((StorageFile)((Image)sender).Tag);
        }

        async private void query_videos()
        {
            QueryOptions queryOption = new QueryOptions(
                CommonFileQuery.OrderByDate, new string[] { ".mp4" /*, ".wma"*/ });
            //queryOption.FolderDepth = FolderDepth.Deep;
            Queue<IStorageFolder> folders = new Queue<IStorageFolder>();

            var files = await KnownFolders.VideosLibrary.CreateFileQueryWithOptions(queryOption).GetFilesAsync();

            //int max = 30, c = 0; // memory limit
            
            foreach (var file in files)
            {
                //if (max < ++c) break;
                var elem = new Image() {
                    Tag = file
                };
                var tooltip = new ToolTip() {
                    Content = file.Name.Replace(".mp4", "").Replace("_", " "),
                    Margin = new Thickness(15),
                    BorderThickness = new Thickness(0),
                    Opacity = .7,
                };
                ToolTipService.SetToolTip(elem, tooltip);
                elem.PointerEntered += preview_hover;
                elem.PointerExited += preview_out;
                elem.Tapped += Preview_click;
                TheGrid.Children.Add(elem);
                // memory error
                //IAsyncAction asyncAction = ThreadPool.RunAsync(async (workItem) => {
                //elem.Source = await readImage(file);
                //});
                Render_Timer.queue.Add(elem);
                //Render_Timer.Start();
            }
            Render_Timer.work();

            ApplicationView.GetForCurrentView().Title = $"Media App :: {TheGrid.Children.Count}";
            
        }

        private void grid_sizeChanged(object sender, SizeChangedEventArgs e)
        {
            // keep items Fitting the whole space
            int items = (int)Math.Floor(e.NewSize.Width / 300); // minimum item width
            TheGrid.ItemWidth = Math.Floor(e.NewSize.Width / items);
            TheGrid.ItemHeight = TheGrid.ItemWidth / 16 * 9;
        }

        public static async Task<BitmapImage> readImage(StorageFile file,int position=1,int max_frames=15) {
            //var thumbnail = await GetThumbnailAsync(file);
            var mediaClip = await MediaClip.CreateFromFileAsync(file);
            var mediaComposition = new MediaComposition();
            mediaComposition.OverlayLayers.Clear();
            mediaComposition.Clips.Add(mediaClip);

            var frame = (int) (mediaComposition.Duration.TotalSeconds/max_frames);

            var thumbnail = await mediaComposition.GetThumbnailAsync(
                TimeSpan.FromSeconds(position%max_frames*frame+1), 256, 0, VideoFramePrecision.NearestFrame);
            var bitmapImage = new BitmapImage();
            var randomAccessStream = new InMemoryRandomAccessStream();
            await RandomAccessStream.CopyAsync(thumbnail, randomAccessStream);
            randomAccessStream.Seek(0);
            bitmapImage.SetSource(randomAccessStream);
            return bitmapImage;
        }

        internal class FileExtensions
        {
            public static readonly string[] Video = new string[] { ".mp4", ".wmv", ".flv", ".mkv" };
        }

        internal class RenderTimer
        {
            public List<Image> queue = new List<Image>();
            
            async public Task work_item()
            {
                if (queue.Count > 0) {
                    var img = queue[0];
                    queue.Remove(img);
                    await first_preview(img);
                }
            }
            async public Task work()
            {
                while (queue.Count > 0) await work_item();
            }
            async public Task work_parallel()
            {
                int i = 0; // uses 2 gpu threads
                do work(); while (++i < 2);
            }
            async public Task first_preview(Image img)
            {
                img.Source = await readImage((StorageFile)img.Tag);
            }
        }
        
        internal class PreviewTimer {
            public Image target;
            public int offset = 1;
            protected DispatcherTimer timer;
            private void action(object sender, object e)
            {
                if(target != null) {
                    update_image();
                }
                 
            }
            async private Task update_image()
            {
                target.Source = await readImage((StorageFile)target.Tag, offset++);
            }
            public PreviewTimer (int int_ms=450)
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(int_ms);
                timer.Tick += action;
                timer.Start();
            }
        }
        
        private void preview_hover(object sender, PointerRoutedEventArgs e)
        {
            Preview_Timer.target = (Image)sender;
            //Preview_Timer.offset = 0;
        }

        private void preview_out(object sender, PointerRoutedEventArgs e)
        {
            Preview_Timer.target = null;
            Preview_Timer.offset = 0;
        }
        
        // allows use of BitmapImages
        [Windows.Foundation.Metadata.ContractVersion(typeof(Windows.Foundation.UniversalApiContract), 65536)]
        [Windows.Foundation.Metadata.MarshalingBehavior(Windows.Foundation.Metadata.MarshalingType.Agile)]
        [Windows.Foundation.Metadata.Threading(Windows.Foundation.Metadata.ThreadingModel.Both)]
        [Windows.Foundation.Metadata.WebHostHidden]
        //[Windows.Foundation.Metadata.Activatable(typeof(Windows.UI.Xaml.Media.Imaging.IBitmapImageFactory), 65536, "Windows.Foundation.UniversalApiContract")]
        [Windows.Foundation.Metadata.Activatable(65536, "Windows.Foundation.UniversalApiContract")]
        //[Windows.Foundation.Metadata.Static(typeof(Windows.UI.Xaml.Media.Imaging.IBitmapImageStatics2), 65536, "Windows.Foundation.UniversalApiContract")]
        //[Windows.Foundation.Metadata.Static(typeof(Windows.UI.Xaml.Media.Imaging.IBitmapImageStatics), 65536, "Windows.Foundation.UniversalApiContract")]
        //[Windows.Foundation.Metadata.Static(typeof(Windows.UI.Xaml.Media.Imaging.IBitmapImageStatics3), 196608, "Windows.Foundation.UniversalApiContract")]
        public sealed class BitmapImage : BitmapSource { }

        private void player_keydown(object sender, KeyRoutedEventArgs e)
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
        /*
        private void player_tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ThePlayer.MediaPlayer.CurrentState == MediaPlayerState.Playing)
            {
                ThePlayer.MediaPlayer.Pause();
            }
            else ThePlayer.MediaPlayer.Play();
        }
        */
        private void player_doubletapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ThePlayer.IsFullWindow = false;
        }

        private void mouse_moved(object sender, object e)
        {
            if (Window.Current.CoreWindow.PointerCursor == null)
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            if (ThePlayer.IsFullWindow)
                cursor_timer.Start();
        }
        private void hide_mouse(object sender, object e)
        {
            Window.Current.CoreWindow.PointerCursor = null;
            cursor_timer.Stop();
        }
    }
}



