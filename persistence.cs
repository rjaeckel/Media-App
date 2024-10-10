using Windows.UI.Xaml;
using Windows.Storage;
using System;

using CV =  Windows.Storage.ApplicationDataCompositeValue;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Windows.Graphics.Imaging;


namespace Media_App;

class localStorage {
    protected static ApplicationDataContainer _settings = ApplicationData.Current.LocalSettings;
    protected static StorageFolder _storage = ApplicationData.Current.LocalFolder;
    protected static CV _c { get { return new CV(); } }
    protected static T _v<T>(string key) { return (T)_settings.Values[key]; }
    protected static CV _cv(string key) { return _v<CV>(key); }
    protected static void _s (string k, object v) {
        _settings.Values[k] = v;
    }
    private void Dummy ()
    {

        // Simple setting
        _settings.Values["exampleSetting"] = "Hello Windows";
        // Simple getting
        Object value = _settings.Values["exampleSetting"];
        // Composite setting
        var c = _c;
        c["intVal"] = 1;
        c["strVal"] = "string";
        _settings.Values["exampleCompositeSetting"] = c;
        // Composite getting
        var c_ = _cv("exampleCompositeSetting");
        if (c_ != null) {
            // Access data in c_["intVal"] and c_["strVal"]
        }
        // .. https://learn.microsoft.com/en-us/windows/apps/design/app-settings/store-and-retrieve-app-data#create-and-read-a-local-file
    }
}

class ImageCache : localStorage {
    public static async Task<StorageFile> StoreJpegThumbnail(ImageStream strm, string fName, short z=0) {
        var file = await _storage.CreateFileAsync(SHA256.Create(fName) + $".{z}.jpg");
        return await ImageConverter.MakeJpeg(strm, file);
    }
}

class Config : localStorage
{

    public static int ImagesPerLine { get; set; } = 6;
    // public static bool UseDicreteGraphics { get; set; } = true;
    public static CornerRadius ImageCorners { get; set; } = new (13);
    //public static short RenderThreads { get; set; } = 3;
    public static short PreviewImages { get; set; } = 15;
    public static Thickness ImageMargin { get; set; } = new (1);
    public static int PreviewDuration { get; set; } = 450;
    public static Thickness ImageBorders {get; set; } = new (0, 0, 3, 3);


}


