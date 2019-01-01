using System;
using System.IO;
using System.Reflection;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.ComponentModel;

namespace ShaderEdit
{
    /// <summary>
    /// Interaction logic for HLSLEditor.xaml
    /// </summary>
    public partial class HLSLEditor : UserControl
    {
        public TextEditor Editor => CodeEditor;

        public string FileName;

        public HLSLEditor()
        {
            bool designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
            InitializeComponent();
            if (!designMode)
            {
                FileName = "pixelshader.fx";
                var resourcename = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(x => x.EndsWith("HLSL.xshd"));
                var reader = XmlReader.Create(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcename)));
                IHighlightingDefinition hlslsyntax = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                HighlightingManager.Instance.RegisterHighlighting("HLSL", new string[] { ".fx", ".fxh", ".hlsl" }, hlslsyntax);
                Editor.Background = Brushes.DarkSlateGray;
                Editor.SyntaxHighlighting = hlslsyntax;
                Editor.Loaded += (o, e) => { LoadFile(); };
            }
        }

        public void LoadFile()
        {
            Editor.Load(FileName);
        }

        public void SaveFile()
        {
            Editor.Save(FileName);
        }
    }
}
