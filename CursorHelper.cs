using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace raskraski
{
    public static class CursorHelper
    {
        public static Cursor FromResource(string resourcePath, int xHotspot, int yHotspot, double scale = 1.0)
        {
            // resourcePath, например: "pack://application:,,,/raskraski;component/Assets/Cursors/my_cursor.png"
            using (var stream = Application.GetResourceStream(new Uri(resourcePath)).Stream)
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();

                return FromBitmapSource(image, xHotspot, yHotspot, scale);
            }
        }

        private static Cursor FromBitmapSource(BitmapSource image, int xHotspot, int yHotspot, double scale = 1.0)
        {
            BitmapSource scaledImage = image;
            if (Math.Abs(scale - 1.0) > 0.0001)
            {
                var transform = new ScaleTransform(scale, scale);
                scaledImage = new TransformedBitmap(image, transform);
                scaledImage.Freeze();
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(scaledImage));

            using (var pngStream = new MemoryStream())
            {
                encoder.Save(pngStream);
                byte[] pngBytes = pngStream.ToArray();
                byte[] curBytes = BuildCurFile(pngBytes, xHotspot, yHotspot, scaledImage.PixelWidth, scaledImage.PixelHeight);
                return new Cursor(new MemoryStream(curBytes));
            }
        }

        // Формирование структуры CUR
        private static byte[] BuildCurFile(byte[] pngData, int xHotspot, int yHotspot, int width, int height)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // Header
                bw.Write((ushort)0);   // Reserved
                bw.Write((ushort)2);   // Type = cursor
                bw.Write((ushort)1);   // Image count = 1

                // Directory entry (16 bytes)
                bw.Write((byte)width);    // Width
                bw.Write((byte)height);   // Height
                bw.Write((byte)0);        // Color count
                bw.Write((byte)0);        // Reserved
                bw.Write((ushort)xHotspot);
                bw.Write((ushort)yHotspot);
                bw.Write(pngData.Length); // Bytes in image
                bw.Write(22);             // Offset (6 header + 16 entry)

                // PNG image
                bw.Write(pngData);

                return ms.ToArray();
            }
        }
    }
}
