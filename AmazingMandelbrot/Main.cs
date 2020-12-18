using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;
using AmazingMandelbrot.GuiComponents;
using System.Diagnostics;
using OpenTK.Graphics;
using System.Drawing.Imaging;

namespace AmazingMandelbrot
{
    
    class Main
    {
        public Size Size;
        public Matrix4 projectionMatrix;
        public GuiHandler GuiHandler;
        Slider IterationSlider;
        TextDisplay IterationDisplay;
        FractalWindow MainFractalWindow;
        //FractalWindow MinibrotButton;
        FractalWindow ColorMenuButton;
        //EmptyComponent MinibrotContainer;
        EmptyComponent RotationCompass;
        FractalWindow ScreenshotButton;
        FractalWindow AutoZoomButton;
        EmptyComponent ExampleButton;
        ExampleLocationComponent ExampleLocationComponent;
        bool EnableMinibrots;
        int CurrentMinibrotOrder;
        List<FractalWindow> MinibrotWindows = new List<FractalWindow>();
        MinibrotFinder MinibrotFinder = new MinibrotFinder();
        TextDisplay TextDisplay;
        Font Font = new Font("Arial Black", 10);
        StringFormat SF = new StringFormat();
        List<MinibrotFinder.MinibrotInfo> AllMinibrots=new List<MinibrotFinder.MinibrotInfo>();
        const int MinimumMinibrotAmount = 10;
        const int MinibrotWindowSize=120;
        Stopwatch stopwatch = new Stopwatch();
        double T = 0;
        Complex[,] MainCoefficientArray;
        Complex[,] OldCoefficientArray;
        Complex[,] DifferenceCoefficientArray;
        TextDisplay PolynomialTextDisplay;
        PolynomialParser polynomialParser=new PolynomialParser();
        Timer ErrorTimer = new Timer(50);
        int TextCursorTimer;
        public static double PolynomialAnimationTimer = 1;
        MainCursorSystem CursorSystem;
        int ResizeTimer = 0;
        ColorEditor ColorEditor;
        FractalMath fractalMath = new FractalMath();
        bool CompassActive = false;
        bool CompassHold = false;
        Vector2 CompassStart;
        Vector2 CompassEnd;
        int CompassResetTimer = 0;
        const int CompassResetTimerMax = 20;
        const float CompassRad = 0.25f;
        double CompassResetAngle;
        AutoZoomController AutoZoomController=new AutoZoomController();
        float ScreenshotActive = 0;
        FileController fileController;
        public Main(Size Size)
        {
            SetOrthographicProjection(0, 0, Size.Width, Size.Height);
            
            SF.Alignment = StringAlignment.Center;
            SF.LineAlignment = StringAlignment.Center;
            this.Size = Size;
            GuiHandler = new GuiHandler();
            fileController = new FileController();
            MainFractalWindow = new GuiComponents.FractalWindow(new Rectangle(0, 0, Size.Width, Size.Height));
            MainFractalWindow.LateDraw += MainDrawLate;
            GuiHandler.Elements.Add(MainFractalWindow);
            int Y = 10;
            PolynomialTextDisplay = new TextDisplay(new RectangleF(10, Y, 300, 40));
            GuiHandler.Elements.Add(PolynomialTextDisplay);

            IterationDisplay = new GuiComponents.TextDisplay(new Rectangle(10, Y += 50, 200, 30));
            IterationSlider = new GuiComponents.Slider(new Rectangle(10, Y+=40, 200, 30));
            IterationSlider.Value = 0.5;

            GuiHandler.Elements.Add(IterationSlider);
            GuiHandler.Elements.Add(IterationDisplay);


            ColorMenuButton = new FractalWindow(new Rectangle(10, Y += 40, 70, 70));
            ColorMenuButton.Controller.CameraPos = new Complex(-0.5,0);
            ColorMenuButton.Controller.Zoom = 1.5;
            ColorMenuButton.Controller.ColorScale = 20;
            ColorMenuButton.EnableInteraction = false;
            //ColorMenuButton.Controller.ColorPalette = new Color4[] {Color4.Red,Color4.Green,Color4.Blue };
            ColorMenuButton.MouseDownEvent += ColorMenuButtonClicked;
            ColorMenuButton.HoverEvent += ColorMenuButtonHover;
            GuiHandler.Elements.Add(ColorMenuButton);

            RotationCompass = new EmptyComponent(new Rectangle(10, Y += 80, 70, 70));
            RotationCompass.LateDraw += CompassDrawLate;
            RotationCompass.MouseDownEvent += CompassClicked;
            GuiHandler.Elements.Add(RotationCompass);

            AutoZoomButton = new FractalWindow(new Rectangle(10, Y += 80, 70, 70));
            AutoZoomButton.MouseDownEvent += AutoZoomClicked;
            AutoZoomButton.LateDraw += AutoZoomDrawLate;
            AutoZoomButton.Controller.CameraPos = new Complex(-1.20635, -0.3161);
            AutoZoomButton.Controller.Zoom = 0.0012;
            AutoZoomButton.Controller.CenterDotStrength = 0.5f;
            AutoZoomButton.EnableInteraction = false;
            GuiHandler.Elements.Add(AutoZoomButton);

            ScreenshotButton = new FractalWindow(new Rectangle(10, Y += 80, 70, 70));
            ScreenshotButton.MouseDownEvent += ScreenshotButtonClicked;
            ScreenshotButton.LateDraw += ScreenshotButtonDrawLate;
            ScreenshotButton.Controller.CameraPos = new Complex( -1.20653,-0.316380400581047);
            ScreenshotButton.Controller.Zoom = 0.0015;
            ScreenshotButton.Controller.CenterDotStrength = 0.5f;
            ScreenshotButton.EnableInteraction = false;
            GuiHandler.Elements.Add(ScreenshotButton);

            DrawIterationDisplay();
            
            TextDisplay = new TextDisplay(new RectangleF(10, Y+=80, 100, 30));
            //GuiHandler.Elements.Add(TextDisplay);
            int H = MinibrotWindowSize + 10;
            //MinibrotContainer = new EmptyComponent(new RectangleF(10, Size.Height - H-10, Size.Width-20,H));
            //GuiHandler.Elements.Add(MinibrotContainer);
            //MinibrotContainer.Enabled = false;
            
            UpdatePolynomialTextDisplay();

            MainCoefficientArray = polynomialParser.CoefficientArray;
            MainFractalWindow.Controller.CoefficientArray = MainCoefficientArray;
            MainFractalWindow.DragEvent += MainDrag;
            CursorSystem = new MainCursorSystem(MainFractalWindow);
            MainFractalWindow.MouseDownEvent += MainWindowClick;
            MainFractalWindow.MouseUpEvent += MainRelease;

            CursorSystem.JuliaWindow.Enabled = true;
            for (int i = 0; i < CursorSystem.juliaController.AllButtons.Length; i++)
            {
                CursorSystem.juliaController.AllButtons[i].Enabled = true;
            }

            GuiHandler.PrepareAll(this);
            
            CursorSystem.ComputeAll();
            AutoZoomButton.Controller.Compute();
            ScreenshotButton.Controller.Compute();
            MainFractalWindow.Controller.Compute();
            ColorMenuButton.Controller.Compute();
            for (int i = 0; i < CursorSystem.juliaController.AllButtons.Length; i++)
            {
                CursorSystem.juliaController.AllButtons[i].Enabled = false;
            }
            CursorSystem.JuliaWindow.Enabled = false;
            
            ColorEditor = new ColorEditor(new RectangleF(250,10,380,150));
            GuiHandler.Elements.Add(ColorEditor);
            ColorEditor.Enabled = false;
            ColorEditor.fractalWindows.Add(MainFractalWindow);
            ColorEditor.fractalWindows.Add(CursorSystem.JuliaWindow);
            ColorEditor.UpdateFractalWindows();
            
            AutoZoomController.MainWindow = MainFractalWindow;
            AutoZoomController.JuliaWindow = CursorSystem.JuliaWindow;
            AutoZoomController.ColorEditor = ColorEditor;
        }
        public void Update()
        {
            if(CompassResetTimer>0)
            {
                CompassResetTimer--;
                double A = CompassResetTimer / (double)CompassResetTimerMax;
                A = Beizer(A);
                MainFractalWindow.Controller.Angle = (float)(CompassResetAngle*A);
                CursorSystem.JuliaWindow.Controller.Angle = MainFractalWindow.Controller.Angle;
                if(CursorSystem.JuliaActive)
                {
                    CursorSystem.JuliaWindow.Controller.Compute(false);
                }
                MainFractalWindow.Controller.Compute(false);
            }
            
            AutoZoomController.Update();
            if (!ColorEditor.Enabled)
                ColorEditor.Update();
            fractalMath.CoefficientArray = MainCoefficientArray;
            fractalMath.PolynomialConstants=fractalMath.GetCoefficients(CursorSystem.mainController.CursorWorldPosition,MainCoefficientArray);
            Complex Z = new Complex();
            for (int i = 0; i < 40; i++)
            {
                Z = fractalMath.Compute(Z);
            }
            MainFractalWindow.Controller.IterationPoint = Z;

            T += 0.02;
            MainFractalWindow.Controller.CenterDotStrength = (float)ColorEditor.DotStrengthSlider.Value;
            CursorSystem.JuliaWindow.Controller.CenterDotStrength = (float)ColorEditor.DotStrengthSlider.Value;

            //MainFractalWindow.Controller.ColorOffset = (MainFractalWindow.Controller.ColorOffset + 0.01f)%1;
            //JuliaWindow.Controller.JuliaPos.real = Math.Cos(T)*0.01-1.01;
            //JuliaWindow.Controller.JuliaPos.imag = 0;
            //JuliaWindow.Controller.Compute();


            //MainFractalWindow.Controller.Compute();

            if (IterationSlider.HoldingHandle)
            {
                double A = 2*(IterationSlider.Value - 0.5);
                MainFractalWindow.Controller.Iterations += (int)(10*A*A*Math.Sign(A));
                if (MainFractalWindow.Controller.Iterations < 10)
                    MainFractalWindow.Controller.Iterations = 10;
                DrawIterationDisplay();
                
            }
            else if(IterationSlider.Value != 0.5)
            {
                MainFractalWindow.Controller.Compute();
                IterationSlider.Value = 0.5;
            }
            
            Complex Corner1 = MainFractalWindow.GetWorldFromScreen(new Vector2(0, 0));
            Complex Corner2 = MainFractalWindow.GetWorldFromScreen(new Vector2(Size.Width, Size.Height));
            
            int GetMinibrots = MinibrotFinder.GetRootsInRegion(Corner1, Corner2);
            //TextDisplay.PrepareWrite();
            //TextDisplay.gfx.DrawString(String.Format("Roots:{0}", GetMinibrots), Font, Brushes.Black, TextDisplay.Rect.Width / 2, TextDisplay.Rect.Height / 2, SF);
            //TextDisplay.PrepareDraw();
            /*if(EnableMinibrots)
            {
                UpdateMinibrots();
            }*/
            UpdatePolynomialTextDisplay();
            if(PolynomialAnimationTimer<1)
            {
                
                int MaxZ = Math.Max(polynomialParser.CoefficientArray.GetLength(0), OldCoefficientArray.GetLength(0));
                int MaxC = Math.Max(polynomialParser.CoefficientArray.GetLength(1), OldCoefficientArray.GetLength(1));
                
                DifferenceCoefficientArray = new Complex[MaxZ, MaxC];
                for (int i = 0; i < MaxZ; i++)
                {
                    for (int j = 0; j < MaxC; j++)
                    {
                        if (i >= polynomialParser.CoefficientArray.GetLength(0) || j >= polynomialParser.CoefficientArray.GetLength(1))
                        {
                            DifferenceCoefficientArray[i, j] = -OldCoefficientArray[i, j];
                        }
                        else if (i >= OldCoefficientArray.GetLength(0) || j >= OldCoefficientArray.GetLength(1))
                        {
                            DifferenceCoefficientArray[i, j] = polynomialParser.CoefficientArray[i, j];
                        }
                        else
                        {
                            DifferenceCoefficientArray[i, j] = polynomialParser.CoefficientArray[i, j] - OldCoefficientArray[i, j];
                        }
                    }
                }
                int FollowSteps = 10;
                int FollowIter = 30;
                Complex Divergence;
                for (int L = 0; L < FollowSteps; L++)
                {
                    double DeltaLerp = Beizer(PolynomialAnimationTimer);
                    PolynomialAnimationTimer += 0.015/ FollowSteps;
                    double Lerp = Beizer(PolynomialAnimationTimer);
                    DeltaLerp = Lerp - DeltaLerp;
                    double LerpNew = Lerp;
                    double LerpOld = 1 - Lerp;


                    if (MainFractalWindow.Controller.Zoom < 1)
                    {
                        MainFractalWindow.Controller.CameraPos = fractalMath.FollowFractalFlow(MainCoefficientArray, DifferenceCoefficientArray, DeltaLerp, MainFractalWindow.Controller.CameraPos, FollowIter, out Divergence);
                        double D = Math.Min(Math.Max(Divergence.real, -5), 5);

                        MainFractalWindow.Controller.Zoom *= Math.Exp(D * DeltaLerp);
                        MainFractalWindow.Controller.Angle -= (float)(Divergence.imag * DeltaLerp);
                    }
                    if (MainFractalWindow.Controller.CameraPos.MagSq() > 10 * 10)
                    {
                        MainFractalWindow.Controller.CameraPos = new Complex();
                        MainFractalWindow.Controller.Zoom = 2;
                    }
                    MainCoefficientArray = new Complex[MaxZ, MaxC];
                    for (int i = 0; i < polynomialParser.CoefficientArray.GetLength(0); i++)
                    {
                        for (int j = 0; j < polynomialParser.CoefficientArray.GetLength(1); j++)
                        {
                            MainCoefficientArray[i, j] += polynomialParser.CoefficientArray[i, j] * LerpNew;
                        }
                    }
                    for (int i = 0; i < OldCoefficientArray.GetLength(0); i++)
                    {
                        for (int j = 0; j < OldCoefficientArray.GetLength(1); j++)
                        {
                            MainCoefficientArray[i, j] += OldCoefficientArray[i, j] * LerpOld;
                        }
                    }
                }
                

                if (PolynomialAnimationTimer >= 1)
                    MainCoefficientArray = polynomialParser.CoefficientArray;
                MainFractalWindow.Controller.CoefficientArray = MainCoefficientArray;
                MainFractalWindow.Controller.Compute();
                
                CursorSystem.JuliaWindow.Controller.CoefficientArray = MainCoefficientArray;
                //CursorSystem.Julia3dButton.Controller.CoefficientArray = MainCoefficientArray;
                //CursorSystem.Julia3dCutoffButton.Controller.CoefficientArray = MainCoefficientArray;
                if (CursorSystem.JuliaActive)
                {
                    CursorSystem.UpdateJulia();
                }
            }
            else
            {
                OldCoefficientArray = MainCoefficientArray;
                //MainCoefficientArray = polynomialParser.CoefficientArray;
                //MainFractalWindow.Controller.CoefficientArray = MainCoefficientArray;
                //JuliaWindow.Controller.CoefficientArray = MainCoefficientArray;
                //MainFractalWindow.Controller.Compute();

            }
            CursorSystem.Update();
            
            ErrorTimer.Update();
        }
        public double Beizer(double X)
        {
            return X * X * (3 - X * 2);
        }
        public void Draw(PointF MousePos, MouseButtons MouseButtons, bool IsFocused)
        {
            
            SetOrthographicProjection(0, 0, Size.Width, Size.Height);
            GuiHandler.UpdateCurrentElement();
            if (IsFocused)
                GuiHandler.UpdateMouse(MousePos, MouseButtons);
            GuiHandler.ShowAll(this);
            
        }
        public void MainDrawLate(GuiElement Sender, Main M)
        {
            if (EnableMinibrots)
            {
                for (int i = 0; i < AllMinibrots.Count; i++)
                {
                    Complex Pos = AllMinibrots[i].Pos;
                    Vector2 P = MainFractalWindow.GetScreenFromWorld(Pos);
                    GL.Color3(Color.White);
                    GL.Rect(P.X - 5, P.Y - 5, P.X + 5, P.Y + 5);
                    Vector2 P2 = MainFractalWindow.GetScreenFromWorld(AllMinibrots[i].Cusp);
                    GL.Color3(Color.Orange);
                    GL.Rect(P2.X - 5, P2.Y - 5, P2.X + 5, P2.Y + 5);
                }
            }
            /*Complex input = CursorSystem.CursorWorldPosition;
            Vector2 A = MainFractalWindow.GetScreenFromWorld(input);
            Complex Output = fractalMath.GetFractalFlow(MainCoefficientArray, DifferenceCoefficientArray, input, 20);
            Vector2 B = MainFractalWindow.GetScreenFromWorld(input + Output * 0.1f * MainFractalWindow.Controller.Zoom);
            GL.Rect(A.X - 3, A.Y - 3, A.X + 3, A.Y + 3);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex2(A);
            GL.Vertex2(B);
            GL.End();*/
            if (PolynomialAnimationTimer < 0)
            {
                int dx = 120;
                GL.LineWidth(2);
                
                
                for (int i = dx/2; i < MainFractalWindow.Rect.Width; i+= dx)
                {
                    for (int j = dx / 2; j < MainFractalWindow.Rect.Height; j+= dx)
                    {
                        Vector2 A = new Vector2(i, j);
                        Complex input = MainFractalWindow.GetWorldFromScreen(A);
                        Complex Output = fractalMath.GetFractalFlow(MainCoefficientArray, DifferenceCoefficientArray, input, 3);
                        //if(Output.MagSq()>0.4* 0.4)
                        //{
                        //    Output *= 0.4 / Math.Sqrt(Output.MagSq());
                        //}
                        Vector2 B = MainFractalWindow.GetScreenFromWorld(input+Output * 0.1f*MainFractalWindow.Controller.Zoom);
                        GL.Rect(A.X-3, A.Y - 3, A.X + 3, A.Y + 3);
                        
                        GL.Color3(Color.Blue);
                        GL.Begin(PrimitiveType.Lines);
                        GL.Vertex2(A);
                        for (double t = 0; t < 0.5; t+=0.02)
                        {
                            Output = fractalMath.FollowFractalFlow(MainCoefficientArray, DifferenceCoefficientArray,t, input, 3, out Complex Divergence);
                            Vector2 C = MainFractalWindow.GetScreenFromWorld(Output);
                            GL.Vertex2(C);
                            GL.Vertex2(C);
                        }
                        GL.End();
                        GL.Color3(Color.White);
                        GL.Begin(PrimitiveType.Lines);
                        GL.Vertex2(A);
                        GL.Vertex2(B);
                        GL.End();
                        
                    }
                }
                
                
            }
            if(CompassActive)
            {
                GL.PushMatrix();
                double Rad = MainFractalWindow.Rect.Height * CompassRad;
                double thickness = 20;
                GL.Translate(MainFractalWindow.Rect.Width / 2, MainFractalWindow.Rect.Height / 2,0);
                int n = 40;
                if (CompassHold)
                {
                    double angleStart = Math.Atan2(CompassStart.Y, CompassStart.X);
                    double angleEnd = Math.Atan2(CompassEnd.Y, CompassEnd.X);
                    double angleOffset = angleEnd - angleStart;
                    angleOffset = (Math.PI * 3 + angleOffset) % (Math.PI * 2) - Math.PI;
                    GL.Begin(PrimitiveType.TriangleStrip);
                    GL.Color4(0.7, 0.7, 0.7, 0.8);
                    for (int i = 0; i <= n; i++)
                    {
                        double angle = (angleOffset * i) / n;
                        Vector2d V1 = new Vector2d(Math.Cos(angle + angleStart), Math.Sin(angle + angleStart));
                        GL.Vertex2(V1 * (Rad - thickness * 1.5));
                        GL.Vertex2(V1 * (Rad - thickness*2));
                    }
                    GL.End();
                }
                GL.PushMatrix();
                GL.Rotate(MainFractalWindow.Controller.Angle * 180 / Math.PI, 0, 0, 1);
                GL.Begin(PrimitiveType.TriangleStrip);
                GL.Color4(0.4,0.4,0.4,0.8);
                
                for (int i = 0; i <= n; i++)
                {
                    double angle = (2*Math.PI * i) / n;
                    Vector2d V1 = new Vector2d(Math.Cos(angle), Math.Sin(angle));
                    GL.Vertex2(V1*Rad);
                    GL.Vertex2(V1 * (Rad- thickness));
                }
                GL.End();
                Vector3d[] Colors = new Vector3d[] {
                    new Vector3d(1.0,0.0,0.0),
                new Vector3d(1.0, 0.6, 0.0),
                new Vector3d(0.2, 0.2,1.0),
                new Vector3d(1.0, 0.6, 0.0)};
                Vector2d Vx = new Vector2d(1,0);
                Vector2d Vy = new Vector2d(0, 1);
                GL.Begin(PrimitiveType.Triangles);
                for (int i = 0; i < 4; i++)
                {
                    GL.Color3(Colors[i].X, Colors[i].Y, Colors[i].Z);
                    GL.Vertex2(Vx* Rad);
                    GL.Vertex2(Vx * Rad + Vy * thickness - Vx * thickness);
                    GL.Vertex2(Vx * Rad - Vy * thickness - Vx * thickness);
                    Vector2d temp = Vy;
                    Vy = Vx;
                    Vx = -temp;
                }
                GL.End();
                GL.PopMatrix();
                GL.Color3(Colors[0].X, Colors[0].Y, Colors[0].Z);
                GL.Begin(PrimitiveType.Triangles);
                GL.Vertex2(Vx * Rad);
                GL.Vertex2(Vx * Rad + Vy * thickness + Vx * thickness);
                GL.Vertex2(Vx * Rad - Vy * thickness + Vx * thickness);
                GL.End();
                GL.PopMatrix();
            }
            if(ScreenshotActive>0)
            {
                if (ScreenshotActive == 1)
                {
                    var bmp = RenderToBitmap();
                    fileController.SaveBitmap(bmp);
                    Clipboard.SetImage(bmp);
                }
                ScreenshotActive -= 0.05f;
            }
        }
        public void CompassDrawLate(GuiElement Sender, Main M)
        {
            double angle = MainFractalWindow.Controller.Angle*180/Math.PI;
            double radius = RotationCompass.Rect.Width/2;
            GL.Translate(radius, radius, 0);

            GL.Rotate(angle, 0,0,1);
            double innerRadius = 0.2 * radius;
            double longSpokes = 0.9 * radius;
            double shortSpokes = 0.6 * radius;
            GL.Begin(PrimitiveType.Triangles);

            GL.Color3(1.0,0.0,0.0);
            GL.Vertex2(0, innerRadius);
            GL.Vertex2(0, -innerRadius);
            GL.Vertex2(longSpokes, 0);

            GL.Color3(0.2, 0.2,1.0);
            GL.Vertex2(0, innerRadius);
            GL.Vertex2(0, -innerRadius);
            GL.Vertex2(-longSpokes, 0);

            GL.End();
            GL.Begin(PrimitiveType.Quads);
            GL.Color3(1.0, 0.6, 0.0);

            GL.Vertex2(innerRadius, 0);
            GL.Vertex2(0, shortSpokes);
            GL.Vertex2(-innerRadius, 0);
            GL.Vertex2(0, -shortSpokes);

            GL.Color3(0.3, 0.3, 0.3);
            GL.Vertex2(innerRadius, 0);
            GL.Vertex2(0, innerRadius);
            GL.Vertex2(-innerRadius, 0);
            GL.Vertex2(0, -innerRadius);

            GL.End();
        }
        void AutoZoomDrawLate(GuiElement Sender, Main M)
        {
            float w = AutoZoomButton.Rect.Width;
            float Rad = w/4;
            int n = 20;
            GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            GL.Color4(1.0, 1.0, 1.0, 0.15);
            GL.Rect(0,0,w,w);
            Vector2d Offset = new Vector2d(0.4,0.4) * w;
            GL.Enable(EnableCap.StencilTest);
            GL.StencilMask(~0);
            GL.ClearStencil(0);
            GL.Clear(ClearBufferMask.StencilBufferBit);
            GL.StencilFunc(StencilFunction.Always, 1, ~0);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.ColorMask(false, false, false, false);
            GL.DepthMask(false);
            GL.Begin(PrimitiveType.Polygon);
            for (int i = 0; i < n; i++)
            {
                double angle = (2 * Math.PI * i) / n;
                Vector2d V1 = new Vector2d(Math.Cos(angle), Math.Sin(angle));
                GL.Vertex2(Offset+V1 * Rad);
            }
            GL.End();

            GL.ColorMask(true, true, true, true);
            GL.DepthMask(true);
            GL.StencilFunc(StencilFunction.Notequal, 1, ~0);
            GL.StencilMask(0);

            GL.Color3(AutoZoomButton.BackgroundColor);
            GL.Rect(0, 0,w , w);
            GL.Color3(Color.LightGray);
            GL.Begin(PrimitiveType.Polygon);
            for (int i = 0; i < n; i++)
            {
                double angle = (2 * Math.PI * i) / n;
                Vector2d V1 = new Vector2d(Math.Cos(angle), Math.Sin(angle));
                GL.Vertex2(Offset + V1 * Rad*1.4);
            }
            GL.End();
            GL.PushMatrix();
            GL.Translate(Offset.X,Offset.Y,0);
            GL.Rotate(45, 0, 0, 1);
            float s = w * 0.07f;
            GL.Color3(Color.LightGray);
            GL.Rect(0, s, w*0.65, -s);
            GL.PopMatrix();
            
            GL.Disable(EnableCap.StencilTest);
        }
        void ScreenshotButtonDrawLate(GuiElement Sender, Main M)
        {
            GL.PushMatrix();
            float width = ScreenshotButton.Rect.Width;
            float w = width * 0.4f;
            float h = w * 0.7f;
            float Rad = h * 0.6f;
            int n = 20;
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            GL.Color4(1.0, 1.0, 1.0, 0.15);
            GL.Rect(0, 0, width, width);
            GL.Translate(width / 2, width / 2, 0);
            GL.Enable(EnableCap.StencilTest);
            GL.StencilMask(~0);
            GL.ClearStencil(0);
            GL.Clear(ClearBufferMask.StencilBufferBit);
            GL.StencilFunc(StencilFunction.Always, 1, ~0);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.ColorMask(false, false, false, false);
            GL.DepthMask(false);
            GL.Begin(PrimitiveType.Polygon);
            for (int i = 0; i < n; i++)
            {
                double angle = (2 * Math.PI * i) / n;
                Vector2d V1 = new Vector2d(Math.Cos(angle), Math.Sin(angle));
                GL.Vertex2(V1 * Rad);
            }
            GL.End();

            GL.ColorMask(true, true, true, true);
            GL.DepthMask(true);
            GL.StencilFunc(StencilFunction.Notequal, 1, ~0);
            GL.StencilMask(0);
            GL.Color3(ScreenshotButton.BackgroundColor);
            GL.Rect(-width / 2, -width / 2, width/2, width/2);

            GL.Color3(Color.LightGray);
            GL.Rect(-h * 0.5, 0, h * 0.5, -h * 1.4);
            GL.Color3(Color.White);
            GL.Rect(-h * 0.4, h, h * 0.4, -h * 1.2);
            
            GL.Color3(Color.LightGray);
            GL.Rect(-w, -h, w, h);
            GL.Color3(ScreenshotActive>0?Color.Yellow:Color.Orange);
            float a = h * 0.2f;
            GL.Rect(-w+a, -h+a, -w+3*a, -h+3*a);
            GL.Color3(Color.White);
            GL.Begin(PrimitiveType.Polygon);
            for (int i = 0; i < n; i++)
            {
                double angle = (2 * Math.PI * i) / n;
                Vector2d V1 = new Vector2d(Math.Cos(angle), Math.Sin(angle));
                GL.Vertex2(V1 * Rad*1.3f);
            }
            GL.End();

            GL.Disable(EnableCap.StencilTest);
            GL.PopMatrix();
        }
        public void CompassClicked(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            CompassActive = !CompassActive;
            MainFractalWindow.EnableInteraction = !CompassActive;
            MainFractalWindow.Controller.UseOldAngle = CompassActive;
            MainFractalWindow.Controller.OldAngle = MainFractalWindow.Controller.Angle;
            if(!CompassActive)
            {
                if (CursorSystem.JuliaActive)
                {
                    CursorSystem.JuliaWindow.Controller.Compute();
                }
                MainFractalWindow.Controller.Compute();
            }
        }
        public void AutoZoomClicked(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            AutoZoomController.Prepare();
        }
        void MainDrag(GuiElement Sender, PointF MousePos, PointF StartPos, PointF DeltaPos, MouseButtons ButtonStatus)
        {
            if(CompassActive)
            {
                Vector2 V1 = new Vector2(DeltaPos.X, DeltaPos.Y);
                Vector2 V2 = new Vector2(MousePos.X, MousePos.Y);
                Vector2 V3 = new Vector2(StartPos.X, StartPos.Y);
                
                V2 -= new Vector2(MainFractalWindow.Rect.Width, MainFractalWindow.Rect.Height) / 2;
                V3 -= new Vector2(MainFractalWindow.Rect.Width, MainFractalWindow.Rect.Height) / 2;
                CompassStart = V3;
                CompassEnd = V2;
                float Cross = V1.X*V2.Y -V1.Y * V2.X;
                float AngleChange = Cross / (V2.Length* V2.Length);
                MainFractalWindow.Controller.Angle += AngleChange;
                CursorSystem.JuliaWindow.Controller.Angle = MainFractalWindow.Controller.Angle;
                if (CursorSystem.JuliaActive)
                {
                    CursorSystem.JuliaWindow.Controller.Compute(false);
                }
                MainFractalWindow.Controller.Compute(false);
                CompassHold = true;
            }
        }
        void MainRelease(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            CompassHold = false;
        }
        public void SetOrthographicProjection(int x, int y, int w, int h)
        {
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            projectionMatrix = Matrix4.CreateOrthographicOffCenter(x, w, h, y, 1f, -1f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projectionMatrix);
        }
        void DrawIterationDisplay()
        {
            IterationDisplay.PrepareWrite();
            IterationDisplay.gfx.DrawString(String.Format("Iterations:{0}",MainFractalWindow.Controller.Iterations),Font,Brushes.Black,IterationDisplay.Rect.Width/2, IterationDisplay.Rect.Height / 2,SF);
            IterationDisplay.PrepareDraw();
        }

