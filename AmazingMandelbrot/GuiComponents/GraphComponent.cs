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
    class GraphComponent : GuiElement
    {
        public double[] Array;
        readonly Color Color;
        double Average;
        double max = 0;
        double min = 0;
        readonly int[] LineSpaceArr = new int[] { 5, 10, 50, 100 };
        readonly int spacing;
        public Font Font = new Font("Arial Black", 12);
        int StartIndex = 0;
        TextDisplay TextDisplay;
        Rectangle TextRect;
        public const int TextDisplayWidth = 75;
        public bool DrawLines = true;
        bool Dots;
        public GraphComponent(RectangleF Rect, Color color, int Spacing = 1, bool Dots = false) : base(Rect)
        {
            this.Dots = Dots;
            this.spacing = Spacing;
            Color = color;
            this.Rect = Rect;
            Array = new double[(int)Rect.Width / spacing];
            StartIndex = Array.Length;
            TextRect = new Rectangle((int)Rect.Width, 0, TextDisplayWidth, (int)Rect.Height);
            TextDisplay = new TextDisplay(TextRect);
            ChildElements.Add(TextDisplay);
        }
        public void Add(double a)
        {
            Average = max = min = a;
            if (StartIndex > 0)
                StartIndex--;
            for (int i = StartIndex; i < Array.Length - 1; i++)
            {
                Array[i] = Array[i + 1];
                Average += Array[i];
                max = max > Array[i] ? max : Array[i];
                min = min < Array[i] ? min : Array[i];
            }
            Array[Array.Length - 1] = a;
            Average = Average / Array.Length;
        }
        public void Reset()
        {
            StartIndex = Array.Length;
        }
        public void Reset(double a)
        {
            Average = max = min = a;

            for (int i = 0; i < Array.Length; i++)
            {
                Array[i] = a;
            }
        }
        public void Reset(int[] Arr)
        {
            for (int i = 0; i < Array.Length; i++)
            {
                Array[i] = Arr[i];
                Average += Array[i];
                max = max > Array[i] ? max : Array[i];
                min = min < Array[i] ? min : Array[i];
            }
            Average = Average / Array.Length;
        }
        public override void Update()
        {

        }
        public override void Show(Main M)
        {
            PointF[] Points = new PointF[Array.Length];

            double slope = (Rect.Height / (max - min));

            double offset = (-(max) * slope + Rect.Height / 2);
            double LineSpace;
            double StartPoint;
            if (max == min)
            {
                LineSpace = 1;
                StartPoint = max;
                slope = 0;
                offset = 0;
            }
            else
            {
                double Power = IntegerLogPower(max - min);//gets the closest power of 10 below (max - min)
                double Fraction = (max - min) / Power;
                int K = (int)(Fraction / 5);
                LineSpace = ((1 - K) / 2.0 + K) * Power;
                StartPoint = LineSpace * (Math.Ceiling(min / LineSpace));
            }
            GL.LineWidth(2);
            GL.Color3(0, 0, 0);
            GL.Begin(PrimitiveType.Lines);
            StringFormat SF = new StringFormat()
            {
                LineAlignment = StringAlignment.Center
            };
            TextDisplay.PrepareWrite();
            if(DrawLines)
            for (double i = StartPoint; i <= max; i += LineSpace)
            {
                double Y = Rect.Height / 2 - (i * slope + offset);
                GL.Vertex2(0, Y);
                GL.Vertex2(Rect.Width, Y);
                TextDisplay.gfx.DrawString(i.ToString(), Font, Brushes.White, 7, (int)Y, SF);
            }

            TextDisplay.PrepareDraw();
            GL.End();
            GL.Color3(Color);
            if (Dots)
            {
                GL.Begin(PrimitiveType.Quads);
                int Radius = 3;
                for (int i = StartIndex; i < Points.Length; i++)
                {
                    double X = i * spacing;
                    double Y = Rect.Height / 2 - (Array[i] * slope + offset);
                    GL.Vertex2(X - Radius, Y - Radius);
                    GL.Vertex2(X + Radius, Y - Radius);
                    GL.Vertex2(X + Radius, Y + Radius);
                    GL.Vertex2(X - Radius, Y + Radius);
                }
                GL.End();
            }
            else
            {
                GL.Begin(PrimitiveType.LineStrip);
                for (int i = StartIndex; i < Points.Length; i++)
                {
                    GL.Vertex2(i * spacing, Rect.Height / 2 - (Array[i] * slope + offset));
                }
                GL.End();
            }
        }
        double IntegerLogPower(double Input)
        {
            if (Input <= 0)
                throw new Exception("Input to a logarithm must be more than 0");
            double K = 1;
            while (Input >= K * 10)
            {
                K *= 10;
            }
            while (Input < K)
            {
                K /= 10;
            }
            return K;
        }
    }
}
