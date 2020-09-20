﻿using AmazingMandelbrot.Properties;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Text;

namespace AmazingMandelbrot
{
    class ShaderController
    {
        public GuiComponents.FractalWindow fractalWindow;
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
        public static int BackCopyProgramId;
        public static int RaymarchProgramId;
        public static int DisplayProgramId2;
        public float ColorOffset = 0;
        public float ColorScale = 1.7f;
        const int GroupSize = 16;
        int ImageTexHandle;
        int IntermediateTexHandle;
        int ReverseTexHandle;
        int IntermediateFramebuffer;
        int ReverseFramebuffer;
        float Time = 0;
        public Complex[,] CoefficientArray;
        public FinishCompute FinishEvent;
        public int PeriodHighlight=0;
        public bool QuaternionJulia = false;
        public bool QuaternionJuliaCutoff = true;
        public Point PixelShift;
        public Matrix4 projectionMatrix;
        public ShaderController(int Width,int Height)
        {
            CoefficientArray = new Complex[,] {
                {new Complex(0, 0),new Complex(1, 0)},
                {new Complex(0, 0),new Complex(0, 0)},
                {new Complex(1, 0),new Complex(0, 0)}
            };
            mWidth = Width;
            mHeight = Height;
            
            
            
            //ImageTexHandle = GenerateTex("MainImage");
            
            IntermediateTexHandle = GenerateTex("IntermediateTexture");
            ReverseTexHandle = GenerateTex("BackTexture");
            ReverseFramebuffer= GenerateFrameBuffer(ReverseTexHandle);
            IntermediateFramebuffer = GenerateFrameBuffer(IntermediateTexHandle);
        }
        public static void GenreateComputeProgram()
        {
            
            //ComputeProgramId = SetupComputeProgram(Encoding.Default.GetString(Resources.GenerateShader));
            //DisplayProgramId = SetupComputeProgram(Encoding.Default.GetString(Resources.DisplayShader));
            //RaymarchProgramId = SetupComputeProgram(Encoding.Default.GetString(Resources.RaymarchShader));
        }
        public static void GenerateFragProgram()
        {
            BackCopyProgramId = SetupFragProgram(Encoding.Default.GetString(Resources.BackCopyVert), Encoding.Default.GetString(Resources.BackCopyFrag));
            DisplayProgramId = SetupFragProgram(Encoding.Default.GetString(Resources.DisplayVert), Encoding.Default.GetString(Resources.DisplayFrag));
            ComputeProgramId = SetupFragProgram(Encoding.Default.GetString(Resources.BackCopyVert), Encoding.Default.GetString(Resources.GenerateShaderFrag));
            RaymarchProgramId = SetupFragProgram(Encoding.Default.GetString(Resources.BackCopyVert), Encoding.Default.GetString(Resources.RaymarchFrag));
        }