        /*void MinibrotButtonClicked(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            EnableMinibrots = !EnableMinibrots;
            if (EnableMinibrots)
            {
                MinibrotWindows.Clear();
                MinibrotContainer.ChildElements.Clear();
                AllMinibrots.Clear();
                CurrentMinibrotOrder = 3;
                
            }
            MinibrotContainer.Enabled = EnableMinibrots;
        }*/
        void ColorMenuButtonHover(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            ColorMenuButton.Controller.ColorOffset += 0.02f;
        }
        void ColorMenuButtonClicked(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            ColorEditor.Enabled = !ColorEditor.Enabled;
        }
        void MainWindowClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            if(CompassActive&& ButtonStatus == MouseButtons.Left)
            {
                Vector2 V1 = new Vector2(MousePos.X, MousePos.Y);
                float Rad = MainFractalWindow.Rect.Height * CompassRad;
                if ((V1- new Vector2(MainFractalWindow.Rect.Width, MainFractalWindow.Rect.Height) / 2-new Vector2(Rad+20,0)).Length<20)
                {
                    CompassResetTimer = CompassResetTimerMax;
                    CompassResetAngle = ((MainFractalWindow.Controller.Angle% (Math.PI * 2)) +Math.PI*3)%(Math.PI*2)- Math.PI;
                }
            }
        }
        void ScreenshotButtonClicked(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            ScreenshotActive = 1;
        }
        /*void UpdateMinibrots()
        {
            Complex Corner1 = MainFractalWindow.GetWorldFromScreen(new Vector2(0, 0));
            Complex Corner2 = MainFractalWindow.GetWorldFromScreen(new Vector2(Size.Width, Size.Height));
            if (AllMinibrots.Count< MinimumMinibrotAmount&& CurrentMinibrotOrder < 80)
            {
                List<MinibrotFinder.MinibrotInfo> NewMinibrots = MinibrotFinder.GetMinibrots(CurrentMinibrotOrder, Corner1, Corner2);
                foreach (var M in NewMinibrots)
                {
                    FractalWindow Window = new FractalWindow(new Rectangle(5 + AllMinibrots.Count * (MinibrotWindowSize+5), 5, MinibrotWindowSize, MinibrotWindowSize));
                    MinibrotWindows.Add(Window);
                    MinibrotContainer.ChildElements.Add(Window);
                    Window.Controller.CameraPos = M.Pos+ (M.Pos - M.Cusp);
                    Window.Controller.Zoom = 8 * Math.Sqrt((M.Pos - M.Cusp).MagSq());
                    Window.Controller.Compute();
                    AllMinibrots.Add(M);
                }
                
                CurrentMinibrotOrder++;
            }

        }*/
        public void Resize(int w, int h)
        {
            Size = new Size(w, h);
            SetOrthographicProjection(0, 0, Size.Width, Size.Height);
            GuiHandler.PrepareAll(this);
            MainFractalWindow.Resize(w, h);
            MainFractalWindow.Controller.Compute();
            MainFractalWindow.Show(this);
            PolynomialTextDisplay.Rect = new RectangleF(10, Size.Height - 50, 300, 40);
            CursorSystem.RepositionJulia();
        }
        public void TypeChar(char C)
        {
            polynomialParser.InputChar(C);
        }
        public void TypeKey(Keys K)
        {
            polynomialParser.InputKey(K);
            if(K == Keys.Enter)
            {
                if (polynomialParser.Success)
                    PolynomialAnimationTimer = 0;
                else
                {
                    ErrorTimer.Reset();
                    ErrorTimer.Start();
                    //ErrorMessageTextDisplay.Enabled = true;
                    string S = polynomialParser.ErrorMessage;
                    PolynomialTextDisplay.PrepareWrite();
                    PolynomialTextDisplay.gfx.DrawString(S, new Font("Arial Black", 12), Brushes.Blue, PolynomialTextDisplay.Rect.Width / 2, PolynomialTextDisplay.Rect.Height / 2, SF);
                    PolynomialTextDisplay.PrepareDraw();
                }
            }
            if(K==Keys.Space)
            {
                AutoZoomController.Start();
            }
        }
        void UpdatePolynomialTextDisplay()
        {
            TextCursorTimer++;
            string S = polynomialParser.InputString;
            if (TextCursorTimer>20)
            {
                if(polynomialParser.CursorPos<S.Length)
                {
                    S = S.Remove(polynomialParser.CursorPos, 1).Insert(polynomialParser.CursorPos, "_");
                }
                if(TextCursorTimer>40)
                {
                    TextCursorTimer = 0;
                }
            }
            if (!ErrorTimer.Active)
            {
                PolynomialTextDisplay.PrepareWrite();
                PolynomialTextDisplay.gfx.DrawString(S, new Font("Arial Black", 12), Brushes.Black, PolynomialTextDisplay.Rect.Width / 2, PolynomialTextDisplay.Rect.Height / 2, SF);
                PolynomialTextDisplay.PrepareDraw();
            }
        }
        public Bitmap RenderToBitmap()
        {
            Bitmap bitmap = new Bitmap(Size.Width, Size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData bData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            GL.ReadPixels(0, 0, Size.Width, Size.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bData.Scan0);
            bitmap.UnlockBits(bData);
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bitmap;
        }
        
        float Lerp(float A, float B, float T) => A * (1 - T) + T * B;
        public class Timer
        {
            public readonly int MaxTime;
            public int CurrentTime = 0;
            public bool CountingBackwards = false;
            public bool Active = false;
            public float Value = 0;
            public delegate void TimerEvent();
            public TimerEvent FinishedEvent;
            public TimerEvent Tick;
            public Timer(int MaxTime)
            {
                this.MaxTime = MaxTime;
            }
            public void Update()
            {
                if(Active)
                {
                    
                    if (CountingBackwards)
                    {
                        CurrentTime--;
                        Value = (float)CurrentTime / MaxTime;
                        Tick();
                        if (CurrentTime <= 0)
                        {
                            Finished();
                        }
                    }
                    else
                    {
                        CurrentTime++;
                        Value = (float)CurrentTime / MaxTime;
                        Tick?.Invoke();
                        if (CurrentTime >= MaxTime)
                        {
                            Finished();
                        }
                    }  
                }     
            }
            public void Start()
            {
                Active = true;
                
            }
            public void Reset()
            {
                CountingBackwards = false;
                Value = 0;
                CurrentTime = 0;
            }
            void Finished()
            {
                Active = false;
                CountingBackwards = !CountingBackwards;
                FinishedEvent?.Invoke();
            }
        }
    }
}
