using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AmazingMandelbrot.GuiComponents
{
    class FractalWindow : GuiElement
    {
        
        public ShaderController Controller;
        public bool EnableInteraction = true;
        public double ZoomSpeed = 1.5;
        public short LineStrippleOffset = 15;
        public FractalMath fractalMath = new FractalMath();
        public bool OrbitActive;
        public Complex OrbitPosition;
        public float OrbitScale = 1;
        public int StrippleScale = 2;
        public FractalWindow(RectangleF Rect) : base(Rect)
        {
            Controller = new ShaderController((int)Rect.Width, (int)Rect.Height);
            Controller.fractalWindow = this;
            Controller.camera3D = new Camera3d(new Vector3(0,3,-3)*200,Rect,this);
            //Controller.projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, Rect.Width, Rect.Height, 0, 1f, -1f);
            //Controller.projectionMatrix = ;
            ScrollEvent += Scroll;
            
            DragEvent += Drag;
            
        }
        public override void Update()
        {
            Controller.projectionMatrix = projectionMatrix;


        }
        public override void Show(Main D)
        {
            Controller.projectionMatrix = projectionMatrix;
            Controller.Draw();
            
            if (OrbitActive)
            {
                GL.Enable(EnableCap.StencilTest);
                GL.StencilMask(~0);
                GL.ClearStencil(0);
                GL.Clear(ClearBufferMask.StencilBufferBit);

                GL.StencilFunc(StencilFunction.Always, 1, ~0);
                GL.StencilOp( StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
                

                GL.ColorMask(false,false,false,false);
                GL.DepthMask(false);
                GL.Rect(0,0,Rect.Width, Rect.Height);
                GL.ColorMask(true, true, true, true);
                GL.DepthMask(true);
                GL.StencilFunc( StencilFunction.Equal, 1, ~0);
                GL.StencilMask(0);
                fractalMath.CoefficientArray = Controller.CoefficientArray;
                int PathLength = 30;
                Complex[] Path = new Complex[PathLength];
                Vector2[] WorldPath = new Vector2[PathLength];
                Complex Z;
                Complex C;
                if(Controller.Julia)
                {
                    Z = OrbitPosition;
                    C = Controller.JuliaPos;
                    fractalMath.SetCoefficients(C);

                }
                else
                {
                    Z = new Complex(0, 0);
                    C = OrbitPosition;
                    fractalMath.SetCoefficients(C);
                    Z = fractalMath.Compute(Z);
                }
                
                for (int i = 0; i < PathLength; i++)
                {
                    Path[i] = Z;
                    WorldPath[i] = GetScreenFromWorld(Z);
                    Z = fractalMath.Compute(Z);
                    if (Z.MagSq() > 100)
                    {
                        PathLength = i + 1;
                        break;
                    }
                }
                GL.LineWidth(OrbitScale*2);
                GL.Color3(Color.White);
                GL.Enable(EnableCap.LineStipple);
                LineStrippleOffset <<= 1;
                if ((LineStrippleOffset & 256) > 0)
                {
                    LineStrippleOffset++;
                }
                GL.LineStipple(StrippleScale, LineStrippleOffset);
                for (int i = 0; i < PathLength - 1; i++)
                {
                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex2(WorldPath[i]);
                    GL.Vertex2(WorldPath[i + 1]);
                    GL.End();
                }
                GL.Disable(EnableCap.LineStipple);
                Vector2 V1 = new Vector2(3, 0)* OrbitScale;
                Vector2 V2 = new Vector2(0, 3)* OrbitScale;
                GL.Begin(PrimitiveType.Quads);
                for (int i = 0; i < PathLength; i++)
                {
                    GL.Color3(Color.Gray);
                    GL.Vertex2(WorldPath[i] + V1 * 1.5f);
                    GL.Vertex2(WorldPath[i] + V2 * 1.5f);
                    GL.Vertex2(WorldPath[i] - V1 * 1.5f);
                    GL.Vertex2(WorldPath[i] - V2 * 1.5f);
                    GL.Color3(Color.Orange);
                    GL.Vertex2(WorldPath[i] + V1);
                    GL.Vertex2(WorldPath[i] + V2);
                    GL.Vertex2(WorldPath[i] - V1);
                    GL.Vertex2(WorldPath[i] - V2);
                }
                GL.End();
                
                
               
                GL.Disable(EnableCap.StencilTest);
            }
        }
        void Scroll(GuiElement sender, PointF MousePos,int Dir)
        {
            if (EnableInteraction && Main.PolynomialAnimationTimer >= 1)
            {
                if (Controller.MeshActive)
                {
                    if (Dir < 0)
                    {
                        Controller.CameraDistance *= 1.2;
                    }else if (Dir > 0)
                    {
                        Controller.CameraDistance /= 1.2;
                    }
                }
                else
                {
                    double ScaleFactor = 1;
                    if (Dir < 0)
                    {
                        ScaleFactor = ZoomSpeed;
                    }
                    else if (Dir > 0)
                    {
                        ScaleFactor = 1 / ZoomSpeed;
                    }
                    Complex CurrentPos = GetWorldFromScreen(new Vector2(MousePos.X, MousePos.Y));
                    CurrentPos -= Controller.CameraPos;
                    CurrentPos += Controller.CameraPos;
                    Controller.CameraPos = Controller.CameraPos * ScaleFactor + CurrentPos * (1 - ScaleFactor);
                    Controller.Zoom *= ScaleFactor;

                    Controller.Compute();
                }
            }
        }
        void Drag(GuiElement Sender, PointF MousePos, PointF StartPos, PointF DeltaPos, MouseButtons ButtonStatus)
        {
            if (EnableInteraction&& ButtonStatus== MouseButtons.Left&&Main.PolynomialAnimationTimer>=1)
            {
                if (Controller.MeshActive)
                {
                    Controller.CameraAngleY -= DeltaPos.Y * 0.005;
                    Controller.CameraAngleX += DeltaPos.X * 0.005;
                    Controller.CameraAngleY = Math.Max(Math.Min(Controller.CameraAngleY,1.5),-1.5);
                }
                else
                {
                    Controller.CameraPos += Rotate(new Complex(DeltaPos.X, DeltaPos.Y), Controller.Angle) * Controller.Zoom / Rect.Width * 2;
                    Controller.PixelShift = new Point((int)DeltaPos.X, (int)DeltaPos.Y);
                    Controller.Compute();
                    //Controller.Draw();
                }
            }
        }
        public Complex Rotate(Complex C,float Angle)
        {
            double cos = Math.Cos(Angle);
            double sin = Math.Sin(Angle);
            return new Complex(C.real*cos+ C.imag * sin, C.imag * cos - C.real * sin);
        }
        public void Resize(float Width,float Height)
        {
            Controller.Resize((int)Width, (int)Height);
            Rect.Width = Width;
            Rect.Height = Height;
        }
        public Complex GetWorldFromScreen(Vector2 Point)
        {
            Complex Rotation = new Complex(Math.Cos(Controller.Angle), -Math.Sin(Controller.Angle));
            Complex position = ((new Complex(Point.X,Point.Y)) + new Complex(0, (Rect.Width - Rect.Height) / 2)) / Rect.Width;
            return ((position  * 2 * Controller.Zoom - new Complex(Controller.Zoom, Controller.Zoom)) * Rotation + Controller.CameraPos)  ;
        }
        public Vector2 GetScreenFromWorld(Complex Complex)
        {
            Complex Rotation = new Complex(Math.Cos(Controller.Angle), -Math.Sin(Controller.Angle));
            Complex position = ((Complex - Controller.CameraPos) / Rotation + new Complex(Controller.Zoom, Controller.Zoom)) / (2 * Controller.Zoom );
            Complex Point = position * Rect.Width - new Complex(0, (Rect.Width - Rect.Height) / 2);
            return new Vector2((float)Point.real, (float)Point.imag);
            
        }
    }
}
