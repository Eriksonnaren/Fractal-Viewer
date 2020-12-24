using AmazingMandelbrot.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;
using OpenTK;

namespace AmazingMandelbrot.GuiComponents
{
    class ExampleLocationComponent : GuiElement
    {
        const int Width = 300;
        const int Height = 190;
        const int FractalWindowSize = 70;
        const int ImageSize = 50;
        const int SideButtonWidth = 20;
        struct ExampleFunctionStruct
        {
            public string functionString;
            public List<ExampleLocationStructs> exampleLocations;
        }
        struct ExampleLocationStructs
        {
            public Complex position;
            public double zoom;
            public int iter;
        }
        List<ExampleFunctionStruct> Examples=new List<ExampleFunctionStruct>();
        ExampleFunctionStruct currentExample;
        PolynomialParser parser = new PolynomialParser();
        TextDisplay polynomialDisplay;
        FractalWindow FunctionFractalWindow;
        FractalWindow[] ExampleLocationWindows=new FractalWindow[3];
        int currentFunctionId = 0;
        int currentLocationOffsetId = 0;
        public AutoZoomController zoomController;
        public ExampleLocationComponent(float x,float y, AutoZoomController zoomController) : base(new RectangleF(x,y,Width,Height))
        {
            this.zoomController = zoomController;
            string Data = Resources.ExampleLocations;
            string[] lines = Data.Split(new string[] {"\r","\n" },StringSplitOptions.RemoveEmptyEntries);
            
            List<ExampleLocationStructs> locations = new List<ExampleLocationStructs>();
            for (int i = lines.Length-1; i >= 0; i--)
            {
                string[] parts = lines[i].Split(':');
                if(parts[0]=="function")
                {
                    var exampleFunction = new ExampleFunctionStruct();
                    exampleFunction.exampleLocations = locations;
                    exampleFunction.functionString = parts[1];
                    locations = new List<ExampleLocationStructs>();
                    Examples.Insert(0,exampleFunction);
                }
                else if(parts[0]=="location")
                {
                    var exampleLocation = new ExampleLocationStructs();
                    string[] locationParts = parts[1].Split('|');
                    exampleLocation.position = new Complex();
                    if(!double.TryParse(locationParts[0], out exampleLocation.position.real))
                    {
                        Console.WriteLine(locationParts[0] + " is not a valid double");
                        break;
                    }
                    if (!double.TryParse(locationParts[1], out exampleLocation.position.imag))
                    {
                        Console.WriteLine(locationParts[1] + " is not a valid double");
                        break;
                    }
                    if (!double.TryParse(locationParts[2], out exampleLocation.zoom))
                    {
                        Console.WriteLine(locationParts[2] + " is not a valid double");
                        break;
                    }
                    if (!int.TryParse(locationParts[3], out exampleLocation.iter))
                    {
                        Console.WriteLine(locationParts[3] + " is not a valid integer");
                        break;
                    }
                    locations.Add(exampleLocation);
                }
                else
                {
                    Console.WriteLine(parts[0] + " is not a valid modifier");
                    break;
                }
            }
            for (int i = 0; i < ExampleLocationWindows.Length; i++)
            {
                ExampleLocationWindows[i] = new FractalWindow(new RectangleF(10+ SideButtonWidth + (FractalWindowSize + 10) * i, FractalWindowSize + 32, FractalWindowSize, FractalWindowSize));
                ChildElements.Add(ExampleLocationWindows[i]);
                ExampleLocationWindows[i].EnableInteraction = false;
                ExampleLocationWindows[i].MouseDownEvent += FractalWindowClick;
            }

            int X = (FractalWindowSize + 10) * (ExampleLocationWindows.Length-1);
            FunctionFractalWindow = new FractalWindow(new RectangleF(10 + SideButtonWidth + X, 10, FractalWindowSize, FractalWindowSize));
            FunctionFractalWindow.EnableInteraction = false;
            FunctionFractalWindow.MouseDownEvent += FractalWindowClick;
            ChildElements.Add(FunctionFractalWindow);
            polynomialDisplay = new TextDisplay(new RectangleF(10 + SideButtonWidth,10,X-10, FractalWindowSize));
            polynomialDisplay.PrepareDraw();
            ChildElements.Add(polynomialDisplay);
            currentExample = Examples[0];
            MouseDownEvent += MainWindowClicked;
        }

