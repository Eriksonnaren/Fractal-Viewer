﻿using AmazingMandelbrot.Properties;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Text;
using OpenTK.Graphics;

namespace AmazingMandelbrot
{
    class ShaderController
    {

        struct DataStruct
        {
            public double IterationCount;
            public double MinDistance;
            public double DistEstimate;
            public int RawIterations;
            public int Period;
            public Vector2d EndPoint;
        }
        public GuiComponents.FractalWindow fractalWindow;
        public delegate void FinishCompute();
        public int mWidth;
        public int mHeight;
        public Complex CameraPos = new Complex(0, 0);
        public double CameraAngleX = 0.7;
        public double CameraAngleY = 0.7;
        public double CameraDistance = 1.7;

        public double Zoom = 2;
        public int Iterations = 300;
        public bool Julia = false;
        public Complex JuliaPos;
        public Complex OffsetPos;
        public static int ComputeProgramId;
        public static int DisplayProgramId;
        public static int BackCopyProgramId;
        public static int RaymarchProgramId;
        public static int DisplayProgramId2;
        public float ColorOffset = 0;
        public float ColorScale = 2.0f;
        const int GroupSize = 16;
        int ImageTexHandle;
        int IntermediateTexHandle;
        int ReverseTexHandle;
        int IntermediateFramebuffer;
        int ReverseFramebuffer;
        int IntermediateShaderBuffer;
        int ReverseShaderBuffer;
        float Time = 0;
        public float Angle = 0;
        public float OldAngle = 0;
        public bool UseOldAngle = false;
        public Complex[,] CoefficientArray;
        public FinishCompute FinishEvent;
        public int PeriodHighlight = 10;
        public bool QuaternionJulia = false;
        public bool QuaternionJuliaCutoff = true;
        public Point PixelShift;
        public Matrix4 projectionMatrix;
        public Color4[] ColorPalette = new Color4[] { Color4.Orange, Color4.DarkBlue, Color4.Cyan };
        public Color4 InteriorColor = Color.Black;
        public float[] PalettePositions = new float[] { 0, 0.33f, 0.66f };
        public int PaletteSize = 3;
        public double AverageIteration;
        int BufferExtractTimer = 0;
        DataStruct[] Buffer;
        public Complex IterationPoint = new Complex(0, 0);
        bool UpdateBackgroundTexture = true;
        public float CenterDotStrength = 0.8f;
        public Complex FinalDotPosition = new Complex(0, 0);
        public float FinalDotStrength = 0.0f;
        public bool MeshActive{get; private set;}
        public Camera3d camera3D;
        public Matrix4 modelMatrix=Matrix4.Identity;
        int MeshSize;
        int MeshResolution;
        int VertexBuffer;
        int VertexArray;
        int IndexBuffer;
        Vector3[] Vertices;
        uint[] Indices;
        Size WindowResolution;
        public bool DistanceEstimateEnabled=false;
        public double DistanceEstimateColoringLerp = 0;
        public BuddhaShaderController buddhaController;
        public bool canUseBuddhaMode { get; private set; } = false;
        public bool buddhaActive;
        bool BuddhaReset;

