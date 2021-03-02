using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using AmazingMandelbrot.GuiComponents;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;

namespace AmazingMandelbrot
{
    class MainCursorSystem
    {
        public CursorController mainController;
        public CursorController juliaController;
        public bool JuliaActive;
        public bool OrbitActive;
        public bool JuliaOrbitActive;
        public bool SoundActive;
        public bool OffsetActive;

        public FractalWindow JuliaButton;
        public FractalWindow OrbitButton;
        public EmptyComponent SoundButton;

        public FractalWindow JuliaOrbitButton;
        public FractalWindow JuliaPeriodButton;
        public EmptyComponent JuliaOffsetButton;

        public FractalWindow Julia3dButton;
        public FractalWindow Julia3dCutoffButton;
       
        //public FractalWindow OrbitButton;
        FractalWindow MainWindow;
        
        
        FractalMath FractalMath = new FractalMath();
        public FractalWindow JuliaWindow;
        const float JuliaSizeBoxSize=40;
        EmptyComponent JuliaSizeBox;
        float JuliaWindowSize=500;
        public MainCursorSystem(FractalWindow mainWindow)
        {
            MainWindow = mainWindow;
            float size = CursorController.ButtonSize;
            JuliaWindow = new FractalWindow(new RectangleF(MainWindow.Rect.Width - JuliaWindowSize - 20, 20, JuliaWindowSize, JuliaWindowSize));
            //AllButtons[0] = PinButton = new EmptyComponent(new RectangleF(0, 0, ButtonSize, ButtonSize));
            JuliaButton = new FractalWindow(new RectangleF(0, 0, size, size));
            OrbitButton = new FractalWindow(new RectangleF(0, 0, size, size));
            SoundButton = new EmptyComponent(new RectangleF(0, 0, size, size));
            
            mainController = new CursorController(mainWindow,
                new GuiElement[] {
                    OrbitButton,
                    SoundButton,
                    JuliaButton
                    
                });
            JuliaPeriodButton = new FractalWindow(new RectangleF(0, 0, size, size));
            JuliaOrbitButton = new FractalWindow(new RectangleF(0, 0, size, size));
            JuliaOffsetButton = new EmptyComponent(new RectangleF(0, 0, size, size));
            juliaController = new CursorController(JuliaWindow,
                new GuiElement[]
                {
                    JuliaOrbitButton,
                    JuliaPeriodButton,
                    JuliaOffsetButton
                }
                );
            SoundButton.LateDraw += SoundButtonLateDraw;
            SoundButton.MouseDownEvent += SoundButtonClick;
            mainController.MenuUpdatedEvent += MenuUpdated;
            mainController.PositionEvent += CursorChanged;
            juliaController.MenuUpdatedEvent += JuliaMenuUpdated;
            juliaController.PositionEvent += JuliaCursorChanged;
            

            //AllButtons[3] = OrbitButton = new FractalWindow(new RectangleF(0, 0, ButtonSize, ButtonSize));
            JuliaButton.Controller.Iterations = 1000;
            JuliaButton.Controller.Zoom = 0.14;
            JuliaButton.Controller.ColorScale = 2;
            JuliaButton.Controller.ColorOffset = 0.9f;
            JuliaButton.Controller.Julia = true;
            JuliaButton.Controller.JuliaPos = new Complex(-0.16037822, -1.0375242);
            JuliaButton.EnableInteraction = false;


            OrbitButton.Controller.CameraPos = new Complex(-0.0978, -0.747457);
            OrbitButton.Controller.Zoom = 0.15;
            OrbitButton.Controller.PeriodHighlight = 5;
            OrbitButton.OrbitActive = true;
            OrbitButton.OrbitPosition= new Complex(-0.03137, -0.79095);
            OrbitButton.OrbitScale = 0.5f;
            OrbitButton.StrippleScale = 1;
            OrbitButton.EnableInteraction = false;

            JuliaOrbitButton.EnableInteraction = false;
            JuliaOrbitButton.Controller.Zoom = 1.0;
            JuliaOrbitButton.OrbitActive = true;
            JuliaOrbitButton.Controller.CameraPos = new Complex(-0.3, -0.3);
            JuliaOrbitButton.Controller.JuliaPos = new Complex(-0.504455304069502, -0.562767203932328);
            JuliaOrbitButton.Controller.Julia = true;
            JuliaOrbitButton.OrbitScale = 0.5f;
            JuliaOrbitButton.StrippleScale = 1;
            JuliaOrbitButton.Controller.CenterDotStrength = 0;

            JuliaPeriodButton.EnableInteraction = false;
            JuliaPeriodButton.Controller.JuliaPos = new Complex(-0.153974876797236, -1.03769787591661);
            JuliaPeriodButton.Controller.Julia = true;
            JuliaPeriodButton.Controller.Zoom = 0.117;
            JuliaPeriodButton.Controller.FinalDotStrength = 0.001f;
            JuliaPeriodButton.fractalMath.CoefficientArray = JuliaWindow.Controller.CoefficientArray;
            JuliaPeriodButton.fractalMath.SetCoefficients(JuliaPeriodButton.Controller.JuliaPos);
            Complex Z = new Complex(0,0);
            for (int i = 0; i < JuliaPeriodButton.Controller.Iterations; i++)
            {
                Z = JuliaPeriodButton.fractalMath.Compute(Z);
            }
            JuliaPeriodButton.Controller.FinalDotPosition = Z;

            

            //JuliaWindow = new FractalWindow(new RectangleF(20, 20, JuliaWindowSize, JuliaWindowSize));
            JuliaWindow.Controller.Julia = true;
            JuliaWindow.Enabled = false;
            JuliaWindow.Controller.QuaternionJulia = false;
            JuliaSizeBox = new EmptyComponent(new RectangleF(5, JuliaWindowSize- JuliaSizeBoxSize-5, JuliaSizeBoxSize, JuliaSizeBoxSize));
            mainWindow.ChildElements.Add(JuliaWindow);
            JuliaWindow.ChildElements.Add(JuliaSizeBox);
            JuliaSizeBox.DragEvent += JuliaSizeBoxDrag;
            JuliaSizeBox.LateDraw += JuliaResizeLateDraw;
            JuliaSizeBox.DrawFrame = false;

            /*Julia3dButton = new FractalWindow(new RectangleF(5, 5, size, size));
            Julia3dButton.Controller.Julia=true;
            Julia3dButton.Controller.QuaternionJulia = true;

            Julia3dCutoffButton = new FractalWindow(new RectangleF(10 + size, 5, size, size));
            Julia3dCutoffButton.Controller.Julia = true;
            Julia3dCutoffButton.Controller.QuaternionJulia = true;
            Julia3dCutoffButton.Controller.QuaternionJuliaCutoff = false;
            Julia3dCutoffButton.Enabled = false;
            JuliaWindow.ChildElements.Add(Julia3dButton);
            JuliaWindow.ChildElements.Add(Julia3dCutoffButton);
            Julia3dButton.ClickEvent += Julia3dClick;
            Julia3dCutoffButton.ClickEvent += JuliaCutoffClick;*/

            //CursorWorldPosition = new Complex(-Math.PI/4, -0.15);
            //CursorPosition = MainWindow.GetScreenFromWorld(CursorWorldPosition);
            //CursorActive = true;
            //CursorElement.Enabled = true;
            

            OrbitButton.MouseDownEvent += OrbitClick;
            JuliaButton.MouseDownEvent += JuliaClick;

            JuliaOrbitButton.MouseDownEvent += JuliaOrbitClick;
            JuliaPeriodButton.MouseDownEvent += JuliaPeriodClick;
            JuliaOffsetButton.MouseDownEvent += JuliaOffsetClick;
        }
        public void Update()
        {
            FractalMath.CoefficientArray = MainWindow.Controller.CoefficientArray;
            mainController.Update();
            juliaController.Update();
            

        }
        void FinalDotUpdate()
        {
            if (juliaController.ButtonsActive[1])
            {
                JuliaWindow.fractalMath.CoefficientArray = JuliaWindow.Controller.CoefficientArray;
                JuliaWindow.fractalMath.SetCoefficients(JuliaWindow.Controller.JuliaPos);
                Complex Z = juliaController.CursorWorldPosition;
                for (int i = 0; i < JuliaWindow.Controller.Iterations; i++)
                {
                    Z = JuliaWindow.fractalMath.Compute(Z);
                }
                JuliaWindow.Controller.FinalDotPosition = Z;
                JuliaWindow.Controller.FinalDotStrength = 0.001f;
            }
            else
            {
                JuliaWindow.Controller.FinalDotStrength = 0;
            }
        }
        public void MenuUpdated()
        {
            JuliaButton.Controller.Compute();
            OrbitButton.Controller.Compute();
        }
        public void CursorChanged()
        {
            UpdateJulia();
            MainWindow.OrbitActive = OrbitActive;
            MainWindow.OrbitPosition = mainController.CursorWorldPosition;
        }
        public void JuliaMenuUpdated()
        {
            JuliaPeriodButton.Controller.Compute();
            JuliaOrbitButton.Controller.Compute();
        }
        public void JuliaCursorChanged()
        {
            
            JuliaWindow.OrbitActive = JuliaOrbitActive;
            JuliaWindow.OrbitPosition = juliaController.CursorWorldPosition;
            FinalDotUpdate();
            if (OffsetActive)
            {
                MainWindow.Controller.OffsetPos = juliaController.CursorWorldPosition;
                MainWindow.Controller.Compute();
            }
            else
            {
                MainWindow.Controller.OffsetPos = new Complex(0, 0);
            }
        }
        public void ComputeAll()
        {
            JuliaButton.Controller.Compute();
            OrbitButton.Controller.Compute();
            JuliaPeriodButton.Controller.Compute();
            JuliaOrbitButton.Controller.Compute();
            JuliaWindow.Controller.Compute();
        }
        public void RepositionJulia()
        {
            JuliaWindow.Rect=new RectangleF(MainWindow.Rect.Width - JuliaWindowSize - 20, 20, JuliaWindowSize, JuliaWindowSize);
            JuliaSizeBox.Rect = new RectangleF(5, JuliaWindowSize - JuliaSizeBoxSize - 5, JuliaSizeBoxSize, JuliaSizeBoxSize);
        }
        void JuliaSizeBoxDrag(GuiElement Sender, PointF MousePos, PointF StartPos, PointF DeltaPos, MouseButtons ButtonStatus)
        {
            float X = JuliaWindowSize-(JuliaSizeBox.Rect.X + MousePos.X);
            float Y = JuliaSizeBox.Rect.Y + +MousePos.Y;
            JuliaWindowSize = Math.Max((X+Y)/2+ JuliaSizeBoxSize/2+5,200);
            JuliaWindow.Resize(JuliaWindowSize, JuliaWindowSize);
            JuliaWindow.Controller.Compute();
            RepositionJulia();
        }
        void JuliaResizeLateDraw(GuiElement sender, Main M)
        {
            GL.Color3(JuliaSizeBox.BackgroundColor);
            GL.Begin(PrimitiveType.Triangles);
            GL.Vertex2(0, 0);
            GL.Vertex2(0, JuliaSizeBoxSize);
            GL.Vertex2(JuliaSizeBoxSize, JuliaSizeBoxSize);
            GL.End();
            GL.LineWidth(2);
            GL.Color3(JuliaSizeBox.FrameColor);
            GL.Begin(PrimitiveType.LineLoop);
            GL.Vertex2(0, 0);
            GL.Vertex2(0, JuliaSizeBoxSize);
            GL.Vertex2(JuliaSizeBoxSize,JuliaSizeBoxSize);
            GL.End();
        }
        
