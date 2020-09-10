using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace AmazingMandelbrot.GuiComponents
{
    class FractalWindow : GuiElement
    {
        
        public ShaderController Controller;
        public bool EnableInteraction = true;
        public double ZoomSpeed = 1.5;
        public FractalWindow(RectangleF Rect) : base(Rect)
        {
            Controller = new ShaderController((int)Rect.Width, (int)Rect.Height);
            ScrollEvent += Scroll;
            
            DragEvent += Drag;
        }
        public override void Update()
        {
            
            
        }
        public override void Show(Main D)
        {
            Controller.Draw();
        }
        void Scroll(GuiElement sender, PointF MousePos,int Dir)
        {
            if (EnableInteraction)
            {

                double ScaleFactor = 1;
                if(Dir<0)
                {
                    ScaleFactor = ZoomSpeed;
                }else if(Dir > 0)
                {
                    ScaleFactor = 1 / ZoomSpeed;
                }
                Complex CurrentPos = GetWorldFromScreen(MousePos);

                Controller.CameraPos = Controller.CameraPos * ScaleFactor + CurrentPos * (1 - ScaleFactor);
                Controller.Zoom *= ScaleFactor;
                /*if (Dir > 0)
                {
                    Controller.CameraPos = ((new Complex(MousePos.X-Rect.Width/2, MousePos.Y - Rect.Height / 2) / Rect.Width) * Controller.Zoom * 2) + Controller.CameraPos;
                    Controller.Zoom /= 2;
                }
                if (Dir < 0)
                {
                    //Controller.CameraPos = ((new Complex(MousePos.X - Rect.Width / 2, MousePos.Y - Rect.Height / 2) / Rect.Width) * Controller.Zoom * 2) + Controller.CameraPos;
                    Controller.Zoom *= 2;
                }*/
                Controller.Compute();
                //Console.WriteLine(Controller.CameraPos.ToString());
            }
        }
        void Drag(GuiElement Sender, PointF MousePos, PointF StartPos, PointF DeltaPos, MouseButtons ButtonStatus)
        {
            if (EnableInteraction&& ButtonStatus== MouseButtons.Left)
            {
                Controller.CameraPos += new Complex(DeltaPos.X, DeltaPos.Y) * Controller.Zoom / Rect.Width * 2;
                Controller.PixelShift = new Point((int)DeltaPos.X, (int)DeltaPos.Y);
                Controller.Compute();
            }
        }
        public void Resize(float Width,float Height)
        {
            Controller.ReSize((int)Width, (int)Height);
            Rect.Width = Width;
            Rect.Height = Height;
        }
        public Complex GetWorldFromScreen(PointF Point)
        {
            Complex position = ((new Complex(Point.X,Point.Y)) + new Complex(0, (Rect.Width - Rect.Height) / 2)) / Rect.Width;
            return position * 2 * Controller.Zoom - new Complex(Controller.Zoom, Controller.Zoom) + Controller.CameraPos;
        }
        public PointF GetScreenFromWorld(Complex Complex)
        {
            Complex position = (Complex - Controller.CameraPos + new Complex(Controller.Zoom, Controller.Zoom)) / (2 * Controller.Zoom);
            Complex Point = position * Rect.Width - new Complex(0, (Rect.Width - Rect.Height) / 2);
            return new PointF((float)Point.real, (float)Point.imag);
            
        }
    }
}
