using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmazingMandelbrot.GuiComponents;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Windows.Forms;

namespace AmazingMandelbrot
{
    class CursorController
    {
        public delegate void MenuUpdated();
        public MenuUpdated MenuUpdatedEvent;
        public delegate void PositionChanged();
        public PositionChanged PositionEvent;
        public EmptyComponent CursorElement;
        public GuiElement[] AllButtons;
        public bool[] ButtonsActive;
        public FractalWindow MainWindow;
        const float CursorSize = 20;
        public const float ButtonSize = 60;
        const float ButtonDistance = 70;
        public bool MenuActive;
        public Complex CursorWorldPosition;
        public Vector2 CursorPosition;
        public Vector2 ClampedCursorPosition;

        Main.Timer TeleportTimer = new Main.Timer(15);
        Main.Timer ExtendTimer = new Main.Timer(15);
        PointF TargetCursorPosition;
        PointF OldCursorPosition;
        const float ClampDist = 25;
        public CursorController(FractalWindow mainWindow, GuiElement[] buttons)
        {
            ButtonsActive = new bool[buttons.Length];
            AllButtons = buttons;
            MainWindow = mainWindow;
            MainWindow = mainWindow;
            CursorElement = new EmptyComponent(new RectangleF(0, 0, CursorSize, CursorSize));
            mainWindow.ChildElements.Add(CursorElement);
            MainWindow.MouseDownEvent += MainWindowClick;
            for (int i = 0; i < AllButtons.Length; i++)
            {
                AllButtons[i].Enabled = false;
                mainWindow.ChildElements.Add(AllButtons[i]);
                AllButtons[i].FrameColor = Color.Red;
                AllButtons[i].MouseDownEvent += ButtonClick;
            }
            CursorElement.Enabled = false;
            CursorElement.DrawFrame = false;
            CursorElement.DragEvent += CursorDragged;
            CursorElement.LateDraw += CursorLateDraw;
            ExtendTimer.FinishedEvent += ExtendTimerFinish;
            ExtendTimer.Tick += ExtendTimerTick;
            TeleportTimer.FinishedEvent += TeleportTimerFinish;
            TeleportTimer.Tick += TeleportTimerTick;
            CursorElement.MouseDownEvent += CursorClick;
            MainWindow.Controller.FinishEvent += MainComputed;
        }
        public void Update()
        {
            ExtendTimer.Update();
            TeleportTimer.Update();
        }
        
