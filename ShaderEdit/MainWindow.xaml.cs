using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModernChrome;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShaderEdit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ShowCaptionIcon = true;
            ThemeManager.ChangeTheme(App.Current, "Blend");
            BorderBrush = Application.Current.FindResource("StatusBarPurpleBrushKey") as SolidColorBrush;
        }

        private void Menu_Save(object sender, RoutedEventArgs e)
        {
            ShaderEditor.SaveFile();
            D3DContext.UpdateShader();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
