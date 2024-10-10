namespace Media_App;

using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

class ImageConverter { 
    private async Task<StorageFile> ConvertImageToJpegAsync(StorageFile sourceFile, StorageFile outputFile)
    {
        //you can use WinRTXamlToolkit StorageItemExtensions.GetSizeAsync to get file size (if you already plugged this nuget in)
        var sourceFileProperties = await sourceFile.GetBasicPropertiesAsync();
        var fileSize = sourceFileProperties.Size;
        var imageStream = await sourceFile.OpenReadAsync();
    
        using (imageStream)
        {
            var decoder = await BitmapDecoder.CreateAsync(imageStream);
            var pixelData = await decoder.GetPixelDataAsync();
            var detachedPixelData = pixelData.DetachPixelData();
            pixelData = null;
            double jpegImageQuality = 0.79;
            var imageWriteableStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite);
            ulong jpegImageSize = 0;
            using (imageWriteableStream)
            {
                var propertySet = new BitmapPropertySet();
                var qualityValue = new BitmapTypedValue(jpegImageQuality, Windows.Foundation.PropertyType.Single);
                propertySet.Add("ImageQuality", qualityValue);
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, imageWriteableStream, propertySet);
                //key thing here is to use decoder.OrientedPixelWidth and decoder.OrientedPixelHeight otherwise you will get garbled image on devices on some photos with orientation in metadata
                encoder.SetPixelData(decoder.BitmapPixelFormat, decoder.BitmapAlphaMode, decoder.OrientedPixelWidth, decoder.OrientedPixelHeight, decoder.DpiX, decoder.DpiY, detachedPixelData);
                await encoder.FlushAsync();
                await imageWriteableStream.FlushAsync();
                jpegImageSize = imageWriteableStream.Size;
            }
        }
        return outputFile;
    }
    public static async Task<StorageFile> MakeJpeg (ImageStream strm, StorageFile outFile)
    {
        using (strm)
        {
            var decoder = await BitmapDecoder.CreateAsync(strm);

            var pixelData = await decoder.GetPixelDataAsync();
            var detachedPixelData = pixelData.DetachPixelData();
            pixelData = null;
            double jpegImageQuality = 0.79;
            var imageWriteableStream = await outFile.OpenAsync(FileAccessMode.ReadWrite);
            ulong jpegImageSize = 0;
            using (imageWriteableStream)
            {
                var propertySet = new BitmapPropertySet();
                var qualityValue = new BitmapTypedValue(jpegImageQuality, Windows.Foundation.PropertyType.Single);
                propertySet.Add("ImageQuality", qualityValue);
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, imageWriteableStream, propertySet);
                //key thing here is to use decoder.OrientedPixelWidth and decoder.OrientedPixelHeight otherwise you will get garbled image on devices on some photos with orientation in metadata
                encoder.SetPixelData(decoder.BitmapPixelFormat, decoder.BitmapAlphaMode, decoder.OrientedPixelWidth, decoder.OrientedPixelHeight, decoder.DpiX, decoder.DpiY, detachedPixelData);
                await encoder.FlushAsync();
                await imageWriteableStream.FlushAsync();
                jpegImageSize = imageWriteableStream.Size;
            }
        }
        return outFile;
    }
}