        public ShaderController(int Width,int Height)
        {
            WindowResolution = new Size(Width, Height);
            CoefficientArray = new Complex[,] {
                {new Complex(0, 0),new Complex(1, 0)},
                {new Complex(0, 0),new Complex(0, 0)},
                {new Complex(1, 0),new Complex(0, 0)}
            };
            mWidth = Width;
            mHeight = Height;
            Buffer = new DataStruct[mWidth * mHeight];


            //ImageTexHandle = GenerateTex("MainImage");

            IntermediateTexHandle = GenerateTex("IntermediateTexture");
            ReverseTexHandle = GenerateTex("BackTexture");
            ReverseFramebuffer= GenerateFrameBuffer(ReverseTexHandle);
            IntermediateFramebuffer = GenerateFrameBuffer(IntermediateTexHandle);
            int BufferSize = mWidth * mHeight * (sizeof(double)*4+sizeof(double)*2);

            //Swapping these 2 lines breaks the shaderbuffers for some strange reason, i have no idea why...
            ReverseShaderBuffer = GenerateShaderBuffer(BufferSize);
            IntermediateShaderBuffer = GenerateShaderBuffer(BufferSize);
            
        }
        public void GenerateMesh(int Size,int Resolution)
        {
            MeshSize = Size;
            uint s = (uint)MeshSize;
            MeshResolution = Resolution;
            Vertices = new Vector3[MeshSize * MeshSize];
            Indices=new uint[((MeshSize-1) * (MeshSize-1))*6];
            Random r = new Random();
            for (int i = 0; i < MeshSize * MeshSize; i++)
            {
                float x = i % MeshSize;
                float y = i / MeshSize;
                Vertices[i] = (new Vector3(x, y, 0) *MeshResolution)/ MeshSize;
            }
            int k = 0;
            for (uint i = 0; i < MeshSize - 1 ; i++)
            {
                for (uint j = 0; j < MeshSize - 1; j++)
                {
                    uint u = (i + j * s);
                    Indices[k++] = u;
                    Indices[k++] = u + 1;
                    Indices[k++] = u + s;
                    Indices[k++] = u + s + 1;
                    Indices[k++] = u + 1;
                    Indices[k++] = u + s;
                }
            }
            GL.GenBuffers(1, out VertexBuffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * sizeof(float)*3), Vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out IndexBuffer);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Indices.Length * sizeof(uint)), Indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            // create new vertex array object
            GL.GenVertexArrays(1, out VertexArray);

            // bind the object so we can modify it
            GL.BindVertexArray(VertexArray);

