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
using MonoGame.Framework.WpfInterop;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;
using System.Collections.Generic;
using MonoGame.Framework.WpfInterop.Input;

namespace ShaderEdit
{

    public enum TextureType
    {
        Texture2D,
        CubeMap,
        Volume,
    }

    public class Channel
    {
        public TextureType Type;
        public Texture Texture;
        public float Time;

        public Channel(Texture texture,TextureType type)
        {
            Texture = texture;
            Type = type;
            Time = 0;
        }

        public Vector3 Resolution()
        {
            Vector3 Result = new Vector3();
            switch (Type)
            {
                case TextureType.Texture2D:
                    var tex2d = Texture as Texture2D;
                    Result = new Vector3(tex2d.Width,tex2d.Height, 1);
                    break;
                case TextureType.CubeMap:
                    var texcube = Texture as TextureCube;
                    Result = new Vector3(texcube.Size,texcube.Size, 6);
                    break;
                case TextureType.Volume:
                    var tex3d = Texture as Texture3D;
                    Result = new Vector3(tex3d.Width,tex3d.Height,tex3d.Depth);
                    break;
                default:
                    break;
            }
            return Result;
        }

    }

    public class GraphicsContext : WpfGame
    {
        private IGraphicsDeviceService _graphicsDeviceManager;
        private WpfKeyboard _keyboard;
        private WpfMouse _mouse;

        
        private Effect Shader;
        private VertexBuffer VertexBuffer;
        private IndexBuffer IndexBuffer;

        
        // Uniforms
        private Vector3 Resolution;           // viewport resolution (in pixels)
        private float Time;                 // shader playback time (in seconds)
        private float TimeDelta;            // render time (in seconds)
        private int Frame;                // shader playback frame
        private Vector4 Mouse;                // mouse pixel coords. xy: current (if MLB down), zw: click
        private Channel Channel0;          // input channel. XX = 2D/Cube
        private Channel Channel1;          // input channel. XX = 2D/Cube
        private Channel Channel2;          // input channel. XX = 2D/Cube
        private Channel Channel3;          // input channel. XX = 2D/Cube
        private Vector4 Date;                 // (year, month, day, time in seconds)
        private float SampleRate;           // sound sample rate (i.e., 44100)

        private DateTime LastShaderWriteTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsContext"/> class.
        /// </summary>
        public GraphicsContext()
        {
            
        }
        

