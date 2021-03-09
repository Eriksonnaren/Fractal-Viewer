using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmazingMandelbrot.GuiComponents;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AmazingMandelbrot.GuiComponents
{
    
    class ParameterController : GuiElement
    {
        public class ParameterContainer
        {
            public char variable;
            public TextDisplay variableDisplay;
            public TextDisplay numberDisplay;
            public FractalWindow linkedFractalWindow;
        }
        List<PolynomialParser2.PolynomialNode> PolynomialList;
        public List<char> Variables = new List<char>();
        PolynomialParser2 parser;
        List<Complex> ParameterValues;
        public List<ParameterContainer> parameterContainers=new List<ParameterContainer>();
        const int VariableDisplaySize = 40;
        public ParameterController(Rectangle rect, PolynomialParser2 parser) : base(rect)
        {
            this.parser = parser;
            parser.parseEvent += HasParsed;
            Variables = parser.Variables;
            PolynomialList = parser.PolynomialList;
            ParameterValues = new List<Complex> { 0,0};
            parameterContainers = new List<ParameterContainer>();
            for (int i = 0; i < 2; i++)
            {
                AddVariable(Variables[i]);
                ParameterValues[i] = 0;
            }
            UpdatePositions();
        }

        public override void Update()
        {
            
        }

        public override void Show(Main M)
        {
            GL.LineWidth(1);
            GL.Begin(PrimitiveType.Lines);
            GL.Color3(Color.White);
            for (int i = 1; i < parameterContainers.Count; i++)
            {
                float Y = parameterContainers[i].numberDisplay.Rect.Y - 10;
                GL.Vertex2(15, Y);
                GL.Vertex2(Rect.Width - 15, Y);

            }
            GL.End();
            for (int i = 0; i < parameterContainers.Count; i++)
            {
                var p = parameterContainers[i];
                p.numberDisplay.PrepareWrite();
                //p.numberDisplay.DrawCenteredText(ParameterValues[i].ToString(), Brushes.Black, 8);
                p.numberDisplay.PrepareDraw();
                if(p.linkedFractalWindow!=null)
                {
                    Matrix4 T = Matrix4.CreateTranslation(10 + p.variableDisplay.Rect.Right, p.numberDisplay.Rect.Y, 0);
                    p.linkedFractalWindow.Controller.DrawExternal(VariableDisplaySize, VariableDisplaySize,T*projectionMatrix);
                }
            }
        }
        void HasParsed()
        {
            for (int i = 0; i < Variables.Count; i++)
            {
                //checks if the parsed output has a variable that does not already exist in the list
                if(!parameterContainers.Exists(x=>x.variable==Variables[i]))
                {
                    AddVariable(Variables[i]);
                }
            }
            for (int i = parameterContainers.Count-1; i >= 0; i--)
            {
                //checks if the parameter list has a variable that the parsed output does not contains
                if (!Variables.Contains(parameterContainers[i].variable))
                {
                    RemoveVariable(i);
                }
            }
            UpdatePositions();
        }
        void UpdatePositions()
        {
            int Y = 0;
            for (int i = 0; i < parameterContainers.Count; i++)
            {
                Y += 10;
                var p = parameterContainers[i];
                p.variableDisplay.Rect.Y = Y;
                p.numberDisplay.Rect.Y = Y;
                Y += VariableDisplaySize + 10;
            }
            Rect.Height = Y;
        }
        void AddVariable(char v)
        {
            Complex pv = 0;
            ParameterValues.Add(pv);
            var container = new ParameterContainer();
            container.variable = v;
            container.variableDisplay = new TextDisplay(new RectangleF(10, 0, VariableDisplaySize, VariableDisplaySize));
            container.variableDisplay.PrepareWrite();
            container.variableDisplay.DrawCenteredText(v.ToString(), Brushes.Black, 10);
            container.variableDisplay.PrepareDraw();
            ChildElements.Add(container.variableDisplay);

            container.numberDisplay = new TextDisplay(new RectangleF(20 + VariableDisplaySize, 0, Rect.Width - VariableDisplaySize * 2 - 30, VariableDisplaySize));
            container.numberDisplay.PrepareWrite();
            container.numberDisplay.DrawCenteredText(pv.ToString(), Brushes.Black, 8);
            container.numberDisplay.PrepareDraw();
            container.numberDisplay.DrawFrame = false;
            ChildElements.Add(container.numberDisplay);

            parameterContainers.Add(container);
        }
        void RemoveVariable(int index)
        {
            ChildElements.Remove(parameterContainers[index].numberDisplay);
            ChildElements.Remove(parameterContainers[index].variableDisplay);
            parameterContainers.RemoveAt(index);
            ParameterValues.RemoveAt(index);

        }
    }
}