        public override void Show(Main D)
        {
            GL.Color3(FrameColor);
            GL.Rect(0, FractalWindowSize+20,Rect.Width, FractalWindowSize+22);
            float h = Rect.Height / 4;
            float w = 15;
            Vector2[] ArrowPositions=new Vector2[] {
                new Vector2(10, h),
                new Vector2(10, 3*h),
                new Vector2(Rect.Width-10, h),
                new Vector2(Rect.Width-10, 3*h)
            };
            int[] ArrowDirections = new int[] { 1, 1, -1, -1 };
            GL.LineWidth(3);
            GL.Color3(Color.Black);
            GL.Begin(PrimitiveType.Lines);
            int k = 2;
            if (currentExample.exampleLocations.Count > 3)
                k = 1;
            for (int i = 0; i < 4; i+=k)
            {
                GL.Vertex2(ArrowPositions[i]);
                GL.Vertex2(ArrowPositions[i]+ ArrowDirections[i] * new Vector2(w, w*1.5f));
                GL.Vertex2(ArrowPositions[i]);
                GL.Vertex2(ArrowPositions[i]+ ArrowDirections[i] * new Vector2(w, -w*1.5f));
            }
            GL.End();
        }

        public override void Update()
        {

        }
        void MainWindowClicked(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            bool TopClicked = false;
            bool BottomClicked = false;
            if (MousePos.Y <Rect.Height/2)
            {
                TopClicked = true;
            }
            else if(currentExample.exampleLocations.Count>3)
            {
                BottomClicked = true;
            }
            
            if (MousePos.X< SideButtonWidth+10)
            {
                if(TopClicked)
                {
                    currentFunctionId--;
                    currentFunctionId = (currentFunctionId+ Examples.Count) %Examples.Count;
                    currentLocationOffsetId = 0;
                }
                if(BottomClicked)
                {
                    currentLocationOffsetId--;
                    currentLocationOffsetId = (currentLocationOffsetId + currentExample.exampleLocations.Count) % currentExample.exampleLocations.Count;
                }
            }
            else if(MousePos.X > Rect.Width-(SideButtonWidth + 10))
            {
                if (TopClicked)
                {
                    currentFunctionId++;
                    currentFunctionId %= Examples.Count;
                    currentLocationOffsetId = 0;
                }
                if (BottomClicked)
                {
                    currentLocationOffsetId++;
                    currentLocationOffsetId %= currentExample.exampleLocations.Count;
                }
            }
            SetFunctionId();
        }
        void FractalWindowClick(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus)
        {
            if (Sender is FractalWindow window)
            {
                zoomController.PrepareFromExisting(window);
                zoomController.Start();
            }
        }
        public void SetFunctionId()
        {

            currentExample = Examples[currentFunctionId];
            StringFormat SF = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            polynomialDisplay.PrepareWrite();
            polynomialDisplay.gfx.DrawString(currentExample.functionString, new Font("Arial Black", 10), Brushes.Black, polynomialDisplay.Rect.Width / 2, polynomialDisplay.Rect.Height / 2, SF);
            polynomialDisplay.PrepareDraw();

            parser.InputString = currentExample.functionString;
            parser.Parse();
            FunctionFractalWindow.Controller.CoefficientArray = parser.CoefficientArray;
            FunctionFractalWindow.Controller.Compute();
            for (int i = 0; i < ExampleLocationWindows.Length; i++)
            {
                if (currentExample.exampleLocations.Count > i)
                {
                    int j = (i + currentLocationOffsetId) % currentExample.exampleLocations.Count;
                    ExampleLocationWindows[i].Controller.CoefficientArray = parser.CoefficientArray;
                    ExampleLocationWindows[i].Controller.CameraPos = currentExample.exampleLocations[j].position;
                    ExampleLocationWindows[i].Controller.Zoom = currentExample.exampleLocations[j].zoom;
                    ExampleLocationWindows[i].Controller.Iterations = currentExample.exampleLocations[j].iter;
                    ExampleLocationWindows[i].Controller.Compute();
                    ExampleLocationWindows[i].Enabled = true;
                }
                else
                {
                    ExampleLocationWindows[i].Enabled = false;
                }
            }
        }
    }
}
