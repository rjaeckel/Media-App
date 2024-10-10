using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Media_App;

internal class RenderTimer
{
    public Queue<Image> queue = new();

    public int max_threads = 3; // Environment.ProcessorCount / 2; //might crash app
    public ProgressBar p;
    protected bool IsWorking = false;
    async public Task Work_Item()
    {
        if (queue.Count > 0)
        {
            Image img;
            lock (queue) { img = queue.Dequeue(); }
            await FirstPreview(img);
        }
    }
    async public Task Work()
    {
        while (queue.Count > 0) await Work_Item();
        IsWorking = false;
        p.Visibility = Visibility.Collapsed;

    }
    public void Work_parallel()
    {
        if (IsWorking) return;
        int threads = 0;
        IsWorking = true;
        do _ = Work(); while (++threads < max_threads);
    }
    async public Task FirstPreview(Image img)
    {
        var _sf = (StorageFile)img.Tag;
        //var _i = await Imaging.Read_Image(_sf);
        //var _x = await ImageCache.StoreJpegThumbnail(_i,_sf.Path,0);
        //img.Source = new BitmapImage(new Uri(_x.Path,UriKind.Absolute));
        // img.Source.SetValue( _x.Path);
        
        //img.Source = new BitmapImage();
        img.Source = await Imaging.Read_Image(_sf);
        if (null != p) {
            (new Task(() => p.Value += 1)).RunSynchronously();
        }
    }

}

internal class PreviewTimer
{
    public Image target;
    public short offset = 1;
    protected DispatcherTimer timer;
    private void Action(object sender, object e)
    {
        if (target != null) _ = Update_Image();
    }
    async private Task Update_Image()
    {
        target.Source = await Imaging.Read_Image((StorageFile)target.Tag, offset++);
    }
    public PreviewTimer(int int_ms = 450)
    {
        DispatcherTimer timer = new() {
            Interval = TimeSpan.FromMilliseconds(int_ms),
        };
        timer.Tick += Action;
        timer.Start();
    }
}
internal class Metrics_Timer
{
    private const int Sample_Count = 8;
    private readonly Timer _timer = new((object s) => { ReadMetrics(); });
    private static TimeSpan _ProcessCpuTime = TimeSpan.FromSeconds(0);

    //private static Queue<double> gpuSamples = new();
    private static Queue<double> cpuSamples = new();

    protected static void ReadMetrics()
    {

    }
    /*
    private static double Get_RecentCpuLoad() {
        var time_elapsed = ProcessDiagnosticInfo.GetForCurrentProcess().ProcessStartTime;

        ProcessDiagnosticInfo.GetForProcesses().Sum((e) =>
        {
            var report = e.CpuUsage.GetReport();
            return report.KernelTime.TotalMilliseconds + report.UserTime.TotalMilliseconds;
        });

        return (ApplicationCpuTime - _ProcessCpuTime).TotalMilliseconds / Environment.ProcessorCount;
    }
    */
}

internal class FileSystemRefresh_Timer {

}