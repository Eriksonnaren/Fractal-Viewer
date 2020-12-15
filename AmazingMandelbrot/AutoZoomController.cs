using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazingMandelbrot
{
    class AutoZoomController
    {
        double ZoomSpeed = 1.07;//must be above 1.0
        double StartZoom = 4;
        int TargetIter = 0;
        public GuiComponents.FractalWindow MainWindow;
        public GuiComponents.FractalWindow JuliaWindow;
        public GuiComponents.ColorEditor ColorEditor;
        double TargetZoom;
        float TargetRotation;
        bool Prepared = false;
        bool Active = false;
        int Frames;
        double Progress=0;
        double TotalTime;
        bool JuliaActive;
        public void Prepare()
        {
            Frames = 0;
            Progress = 0;
            TargetIter = MainWindow.Controller.Iterations;
            TargetZoom = MainWindow.Controller.Zoom;
            TargetRotation = MainWindow.Controller.Angle;
            JuliaActive = JuliaWindow.Enabled;
            foreach (var item in MainWindow.ChildElements)
            {
                item.Enabled = false;
            }
            JuliaWindow.Enabled = JuliaActive;
            MainWindow.Controller.Angle = 0;

            MainWindow.Controller.Zoom = StartZoom;
            MainWindow.Controller.Compute();
            if (JuliaActive)
            {
                JuliaWindow.Controller.CameraPos = new Complex(0, 0);
                JuliaWindow.Controller.Angle = 0;
                JuliaWindow.Controller.Zoom = Math.Sqrt(2 * MainWindow.Controller.Zoom); ;
                JuliaWindow.Controller.Compute();
            }
            Prepared = true;
            TotalTime = -Math.Log(TargetZoom/ StartZoom) /Math.Log(ZoomSpeed);

        }
        public void Start()
        {
            if(Prepared&&! Active)
            {
                Active = true;
                Prepared = false;
            }else if(Active)
            {
                Active = false;
            }
        }
        public void Update()
        {
            if(Active)
            {
                if(Frames< TotalTime)
                {
                    Frames++;
                    Progress = Frames / TotalTime;
                    MainWindow.Controller.Angle = (float)Progress * TargetRotation;
                    MainWindow.Controller.Zoom /= ZoomSpeed;
                    MainWindow.Controller.Iterations = (int)Lerp(300, TargetIter, (float)Progress);
                    MainWindow.Controller.Compute();
                    if (JuliaActive)
                    {
                       JuliaWindow.Controller.Angle = (float)Progress * TargetRotation;
                        JuliaWindow.Controller.Zoom = Math.Sqrt(2 * MainWindow.Controller.Zoom);
                        JuliaWindow.Controller.Compute();
                    }
                }else
                {
                    Prepared = false;
                    Active = false;
                }
            }
        }
        float Lerp(float A, float B, float T) => A * (1 - T) + T * B;
    }
}
