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
            RoutedCommand SaveCommand = new RoutedCommand();
            SaveCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            Editor.CommandBindings.Add(new CommandBinding(SaveCommand,(o,e)=> { SaveFile(); }));
            if (!designMode)
            {
                var resourcename = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(x => x.EndsWith("HLSL.xshd"));
                var reader = XmlReader.Create(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcename)));
                IHighlightingDefinition hlslsyntax = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                HighlightingManager.Instance.RegisterHighlighting("HLSL", new string[] { ".fx", ".fxh", ".hlsl" }, hlslsyntax);
                Editor.Background = new SolidColorBrush(Color.FromRgb(22,22,22));
                Editor.Foreground = Brushes.White;
                Editor.SyntaxHighlighting = hlslsyntax;
                var templateCode = @"
float4 mainImage(float2 texCoord)
{
	// Normalized pixel coordinates (from 0 to 1)
    float2 uv = texCoord/Resolution.xy;
    float3 texel = SAMPLE_TEXTURE2D(Channel0,texCoord).xyz;
    // Time varying pixel color
    float3 col = 0.5 + texel+ 0.5*cos(Time+uv.xyx+float3(0,2,4));

    // Output to screen
    return float4(col,1.0);
}";
                Editor.Text = templateCode;
                if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
                FileName = "temp/pixelshader.fx";
                SaveFile();
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
