using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;

namespace AmazingMandelbrot.GuiComponents
{
    class Slider : GuiElement
    {
        public double Value = 0;
        public SizeF HandleRectangleSize;
        public const int Padding = 2;
        public bool HoldingHandle = false;
        double StartValue = 0;
        public RectangleF HandleRect;
        public SliderEventHandler SliderEvent;
        public delegate void SliderEventHandler(double NewValue);
        public Color HandleColor = Color.LightSlateGray;
        public enum SliderOrientation
        {
            Horizontal,
            Vertical
        }
        public SliderOrientation Orientation;
        public const int HandleLength = 16;
        public bool AllowSmoothMotion;
        bool DoingSmoothMotion = false;
        float SmoothStart;
        float SmoothEnd;
        float SmoothDeadZone = 5;
        float SmoothMaxSpeed = 0.004f;
        float SmoothMaxDistance = 80;
        public Slider(RectangleF Rect, bool AllowSmoothMotion = false) : base(Rect)
        {
            this.AllowSmoothMotion = AllowSmoothMotion;
            Orientation = (Rect.Width > Rect.Height) ? SliderOrientation.Horizontal : SliderOrientation.Vertical;

            HandleRectangleSize = Orientation == SliderOrientation.Horizontal ? new SizeF(HandleLength, Rect.Height - Padding * 2) : new SizeF(Rect.Width - Padding * 2, HandleLength);
            DragEvent += Drag;
            MouseUpEvent += MouseUp;
            MouseDownEvent += MouseDown;
            HoverEndEvent += LeaveHover;

        }
        public override void Update()
        {
            if (DoingSmoothMotion)
            {

            }
        }
        public override void Show(Main M)
        {
            float LinePos = Padding + HandleRectangleSize.Width / 2;
            //M.G.DrawLine(new Pen(Color.White, 4), LinePos, Padding, LinePos, Rect.Height - Padding);
            if (Orientation == SliderOrientation.Horizontal)
            {
                int HandlePos = (int)(Value * (Rect.Width - HandleRectangleSize.Width - Padding * 2)) + Padding;
                HandleRect = new RectangleF(HandlePos, Padding, HandleRectangleSize.Width, HandleRectangleSize.Height);
            }
            else
            {
                int HandlePos = (int)((1 - Value) * (Rect.Height - HandleRectangleSize.Height - Padding * 2)) + Padding;
                HandleRect = new RectangleF(Padding, HandlePos, HandleRectangleSize.Width, HandleRectangleSize.Height);
            }
            Color Border = HoldingHandle ? Color.White : Color.LightGray;
            float Padding2 = 3;
            GL.Color3(Border);
            GL.Rect(HandleRect);
            GL.Color3(HandleColor);
            GL.Rect(HandleRect.X + Padding2, HandleRect.Y + Padding2, HandleRect.Right - Padding2, HandleRect.Bottom - Padding2);
            if (DoingSmoothMotion)
            {
                Vector2 I;
                Vector2 J;
                Vector2 S;
                float W = 10;
                int K = -Math.Sign(SmoothStart - SmoothEnd);
                bool IsInDeadZone = Math.Abs(SmoothStart - SmoothEnd) < SmoothDeadZone;
                if (Orientation == SliderOrientation.Horizontal)
                {
                    I = new Vector2(1, 0);
                    J = new Vector2(0, 1);
                    S = new Vector2(Rect.Height, Rect.Width);
                }
                else
                {
                    I = new Vector2(0, 1);
                    J = new Vector2(1, 0);
                    S = new Vector2(Rect.Width, Rect.Height);
                }
                Vector2 Start = J * (S.X / 2) + I * SmoothStart;
                Vector2 End = J * (S.X / 2) + I * SmoothEnd;
                GL.LineWidth(2);
                GL.Color3(1.0, 1.0, 1.0);
                GL.Begin(PrimitiveType.Lines);
                GL.Vertex2(Start - J * W);
                GL.Vertex2(Start + J * W);
                GL.Vertex2(Start);
                GL.Vertex2(End);
                if (IsInDeadZone)
                {
                    GL.Vertex2(End - J * W);
                    GL.Vertex2(End + J * W);
                }
                GL.End();
                if (!IsInDeadZone)
                {
                    GL.Begin(PrimitiveType.Triangles);
                    GL.Vertex2(End - J * W);
                    GL.Vertex2(End + J * W);
                    GL.Vertex2(End + I * (W * K / 2));
                    GL.End();

                    double Speed = Math.Abs(SmoothEnd - SmoothStart) - SmoothDeadZone;
                    Speed /= (SmoothMaxDistance - SmoothDeadZone);
                    Speed *= SmoothMaxSpeed;
                    Speed *= -K;
                    Value += Speed;
                    if (Value > 1)
                        Value = 1;
                    if (Value < 0)
                        Value = 0;
                    SliderEvent?.Invoke(Value);
                }
            }
        }
        void Drag(GuiElement Sender, PointF MousePos, PointF StartPos, PointF DeltaPos, MouseButtons ButtonStatus)
        {
            if (ButtonStatus == MouseButtons.Left)
            {
                if (HoldingHandle)
                {
                    if (Orientation == SliderOrientation.Horizontal)
                        Value = 1 - (1 - StartValue + (StartPos.X - MousePos.X) / (Rect.Width - HandleRectangleSize.Width - Padding * 2));
                    else
                        Value = StartValue + (StartPos.Y - MousePos.Y) / (Rect.Height - HandleRectangleSize.Height - Padding * 2);
                    if (Value > 1)
                    {
                        Value = 1;
                    }
                    if (Value < 0)
                    {
                        Value = 0;
                    }
                    if (SliderEvent != null)
                    {
                        SliderEvent.Invoke(Value);
                    }
                }
            }
            else if (AllowSmoothMotion && ButtonStatus == MouseButtons.Right)
            {
                DoingSmoothMotion = true;
                if (Orientation == SliderOrientation.Horizontal)
                {
                    SmoothStart = StartPos.X;
                    SmoothEnd = MousePos.X;
                }
                else
                {
                    SmoothStart = StartPos.Y;
                    SmoothEnd = MousePos.Y;
                }
                if (Math.Abs(SmoothEnd - SmoothStart) > SmoothMaxDistance)
                {
                    SmoothEnd = SmoothStart + Math.Sign(SmoothEnd - SmoothStart) * SmoothMaxDistance;
                }
            }
        }
        void MouseUp(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            HoldingHandle = false;
            DoingSmoothMotion = false;
        }
        void LeaveHover(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            HoldingHandle = false;
            DoingSmoothMotion = false;
        }
        void MouseDown(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            if (HandleRect.Contains((int)MousePos.X, (int)MousePos.Y))
            {
                HoldingHandle = true;
                StartValue = Value;
            }
        }
    }
}
