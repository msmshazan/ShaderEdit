using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Utilities.Png;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;


namespace ShaderEdit
{
    /// <summary>
    /// Host a Direct3D 11 scene.
    /// </summary>
    public class D3D11Host : Image
    {
        // The Direct3D 11 device (shared by all D3D11Host elements):
        private static GraphicsDevice _graphicsDevice;
        private static int _referenceCount;
        private static readonly object _graphicsDeviceLock = new object();

        // Image source:
        private RenderTarget2D _renderTarget;
        private D3D11Image _d3D11Image;
        private bool _resetBackBuffer;

        // Render timing:
        private readonly Stopwatch _timer;
        private TimeSpan _lastRenderingTime;

        /// <summary>
        /// Gets a value indicating whether the controls runs in the context of a designer (e.g.
        /// Visual Studio Designer or Expression Blend).
        /// </summary>
        /// <value>
        /// <see langword="true" /> if controls run in design mode; otherwise, 
        /// <see langword="false" />.
        /// </value>
        public static bool IsInDesignMode
        {
            get
            {
                if (!_isInDesignMode.HasValue)
                    _isInDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty, typeof(FrameworkElement)).Metadata.DefaultValue;

                return _isInDesignMode.Value;
            }
        }
        private static bool? _isInDesignMode;


        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice
        {
            get { return _graphicsDevice; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="D3D11Host"/> class.
        /// </summary>
        public D3D11Host()
        {
            _timer = new Stopwatch();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        
        private void OnLoaded(object sender, RoutedEventArgs eventArgs)
        {
            if (IsInDesignMode)
                return;

            InitializeGraphicsDevice();
            InitializeImageSource();
            Initialize();
            StartRendering();
        }


        private void OnUnloaded(object sender, RoutedEventArgs eventArgs)
        {
            if (IsInDesignMode)
                return;

            StopRendering();
            Unitialize();
            UnitializeImageSource();
            UninitializeGraphicsDevice();
        }


        private static void InitializeGraphicsDevice()
        {
            lock (_graphicsDeviceLock)
            {
                _referenceCount++;
                if (_referenceCount == 1)
                {
                    // Create Direct3D 11 device.
                    var presentationParameters = new PresentationParameters
                    {
                        // Do not associate graphics device with window.
                        DeviceWindowHandle = IntPtr.Zero,
                    };
                    _graphicsDevice = new GraphicsDevice( GraphicsAdapter.DefaultAdapter ,GraphicsProfile.HiDef, presentationParameters);
                }
            }
        }


        private static void UninitializeGraphicsDevice()
        {
            lock (_graphicsDeviceLock)
            {
                _referenceCount--;
                if (_referenceCount == 0)
                {
                    _graphicsDevice.Dispose();
                    _graphicsDevice = null;
                }
            }
        }


        private void InitializeImageSource()
        {
            _d3D11Image = new D3D11Image();
            _d3D11Image.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;
            CreateBackBuffer();
            Source = _d3D11Image;
        }


        private void UnitializeImageSource()
        {
            _d3D11Image.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            Source = null;

            if (_d3D11Image != null)
            {
                _d3D11Image.Dispose();
                _d3D11Image = null;
            }
            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
                _renderTarget = null;
            }
        }


        private void CreateBackBuffer()
        {
            _d3D11Image.SetBackBuffer(null);
            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            int width = Math.Max((int)ActualWidth, 1);
            int height = Math.Max((int)ActualHeight, 1);
            _renderTarget = new RenderTarget2D(_graphicsDevice, width, height, false, SurfaceFormat.Bgr32, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents, true);
            _d3D11Image.SetBackBuffer(_renderTarget);
        }


        private void StartRendering()
        {
            if (_timer.IsRunning)
                return;

            CompositionTarget.Rendering += OnRendering;
            _timer.Start();
        }


        private void StopRendering()
        {
            if (!_timer.IsRunning)
                return;

            CompositionTarget.Rendering -= OnRendering;
            _timer.Stop();
        }


        private void OnRendering(object sender, EventArgs eventArgs)
        {
            if (!_timer.IsRunning)
                return;

            // Recreate back buffer if necessary.
            if (_resetBackBuffer)
                CreateBackBuffer();

            // CompositionTarget.Rendering event may be raised multiple times per frame
            // (e.g. during window resizing).
            var renderingEventArgs = (RenderingEventArgs)eventArgs;
            if (_lastRenderingTime != renderingEventArgs.RenderingTime || _resetBackBuffer)
            {
                _lastRenderingTime = renderingEventArgs.RenderingTime;

                GraphicsDevice.SetRenderTarget(_renderTarget);
                Render(_timer.Elapsed);
                GraphicsDevice.Flush();
            }

            _d3D11Image.Invalidate(); // Always invalidate D3DImage to reduce flickering
                                      // during window resizing.

            _resetBackBuffer = false;
        }


        /// <summary>
        /// Raises the <see cref="FrameworkElement.SizeChanged" /> event, using the specified 
        /// information as part of the eventual event data.
        /// </summary>
        /// <param name="sizeInfo">Details of the old and new size involved in the change.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            _resetBackBuffer = true;
            base.OnRenderSizeChanged(sizeInfo);
        }


        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (_d3D11Image.IsFrontBufferAvailable)
            {
                StartRendering();
                _resetBackBuffer = true;
            }
            else
            {
                StopRendering();
            }
        }

        

