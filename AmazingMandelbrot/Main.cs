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
        FractalWindow MinibrotButton;
        EmptyComponent MinibrotContainer;
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
        TextDisplay PolynomialTextDisplay;
        TextDisplay ErrorMessageTextDisplay;
        PolynomialParser polynomialParser=new PolynomialParser();
        Timer ErrorTimer = new Timer(50);
        int TextCursorTimer;
        double PolynomialAnimationTimer = 1;
        CursorSystem CursorSystem;
        int ResizeTimer = 0;
        public Main(Size Size)
        {
            SF.Alignment = StringAlignment.Center;
            SF.LineAlignment = StringAlignment.Center;
            this.Size = Size;
            GuiHandler = new GuiHandler();
            MainFractalWindow = new GuiComponents.FractalWindow(new Rectangle(0, 0, Size.Width, Size.Height));
            MainFractalWindow.LateDraw += MainDrawLate;
            GuiHandler.Elements.Add(MainFractalWindow);
            int Y = 10;
            
            IterationDisplay = new GuiComponents.TextDisplay(new Rectangle(10, Y, 200, 30));
            IterationSlider = new GuiComponents.Slider(new Rectangle(10, Y+=40, 200, 30));
            
            GuiHandler.Elements.Add(IterationSlider);
            GuiHandler.Elements.Add(IterationDisplay);

            MinibrotButton = new FractalWindow(new Rectangle(10, Y += 40, 70, 70));
            MinibrotButton.Controller.Iterations = 1000;
            MinibrotButton.Controller.Zoom = 0.00000003;
            MinibrotButton.Controller.ColorScale = 1;
            MinibrotButton.Controller.ColorOffset = 0.9f;
            MinibrotButton.Controller.CameraPos = new Complex(-0.12681960215148277, 0.9871247652863216);
            MinibrotButton.Controller.Compute();
            MinibrotButton.EnableInteraction = false;
            MinibrotButton.MouseDownEvent += MinibrotButtonClicked;
            GuiHandler.Elements.Add(MinibrotButton);
            
            DrawIterationDisplay();
            /*MainFractalWindow.Controller.Iterations = 1500;
            MainFractalWindow.Controller.CameraPos.real = -0.12681960215148277;
            MainFractalWindow.Controller.CameraPos.imag = 0.9871247652863216;
            MainFractalWindow.Controller.Zoom = 0.00000016;*/
            
            TextDisplay = new TextDisplay(new RectangleF(10, Y+=80, 100, 30));
            //GuiHandler.Elements.Add(TextDisplay);
            int H = MinibrotWindowSize + 10;
            MinibrotContainer = new EmptyComponent(new RectangleF(10, Size.Height - H-10, Size.Width-20,H));
            GuiHandler.Elements.Add(MinibrotContainer);
            MinibrotContainer.Enabled = false;
            PolynomialTextDisplay = new TextDisplay(new RectangleF(10,Size.Height-50,300,40));
            GuiHandler.Elements.Add(PolynomialTextDisplay);

            ErrorMessageTextDisplay = new TextDisplay(new RectangleF(
                0, 
                - PolynomialTextDisplay.Rect.Height-10,
                PolynomialTextDisplay.Rect.Width, PolynomialTextDisplay.Rect.Height));
            PolynomialTextDisplay.ChildElements.Add(ErrorMessageTextDisplay);
            ErrorMessageTextDisplay.PrepareDraw();
            ErrorMessageTextDisplay.Enabled = false;
            ErrorMessageTextDisplay.DrawFrame = false;
            UpdatePolynomialTextDisplay();

            MainCoefficientArray = polynomialParser.CoefficientArray;
            MainFractalWindow.Controller.CoefficientArray = MainCoefficientArray;
            MainFractalWindow.Controller.Compute();

            CursorSystem = new CursorSystem(MainFractalWindow);
            MainFractalWindow.MouseDownEvent += MainWindowClick;

            ErrorTimer.FinishedEvent += ErrorTimerFinish;
        }
        public void Update()
        {
            T += 0.02;
            //MainFractalWindow.Controller.ColorOffset = (MainFractalWindow.Controller.ColorOffset + 0.01f)%1;
            //JuliaWindow.Controller.JuliaPos.real = Math.Cos(T)*0.01-1.01;
            //JuliaWindow.Controller.JuliaPos.imag = 0;
            //JuliaWindow.Controller.Compute();


            //MainFractalWindow.Controller.Compute();
            
            if (IterationSlider.HoldingHandle)
            {
                double A = 10*(IterationSlider.Value - 0.5);
                MainFractalWindow.Controller.Iterations += (int)(A*A*Math.Sign(A));
                if (MainFractalWindow.Controller.Iterations < 10)
                    MainFractalWindow.Controller.Iterations = 10;
                DrawIterationDisplay();
                MainFractalWindow.Controller.Compute();
            }
            else
            {
                IterationSlider.Value = 0.5;
            }
            
            Complex Corner1 = MainFractalWindow.GetWorldFromScreen(new PointF(0, 0));
            Complex Corner2 = MainFractalWindow.GetWorldFromScreen(new PointF(Size.Width, Size.Height));
            
            int GetMinibrots = MinibrotFinder.GetRootsInRegion(Corner1, Corner2);
            //TextDisplay.PrepareWrite();
            //TextDisplay.gfx.DrawString(String.Format("Roots:{0}", GetMinibrots), Font, Brushes.Black, TextDisplay.Rect.Width / 2, TextDisplay.Rect.Height / 2, SF);
            //TextDisplay.PrepareDraw();
            if(EnableMinibrots)
            {
                UpdateMinibrots();
            }
            UpdatePolynomialTextDisplay();
            if(PolynomialAnimationTimer<1)
            {
                PolynomialAnimationTimer += 0.03;
                double Lerp = Beizer(PolynomialAnimationTimer);
                double LerpNew = Lerp;
                double LerpOld= 1- Lerp;
                int MaxZ = Math.Max(polynomialParser.CoefficientArray.GetLength(0), OldCoefficientArray.GetLength(0));
                int MaxC = Math.Max(polynomialParser.CoefficientArray.GetLength(1), OldCoefficientArray.GetLength(1));
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
                if (PolynomialAnimationTimer >= 1)
                    MainCoefficientArray = polynomialParser.CoefficientArray;
                MainFractalWindow.Controller.CoefficientArray = MainCoefficientArray;
                MainFractalWindow.Controller.Compute();
                CursorSystem.JuliaWindow.Controller.CoefficientArray = MainCoefficientArray;
                CursorSystem.Julia3dButton.Controller.CoefficientArray = MainCoefficientArray;
                CursorSystem.Julia3dCutoffButton.Controller.CoefficientArray = MainCoefficientArray;
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
                    PointF P = MainFractalWindow.GetScreenFromWorld(Pos);
                    GL.Color3(Color.White);
                    GL.Rect(P.X - 5, P.Y - 5, P.X + 5, P.Y + 5);
                    PointF P2 = MainFractalWindow.GetScreenFromWorld(AllMinibrots[i].Cusp);
                    GL.Color3(Color.Orange);
                    GL.Rect(P2.X - 5, P2.Y - 5, P2.X + 5, P2.Y + 5);
                }
            }
            
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

        void MinibrotButtonClicked(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
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
        }
        void MainWindowClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            if (ButtonStatus == MouseButtons.Right)
            {
                CursorSystem.RightClick(MousePos);
            }
        }
        void UpdateMinibrots()
        {
            Complex Corner1 = MainFractalWindow.GetWorldFromScreen(new PointF(0, 0));
            Complex Corner2 = MainFractalWindow.GetWorldFromScreen(new PointF(Size.Width, Size.Height));
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

        }
        public void Resize(int w, int h)
        {
            Size = new Size(w, h);
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
                    ErrorMessageTextDisplay.Enabled = true;
                    string S = polynomialParser.ErrorMessage;
                    ErrorMessageTextDisplay.PrepareWrite();
                    ErrorMessageTextDisplay.gfx.DrawString(S, new Font("Arial Black", 12), Brushes.Blue, PolynomialTextDisplay.Rect.Width / 2, PolynomialTextDisplay.Rect.Height / 2, SF);
                    ErrorMessageTextDisplay.PrepareDraw();
                }
            }
        }
        void ErrorTimerFinish()
        {
            ErrorMessageTextDisplay.Enabled = false;
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
            PolynomialTextDisplay.PrepareWrite();
            PolynomialTextDisplay.gfx.DrawString(S, new Font("Arial Black",12), Brushes.Black, PolynomialTextDisplay.Rect.Width / 2, PolynomialTextDisplay.Rect.Height / 2, SF);
            PolynomialTextDisplay.PrepareDraw();
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
