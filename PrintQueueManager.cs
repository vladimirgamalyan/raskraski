using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace raskraski
{
    public class PrintJob
    {
        //public int Id { get; set; }
        public string FilePath { get; set; } = "";

        //public ImageItem Item { get; set; } = null!;
    }

    public static class PrintQueueManager
    {
        private static readonly BlockingCollection<PrintJob> _queue = new();
        private static readonly Thread _workerThread;

        static PrintQueueManager()
        {
            _workerThread = new Thread(ProcessQueue)
            {
                IsBackground = true
            };
            _workerThread.Start();
        }

        public static void Enqueue(PrintJob job) => _queue.Add(job);

        private static void ProcessQueue()
        {
            foreach (var job in _queue.GetConsumingEnumerable())
            {
                PrintOnStaThread(job);
            }
        }

        private static void PrintOnStaThread(PrintJob job)
        {
            var t = new Thread(() =>
            {
                try
                {
                    if (!File.Exists(job.FilePath))
                    {
                        //Application.Current.Dispatcher.Invoke(() =>
                        //    MessageBox.Show($"Файл для печати не найден: {job.FilePath}"));
                        return;
                    }

                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(job.FilePath);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();

                    PrintDialog printDlg = new PrintDialog();

                    double pageWidth = printDlg.PrintableAreaWidth;
                    double pageHeight = printDlg.PrintableAreaHeight;

                    FixedDocument doc = new FixedDocument();
                    PageContent pageContent = new PageContent();
                    FixedPage page = new FixedPage
                    {
                        Width = pageWidth,
                        Height = pageHeight
                    };

                    var img = new System.Windows.Controls.Image
                    {
                        Source = bmp,
                        Stretch = Stretch.Uniform,
                        Width = pageWidth,
                        Height = pageHeight
                    };

                    page.Children.Add(img);
                    ((IAddChild)pageContent).AddChild(page);
                    doc.Pages.Add(pageContent);

                    var writer = System.Printing.PrintQueue.CreateXpsDocumentWriter(printDlg.PrintQueue);
                    writer.Write(doc);
                    Debug.WriteLine($"Напечатано изображение {job.FilePath}");

                }
                catch (Exception /*ex*/)
                {
                    //Application.Current.Dispatcher.Invoke(() =>
                    //    MessageBox.Show("Ошибка печати: " + ex.Message));
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
            t.Join(); // ждём завершения этого задания, чтобы очередь шла последовательно
        }
    }

}
