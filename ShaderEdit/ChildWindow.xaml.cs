﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ModernChrome;
using System.Diagnostics;

namespace ShaderEdit
{
    /// <summary>
    /// Interaction logic for ChildWindow.xaml
    /// </summary>
    public partial class ChildWindow : ModernWindow
    {
        public ChildWindow()
        {
            InitializeComponent();
            ShowCaptionIcon = true;
            ThemeManager.ChangeTheme(Application.Current, "Blend");
            BorderBrush = new SolidColorBrush(Colors.Blue);
            GlowBrush = new SolidColorBrush(Colors.Gold);
        }
    }
}