        public void CursorLateDraw(GuiElement sender, Main M)
        {
            GL.LineWidth(3);
            GL.Color3(0.5, 0.8, 1);
            Vector2 Center = new Vector2(CursorSize) / 2;
            GL.Translate(Center.X, Center.Y, 0);
            if ((ClampedCursorPosition - CursorPosition).LengthSquared < 1)
            {
                float Offset1 = CursorSize / 2;
                float Offset2 = Offset1 * 1.5f;
                GL.Begin(PrimitiveType.LineLoop);
                GL.Vertex2(Offset1, 0);
                GL.Vertex2(0, Offset1);
                GL.Vertex2(-Offset1, 0);
                GL.Vertex2(0, -Offset1);
                GL.End();
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex2(Offset1, 0); GL.Vertex2(Offset2, 0);
                GL.Vertex2(0, Offset1); GL.Vertex2(0, Offset2);
                GL.Vertex2(-Offset1, 0); GL.Vertex2(-Offset2, 0);
                GL.Vertex2(0, -Offset1); GL.Vertex2(0, -Offset2);
                GL.End();
            }else
            {
                float s = CursorSize / 2;
                Vector2 Diff = CursorPosition- ClampedCursorPosition;
                Diff.Normalize();
                Diff *= s;
                Vector2 Tangent = new Vector2(Diff.Y, -Diff.X);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex2(Diff);
                GL.Vertex2(-Diff+ Tangent);
                GL.Vertex2(Diff);
                GL.Vertex2(-Diff - Tangent);
                GL.End();
            }
        }
        void CursorDragged(GuiElement Sender, PointF MousePos, PointF StartPos, PointF DeltaPos, MouseButtons ButtonStatus)
        {
            CursorPosition = new Vector2(MousePos.X+ CursorElement.Rect.X, MousePos.Y+ CursorElement.Rect.Y);
            ClampCursor();
            CursorElement.Rect.X = ClampedCursorPosition.X - CursorElement.Rect.Width / 2;
            CursorElement.Rect.Y = ClampedCursorPosition.Y - CursorElement.Rect.Height / 2;
            //CursorElement.Rect.X += MousePos.X - CursorElement.Rect.Width / 2;
            //CursorElement.Rect.Y += MousePos.Y - CursorElement.Rect.Height / 2;

            UpdateButtonPositions();
            UpdateCursorWorldPos();

        }
        public void UpdateCursorWorldPos()
        {
            CursorWorldPosition = MainWindow.GetWorldFromScreen(new Vector2(
                CursorElement.Rect.X + CursorElement.Rect.Width / 2,
                CursorElement.Rect.Y + CursorElement.Rect.Height / 2
                ));
            PositionEvent?.Invoke();
        }
        void MainWindowClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            if (ButtonStatus == MouseButtons.Right)
            {
                if (MenuActive)
                {
                    DisableMenu();
                }
                else
                {
                    TargetCursorPosition = new PointF(MousePos.X - CursorSize / 2, MousePos.Y - CursorSize / 2);
                    OldCursorPosition = new PointF(CursorPosition.X - CursorSize / 2, CursorPosition.Y - CursorSize / 2);
                    if (CursorElement.Enabled)
                    {
                        TeleportTimer.Reset();
                        TeleportTimer.Start();
                    }
                    else
                    {
                        CursorElement.Rect.Location = TargetCursorPosition;
                        CursorPosition = new Vector2(MousePos.X, MousePos.Y);
                        ClampCursor();
                        UpdateCursorWorldPos();
                        EnableMenu();
                    }
                }
            }
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
                AllButtons[i].Rect.Location = new PointF((float)Math.Cos(Angle) * ButtonDistance * L + Offset + OffsetX, (float)Math.Sin(Angle) * ButtonDistance * L + Offset + OffsetY);
            }
        }
        public void MainComputed()
        {
            if (!MenuActive)
            {
                CursorPosition = MainWindow.GetScreenFromWorld(CursorWorldPosition);
                ClampCursor();
                CursorElement.Rect.Location = new PointF(ClampedCursorPosition.X - CursorElement.Rect.Width / 2,
                ClampedCursorPosition.Y - CursorElement.Rect.Height / 2);
            }
        }
        void ClampCursor()
        {
            ClampedCursorPosition.X = Math.Min(Math.Max(CursorPosition.X, ClampDist),MainWindow.Rect.Width- ClampDist);
            ClampedCursorPosition.Y = Math.Min(Math.Max(CursorPosition.Y, ClampDist), MainWindow.Rect.Height - ClampDist);
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
        void ButtonClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            int a = 0;
            for (int i = 0; i < AllButtons.Length; i++)
            {
                if(Sender==AllButtons[i])
                {
                    a = i;
                }
            }
            ButtonsActive[a] = !ButtonsActive[a];
            Sender.FrameColor = ButtonsActive[a] ? Color.Green : Color.Red;
        }
        void EnableMenu()
        {
            MenuUpdatedEvent?.Invoke();
            CursorElement.Enabled = true;
            MainWindow.EnableInteraction = false;
            ExtendTimer.CountingBackwards = false;
            
            ExtendTimer.Start();
            UpdateCursorWorldPos();
        }
        void DisableMenu()
        {

            ExtendTimer.CountingBackwards = true;
            ExtendTimer.Start();

        }
        void ExtendTimerTick()
        {
            if(ExtendTimer.CurrentTime==1)
            {
                for (int i = 0; i < AllButtons.Length; i++)
                {
                    AllButtons[i].Enabled = true;
                }
            }
            UpdateButtonPositions();
        }
        void ExtendTimerFinish()
        {
            if (MenuActive)
            {
                MainWindow.EnableInteraction = true;

                for (int i = 0; i < AllButtons.Length; i++)
                {
                    AllButtons[i].Enabled = false;
                }
                MenuActive = false;
                bool AnythingEnabled = false;
                for (int i = 0; i < AllButtons.Length; i++)
                {
                    AnythingEnabled= AnythingEnabled||ButtonsActive[i];
                }
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
            CursorElement.Rect.Location = new PointF(LerpF(OldCursorPosition.X, TargetCursorPosition.X, L), LerpF(OldCursorPosition.Y, TargetCursorPosition.Y, L));
            UpdateCursorWorldPos();
            CursorPosition = new Vector2(CursorElement.Rect.X+CursorSize / 2, CursorElement.Rect.Y + CursorSize / 2);
            ClampCursor();
        }
        void TeleportTimerFinish()
        {
            //EnableMenu();
        }
        float LerpF(float A, float B, float T) => A * (1 - T) + B * T;
        float Beizer(float X) => X * X * (3 - 2 * X);
    }
}