        public void UpdatePeriod()
        {
            if (OrbitActive)
            {
                MainWindow.Controller.PeriodHighlight = FractalMath.FindPeriod(mainController.CursorWorldPosition);
            }
        }
        public void UpdateJulia()
        {
            if (JuliaActive)
            {
                JuliaWindow.Controller.JuliaPos = mainController.CursorWorldPosition;
                FinalDotUpdate();
                JuliaWindow.Controller.Compute();
                //Julia3dButton.Controller.JuliaPos = mainController.CursorWorldPosition;
                //Julia3dButton.Controller.Compute();
                //Julia3dCutoffButton.Controller.JuliaPos = mainController.CursorWorldPosition;
                if (JuliaWindow.Controller.QuaternionJulia)
                {
                    //Julia3dCutoffButton.Controller.Compute();
                }
                
            }
        }
        
        
        void OrbitClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            OrbitActive = mainController.ButtonsActive[0];
            if (!OrbitActive)
            {
                MainWindow.Controller.PeriodHighlight = 0;
                JuliaWindow.Controller.PeriodHighlight = 0;
            }
            else
            {
                MainWindow.Controller.PeriodHighlight = 1;
                JuliaWindow.Controller.PeriodHighlight = 1;
            }
            MainWindow.OrbitActive = OrbitActive;
            MainWindow.OrbitPosition = mainController.CursorWorldPosition;
            //UpdatePeriod();
        }
        void JuliaOrbitClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            JuliaOrbitActive = juliaController.ButtonsActive[0];
            JuliaWindow.OrbitActive = JuliaOrbitActive;
            JuliaWindow.OrbitPosition = juliaController.CursorWorldPosition;
        }
        void JuliaClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            JuliaActive = mainController.ButtonsActive[2];
            JuliaWindow.Enabled = JuliaActive;
            UpdateJulia();
        }
        void JuliaPeriodClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            FinalDotUpdate();
        }
        void JuliaOffsetClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            OffsetActive = juliaController.ButtonsActive[2];
            if (OffsetActive)
            {
                MainWindow.Controller.OffsetPos = juliaController.CursorWorldPosition;
                
            }else
            {
                MainWindow.Controller.OffsetPos = new Complex(0, 0);
            }
            MainWindow.Controller.Compute();
        }
        void SoundButtonClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            SoundActive = mainController.ButtonsActive[1];
        }
        void SoundButtonLateDraw(GuiElement Sender, Main M)
        {
            GL.Color3(Color.LightGray);
            GL.PushMatrix();
            
            
            float w = CursorController.ButtonSize;
            float s = w * 0.2f;
            GL.Translate(w*0.45, w/2, 0);
            GL.Begin(PrimitiveType.Polygon);
            GL.Vertex2(0,s);
            GL.Vertex2(s * 0.5, s*1.5);
            GL.Vertex2(s * 0.5, -s * 1.5);
            GL.Vertex2(0, -s);
            GL.Vertex2(-s, -s);
            GL.Vertex2(-s, s);
            GL.End();
            GL.LineWidth(3);
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex2(s, s);
            GL.Vertex2(s, -s);
            s *= 1.5f;
            GL.Vertex2(s, s);
            GL.Vertex2(s, -s);
            GL.End();
            GL.PopMatrix();
        }
        void Julia3dClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            JuliaWindow.Controller.QuaternionJulia = !JuliaWindow.Controller.QuaternionJulia;
            Julia3dCutoffButton.Enabled = JuliaWindow.Controller.QuaternionJulia;
            Julia3dButton.Controller.QuaternionJulia = !JuliaWindow.Controller.QuaternionJulia;
            UpdateJulia();
        }
        void JuliaCutoffClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            JuliaWindow.Controller.QuaternionJuliaCutoff = !JuliaWindow.Controller.QuaternionJuliaCutoff;
            Julia3dButton.Controller.QuaternionJuliaCutoff = JuliaWindow.Controller.QuaternionJuliaCutoff;
            Julia3dCutoffButton.Controller.QuaternionJuliaCutoff = !JuliaWindow.Controller.QuaternionJuliaCutoff;
            UpdateJulia();
        }
        
    }
}
