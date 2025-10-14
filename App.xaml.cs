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
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            EventManager.RegisterClassHandler(typeof(Window),
                Keyboard.PreviewKeyDownEvent,
                new KeyEventHandler(OnKeyDown));

            PrintedStore.Load();
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
