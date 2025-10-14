using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace raskraski
{
    public partial class MainWindow : Window
    {
        private CategoryView _categoryView;

        public MainWindow()
        {
            InitializeComponent();

            this.Cursor = CursorHelper.FromResource("pack://application:,,,/raskraski;component/Assets/cursor.png",
                ConfigManager.LoadConfig().CursorHotSpotX, ConfigManager.LoadConfig().CursorHotSpotY, ConfigManager.LoadConfig().CursorScale);

            _categoryView = new CategoryView(this);
            MainContent.Content = _categoryView;
        }

        public void ShowCategoryImages(string categoryPath)
        {
            MainContent.Content = new ImageView(this, categoryPath);
        }

        public void BackToCategories()
        {
            MainContent.Content = _categoryView;
        }
    }
}
