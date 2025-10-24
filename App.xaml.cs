using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Input;

namespace raskraski
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string mutexName = "Global\\Raskraski"; // имя уникальное для системы

            bool isNewInstance;
            _mutex = new Mutex(true, mutexName, out isNewInstance);

            if (!isNewInstance)
            {
                // Уже запущен другой экземпляр
                //MessageBox.Show("Приложение уже запущено.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            EventManager.RegisterClassHandler(typeof(Window),
                Keyboard.PreviewKeyDownEvent,
                new KeyEventHandler(OnKeyDown));

            PrintedStore.Load();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
