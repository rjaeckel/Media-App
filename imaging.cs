using System.Threading.Tasks;
using System;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;
using System.IO;

namespace Media_App;

internal class Imaging
{
    public static int ThumbnailWidth = 0;
    public static async Task<BitmapImage> Read_Image(
        StorageFile file, short position = 5, short max_frames = 15 )
    {
        var mediaClip = await MediaClip.CreateFromFileAsync(file);
        var mediaComposition = new MediaComposition();
        mediaComposition.OverlayLayers.Clear();
        mediaComposition.Clips.Add(mediaClip);

        var Time_Step = (int)(mediaComposition.Duration.TotalSeconds / max_frames);

        
        //var thumbnail = await GetThumbnailAsync(file);
        ImageStream thumbnail = await mediaComposition.GetThumbnailAsync(
            TimeSpan.FromSeconds(position % (max_frames+1) * Time_Step +1 ),
            ThumbnailWidth, 0, VideoFramePrecision.NearestFrame );
        //return thumbnail;*/
        InMemoryRandomAccessStream randomAccessStream = new();
        await RandomAccessStream.CopyAsync(thumbnail, randomAccessStream);
        randomAccessStream.Seek(0);
        
        //var thumb = await ImageCache.StoreJpegThumbnail(randomAccessStream, file.Path, position);
        
        BitmapImage bitmapImage = new();
        bitmapImage.SetSource(randomAccessStream);
        //bitmapImage.SetSource(thumb.Path);
        return bitmapImage;
        
    }
    public static Image Create_Thumbnail_Element(StorageFile file)
    {
        var elem = new Image()
        {
            Tag = file,
            Margin = new Thickness(1)
        };

        var tooltip = new ToolTip()
        {
            Content = file.Name.Replace(".mp4", "").Replace("_", " "),
            // should be set in styling setup, not here
            Margin = new Thickness(15),
            BorderThickness = new Thickness(0),
            Opacity = .7,
        };
        ToolTipService.SetToolTip(elem, tooltip);

        return elem;
    }
    /*
    public static byte[] ConvertBitmapImageToJpeg(BitmapImage bitmapImage)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            encoder.Save(memoryStream);
            return memoryStream.ToArray();
        }
    }

    public static byte[] ConvertBitmapImageToPng(BitmapImage bitmapImage)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            encoder.Save(memoryStream);
            return memoryStream.ToArray();
        }
    }*/
}

/**/
// allows use of BitmapImages
[Windows.Foundation.Metadata.ContractVersion(typeof(Windows.Foundation.UniversalApiContract), 65536)]
[Windows.Foundation.Metadata.MarshalingBehavior(Windows.Foundation.Metadata.MarshalingType.Agile)]
[Windows.Foundation.Metadata.Threading(Windows.Foundation.Metadata.ThreadingModel.Both)]
[Windows.Foundation.Metadata.WebHostHidden]
[Windows.Foundation.Metadata.Activatable(65536, "Windows.Foundation.UniversalApiContract")]
//[Windows.Foundation.Metadata.Static(typeof(Windows.UI.Xaml.Media.Imaging.IBitmapImageStatics2), 65536, "Windows.Foundation.UniversalApiContract")]
//[Windows.Foundation.Metadata.Static(typeof(Windows.UI.Xaml.Media.Imaging.IBitmapImageStatics), 65536, "Windows.Foundation.UniversalApiContract")]
//[Windows.Foundation.Metadata.Static(typeof(Windows.UI.Xaml.Media.Imaging.IBitmapImageStatics3), 196608, "Windows.Foundation.UniversalApiContract")]
//[Windows.Foundation.Metadata.Activatable(typeof(Windows.UI.Xaml.Media.Imaging.IBitmapImageFactory), 65536, "Windows.Foundation.UniversalApiContract")]
public sealed class BitmapImage : BitmapSource {
}
/**/