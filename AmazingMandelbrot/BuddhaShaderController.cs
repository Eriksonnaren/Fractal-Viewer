using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using AmazingMandelbrot.Properties;
using System.Diagnostics;

namespace AmazingMandelbrot
{
    class BuddhaShaderController
    {
        readonly int computeShaderId;
        public readonly int colorTexR;
        public readonly int colorTexG;
        public readonly int colorTexB;
        readonly int mHeight;
        readonly int mWidth;
        readonly ShaderController parentController;
        double[] Arr;
        int BufferSize;
        int BufferIndex;
        int MaxBufferData;
        double Zoom=2;
        Vector2d RectangleSize = new Vector2d(1, 1) * 0.01f;// * 0.1f;
        //Vector2 RectanglePos = new Vector2(0);// new Vector2(-0.16f, 1.035f);
        //Vector2 RectanglePos = new Vector2(-0.16f, 1.035f);
        Vector2d RectanglePos = new Vector2d(-0.0443594f, -0.986749f);
        double RectangleScaleFactor=1;
        List<double> DrawnPoints = new List<double>();
        int MaxComputationsPerTick = (int)(6E6);
        int CurrentComputationIndex = 0;
        public int RecurseLevel = 0;
        public int MaxRecursionLevel = 0;
        double StepSize;
        public int FinalSampleDensity = 15;
        int RecursionSampleDensity = 400;
        public int CurrentFinalSample;
        float Time;
        int CurrentStage = 0;
        public static bool BlockBuddhaShader;
        int PermutationFactor;
        int PermutationCycleLength;
        public Complex[,] CoefficientArray;
        public Complex OffsetPos;
        public bool Working = false;
        public BuddhaShaderController(ShaderController parentController)
        {
            
            this.parentController = parentController;
            mWidth = parentController.mWidth;
            mHeight = parentController.mHeight;
            computeShaderId = SetupComputeProgram(Encoding.Default.GetString(Resources.BuddhaCompute));
            colorTexR = GenerateIntTex("BuddhaTexR");
            colorTexG = GenerateIntTex("BuddhaTexG");
            colorTexB = GenerateIntTex("BuddhaTexB");
            BufferIndex = GL.GenBuffer();
            SetCamera(new Complex(-0.0443594, -0.986749),0.01);
            //Start();
            CurrentComputationIndex = -100;
            PermutationFactor = GetPermutationFactor();
            PermutationCycleLength = GetPermutationCycleLength();


        }
        public void Update()
        {
            Time += 0.1f;
            //prevent it from running in the same frame as any of the other fractal computations
            //attempting to render that quad in the same frame as the atomicadds in this shader freezes the program
            //no idea why that happens, although would be nice to know that someday
            if (!BlockBuddhaShader)
            {
                if (CurrentComputationIndex < 0)
                {
                    CurrentComputationIndex++;
                }
                else
                {

                    if (CurrentComputationIndex >= MaxBufferData)
                    {
                        if (RecurseLevel > 0)
                        {
                            RectangleScaleFactor /= 2;
                            if (RectangleScaleFactor < 1)
                                RectangleScaleFactor = 1;

                            RecurseLevel--;
                            CurrentComputationIndex = 0;
                            StepSize /= 2;
                            GetBufferValues(BufferIndex);
                            EmptyBuffer();
                            RefillBuffer();
                            SetBufferValues(BufferIndex);
                        }
                        else if (CurrentFinalSample + 1 < FinalSampleDensity * FinalSampleDensity)
                        {
                            if (RecurseLevel == 0 && CurrentFinalSample == 0)
                            {
                                //Console.WriteLine(GL.GetError().ToString());
                                GetBufferValues(BufferIndex);

                                EmptyBuffer();
                                RefillBufferNoCopy();
                                SetBufferValues(BufferIndex);
                                //Console.WriteLine(GL.GetError().ToString());
                                StepSize /= FinalSampleDensity;
                                CurrentStage = 2;
                                //CountArray();
                            }
                            CurrentFinalSample++;
                            CurrentComputationIndex = 0;

                        }else
                        {
                            Working = false;
                        }

                    }
                    if (CurrentComputationIndex < MaxBufferData)
                        ExecuteGenerateShader();
                }
            }else
            {
                BlockBuddhaShader = false;
            }

            

            /*GL.UseProgram(computeShaderId);
            //int a = GL.GetUniformLocation(computeShaderId, "Clear");
            //int b = ClearTexture ? 1 : 0;
            //GL.Uniform1(a, b);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "Time"), Time);
            //ClearTexture = false;

            GL.DispatchCompute(mWidth, mHeight, 1);

            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);*/

        }
        void ExecuteGenerateShader()
        {


            GL.UseProgram(computeShaderId);
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
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "OffsetReal"), OffsetPos.real);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "OffsetImag"), OffsetPos.imag);

            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "ArrayMaxZ"), CoefficientArray.GetLength(0));
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "ArrayMaxC"), CoefficientArray.GetLength(1));
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "CoefficientArray"), Arr.Length, Arr);
            GL.Uniform2(GL.GetUniformLocation(computeShaderId, "RectangleSize"), RectangleSize.X* RectangleScaleFactor, RectangleSize.Y* RectangleScaleFactor);
            GL.Uniform2(GL.GetUniformLocation(computeShaderId, "RectanglePos"), RectanglePos.X, RectanglePos.Y);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "Zoom"), Zoom);
            GL.Uniform2(GL.GetUniformLocation(computeShaderId, "CameraPos"), RectanglePos.X, RectanglePos.Y);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "CurrentIndex"), CurrentComputationIndex);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "StepSize"), StepSize);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "FinalSampleDensity"), FinalSampleDensity);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "Stage"), CurrentStage);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "CurrentFinalSample"), CurrentFinalSample);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "MaxBufferData"), MaxBufferData);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "TexR"), colorTexR);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "TexG"), colorTexG);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "TexB"), colorTexB);
            GL.Uniform2(GL.GetUniformLocation(computeShaderId, "resolution"), new Vector2(mWidth, mHeight)); 
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "PermutationFactor"), PermutationFactor);
            GL.Uniform1(GL.GetUniformLocation(computeShaderId, "PermutationCycleLength"), PermutationCycleLength);
            int S = Math.Min(MaxComputationsPerTick, MaxBufferData - CurrentComputationIndex);
            //Console.WriteLine("Started compute shader {0} with {1} invocations", CurrentFinalSample, S);
            //Console.WriteLine("Started compute shader at time: {0}", Main.stopwatch.Elapsed.TotalMilliseconds);
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            GL.DispatchCompute((int)Math.Ceiling(S / 100.0), 1, 1);
            GL.MemoryBarrier( MemoryBarrierFlags.AllBarrierBits);
            //Console.WriteLine("Finished compute shader");
            CurrentComputationIndex += S;
            //GenerationSlice++;
            GL.UseProgram(0);


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
        //generates a texture consisting of a single 32-bit integer per pixel
        private int GenerateIntTex(string name)
        {
            int texHandle = GL.GenTexture();

            //GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texHandle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32i, mWidth, mHeight, 0, PixelFormat.RedInteger, PixelType.Int, IntPtr.Zero);
            GL.TextureStorage2D(texHandle, 1, SizedInternalFormat.R32i, mWidth, mHeight);

            GL.BindImageTexture(texHandle, texHandle, 0, true, 0, TextureAccess.ReadWrite, SizedInternalFormat.R32i);
            GL.ObjectLabel(ObjectLabelIdentifier.Texture, texHandle, name.Length, name);
            return texHandle;
        }
        int CreateShaderBuffer(int BufferIndex)
        {
            Console.WriteLine("Buffersize: " + BufferSize);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, BufferIndex);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, BufferSize * 8, IntPtr.Zero, BufferUsageHint.DynamicCopy);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 50, BufferIndex);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            return BufferIndex;

        }
        void SetBufferValues(int BufferIndex)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, BufferIndex);
            
            unsafe
            {
                fixed (double* aaPtr = Arr)
                {
                    IntPtr Point = new IntPtr(aaPtr);
                    GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, Arr.Length * 8, Point);
                }
            }
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

        }
        void GetBufferValues(int BufferIndex)
        {
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, BufferIndex);
            unsafe
            {
                fixed (double* aaPtr = Arr)
                {
                    IntPtr Point = new IntPtr(aaPtr);
                    GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, Arr.Length * 8, Point);
                }
            }
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        public void SetCamera(Complex Pos,double Zoom)
        {
            RectanglePos = new Vector2d(Pos.real,Pos.imag);
            this.Zoom = Zoom;
            RectangleSize = new Vector2d(Zoom, Zoom * mHeight / mWidth);
        }
        public void Start()
        {
            Working = true;
            DrawnPoints.Clear();
            CurrentFinalSample = 0;
            CurrentStage = 0;
            CurrentComputationIndex = 0;
            RectangleScaleFactor = Math.Max(
                Math.Max((2 - RectanglePos.X) / RectangleSize.X, (2 - RectanglePos.Y) / RectangleSize.Y),
                Math.Max((2 + RectanglePos.X) / RectangleSize.X, (2 + RectanglePos.Y) / RectangleSize.Y)
                );
            double BaseStep = 2 * RectangleSize.X / RecursionSampleDensity;
            double StepScale = 1;
            RecurseLevel = 0;
            while (StepScale < RectangleScaleFactor)
            {
                StepScale *= 2;
                RecurseLevel++;
            }
            MaxRecursionLevel = RecurseLevel;
            StepSize = BaseStep * StepScale;
            if (StepSize < 0.01)
            {
                StepSize = 0.01;
            }
            
            FillBaseArray();
            CreateShaderBuffer(BufferIndex);
            SetBufferValues(BufferIndex);

        }
        void EmptyBuffer()
        {
            for (int i = 0; i < BufferSize; i += 2)
            {
                if (Arr[i] != 0 && Arr[i + 1] != 0)
                {
                    DrawnPoints.Add(Arr[i]);
                    DrawnPoints.Add(Arr[i + 1]);
                    Arr[i] = 0;
                    Arr[i + 1] = 0;
                }
            }
        }
        void RefillBuffer()
        {

            Arr = new double[DrawnPoints.Count * 3];
            //enlarge the buffer if needed
            if (Arr.Length > BufferSize)
            {
                BufferSize = Arr.Length;
                CreateShaderBuffer(BufferIndex);
            }
            BufferSize = DrawnPoints.Count * 3;
            MaxBufferData = BufferSize / 2;
            int n = 0;
            for (int i = 0; i < DrawnPoints.Count; i += 2)
            {
                Arr[n++] = DrawnPoints[i] + StepSize;
                Arr[n++] = DrawnPoints[i + 1];
                Arr[n++] = DrawnPoints[i];
                Arr[n++] = DrawnPoints[i + 1] + StepSize;
                Arr[n++] = DrawnPoints[i] + StepSize;
                Arr[n++] = DrawnPoints[i + 1] + StepSize;
            }

        }
        void RefillBufferNoCopy()
        {
            int n = DrawnPoints.Count;
            Arr = new double[n];
            //enlarge the buffer if needed
            if (Arr.Length > BufferSize)
            {
                BufferSize = Arr.Length;
                CreateShaderBuffer(BufferIndex);
            }
            MaxBufferData = n / 2;
            BufferSize = n;
            for (int i = 0; i < n; i++)
            {
                Arr[i] = DrawnPoints[i];
            }
        }
        int GetPermutationFactor()
        {
            int N = FinalSampleDensity * FinalSampleDensity - 1;
            double phi = 1.61803398875;//golden ratio
            int a = (int)(N / phi);
            while (GCD(a, N) > 1)//decrement a until it has no common factors with N
            {
                a--;
            }
            return a;
        }
        int GetPermutationCycleLength()
        {
            int N = FinalSampleDensity * FinalSampleDensity - 1;
            int i = 0;
            int k = PermutationFactor;
            while(k!=1)
            {
                k = (k* PermutationFactor) % N;
                i++;
            }
            return i;
        }
        int GCD(int a, int b)
        {
            if (a > b)
            {
                return GCD(a - b, b);
            }
            else if (a < b)
            {
                return GCD(a, b - a);
            }
            else
                return a;
        }
        public void FillBaseArray()
        {
            int PixelWidth = (int)Math.Ceiling(4.0 / StepSize) + 1;
            MaxBufferData = PixelWidth * PixelWidth;
            BufferSize = MaxBufferData * 2;

            Arr = new double[BufferSize];
            int n = 0;
            for (double i = -2; i < 2; i += StepSize)
            {
                for (double j = -2; j < 2; j += StepSize)
                {
                    Arr[n] = i;
                    Arr[n + 1] = j;
                    n += 2;
                }
            }

        }
    }
}