        public void Compute()
        {
            projectionMatrix = fractalWindow.projectionMatrix;
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
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, IntermediateFramebuffer);
            GL.UseProgram(ProgramId);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, ReverseTexHandle);
            GL.Uniform1(GL.GetUniformLocation(ProgramId, "reverseTex"), 0);
            GL.UniformMatrix4(GL.GetUniformLocation(ProgramId, "projectionMatrix"), false, ref projectionMatrix);
            //GL.Uniform1(GL.GetUniformLocation(ProgramId, "destTex"), IntermediateTexHandle);
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
                //GL.Uniform1(GL.GetUniformLocation(ProgramId, "reverseTex"), ReverseTexHandle);
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "Julia"), Julia ? 1 : 0);
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "Zoom"), Zoom);
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "CameraReal"), CameraPos.real);
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "CameraImag"), CameraPos.imag);
            }
            //GL.DispatchCompute(mWidth / GroupSize + 1, mHeight / GroupSize + 1, 1); // width * height threads in blocks of 16^2
            //GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);

            GL.Enable(EnableCap.Texture2D);
            GL.Color3(1.0, 1.0, 1.0);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(0f, 0f);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(mWidth, 0f);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(mWidth, mHeight);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(0f, mHeight);
            GL.End();
            GL.Disable(EnableCap.Texture2D);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            FinishEvent?.Invoke();
            //Console.WriteLine(GL.GetError());
        }
        public void Draw()
        {
            Time += 0.1f;
            GL.UseProgram(DisplayProgramId); 
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "QuaternionJulia"), QuaternionJulia ? 1 : 0);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "destTex"), ImageTexHandle);
            GL.ActiveTexture(TextureUnit.Texture0);
            //if((Time*0.3)%1<0.5)
                //GL.BindTexture(TextureTarget.Texture2D, ReverseTexHandle);
            //else
            GL.BindTexture(TextureTarget.Texture2D, IntermediateTexHandle);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "sourceTex"), 0);
            
            //GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "reverseTex"), ReverseTexHandle);

            GL.UniformMatrix4(GL.GetUniformLocation(DisplayProgramId, "projectionMatrix"), false, ref projectionMatrix);
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

            //GL.DispatchCompute(mWidth / GroupSize + 1, mHeight / GroupSize + 1, 1);

            GL.Color3(1.0,1.0,1.0);
            //GL.BindTexture(TextureTarget.Texture2D, ImageTexHandle);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

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

            Draw2();
            //Compute();
        }
        public void Draw2()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ReverseFramebuffer);

            GL.UseProgram(BackCopyProgramId);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, IntermediateTexHandle);
            GL.Uniform1(GL.GetUniformLocation(BackCopyProgramId, "sourceTex"), 0);

            GL.UniformMatrix4(GL.GetUniformLocation(BackCopyProgramId, "projectionMatrix"), false, ref projectionMatrix);
            GL.Uniform2(GL.GetUniformLocation(BackCopyProgramId, "resolution"), new Vector2(mWidth, mHeight));
            GL.Enable(EnableCap.Texture2D);
            GL.Color3(1.0, 1.0, 1.0);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(0f, 0f);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(mWidth, 0f);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(mWidth, mHeight);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(0f, mHeight);
            GL.End();
            GL.Disable(EnableCap.Texture2D);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        public int GenerateTex(string Name)
        {
            int texHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texHandle);
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, mWidth, mHeight, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.BindImageTexture(texHandle, texHandle, 0, false, 0, TextureAccess.WriteOnly, SizedInternalFormat.Rgba16f);
            GL.ObjectLabel(ObjectLabelIdentifier.Texture, texHandle, Name.Length, Name);
            return texHandle;
        }
        public int GenerateFrameBuffer(int ColorTex)
        {
            int frameHandle =GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameHandle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorTex, 0);
            FramebufferErrorCode status;
            if ((status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer)) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("FrameBuffer Error: {0}", status.ToString());
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return frameHandle;
        }
        public void Resize(int w,int h)
        {
            mWidth = w;
            mHeight = h;
            GL.DeleteTexture(ImageTexHandle);
            GL.DeleteTexture(IntermediateTexHandle);
            GL.DeleteTexture(ReverseTexHandle);
            ImageTexHandle = GenerateTex("MainImage");
            IntermediateTexHandle = GenerateTex("IntermediateTexture");
            ReverseTexHandle = GenerateTex("BackTexture");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ReverseFramebuffer);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ReverseTexHandle, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, IntermediateFramebuffer);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, IntermediateTexHandle, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
        public static int SetupFragProgram(string vertexSource,string fragmentSource)
        {
            int progHandle = GL.CreateProgram();
            int vp = GL.CreateShader(ShaderType.VertexShader);
            int fp = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(vp, vertexSource);
            GL.ShaderSource(fp, fragmentSource);
            GL.CompileShader(vp);
            int rvalue;
            GL.GetShader(vp, ShaderParameter.CompileStatus, out rvalue);
            if (rvalue != (int)All.True)
            {

                Console.WriteLine("Error in compiling vp");

                Console.WriteLine((All)rvalue);

                Console.WriteLine(GL.GetShaderInfoLog(vp));

            }
            GL.AttachShader(progHandle, vp);
            GL.CompileShader(fp);
            GL.GetShader(fp, ShaderParameter.CompileStatus, out rvalue);
            if (rvalue != (int)All.True)
            {
                Console.WriteLine("Error in compiling fp");
                Console.WriteLine((All)rvalue);
                Console.WriteLine(GL.GetShaderInfoLog(fp));
            }

            GL.AttachShader(progHandle, fp);
            //GL.BindFragDataLocation(progHandle, 0, "color");
            GL.LinkProgram(progHandle);
            //Console.WriteLine(GL.GetProgramInfoLog(progHandle));
            GL.GetProgram(progHandle, GetProgramParameterName.LinkStatus, out rvalue);
            if (rvalue != (int)All.True)
            {
                Console.WriteLine("Error in linking sp");
                Console.WriteLine((All)rvalue);
                Console.WriteLine(GL.GetProgramInfoLog(progHandle));
            }
            GL.ValidateProgram(progHandle);
            GL.GetProgram(progHandle, GetProgramParameterName.ValidateStatus, out rvalue);
            if (rvalue != (int)All.True)
            {
                Console.WriteLine("Validation Error");
                Console.WriteLine((All)rvalue);
                Console.WriteLine(GL.GetProgramInfoLog(progHandle));
            }

            return progHandle;
        }
    }
}