        private bool CompileShader(out string CompileError)
        {
            var MainShaderCode = File.ReadAllText("common/shader.fx");
            if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
            File.WriteAllText("temp/tempshader.fx",MainShaderCode);
            bool error = false;
            CompileError = "";
            if (File.Exists("temp/pixelshader.fx"))
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo("tools/2MGFX.exe", "temp/tempshader.fx temp/tempshader.dx11 /Profile:DirectX_11 /Debug");
                processStartInfo.UseShellExecute = false;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
                var process = Process.Start(processStartInfo);
                while (!process.StandardOutput.EndOfStream)
                {
                    CompileError += process.StandardOutput.ReadLine();
                }

                while (!process.StandardError.EndOfStream)
                {
                    error = true;
                    CompileError += process.StandardError.ReadLine();
                }
                process.WaitForExit();
            }
            else
            {
                CompileError = "file not found / saved";
            }
            return error == true ? false : true;
        }

        private Effect Shader;
        private VertexBuffer VertexBuffer;
        private IndexBuffer IndexBuffer;


        // Uniforms
        Vector3  Resolution;           // viewport resolution (in pixels)
        float Time;                 // shader playback time (in seconds)
        float TimeDelta;            // render time (in seconds)
        // int Frame;                // shader playback frame
       // float iChannelTime0;       // channel playback time (in seconds)
       // float iChannelTime1;       // channel playback time (in seconds)
       // float iChannelTime2;       // channel playback time (in seconds)
      //  float iChannelTime3;       // channel playback time (in seconds)
       // Vector3 iChannelResolution0; // channel resolution (in pixels)
       // Vector3 iChannelResolution1; // channel resolution (in pixels)
       // Vector3 iChannelResolution2; // channel resolution (in pixels)
       // Vector3 iChannelResolution3; // channel resolution (in pixels)
       Vector4  Mouse;                // mouse pixel coords. xy: current (if MLB down), zw: click
        Texture Channel0;          // input channel. XX = 2D/Cube
       // Texture iChannel1;          // input channel. XX = 2D/Cube
       // Texture iChannel2;          // input channel. XX = 2D/Cube
       // Texture iChannel3;          // input channel. XX = 2D/Cube
        Vector4      Date;                 // (year, month, day, time in seconds)
        // float SampleRate;           // sound sample rate (i.e., 44100)

        DateTime LastShaderWriteTime;
        
        private void UpdateUniforms(float totalseconds)
        {
            Date = new Vector4(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,(float)DateTime.Now.TimeOfDay.TotalSeconds);
            TimeDelta = totalseconds - Time;
            Time = totalseconds;
            Resolution = new Vector3(GraphicsDevice.PresentationParameters.Bounds.Size.ToVector2(),1.0f);
            if (Channel0 == null) Channel0 = Texture2D.FromStream(GraphicsDevice, File.OpenRead("assets/textures/test.png"));
        }

        private void SetUniforms(Effect shader)
        {
            if(shader != null)
            {
                if (Shader.Parameters.Any(x => x.Name == "Time")) Shader.Parameters["Time"].SetValue(Time);
                if (Shader.Parameters.Any(x => x.Name == "Date")) Shader.Parameters["Date"].SetValue(Date);
                if (Shader.Parameters.Any(x => x.Name == "Resolution")) Shader.Parameters["Resolution"].SetValue(Resolution);
                if (Shader.Parameters.Any(x => x.Name == "Texture")) Shader.Parameters["Texture"].SetValue(Channel0);
                if (Shader.Parameters.Any(x => x.Name == "MatrixTransform")) Shader.Parameters["MatrixTransform"].SetValue(Matrix.Identity);

            }
        }

        private void Initialize()
        {
            UpdateUniforms(0);
            VertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColorTexture.VertexDeclaration, 4, BufferUsage.None);
            IndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, 6, BufferUsage.None);
            var vertices = new VertexPositionColorTexture[4];
            vertices[0] = new VertexPositionColorTexture(new Vector3(-1, -1, 0), Color.White, new Vector2(0, 1));
            vertices[1] = new VertexPositionColorTexture(new Vector3(1, 1, 0), Color.White, new Vector2(1, 0));
            vertices[2] = new VertexPositionColorTexture(new Vector3(-1, 1, 0), Color.White, new Vector2(0, 0));
            vertices[3] = new VertexPositionColorTexture(new Vector3(1, -1, 0), Color.White, new Vector2(1, 1));
            var indices = new int[] { 0, 2, 1, 0, 3, 1 };
            VertexBuffer.SetData(vertices);
            IndexBuffer.SetData(indices);
            if(CompileShader( out var error))
            {
                var shaderbinary = File.ReadAllBytes($"temp/tempshader.dx11");
                Shader = new Effect(GraphicsDevice, shaderbinary);
                SetUniforms(Shader);
            }
        }
       
        private void Unitialize()
        {
            
        }

        public void UpdateShader()
        {
            if (CompileShader(out var error))
            {
                    var shaderbinary = File.ReadAllBytes("temp/tempshader.dx11");
                    if(Shader != null) Shader.Dispose();
                    Shader = null;
                    Shader = new Effect( GraphicsDevice, shaderbinary);
                    if (Shader.Parameters.Any(x => x.Name == "Texture")) Shader.Parameters["Texture"].SetValue(Channel0);
                    if (Shader.Parameters.Any(x => x.Name == "MatrixTransform")) Shader.Parameters["MatrixTransform"].SetValue(Matrix.Identity);
            }
         
        }

        private void Render(TimeSpan time)
        {
            var totalseconds = (float)time.TotalSeconds;
            UpdateUniforms(totalseconds);
            SetUniforms(Shader);
            GraphicsDevice.Clear(Color.SteelBlue);
            GraphicsDevice.SamplerStates[0] = new SamplerState() {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = TextureFilter.Point   };
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SetVertexBuffer(VertexBuffer);
            GraphicsDevice.Indices = IndexBuffer;
            if (Shader != null)
            {
                foreach (var Technique in Shader.Techniques)
                {
                    foreach (var Pass in Technique.Passes)
                    {
                        Pass.Apply();
                        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
                    }
                }
            }
        }
    }
}
