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
    class CursorSystem
    {
        public bool CursorActive;
        public bool CursorPinned;
        public bool JuliaActive;
        public bool PeriodActive;
        public bool MenuActive;
        public Complex CursorWorldPosition;
        public PointF CursorPosition;
        public EmptyComponent CursorElement;
        public EmptyComponent PinButton;
        public FractalWindow JuliaButton;
        public FractalWindow Julia3dButton;
        public FractalWindow Julia3dCutoffButton;
        public FractalWindow PeriodButton;
        public FractalWindow OrbitButton;
        GuiElement[] AllButtons = new GuiElement[4];
        FractalWindow MainWindow;
        const float CursorSize = 20;
        const float ButtonSize = 50;
        const float ButtonDistance = 70;
        Main.Timer TeleportTimer = new Main.Timer(10);
        Main.Timer ExtendTimer = new Main.Timer(10);
        PointF TargetCursorPosition;
        PointF OldCursorPosition;
        FractalMath FractalMath = new FractalMath();
        public FractalWindow JuliaWindow;
        const float JuliaSizeBoxSize=40;
        EmptyComponent JuliaSizeBox;
        float JuliaWindowSize=500;
        public CursorSystem(FractalWindow mainWindow)
        {
            MainWindow = mainWindow;
            CursorElement = new EmptyComponent(new RectangleF(0,0, CursorSize, CursorSize));
            mainWindow.ChildElements.Add(CursorElement);
            AllButtons[0] = PinButton = new EmptyComponent(new RectangleF(0, 0, ButtonSize, ButtonSize));
            AllButtons[1] = JuliaButton = new FractalWindow(new RectangleF(0, 0, ButtonSize, ButtonSize));
            AllButtons[2] = PeriodButton = new FractalWindow(new RectangleF(0, 0, ButtonSize, ButtonSize));
            AllButtons[3] = OrbitButton = new FractalWindow(new RectangleF(0, 0, ButtonSize, ButtonSize));
            JuliaButton.Controller.Iterations = 1000;
            JuliaButton.Controller.Zoom = 0.14;
            JuliaButton.Controller.ColorScale = 2;
            JuliaButton.Controller.ColorOffset = 0.9f;
            JuliaButton.Controller.Julia = true;
            JuliaButton.Controller.JuliaPos = new Complex(-0.16037822, -1.0375242);
            JuliaButton.EnableInteraction = false;
            
            
            

            PeriodButton.Controller.CameraPos = new Complex(-0.48125,-0.534114583333333);
            PeriodButton.Controller.Zoom = 0.1;
            PeriodButton.Controller.PeriodHighlight = 5;
            PeriodButton.EnableInteraction = false;
            
            OrbitButton.EnableInteraction = false;
            for (int i = 0; i < AllButtons.Length; i++)
            {
                AllButtons[i].Enabled = false;
                mainWindow.ChildElements.Add(AllButtons[i]);
                AllButtons[i].FrameColor = Color.Red;
            }
            CursorElement.Enabled = false;
            CursorElement.DrawFrame = false;
            CursorElement.DragEvent += CursorDragged;
            CursorElement.LateDraw += CursorLateDraw;
            ExtendTimer.FinishedEvent += ExtendTimerFinish;
            ExtendTimer.Tick += ExtendTimerTick;
            TeleportTimer.FinishedEvent+= TeleportTimerFinish;
            TeleportTimer.Tick += TeleportTimerTick;
            CursorElement.MouseDownEvent += CursorClick;
            MainWindow.Controller.FinishEvent += MainComputed;

            PeriodButton.ClickEvent += PeriodClick;
            JuliaButton.ClickEvent += JuliaClick;
            JuliaWindow = new FractalWindow(new RectangleF(MainWindow.Rect.Width- JuliaWindowSize-20,20, JuliaWindowSize, JuliaWindowSize));
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

            Julia3dButton = new FractalWindow(new RectangleF(5, 5, ButtonSize, ButtonSize));
            Julia3dButton.Controller.Julia=true;
            Julia3dButton.Controller.QuaternionJulia = true;

            Julia3dCutoffButton = new FractalWindow(new RectangleF(10 + ButtonSize, 5, ButtonSize, ButtonSize));
            Julia3dCutoffButton.Controller.Julia = true;
            Julia3dCutoffButton.Controller.QuaternionJulia = true;
            Julia3dCutoffButton.Controller.QuaternionJuliaCutoff = false;
            Julia3dCutoffButton.Enabled = false;
            JuliaWindow.ChildElements.Add(Julia3dButton);
            JuliaWindow.ChildElements.Add(Julia3dCutoffButton);
            Julia3dButton.ClickEvent += Julia3dClick;
            Julia3dCutoffButton.ClickEvent += JuliaCutoffClick;
        }
        public void Update()
        {
            FractalMath.CoefficientArray = MainWindow.Controller.CoefficientArray;
            ExtendTimer.Update();
            TeleportTimer.Update();
            
        }
        public void ComputeAll()
        {
            
        }
        public void CursorLateDraw(GuiElement sender,Main M)
        {
            Vector2 Center = new Vector2(CursorSize)/2;
            float Offset1 = CursorSize / 2;
            float Offset2 = Offset1 * 1.5f;
            GL.LineWidth(3);
            GL.Color3(0.5,0.8,1);
            GL.Translate(Center.X,Center.Y,0);
            GL.Begin(PrimitiveType.LineLoop);
            GL.Vertex2(Offset1,0);
            GL.Vertex2(0, Offset1);
            GL.Vertex2(-Offset1, 0);
            GL.Vertex2(0, -Offset1);
            GL.End();
            GL.Begin(PrimitiveType.Lines);
            GL.Vertex2(Offset1, 0); GL.Vertex2(Offset2, 0);
            GL.Vertex2(0, Offset1); GL.Vertex2(0, Offset2);
            GL.Vertex2(-Offset1, 0);GL.Vertex2(-Offset2, 0);
            GL.Vertex2(0, -Offset1);GL.Vertex2(0, -Offset2);
            GL.End();
        }
        void CursorDragged(GuiElement Sender, PointF MousePos, PointF StartPos, PointF DeltaPos, MouseButtons ButtonStatus)
        {
            CursorElement.Rect.X += MousePos.X - CursorElement.Rect.Width / 2;
            CursorElement.Rect.Y += MousePos.Y - CursorElement.Rect.Height / 2;
            UpdateButtonPositions();
            UpdateCursorWorldPos();
            
        }
        void UpdateButtonPositions()
        {
            float OffsetX = CursorElement.Rect.X;
            float OffsetY = CursorElement.Rect.Y;
            float Offset = (CursorSize - ButtonSize) / 2;
            float L = ExtendTimer.Value;
            for (int i = 0; i < AllButtons.Length; i++)
            {
                double Angle = L * Math.PI * 0.5 + (i * Math.PI * 2) / (AllButtons.Length);
                AllButtons[i].Rect.Location = new PointF((float)Math.Cos(Angle) * ButtonDistance * L + Offset+ OffsetX, (float)Math.Sin(Angle) * ButtonDistance * L + Offset+ OffsetY);
            }
        }
        public void MainComputed()
        {
            if (!MenuActive)
            {
                CursorPosition = MainWindow.GetScreenFromWorld(CursorWorldPosition);
                CursorElement.Rect.Location = new PointF(CursorPosition.X - CursorElement.Rect.Width / 2,
                CursorPosition.Y - CursorElement.Rect.Height / 2);
            }
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
        public void UpdateCursorWorldPos()
        {
            CursorWorldPosition = MainWindow.GetWorldFromScreen(new PointF(
                CursorElement.Rect.X + CursorElement.Rect.Width / 2,
                CursorElement.Rect.Y + CursorElement.Rect.Height / 2
                ));
            UpdatePeriod();
            UpdateJulia();
        }
        public void UpdatePeriod()
        {
            if (PeriodActive)
            {
                MainWindow.Controller.PeriodHighlight = FractalMath.FindPeriod(CursorWorldPosition);
            }
        }
        public void UpdateJulia()
        {
            if (JuliaActive)
            {
                JuliaWindow.Controller.JuliaPos = CursorWorldPosition;
                JuliaWindow.Controller.Compute();
                Julia3dButton.Controller.JuliaPos = CursorWorldPosition;
                Julia3dButton.Controller.Compute();
                Julia3dCutoffButton.Controller.JuliaPos = CursorWorldPosition;
                if (JuliaWindow.Controller.QuaternionJulia)
                {
                    Julia3dCutoffButton.Controller.Compute();
                }
            }
        }
        public void RightClick(PointF Pos)
        {
            if (MenuActive)
            {
                DisableMenu();
            }
            else
            {
                TargetCursorPosition = new PointF(Pos.X - CursorSize / 2, Pos.Y - CursorSize / 2);
                OldCursorPosition = CursorElement.Rect.Location;
                if (CursorElement.Enabled)
                {
                    TeleportTimer.Reset();
                    TeleportTimer.Start();
                }else
                {
                    CursorElement.Rect.Location = TargetCursorPosition;
                    UpdateCursorWorldPos();
                    EnableMenu();
                }
            }
        }
        void CursorClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            if (ButtonStatus == MouseButtons.Right)
            {
                if (MenuActive)
                {
                    DisableMenu();
                }
                else
                {
                    EnableMenu();
                }
            }
        }
        void PeriodClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            PeriodActive = !PeriodActive;
            PeriodButton.FrameColor = PeriodActive ? Color.Green:Color.Red;
            if(!PeriodActive)
            {
                MainWindow.Controller.PeriodHighlight = 0;
            }
            UpdatePeriod();
        }
        void JuliaClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            JuliaActive = !JuliaActive;
            JuliaButton.FrameColor = JuliaActive ? Color.Green : Color.Red;
            JuliaWindow.Enabled = JuliaActive;
            UpdateJulia();
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
        void EnableMenu()
        {
            JuliaButton.Controller.Compute();
            PeriodButton.Controller.Compute();
            CursorElement.Enabled = true;
            MainWindow.EnableInteraction = false;
            ExtendTimer.CountingBackwards = false;
            for (int i = 0; i < AllButtons.Length; i++)
            {
                AllButtons[i].Enabled = true;
            }
            ExtendTimer.Start();
        }
        void DisableMenu()
        {
            
            ExtendTimer.CountingBackwards = true;
            ExtendTimer.Start();
            
        }
        void ExtendTimerTick()
        {
            
            UpdateButtonPositions();
        }
        void ExtendTimerFinish()
        {
            if(MenuActive)
            {
                MainWindow.EnableInteraction = true;
                
                for (int i = 0; i < AllButtons.Length; i++)
                {
                    AllButtons[i].Enabled = false;
                }
                MenuActive = false;
                bool AnythingEnabled = PeriodActive||JuliaActive;
                if (!AnythingEnabled)
                {
                    CursorElement.Enabled = false;
                }
            }
            else
            {
                MenuActive = true;
            }
        }
        void TeleportTimerTick()
        {
            float L = Beizer(TeleportTimer.Value);
            CursorElement.Rect.Location = new PointF(LerpF(OldCursorPosition.X,TargetCursorPosition.X,L), LerpF(OldCursorPosition.Y, TargetCursorPosition.Y, L));
            UpdateCursorWorldPos();

        }
        void TeleportTimerFinish()
        {
            //EnableMenu();
        }
        float LerpF(float A, float B, float T) => A * (1 - T) + B * T;
        float Beizer(float X) => X * X * (3 - 2 * X);
    }
}
