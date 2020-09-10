using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.IO;
using AmazingMandelbrot.Properties;
using System.Drawing;

namespace AmazingMandelbrot
{
    class ShaderController
    {
        public delegate void FinishCompute();
        public int mWidth;
        public int mHeight;
        public Complex CameraPos=new Complex(0,0);
        public double Zoom=2;
        public int Iterations =300;
        public bool Julia = false;
        public Complex JuliaPos;
        public static int ComputeProgramId;
        public static int DisplayProgramId;
        public static int RaymarchProgramId;
        public float ColorOffset = 0;
        public float ColorScale = 1.7f;
        const int GroupSize = 16;
        int ImageTexHandle;
        int IntermediateTexHandle;
        int ReverseTexHandle;
        float Time = 0;
        public Complex[,] CoefficientArray;
        public FinishCompute FinishEvent;
        public int PeriodHighlight=0;
        public bool QuaternionJulia = false;
        public bool QuaternionJuliaCutoff = true;
        public Point PixelShift;
        public ShaderController(int Width,int Height)
        {
            CoefficientArray = new Complex[,] {
                {new Complex(0, 0),new Complex(1, 0)},
                {new Complex(0, 0),new Complex(0, 0)},
                {new Complex(1, 0),new Complex(0, 0)}
            };
            mWidth = Width;
            mHeight = Height;

            ImageTexHandle = GenerateTex();
            IntermediateTexHandle = GenerateTex();
            ReverseTexHandle = GenerateTex();
        }
        public static void GenreateComputeProgram()
        {
            
            ComputeProgramId = SetupComputeProgram(Encoding.Default.GetString(Resources.GenerateShader));
            DisplayProgramId = SetupComputeProgram(Encoding.Default.GetString(Resources.DisplayShader));
            RaymarchProgramId = SetupComputeProgram(Encoding.Default.GetString(Resources.RaymarchShader));
        }
        public void Compute()
        {
            double[] Arr = new double[CoefficientArray.GetLength(0) * CoefficientArray.GetLength(1) * 2];
            for (int i = 0; i < CoefficientArray.GetLength(0); i++)
            {
                for (int j = 0; j < CoefficientArray.GetLength(1); j++)
                {
                    int k = i + j * CoefficientArray.GetLength(0);
                    Arr[k * 2] = CoefficientArray[i, j].real;
                    Arr[k * 2 + 1] = CoefficientArray[i, j].imag;
                }
            }
            int ProgramId = QuaternionJulia? RaymarchProgramId : ComputeProgramId;
            GL.UseProgram(ProgramId);

            GL.UseProgram(ProgramId);
            GL.Uniform1(GL.GetUniformLocation(ProgramId, "destTex"), IntermediateTexHandle);
            GL.Uniform2(GL.GetUniformLocation(ProgramId, "resolution"), new Vector2(mWidth, mHeight));
            GL.Uniform1(GL.GetUniformLocation(ProgramId, "Iter"), Iterations);

            GL.Uniform1(GL.GetUniformLocation(ProgramId, "JuliaReal"), JuliaPos.real);
            GL.Uniform1(GL.GetUniformLocation(ProgramId, "JuliaImag"), JuliaPos.imag);

            GL.Uniform1(GL.GetUniformLocation(ProgramId, "ArrayMaxZ"), CoefficientArray.GetLength(0));
            GL.Uniform1(GL.GetUniformLocation(ProgramId, "ArrayMaxC"), CoefficientArray.GetLength(1));
            GL.Uniform1(GL.GetUniformLocation(ProgramId, "CoefficientArray"), Arr.Length, Arr);

            if (QuaternionJulia)
            {
                GL.Uniform1(GL.GetUniformLocation(RaymarchProgramId, "Cutoff"), QuaternionJuliaCutoff ? 1 : 0);
            }
            else
            {
               
                GL.Uniform2(GL.GetUniformLocation(ProgramId, "PixelShift"), PixelShift.X, PixelShift.Y);
                PixelShift.X = PixelShift.Y = 0;
                GL.Uniform1(GL.GetUniformLocation(ProgramId, "reverseTex"), ReverseTexHandle);
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "Julia"), Julia ? 1 : 0);
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "Zoom"), Zoom);
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "CameraReal"), CameraPos.real);
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "CameraImag"), CameraPos.imag);
            }
            GL.DispatchCompute(mWidth / GroupSize + 1, mHeight / GroupSize + 1, 1); // width * height threads in blocks of 16^2
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            FinishEvent?.Invoke();
        }
        public void Draw()
        {
            Time += 0.1f;
            GL.UseProgram(DisplayProgramId); 
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "QuaternionJulia"), QuaternionJulia ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "destTex"), ImageTexHandle);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "sourceTex"), IntermediateTexHandle);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "reverseTex"), ReverseTexHandle);
            

            GL.Uniform2(GL.GetUniformLocation(DisplayProgramId, "resolution"), new Vector2(mWidth, mHeight));

            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "Zoom"), Zoom);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "Iter"), Iterations);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "CameraReal"), CameraPos.real);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "CameraImag"), CameraPos.imag);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "ColorOffset"), ColorOffset);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "ColorScale"), ColorScale);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "JuliaReal"), JuliaPos.real);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "JuliaImag"), JuliaPos.imag);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "Julia"), Julia ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "Time"), Time); 
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "PeriodHighlight"), PeriodHighlight);
            double[] Arr = new double[CoefficientArray.GetLength(0) * CoefficientArray.GetLength(1) * 2];
            for (int i = 0; i < CoefficientArray.GetLength(0); i++)
            {
                for (int j = 0; j < CoefficientArray.GetLength(1); j++)
                {
                    int k = i + j * CoefficientArray.GetLength(0);
                    Arr[k * 2] = CoefficientArray[i, j].real;
                    Arr[k * 2 + 1] = CoefficientArray[i, j].imag;
                }
            }
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "ArrayMaxZ"), CoefficientArray.GetLength(0));
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "ArrayMaxC"), CoefficientArray.GetLength(1));
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "CoefficientArray"), Arr.Length, Arr);

            GL.DispatchCompute(mWidth / GroupSize + 1, mHeight / GroupSize + 1, 1);

            GL.Color3(1.0,1.0,1.0);
            GL.BindTexture(TextureTarget.Texture2D, ImageTexHandle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.Enable(EnableCap.Texture2D);

            GL.Begin(PrimitiveType.Quads);
            float realWidth = mWidth;
            float realHeight = mHeight;
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(0f, 0f);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(realWidth, 0f);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(realWidth, realHeight);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(0f, realHeight);
            GL.End();
            GL.Disable(EnableCap.Texture2D);
        }
        public int GenerateTex()
        {
            int texHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texHandle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, mWidth, mHeight, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.BindImageTexture(texHandle, texHandle, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba16f);
            return texHandle;
        }
        public void ReSize(int w,int h)
        {
            mWidth = w;
            mHeight = h;
            ImageTexHandle = GenerateTex();
            IntermediateTexHandle = GenerateTex();
        }
        private static int SetupComputeProgram(string csSrc)
        {
            int progHandle = GL.CreateProgram();
            int cs = GL.CreateShader(ShaderType.ComputeShader);
            

            GL.ShaderSource(cs, csSrc);
            GL.CompileShader(cs);

            GL.GetShader(cs, ShaderParameter.CompileStatus, out int rvalue);
            if (rvalue != (int)All.True)
            {
                Console.WriteLine(GL.GetShaderInfoLog(cs));
            }
            GL.AttachShader(progHandle, cs);

            GL.LinkProgram(progHandle);
            GL.GetProgram(progHandle, GetProgramParameterName.LinkStatus, out rvalue);
            if (rvalue != (int)All.True)
            {
                Console.WriteLine(GL.GetProgramInfoLog(progHandle));
            }

            GL.UseProgram(progHandle);
            return progHandle;

        }
    }
}