            // bind the vertex buffer object
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer);

            // set all attributes
            //foreach (var attribute in attributes)
                //attribute.Set(program);

            // enable and set attribute
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float,
                false, 3, 0);
            // unbind objects to reset state
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            //Console.WriteLine(GL.GetError());
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

        public void Compute(bool updateBackgroundTexture = true)
        {
            BuddhaShaderController.BlockBuddhaShader = true;//hotfix to prevent program from freezing
            BuddhaReset = true;
            UpdateBackgroundTexture = updateBackgroundTexture;
            BufferExtractTimer = 20;
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

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, IntermediateShaderBuffer);
            int id = GL.GetProgramResourceIndex(ProgramId, ProgramInterface.ShaderStorageBlock, "DataBlock");
            GL.ShaderStorageBlockBinding(ProgramId, id, IntermediateShaderBuffer);
            GL.BindBufferBase( BufferRangeTarget.ShaderStorageBuffer, id, IntermediateShaderBuffer);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ReverseShaderBuffer);
            id = GL.GetProgramResourceIndex(ProgramId, ProgramInterface.ShaderStorageBlock, "OldDataBlock");
            GL.ShaderStorageBlockBinding(ProgramId, id, ReverseShaderBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, id, ReverseShaderBuffer);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            //GL.Uniform1(GL.GetUniformLocation(ProgramId, "DataBlock"), IntermediateShaderBuffer);

            GL.Uniform2(GL.GetUniformLocation(ProgramId, "resolution"),mWidth, mHeight);
            GL.Uniform1(GL.GetUniformLocation(ProgramId, "Iter"), Iterations);

            GL.Uniform1(GL.GetUniformLocation(ProgramId, "JuliaReal"), JuliaPos.real);
            GL.Uniform1(GL.GetUniformLocation(ProgramId, "JuliaImag"), JuliaPos.imag);
            GL.Uniform1(GL.GetUniformLocation(ProgramId, "OffsetReal"), OffsetPos.real);
            GL.Uniform1(GL.GetUniformLocation(ProgramId, "OffsetImag"), OffsetPos.imag);

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
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "Angle"),Angle);
                
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "UseOldAngle"), UseOldAngle ? 1 : 0);
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "OldAngle"), OldAngle);
                GL.Uniform1(GL.GetUniformLocation(ComputeProgramId, "EnableDistanceEstimate"), (DistanceEstimateEnabled||MeshActive)?1:0); 


            }
            //GL.DispatchCompute(mWidth / GroupSize + 1, mHeight / GroupSize + 1, 1); // width * height threads in blocks of 16^2
            //GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            //GL.UseProgram(0);
            GL.Enable(EnableCap.Texture2D);
            //Console.WriteLine("Draw fractal quad at time: {0}", Main.stopwatch.Elapsed.TotalMilliseconds);
            GL.Color3(1.0, 1.0, 1.0);
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0.0f, 0.0f);
            GL.Vertex2(0f, 0f);
            GL.TexCoord2(1.0f, 0.0f);
            GL.Vertex2(mWidth, 0f);
            GL.TexCoord2(1.0f, 1.0f);
            GL.Vertex2(mWidth, mHeight);
            GL.TexCoord2(0.0f, 1.0f);
            GL.Vertex2(0f, mHeight);
            GL.End();
            GL.Disable(EnableCap.Texture2D);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            FinishEvent?.Invoke();
            if(buddhaActive)
            {
                buddhaController.SetCamera(CameraPos,Zoom);
                buddhaController.Start();
                buddhaController.CoefficientArray = CoefficientArray;
                buddhaController.OffsetPos = OffsetPos;
            }
        }
        public void Draw()
        {
            //Matrix3 Rot = Matrix3.CreateRotationZ(0.02f);
            //camera3D.CameraVector *= Rot;
            if (BufferExtractTimer>0)
            {
                BufferExtractTimer--;
                if(BufferExtractTimer==0)
                {
                    GetBufferValues(IntermediateShaderBuffer, Buffer);
                    AverageIteration = 0;
                    for (int i = 0; i < mWidth*mHeight; i++)
                    {
                        AverageIteration+=Buffer[i].IterationCount;
                    }
                    AverageIteration /= mWidth * mHeight;
                }
            }
            Time += 0.1f;
            //ColorOffset += 0.01f;
            GL.UseProgram(DisplayProgramId); 
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "QuaternionJulia"), QuaternionJulia ? 1 : 0);
            GL.ActiveTexture(TextureUnit.Texture0);

            //GL.BindBuffer(BufferTarget.ShaderStorageBuffer, IntermediateShaderBuffer);
            //int id = GL.GetProgramResourceIndex(DisplayProgramId, ProgramInterface.ShaderStorageBlock, "DataBlock");
            //GL.ShaderStorageBlockBinding(DisplayProgramId, id, IntermediateShaderBuffer);
            //GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, id, IntermediateShaderBuffer);
            //GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            //if((Time*0.3)%1<0.5)
            //GL.BindTexture(TextureTarget.Texture2D, ReverseTexHandle);
            //else
            GL.BindTexture(TextureTarget.Texture2D, IntermediateTexHandle);//keep this even if it looks like it isnt needed
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "sourceTex"), 0);

            //GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "reverseTex"), ReverseTexHandle);

            PrepareDrawShader(mWidth,mHeight,projectionMatrix);
            BuddhaReset = false;
            //GL.DispatchCompute(mWidth / GroupSize + 1, mHeight / GroupSize + 1, 1);


            //GL.BindTexture(TextureTarget.Texture2D, ImageTexHandle);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            if (MeshActive)
            {
                GL.UniformMatrix4(GL.GetUniformLocation(DisplayProgramId, "projectionMatrix"), false, ref camera3D.ProjectMatrix);
                GL.UniformMatrix4(GL.GetUniformLocation(DisplayProgramId, "viewMatrix"), false, ref camera3D.ViewMatrix);
                GL.UniformMatrix4(GL.GetUniformLocation(DisplayProgramId, "modelMatrix"), false, ref modelMatrix); 
                GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "MeshMode"), 1);
                //GL.UseProgram(0);
                Render3d();
            }
            else
            {
                GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "MeshMode"), 0);
                GL.Color3(1.0, 1.0, 1.0);
                GL.Enable(EnableCap.Texture2D);

                //Console.WriteLine("Draw rendering quad at time: {0}", Main.stopwatch.Elapsed.TotalMilliseconds);
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
            Draw2();
        }
        public void Draw2()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ReverseFramebuffer);

            GL.UseProgram(BackCopyProgramId);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, IntermediateTexHandle);
            GL.Uniform1(GL.GetUniformLocation(BackCopyProgramId, "sourceTex"), 0);

            GL.UniformMatrix4(GL.GetUniformLocation(BackCopyProgramId, "projectionMatrix"), false, ref projectionMatrix);
            GL.Uniform2(GL.GetUniformLocation(BackCopyProgramId, "resolution"), mWidth, mHeight);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, IntermediateShaderBuffer);
            int id = GL.GetProgramResourceIndex(BackCopyProgramId, ProgramInterface.ShaderStorageBlock, "DataBlock");
            GL.ShaderStorageBlockBinding(BackCopyProgramId, id, IntermediateShaderBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, id, IntermediateShaderBuffer);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ReverseShaderBuffer);
            id = GL.GetProgramResourceIndex(BackCopyProgramId, ProgramInterface.ShaderStorageBlock, "NewDataBlock");
            GL.ShaderStorageBlockBinding(BackCopyProgramId, id, ReverseShaderBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, id, ReverseShaderBuffer);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            GL.Enable(EnableCap.Texture2D);
            GL.Color3(0, 0, 0);
            if (UpdateBackgroundTexture)
            {
                GL.Begin(PrimitiveType.Quads);
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(0f, 0f);
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(mWidth, 0f);
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(mWidth, mHeight);
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(0f, mHeight);
                GL.End();
            }
            GL.Disable(EnableCap.Texture2D);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.UseProgram(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        public void SetMeshActive(bool Active)
        {
            if (MeshActive != Active)
            {
                MeshActive = Active;
                if (MeshActive)
                {
                    Size temp = WindowResolution;
                    Resize(MeshResolution,MeshResolution);
                    WindowResolution = temp;
                    
                }
                else
                {
                    Resize(WindowResolution.Width, WindowResolution.Height);
                }
                Compute();
            }
        }
        void Render3d()
        {
            float realWidth = mWidth;
            float realHeight = mHeight;
            double C1 = Math.Cos(CameraAngleX);
            double S1 = Math.Sin(CameraAngleX);
            double C2 = Math.Cos(CameraAngleY);
            double S2 = Math.Sin(CameraAngleY);
            camera3D.CameraVector = new Vector3((float)(C2 * S1), (float)(C2* C1), -(float)(S2))*500* (float)CameraDistance;
            camera3D.CameraCenter = new Vector3(mWidth/2, mHeight/2, 0);
            camera3D.Prepare3d();
            
            GL.Color3(1.0, 1.0, 1.0);
            /*GL.Enable(EnableCap.Texture2D);
            
            GL.Begin(PrimitiveType.Quads);
            
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0f, 0f,0);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(realWidth, 0f,0);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(realWidth, realHeight,0);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0f, realHeight,0);
            GL.End();*/

            /*GL.Disable(EnableCap.Texture2D);
            GL.Color3(1.0, 0, 0);
            GL.Begin(PrimitiveType.Quads);
            GL.Vertex3(0f, 0f, 0);
            GL.Vertex3(realWidth, 0f, realHeight / 2);
            GL.Vertex3(realWidth, realHeight, 0);
            GL.Vertex3(0f, realHeight, realHeight/2);
            GL.End();*/


            //fractalWindow.DrawCuboid(new Vector3(0,0,0), new Vector3(realWidth, realHeight, realHeight / 5), Color.White);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexArray);
            GL.Color3(1.0f, 1.0f, 1.0f);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer);
            GL.VertexPointer(3, VertexPointerType.Float, sizeof(float)*3, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer);

            GL.DrawElements(BeginMode.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.DisableClientState(ArrayCap.VertexArray);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.UseProgram(0);

            camera3D.End3d();
        }
        void PrepareDrawShader(int width, int height, Matrix4 project)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, IntermediateShaderBuffer);
            int id = GL.GetProgramResourceIndex(BackCopyProgramId, ProgramInterface.ShaderStorageBlock, "DataBlock");
            GL.ShaderStorageBlockBinding(BackCopyProgramId, id, IntermediateShaderBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, id, IntermediateShaderBuffer);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            GL.UniformMatrix4(GL.GetUniformLocation(DisplayProgramId, "projectionMatrix"), false, ref project);
            Matrix4 I = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(DisplayProgramId, "viewMatrix"), false, ref I);
            GL.UniformMatrix4(GL.GetUniformLocation(DisplayProgramId, "modelMatrix"), false, ref I);
            GL.Uniform2(GL.GetUniformLocation(DisplayProgramId, "resolution"), mWidth, mHeight); 
            GL.Uniform2(GL.GetUniformLocation(DisplayProgramId, "outputResolution"), width, height);

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
            float[] ColorArr = new float[ColorPalette.Length * 4];
            for (int i = 0; i < ColorPalette.Length; i++)
            {
                ColorArr[4 * i] = PalettePositions[i];
                ColorArr[4 * i + 1] = ColorPalette[i].R;
                ColorArr[4 * i + 2] = ColorPalette[i].G;
                ColorArr[4 * i + 3] = ColorPalette[i].B;
            }
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "ColorData"), ColorArr.Length, ColorArr);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "PalleteSize"), PaletteSize);
            GL.Uniform4(GL.GetUniformLocation(DisplayProgramId, "InteriorColor"), InteriorColor);

            Vector2 V = new Vector2((float)IterationPoint.real, (float)IterationPoint.imag);
            GL.Uniform2(GL.GetUniformLocation(DisplayProgramId, "IterationPoint"), ref V);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "CenterDotStrength"), CenterDotStrength);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "FinalDotStrength"), FinalDotStrength);
            GL.Uniform2(GL.GetUniformLocation(DisplayProgramId, "FinalDotPosition"), FinalDotPosition.real, FinalDotPosition.imag);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "DistanceEstimateColoringLerp"), (float)DistanceEstimateColoringLerp);
            GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "BuddhaActive"), canUseBuddhaMode && buddhaActive ? 1 : 0);
            if (canUseBuddhaMode && buddhaActive)
            {
                GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "BuddhaTexR"), buddhaController.colorTexR);
                GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "BuddhaTexG"), buddhaController.colorTexG);
                GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "BuddhaTexB"), buddhaController.colorTexB);
                GL.Uniform1(GL.GetUniformLocation(DisplayProgramId, "BuddhaReset"), BuddhaReset ? 1 : 0);
            }
        }
        public void DrawExternal(int width,int height,Matrix4 project)
        {
            GL.UseProgram(DisplayProgramId);
            PrepareDrawShader(width,height,project);

            GL.Color3(1.0, 1.0, 1.0);
            //GL.Enable(EnableCap.Texture2D);

            //Console.WriteLine("Draw rendering quad at time: {0}", Main.stopwatch.Elapsed.TotalMilliseconds);
            GL.Begin(PrimitiveType.Quads);
            float realWidth = width;
            float realHeight = height;
            //GL.TexCoord2(0.0f, 0.0f);
            GL.Vertex2(0f, 0f);

            //GL.TexCoord2(10.0f, 0.0f);
            GL.Vertex2(realWidth, 0f);
            //GL.TexCoord2(1.0f, 1.0f);
            GL.Vertex2(realWidth, realHeight);
            //GL.TexCoord2(0.0f, 1.0f);
            GL.Vertex2(0f, realHeight);
            GL.End();

           // GL.Disable(EnableCap.Texture2D);
            GL.UseProgram(0);
        }
        public void SetupBuddhaController()
        {
            buddhaController = new BuddhaShaderController(this);
            canUseBuddhaMode = true;
        }
        public int GenerateTex(string Name)
        {
            
            int texHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texHandle);
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, mWidth, mHeight, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
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
        int GenerateShaderBuffer(int Size)
        {
            int BufferIndex = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, BufferIndex);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, Size, IntPtr.Zero, BufferUsageHint.DynamicCopy);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, BufferIndex, BufferIndex);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            return BufferIndex;

        }
        void GetBufferValues(int BufferIndex, DataStruct[] Arr)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, BufferIndex);
            unsafe
            {
                fixed (DataStruct* aaPtr = Arr)
                {
                    IntPtr Point = new IntPtr(aaPtr);
                    GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, Arr.Length * sizeof(DataStruct), Point);
                }
            }
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        public void Resize(int w,int h)
        {
            mWidth = w;
            mHeight = h;
            WindowResolution = new Size(w, h);
            Buffer = new DataStruct[mWidth * mHeight];
            int BufferSize = mWidth * mHeight * (sizeof(double) * 4 + sizeof(double) * 2);
            GL.DeleteTexture(ImageTexHandle);
            GL.DeleteTexture(IntermediateTexHandle);
            GL.DeleteTexture(ReverseTexHandle);
            GL.DeleteBuffer(ReverseShaderBuffer);
            GL.DeleteBuffer(IntermediateShaderBuffer);
            ReverseShaderBuffer = GenerateShaderBuffer(BufferSize);
            IntermediateShaderBuffer = GenerateShaderBuffer(BufferSize);
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
