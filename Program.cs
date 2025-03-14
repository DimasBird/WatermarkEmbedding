using SkiaSharp;
using System.Diagnostics;


namespace Program
{
    public static class Program
    {
        public static void Main()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            string picture = Globals.input_image;
            string watermark = Globals.input_watermark;



            WaterMark.Embed(picture, watermark);

            string output_image = Globals.output_image;
            string output_watermark = Globals.output_watermark;
            WaterMark.Extract(output_image);

            WaterMark.CompareWatermarks(watermark, output_watermark);

            WaterMark.ComparePictures(picture, output_image);

            TimeSpan ts = stopWatch.Elapsed;

            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            Console.WriteLine("\nRunTime " + elapsedTime);
        }
    }
}