        private bool CompileShader(out string CompileError)
        {
            var MainShaderCode = File.ReadAllText("common/shader.fx");
            var ChannelsCode = "";
            ChannelsCode = GenerateChannelShaderCode(ChannelsCode,Channel0);
            ChannelsCode = GenerateChannelShaderCode(ChannelsCode,Channel1);
            ChannelsCode = GenerateChannelShaderCode(ChannelsCode,Channel2);
            ChannelsCode = GenerateChannelShaderCode(ChannelsCode,Channel3);
            MainShaderCode = MainShaderCode.Replace("[insert Channel defines here]", ChannelsCode);
            if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");
            File.WriteAllText("temp/tempshader.fx", MainShaderCode);
            bool error = false;
            CompileError = "";
            if (File.Exists("temp/pixelshader.fx"))
            {
                LastShaderWriteTime = File.GetLastWriteTime("temp/pixelshader.fx");
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

        private string GenerateChannelShaderCode(string channelshadercode,Channel channel)
        {
            if (channel != null)
            {
                switch (channel.Type)
                {
                    case TextureType.Texture2D:
                        channelshadercode += "DECLARE_TEXTURE2D";
                        break;
                    case TextureType.CubeMap:
                        channelshadercode += "DECLARE_CUBEMAP";
                        break;
                    case TextureType.Volume:
                        channelshadercode += "DECLARE_TEXTURE3D";
                        break;
                    default:
                        break;
                }
                channelshadercode += "(Channel0,0)";
                channelshadercode += "\n";
            }

            return channelshadercode;
        }

        private void UpdateUniforms(float dt)
        {
            Date = new Vector4(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,(float)DateTime.Now.TimeOfDay.TotalSeconds);
            Mouse = new Vector4(_mouse.GetState().Position.X, _mouse.GetState().Position.Y,(_mouse.GetState().LeftButton== Microsoft.Xna.Framework.Input.ButtonState.Pressed ) ? 1 : 0, (_mouse.GetState().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed) ? 1 : 0);

            TimeDelta = dt;
            Time += dt;
            Resolution = new Vector3(GraphicsDevice.PresentationParameters.Bounds.Size.ToVector2(),1.0f);
            if (Channel0 != null) Channel0.Time += dt;
            if (Channel1 != null) Channel1.Time += dt;
            if (Channel2 != null) Channel2.Time += dt;
            if (Channel3 != null) Channel3.Time += dt;
        }

        private void SetUniforms(Effect shader)
        {
            if(shader != null)
            {
                if (Shader.Parameters.Any(x => x.Name == "Time")) Shader.Parameters["Time"].SetValue(Time);
                if (Shader.Parameters.Any(x => x.Name == "Date")) Shader.Parameters["Date"].SetValue(Date);
                if (Shader.Parameters.Any(x => x.Name == "Resolution")) Shader.Parameters["Resolution"].SetValue(Resolution);
                if (Shader.Parameters.Any(x => x.Name == "Channel0")) Shader.Parameters["Channel0"].SetValue(Channel0.Texture);
                if (Shader.Parameters.Any(x => x.Name == "Channel1")) Shader.Parameters["Channel1"].SetValue(Channel1.Texture);
                if (Shader.Parameters.Any(x => x.Name == "Channel2")) Shader.Parameters["Channel2"].SetValue(Channel2.Texture);
                if (Shader.Parameters.Any(x => x.Name == "Channel3")) Shader.Parameters["Channel3"].SetValue(Channel3.Texture);
                if (Shader.Parameters.Any(x => x.Name == "ChannelResolution0")) Shader.Parameters["ChannelResolution0"].SetValue(Channel0.Resolution());
                if (Shader.Parameters.Any(x => x.Name == "ChannelResolution1")) Shader.Parameters["ChannelResolution1"].SetValue(Channel1.Resolution());
                if (Shader.Parameters.Any(x => x.Name == "ChannelResolution2")) Shader.Parameters["ChannelResolution2"].SetValue(Channel2.Resolution());
                if (Shader.Parameters.Any(x => x.Name == "ChannelResolution3")) Shader.Parameters["ChannelResolution3"].SetValue(Channel3.Resolution());
                if (Shader.Parameters.Any(x => x.Name == "ChannelTime0")) Shader.Parameters["ChannelTime0"].SetValue(Channel0.Time);
                if (Shader.Parameters.Any(x => x.Name == "ChannelTime1")) Shader.Parameters["ChannelTime1"].SetValue(Channel1.Time);
                if (Shader.Parameters.Any(x => x.Name == "ChannelTime2")) Shader.Parameters["ChannelTime2"].SetValue(Channel2.Time);
                if (Shader.Parameters.Any(x => x.Name == "ChannelTime3")) Shader.Parameters["ChannelTime3"].SetValue(Channel3.Time);
                if (Shader.Parameters.Any(x => x.Name == "MatrixTransform")) Shader.Parameters["MatrixTransform"].SetValue(Matrix.Identity);
            }
        }


        protected override void Initialize()
        {
            // must be initialized. required by Content loading and rendering(will add itself to the Services)
        // note that MonoGame requires this to be initialized in the constructor, while WpfInterop requires it to
        // be called inside Initialize (before base.Initialize())
            _graphicsDeviceManager = new WpfGraphicsDeviceService(this) { DpiScalingFactor = 1 , PreferMultiSampling = true };

            // wpf and keyboard need reference to the host control in order to receive input
            // this means every WpfGame control will have it's own keyboard & mouse manager which will only react if the mouse is in the control
            _keyboard = new WpfKeyboard(this);
            _mouse = new WpfMouse(this);

            // must be called after the WpfGraphicsDeviceService instance was created
            base.Initialize();
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
            Channel0 = new Channel(Texture2D.FromStream(GraphicsDevice, File.OpenRead("assets/textures/test.png")), TextureType.Texture2D);
            if (CompileShader( out var error))
            {
                var shaderbinary = File.ReadAllBytes($"temp/tempshader.dx11");
                Shader = new Effect(GraphicsDevice, shaderbinary);
                SetUniforms(Shader);
            }
        }
       

        public void UpdateShader()
        {
            if (File.GetLastWriteTime("temp/pixelshader.fx") > LastShaderWriteTime)
            {
                if (CompileShader(out var error))
                {
                    var shaderbinary = File.ReadAllBytes("temp/tempshader.dx11");
                    if (Shader != null) Shader.Dispose();
                    Shader = null;
                    Shader = new Effect(GraphicsDevice, shaderbinary);
                    SetUniforms(Shader);
                }
            }
        }
        protected override void Update(GameTime time)
        {
            var mouseState = _mouse.GetState();
            var keyboardState = _keyboard.GetState();

        }

        protected override void Draw(GameTime time)
        {
            var dt = (float)time.ElapsedGameTime.TotalSeconds;
            UpdateShader();
            UpdateUniforms(dt);
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
