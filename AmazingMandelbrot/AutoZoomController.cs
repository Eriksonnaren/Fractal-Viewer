using System;

namespace AmazingMandelbrot
{
    class AutoZoomController
    {
        struct LocationData
        {
            public Complex CameraPosition;
            public double LogarithmicZoomFactor;
            public float Angle;
            public int Iterations;
            public Complex[,] CoefficientArray;
        }
        LocationData StartLocation;
        LocationData EndLocation;
        LocationData CurrentLocation;

        const double AnimationSpeed = 0.15;
        const double EndSmoothingFactor = 1;
        const double ZoomSmoothFactor = 5;
        const double PositionSmoothFactor = 1;
        public GuiComponents.FractalWindow MainWindow;
        public GuiComponents.FractalWindow JuliaWindow;
        public GuiComponents.ColorEditor ColorEditor;
        bool Prepared = false;
        bool Active = false;
        double Progress=0;//goes linearly from 0 to TotalTime
        double TotalTime;
        double TotalSmoothTime;//shorter time interval that slows down at the endpoints
        double ZoomOutTime;
        double ZoomInTime;
        bool JuliaActive;
        double MaximumZoomValue;
        public void PrepareForStaticZoom()
        {
            
            EndLocation = GetDataFromWindow(MainWindow.Controller);
            MainWindow.Controller.Angle = 0;
            MainWindow.Controller.Zoom = 4;
            StartLocation = GetDataFromWindow(MainWindow.Controller);
            MainWindow.Controller.Compute();
            PreCompute();

        }
        public void PrepareFromExisting(GuiComponents.FractalWindow Original)
        {
            StartLocation = GetDataFromWindow(MainWindow.Controller);
            EndLocation = GetDataFromWindow(Original.Controller);
            PreCompute();
        }
        void PreCompute()
        {
            double m = (StartLocation.CameraPosition - EndLocation.CameraPosition).Mag()*2;
            MaximumZoomValue = Math.Log(m);
            MaximumZoomValue = Math.Max(MaximumZoomValue,Math.Max(StartLocation.LogarithmicZoomFactor,EndLocation.LogarithmicZoomFactor));

            ZoomOutTime = Math.Sqrt(Math.Max(Sq(ZoomSmoothFactor + MaximumZoomValue - StartLocation.LogarithmicZoomFactor) -Sq(ZoomSmoothFactor), 0));
            ZoomInTime = Math.Sqrt(Math.Max(Sq(ZoomSmoothFactor + MaximumZoomValue - EndLocation.LogarithmicZoomFactor) - Sq(ZoomSmoothFactor), 0));
            TotalSmoothTime = ZoomOutTime + ZoomInTime;
            //TotalSmoothTime = Math.Max(TotalSmoothTime, 2);
            TotalTime = Math.Sqrt(TotalSmoothTime * (TotalSmoothTime + 4 * EndSmoothingFactor));
            Progress = 0;
            JuliaActive = JuliaWindow.Enabled;
            Prepared = true;
            SetJuliaParameters();
        }
        double GetSmoothedTime(double t)
        {
            if(t< TotalTime/2)
            {
                return Math.Sqrt(Sq(t) + Sq(EndSmoothingFactor)) - EndSmoothingFactor;
            }
            return TotalSmoothTime - (Math.Sqrt(Sq(t- TotalTime) + Sq(EndSmoothingFactor)) - EndSmoothingFactor);
        }
        LocationData GetDataFromWindow(ShaderController controller)
        {
            LocationData dat = new LocationData();
            dat.CameraPosition = controller.CameraPos;
            dat.Iterations = controller.Iterations;
            dat.Angle = controller.Angle;
            dat.LogarithmicZoomFactor = Math.Log(controller.Zoom);
            dat.CoefficientArray = controller.CoefficientArray;
            return dat;

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
                if(Progress < TotalTime)
                {
                    Progress += AnimationSpeed;
                    if(Progress> TotalTime)
                    {
                        Progress = TotalTime;
                    }
                    double SmoothTime = GetSmoothedTime(Progress);
                    SmoothTime -= ZoomOutTime;
                    CurrentLocation.LogarithmicZoomFactor = GetZoom(SmoothTime);
                    double LerpParameter = GetPositionLerp(SmoothTime);
                    CurrentLocation.CameraPosition = StartLocation.CameraPosition * (1 - LerpParameter) + EndLocation.CameraPosition * LerpParameter;
                    int MaxZ = Math.Max(StartLocation.CoefficientArray.GetLength(0), EndLocation.CoefficientArray.GetLength(0));
                    int MaxC = Math.Max(StartLocation.CoefficientArray.GetLength(1), EndLocation.CoefficientArray.GetLength(1));
                    CurrentLocation.CoefficientArray = new Complex[MaxZ, MaxC];
                    for (int i = 0; i < MaxZ; i++)
                    {
                        for (int j = 0; j < MaxC; j++)
                        {
                            Complex A = new Complex();
                            if(i< StartLocation.CoefficientArray.GetLength(0)&& j < StartLocation.CoefficientArray.GetLength(1))
                            {
                                A += StartLocation.CoefficientArray[i, j] * (1 - LerpParameter);
                            }
                            if (i < EndLocation.CoefficientArray.GetLength(0) && j < EndLocation.CoefficientArray.GetLength(1))
                            {
                                A += EndLocation.CoefficientArray[i, j] * LerpParameter;
                            }
                            CurrentLocation.CoefficientArray[i, j] = A;
                        }
                    }
                    MainWindow.Controller.CoefficientArray = CurrentLocation.CoefficientArray;
                    MainWindow.Controller.Zoom = Math.Exp(CurrentLocation.LogarithmicZoomFactor);
                    MainWindow.Controller.CameraPos = CurrentLocation.CameraPosition;

                    MainWindow.Controller.Compute();
                    SetJuliaParameters();
                }else
                {
                    Prepared = false;
                    Active = false;
                }
            }
        }
        double GetZoom(double t)
        {
            return ZoomSmoothFactor - Math.Sqrt(Sq(ZoomSmoothFactor) + Sq(t)) + MaximumZoomValue;
        }
        double GetPositionLerp(double t)
        {
            return (GetPositionFactor(t) - GetPositionFactor(-ZoomOutTime)) / (GetPositionFactor(ZoomInTime) - GetPositionFactor(-ZoomOutTime));

        }
        double GetPositionFactor(double t)
        {
            return 1 / (1 + Math.Exp(-PositionSmoothFactor*t));
        }
        void SetJuliaParameters()
        {
            JuliaWindow.Controller.CoefficientArray = CurrentLocation.CoefficientArray;
            if (JuliaActive)
            {
                JuliaWindow.Controller.CameraPos = new Complex(0, 0);
                JuliaWindow.Controller.JuliaPos = CurrentLocation.CameraPosition;
                JuliaWindow.Controller.Angle = CurrentLocation.Angle;
                JuliaWindow.Controller.Zoom = Math.Sqrt(2 * Math.Exp(CurrentLocation.LogarithmicZoomFactor));
                JuliaWindow.Controller.Compute();
            }
        }
        double Sq(double x) => x * x;
        float Lerpf(float A, float B, float T) => A * (1 - T) + T * B;
        double Lerpd(double A, double B, double T) => A * (1 - T) + T * B;
    }
}
