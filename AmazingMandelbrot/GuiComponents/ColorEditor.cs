using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;

namespace AmazingMandelbrot.GuiComponents
{
    class ColorEditor : GuiElement
    {
        const int MaxPaletteSize = 8;
        public Color4[] ColorPalette=new Color4[MaxPaletteSize];
        public float[] PalettePositions = new float[MaxPaletteSize];
        public Color4 InteriorColor;
        public int CurrentPaletteSize;
        float MainCircleOuterRadius;
        float MainCircleInnerRadius;
        float InnerCircleRadius;
        Vector2 MainCirclePosition;
        const int CircleSteps = 180;
        const float NodeWidth = 0.1f;
        public float RotationSpeed = 0;
        bool IsDraggingCircle=false;
        bool IsHoldingCircle = false;
        bool IsHoldingNode = false;
        int SelectedIndex = -1;
        Slider[] ColorSliders = new Slider[3];
        Slider ColorScaleSlider;
        public Slider DotStrengthSlider;
        public float ColorScale;
        public List<FractalWindow> fractalWindows=new List<FractalWindow>();
        public ColorEditor(RectangleF Rect) : base(Rect)
        {
            float Padding = 10;
            float X = Padding;

            ColorScaleSlider = new Slider(new RectangleF(X,Padding,30,Rect.Height-Padding*2));
            ColorScaleSlider.Value = 0.5;
            ChildElements.Add(ColorScaleSlider);
            X += ColorScaleSlider.Rect.Width + Padding;
            MainCircleOuterRadius = Rect.Height / 2 - Padding;
            MainCirclePosition = new Vector2(X+MainCircleOuterRadius, Padding + MainCircleOuterRadius);
            X += MainCircleOuterRadius * 2 + Padding;
            InnerCircleRadius = MainCircleOuterRadius *0.3f;
            MainCircleInnerRadius = MainCircleOuterRadius * 0.6f;

            ColorPalette[0] = Color4.Orange;
            ColorPalette[1] = Color4.DarkBlue;
            ColorPalette[2] = Color4.Cyan;
            PalettePositions[0] = 0.0f;
            PalettePositions[1] = 0.33f;
            PalettePositions[2] = 0.66f;
            CurrentPaletteSize = 3;
            InteriorColor = Color.Black;
            DragEvent += MainDrag;
            MouseDownEvent += MouseDown;
            HoverEvent += MouseHover;
            MouseUpEvent += MouseUp;
            float SliderHeight = (Rect.Height- Padding*4) / 3;
            Color[] Colors = new Color[] {Color.Red,Color.Green,Color.Blue };
            for (int i = 0; i < 3; i++)
            {
                ColorSliders[i] = new Slider(new RectangleF(X, Padding +(SliderHeight+ Padding) *i,140, SliderHeight));
                ColorSliders[i].BackgroundColor = Colors[i];
                ChildElements.Add(ColorSliders[i]);
            }
            X += 140 + Padding;
            DotStrengthSlider = new Slider(new RectangleF(X, Padding,30 , Rect.Height - Padding * 2));
            DotStrengthSlider.Value = 1;
            ChildElements.Add(DotStrengthSlider);
            ColorScaleSlider.SliderEvent += ScaleSliderChanged;
        }
        public override void Update()
        {

            RotatePalette(RotationSpeed);
            SortPalette();
            UpdateFractalWindows();
            if (SelectedIndex<0)
            {
                InteriorColor.R=(float)ColorSliders[0].Value;
                InteriorColor.G=(float)ColorSliders[1].Value;
                InteriorColor.B= (float)ColorSliders[2].Value;
            }
            else
            {
                ColorPalette[SelectedIndex].R = (float)ColorSliders[0].Value;
                ColorPalette[SelectedIndex].G = (float)ColorSliders[1].Value;
                ColorPalette[SelectedIndex].B = (float)ColorSliders[2].Value;
            }
            
        }
        public void RotatePalette(float Amount)
        {
            for (int i = 0; i < CurrentPaletteSize; i++)
            {
                PalettePositions[i] += Amount;
                PalettePositions[i] = (PalettePositions[i] + 1) % 1;
            }
        }
        void SortPalette()
        {
            for (int i = 0; i < CurrentPaletteSize-1; i++)
            {
                for (int j = i+1; j < CurrentPaletteSize; j++)
                {
                    if(PalettePositions[i]> PalettePositions[j])
                    {
                        if (SelectedIndex == i)
                        {
                            SelectedIndex=j;
                        }
                        else if (SelectedIndex == j)
                        {
                            SelectedIndex=i;
                        }
                        float temp = PalettePositions[j];
                        PalettePositions[j] = PalettePositions[i];
                        PalettePositions[i] = temp;
                        Color4 tempColor = ColorPalette[j];
                        ColorPalette[j] = ColorPalette[i];
                        ColorPalette[i] = tempColor;
                    }
                }
            }
        }
        public override void Show(Main D)
        {
            
            GL.Begin(PrimitiveType.TriangleFan);
            GL.Vertex2(MainCirclePosition);
            for (int i = 0; i <= CircleSteps; i++)
            {
                float T = i / (float)CircleSteps;
                double Angle = T *Math.PI*2;
                GL.Color4(GetColorFromPosition(T));
                Vector2 V = new Vector2((float)Math.Sin(Angle), -(float)Math.Cos(Angle))*MainCircleOuterRadius;
                GL.Vertex2(V+MainCirclePosition);
            }
            GL.End();
            
            GL.Begin(PrimitiveType.Triangles);
            for (int i = 0; i < CurrentPaletteSize; i++)
            {
                double Angle = PalettePositions[i] * Math.PI * 2;
                Vector2 V1 = new Vector2((float)Math.Sin(Angle - NodeWidth), -(float)Math.Cos(Angle - NodeWidth)) * MainCircleOuterRadius;
                Vector2 V2 = new Vector2((float)Math.Sin(Angle + NodeWidth), -(float)Math.Cos(Angle + NodeWidth)) * MainCircleOuterRadius;
                if (SelectedIndex == i)
                {
                    V1 *= 1.1f;
                    V2 *= 1.1f;
                }
                GL.Color4(ColorPalette[i]);
                GL.Vertex2(MainCirclePosition);
                GL.Vertex2(MainCirclePosition + V1);
                GL.Vertex2(MainCirclePosition + V2);
            }
            GL.End();
            GL.LineWidth(2);
            GL.Begin(PrimitiveType.Lines);
            
            
            for (int i = 0; i < CurrentPaletteSize; i++)
            {
                double Angle = PalettePositions[i] * Math.PI * 2;
                Vector2 V1 = new Vector2((float)Math.Sin(Angle - NodeWidth), -(float)Math.Cos(Angle - NodeWidth)) * MainCircleOuterRadius;
                Vector2 V2 = new Vector2((float)Math.Sin(Angle + NodeWidth), -(float)Math.Cos(Angle + NodeWidth)) * MainCircleOuterRadius;
                if (SelectedIndex==i)
                {
                    V1 *= 1.1f;
                    V2 *= 1.1f;
                }
                GL.Color3(Color.LightGray);
                GL.Vertex2(MainCirclePosition);
                GL.Vertex2(MainCirclePosition + V1);
                GL.Vertex2(MainCirclePosition);
                GL.Vertex2(MainCirclePosition + V2);
            }
            GL.End();
            /*if (IsHoldingCircle)
            {
                GL.Color3(Color.OrangeRed);
            }
            else
            {
                GL.Color4(InteriorColor);
            }*/
            float Rad = InnerCircleRadius;
            if (SelectedIndex == -1)
            {
                Rad *= 1.3f;
            }
            FillCircle(BackgroundColor, MainCircleInnerRadius);
            FillCircle(InteriorColor, Rad);
            DrawCircle(Color.LightGray, Rad);
            
            IsDraggingCircle = false;
        }
        public void UpdateFractalWindows()
        {
            ColorScale = GetColorScale(ColorScaleSlider.Value);
            
            for (int i = 0; i < fractalWindows.Count; i++)
            {
                fractalWindows[i].Controller.ColorPalette = ColorPalette;
                fractalWindows[i].Controller.PalettePositions = PalettePositions;
                fractalWindows[i].Controller.InteriorColor = InteriorColor;
                fractalWindows[i].Controller.ColorScale = ColorScale;
                fractalWindows[i].Controller.PaletteSize = CurrentPaletteSize;
            }
            
        }
        void FillCircle(Color4 Col,float Radius)
        {
            GL.Color4(Col);
            GL.Begin(PrimitiveType.TriangleFan);
            GL.Vertex2(MainCirclePosition);
            for (int i = 0; i <= CircleSteps; i++)
            {
                float T = i / (float)CircleSteps;
                double Angle = T * Math.PI * 2;
                Vector2 V = new Vector2((float)Math.Sin(Angle), -(float)Math.Cos(Angle)) * Radius;
                GL.Vertex2(V + MainCirclePosition);
            }
            GL.End();
        }
        void DrawCircle(Color Col, float Radius)
        {
            GL.Color3(Col);
            GL.Begin(PrimitiveType.LineLoop);
            //GL.Vertex2(MainCirclePosition);
            for (int i = 0; i < CircleSteps; i++)
            {
                float T = i / (float)CircleSteps;
                double Angle = T * Math.PI * 2;
                Vector2 V = new Vector2((float)Math.Sin(Angle), -(float)Math.Cos(Angle)) * Radius;
                GL.Vertex2(V + MainCirclePosition);
            }
            GL.End();
        }
        void MainDrag(GuiElement Sender, PointF MousePos, PointF StartPos, PointF DeltaPos, MouseButtons ButtonStatus)
        {
            Vector2 Change = new Vector2(DeltaPos.X, DeltaPos.Y);
            Vector2 Pos = new Vector2(MousePos.X, MousePos.Y)- MainCirclePosition;
            double StartAngle = Math.Atan2(Pos.Y, Pos.X);
            double EndAngle = Math.Atan2(Pos.Y+ Change.Y, Pos.X + Change.X);
            float RotationChange = (float)((StartAngle - EndAngle) / (2 * Math.PI));
            if (IsHoldingNode)
            {
                PalettePositions[SelectedIndex] += RotationChange;
            }
            else if (IsHoldingCircle)
            {

                RotationSpeed = RotationChange;
                IsDraggingCircle = true;
            }
        }
        void MouseDown(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            IsHoldingNode = false;

            Vector2 Pos = new Vector2(MousePos.X, MousePos.Y)- MainCirclePosition;
            if (Pos.LengthSquared < InnerCircleRadius * InnerCircleRadius)
            {
                SelectedIndex = -1;
                SetSlidersFromColor(InteriorColor);
                
            }
            else
            if (Pos.LengthSquared < MainCircleOuterRadius * MainCircleOuterRadius)
            {
                
                RotationSpeed = 0;
                bool HasRemoved = false;
                for (int i = 0; i < CurrentPaletteSize; i++)
                {
                    double Angle = PalettePositions[i] * Math.PI * 2;
                    Vector2 V = new Vector2((float)Math.Sin(Angle), -(float)Math.Cos(Angle));

                    if(InsideTriangle(Pos,V))
                    {
                        
                        if (ButtonStatus == MouseButtons.Right&& CurrentPaletteSize>2)
                        {
                            for (int j = i; j < CurrentPaletteSize-1; j++)
                            {
                                PalettePositions[j] = PalettePositions[j+1];
                                ColorPalette[j] = ColorPalette[j + 1];
                            }
                            CurrentPaletteSize--;
                            HasRemoved = true;
                            SelectedIndex = -1;
                            SetSlidersFromColor(InteriorColor);
                            break;
                        }
                        else
                        {
                            SelectedIndex = i;
                            SetSlidersFromColor(ColorPalette[i]);
                            IsHoldingNode = true;
                        }
                        
                    }
                }
                if(!IsHoldingNode&& ButtonStatus == MouseButtons.Right && CurrentPaletteSize < 8&&!HasRemoved)
                {
                    float Angle = (float)(Math.Atan2(Pos.X, -Pos.Y) / (2 * Math.PI));
                    Angle = (Angle + 1) % 1;
                    ColorPalette[CurrentPaletteSize] = GetColorFromPosition(Angle);
                    SelectedIndex = CurrentPaletteSize;
                    SetSlidersFromColor(ColorPalette[CurrentPaletteSize]);
                    IsHoldingNode = true;
                    PalettePositions[CurrentPaletteSize] = Angle;
                    CurrentPaletteSize++;
                    SortPalette();
                    UpdateFractalWindows();
                }
                IsHoldingCircle = true;

            }
        }
        void SetSlidersFromColor(Color4 Col)
        {
            ColorSliders[0].Value = Col.R;
            ColorSliders[1].Value = Col.G;
            ColorSliders[2].Value = Col.B;
        }
        void ScaleSliderChanged(double newValue)
        {
            float OldColorScale = ColorScale;
            ColorScale = GetColorScale(newValue);
            double Iter = fractalWindows[0].Controller.AverageIteration/200;
            double Offset = Iter * (ColorScale- OldColorScale);
            RotatePalette((float)Offset);
        }
        float GetColorScale(double Value)
        {
            float T = (float)(Value * Value);
            return Lerp(0.5f, 10, T);
        }
        bool InsideTriangle(Vector2 Pos,Vector2 TrianglePos)
        {
            Vector2 V = new Vector2(Vector2.Dot(Pos, TrianglePos), Vector2.PerpDot(Pos, TrianglePos));
            V /= TrianglePos.Length;
            V = V*new Vector2(1, 1/NodeWidth);
            return V.X > 0 && Math.Abs(V.Y) < V.X;
        }
        void MouseHover(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            Vector2 Pos = new Vector2(MousePos.X, MousePos.Y) - MainCirclePosition;
            if (ButtonStatus != MouseButtons.None&& !IsDraggingCircle&& Pos.LengthSquared < MainCircleOuterRadius * MainCircleOuterRadius)
            {
                RotationSpeed = 0;
            }
            
        }
        void MouseUp(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            IsHoldingCircle = false;
            IsHoldingNode = false;
        }
        Color4 LerpColor(Color4 A,Color4 B,float t)
        {
            return new Color4(Lerp(A.R,B.R,t), Lerp(A.G, B.G, t), Lerp(A.B, B.B, t),1);
        }
        float Beizer(float t) => t*t*(3-2*t);
        
        Color4 GetColorFromPosition(float t)
        {
            int Id1 = CurrentPaletteSize - 1;
            int Id2 = 0;
            while(t> PalettePositions[Id2]&& Id2< CurrentPaletteSize)
            {
                Id1++;
                Id2++;
                Id1 = Id1 % CurrentPaletteSize;
            }
            Id2 = Id2 % CurrentPaletteSize;
            float LerpParameter = ((t- PalettePositions[Id1]+1)%1) /((PalettePositions[Id2]- PalettePositions[Id1]+1)%1);
            return LerpColor(ColorPalette[Id1], ColorPalette[Id2], Beizer(LerpParameter));
        }
        float Lerp(float a, float b, float t) => a * (1.0f - t) + b * t;
        
    }
}
