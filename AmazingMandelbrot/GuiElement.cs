using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AmazingMandelbrot
{
    abstract class GuiElement
    {
        public Color FrameColor = Color.LightGray;
        //public Pen FramePen = new Pen(Color.LightGray,10)
        //{
        //LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        //};

        public GuiHandler.MouseEventHandler ClickEvent;
        public GuiHandler.MouseEventHandler MouseDownEvent;
        public GuiHandler.MouseEventHandler MouseUpEvent;
        public GuiHandler.MouseDragHandler DragEvent;
        public GuiHandler.MouseHoverHandler HoverEvent;
        public GuiHandler.MouseHoverHandler HoverEndEvent;
        public GuiHandler.MouseScrollHandler ScrollEvent;
        public GuiHandler.ExternalDrawEvent EarlyDraw;
        public GuiHandler.ExternalDrawEvent LateDraw;
        public RectangleF Rect;
        public Vector4 TranslateVector;
        public List<GuiElement> ChildElements = new List<GuiElement>();
        public int FrameSize = 2;
        public bool Enabled = true;
        public bool Visible = true;
        public bool DrawFrame = true;
        public bool CaptureEvents = true;
        public int DrawingLayerOffset = 0;
        public Color BackgroundColor = Color.LightSlateGray;
        public abstract void Update();
        public abstract void Show(Main M);
        public GuiElement(RectangleF Rect)
        {
            this.Rect = Rect;
        }
        public void DrawFrameAndBackground()
        {
            GL.Color3(FrameColor);
            GL.Rect(-FrameSize, -FrameSize, Rect.Width + FrameSize, Rect.Height + FrameSize);
            GL.Color3(BackgroundColor);
            GL.Rect(0, 0, Rect.Width, Rect.Height);

            //RectangleF R = new RectangleF(0, 0, Rect.Width, Rect.Height);
            //G.DrawRectangle(FramePen,new Rectangle(0, 0, (int)Rect.Width, (int)Rect.Height));
            //G.FillRectangle(new SolidBrush(BackgroundColor), R);
        }
        float[] ColorMultipliers = new float[] { 1, 0.95f, 0.9f };
        public void DrawCuboid(Vector3 Corner1, Vector3 Corner2, Color Color)
        {
            GL.Begin(PrimitiveType.Quads);
            Vector3 Diff = Corner2 - Corner1;

            for (int i = 0; i < 6; i++)
            {
                int Axis = i / 2;
                int Face = i % 2;
                GL.Color3(CMult(Color, ColorMultipliers[Axis]));
                Vector3 V1 = GetVectorFromIndex(Diff, (Axis + 1) % 3);
                Vector3 V2 = GetVectorFromIndex(Diff, (Axis + 2) % 3);
                Vector3 V3 = GetVectorFromIndex(Diff, Axis) * Face;
                for (int j = 0; j < 4; j++)
                {
                    int M1 = ((j + 3) & 2) / 2;
                    int M2 = (j & 2) / 2;
                    Vector3 V = Corner1 + V1 * M1 + V2 * M2 + V3;
                    GL.Vertex3(V);
                }
            }
            GL.End();
        }
        Vector3 GetVectorFromIndex(Vector3 V, int i)
        {
            switch (i)
            {
                case 0: return new Vector3(V.X, 0, 0);
                case 1: return new Vector3(0, V.Y, 0);
                case 2: return new Vector3(0, 0, V.Z);
                default: return new Vector3(0, 0, 0);
            }
        }
        public Color CMult(Color C, double M)
        {
            return Color.FromArgb((int)(C.R * M), (int)(C.G * M), (int)(C.B * M));
        }
    }
}
