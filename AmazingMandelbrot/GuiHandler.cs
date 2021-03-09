using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;


namespace AmazingMandelbrot
{
    class GuiHandler
    {
        public delegate void MouseEventHandler(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus);
        public delegate void MouseDragHandler(GuiElement Sender, PointF MousePos, PointF StartPos, PointF DeltaPos, MouseButtons ButtonStatus);
        public delegate void MouseHoverHandler(GuiElement Sender, PointF MousePos, MouseButtons ButtonStatus);
        public delegate void MouseScrollHandler(GuiElement Sender, PointF MousePos, int ScrollDirection);
        public delegate void ExternalDrawEvent(GuiElement Sender, Main M);
        public List<GuiElement> Elements = new List<GuiElement>();
        public PointF MousePos;
        public PointF StartMousePos;
        public PointF MousePosPrev;
        public PointF RelativeMouseOffset;
        PointF LocalRelativeMouseOffset;
        bool IsDragging = false;
        MouseButtons PrevButton;
        public static bool MouseHold;
        public GuiElement CurrentElement = null;
        GuiElement PreviousElement = null;
        Stack<PointF> RelativeMousePosStack = new Stack<PointF>();
        private Matrix4 projectionMatrix;
        private Matrix4 modelViewMatrix;
        public enum CursorTypes
        {
            Default,
            Hand,
            ArrowHorizontal,
            ArrowVertical,
        }
        public static Cursor formCursor;
        public static CursorTypes cursorType{ get { return cursorTypePrivate; } set { cursorTypePrivate = value; cursorChanged = true; } }
        static CursorTypes cursorTypePrivate = CursorTypes.Default;
        static CursorTypes cursorTypeOld = CursorTypes.Default;
        static bool cursorChanged;
        struct ElementDrawStruct
        {
            public GuiElement Element;
            public Matrix Mat;
            public Matrix4 Mat2;

            public ElementDrawStruct(GuiElement element, Matrix mat, Matrix4 mat2)
            {
                Element = element;
                Mat = mat;
                Mat2 = mat2;
            }
        }
        List<List<ElementDrawStruct>> ElementDrawList = new List<List<ElementDrawStruct>>();

        public void ShowAll(Main M)
        {

            foreach (var E in Elements)
            {
                if (E.Enabled)
                    UpdateRecursive(E);
            }
            foreach (var item in ElementDrawList)
            {
                item.Clear();
            }
            ElementDrawList.Clear();
            foreach (var E in Elements)
            {
                //Matrix4.CreateTranslation(E.Rect.X,E.Rect.Y,0)
                if (E.Enabled)
                    PrepareRecursive(M, E, 0, M.projectionMatrix);
            }
            foreach (var E1 in ElementDrawList)
            {
                foreach (var E in E1)
                {
                    //M.G.Transform = E.Mat;
                    Matrix4 Mat = E.Mat2;
                    GL.LoadMatrix(ref Mat);
                    //GL.Viewport((int)E.Element.TranslateVector.X, (int)E.Element.TranslateVector.Y, (int)E.Element.Rect.Width, (int)E.Element.Rect.Height);
                    if (E.Element.DrawFrame)
                        E.Element.DrawFrameAndBackground();
                    //RectangleF R = new RectangleF(0, 0, E.Element.Rect.Width, E.Element.Rect.Height);
                    //M.G.Clip = new Region(R);
                    E.Element.EarlyDraw?.Invoke(E.Element, M);
                    E.Element.Show(M);
                    E.Element.LateDraw?.Invoke(E.Element, M);
                    GL.Clear(ClearBufferMask.DepthBufferBit);
                }
            }
            GL.LoadIdentity();
            //M.G.ResetTransform();
            //M.G.ResetClip();
        }
        public void PrepareAll(Main M)
        {
            foreach (var E in Elements)
            {
                PrepareMatricies(M, E, 0, M.projectionMatrix);
            }
        }
        void PrepareMatricies(Main M, GuiElement E, int Layer, Matrix4 matrix4)
        {
            Layer += E.DrawingLayerOffset;
            matrix4.Row3 += new Vector4(2 * E.Rect.X / Main.Size.Width, -2 * E.Rect.Y / Main.Size.Height, 0, 0);
            E.TranslateVector = matrix4.Row3;
            E.projectionMatrix = matrix4;
            foreach (var E2 in E.ChildElements)
            {
                PrepareRecursive(M, E2, Layer + 1, matrix4);
            }
        }
        void PrepareRecursive(Main M, GuiElement E, int Layer, Matrix4 matrix4)
        {

            Layer += E.DrawingLayerOffset;
            //Matrix Mat = M.G.Transform;
            //M.G.TranslateTransform(E.Rect.X, E.Rect.Y);
            matrix4.Row3 += new Vector4(2 * E.Rect.X / Main.Size.Width, -2 * E.Rect.Y / Main.Size.Height, 0, 0);
            E.TranslateVector = matrix4.Row3;
            E.projectionMatrix = matrix4;

            while (ElementDrawList.Count <= Layer)
                ElementDrawList.Add(new List<ElementDrawStruct>());
            ElementDrawList[Layer].Add(new ElementDrawStruct(E, null, matrix4));

            foreach (var E2 in E.ChildElements)
            {
                if (E2.Enabled)
                    PrepareRecursive(M, E2, Layer + 1, matrix4);
            }
            //M.G.Transform = Mat;
        }
        void UpdateRecursive(GuiElement Element)
        {
            Element.Update();
            foreach (var Child in Element.ChildElements)
            {
                if (Child.Enabled)
                    UpdateRecursive(Child);
            }
        }
        void ShowRecursive(Main M, GuiElement E)
        {
            if (E.DrawFrame)
                E.DrawFrameAndBackground();
            E.Show(M);
            foreach (var E2 in E.ChildElements)
            {
                if (E2.Enabled)
                    ShowRecursive(M, E2);
            }

        }
        public void UpdateMouse(PointF MousePos, MouseButtons ButtonStatus)
        {
            
            this.MousePos = MousePos;
            if (ButtonStatus != MouseButtons.None)
            {
                if (PrevButton == MouseButtons.None)
                {
                    MouseDown(ButtonStatus);
                }
                if (MousePos.X != MousePosPrev.X || MousePos.Y != MousePosPrev.Y)
                {
                    if (!IsDragging)
                        StartMousePos = MousePos;

                    IsDragging = true;
                    MouseDrag(StartMousePos, new PointF(MousePosPrev.X - MousePos.X, MousePosPrev.Y - MousePos.Y), ButtonStatus);
                }
            }
            else
            {
                if (PrevButton != MouseButtons.None)
                {
                    MouseUp(PrevButton);
                    if (!IsDragging)
                        MouseClick(PrevButton);
                    IsDragging = false;
                }
            }
            if (CurrentElement != null && !IsMouseInsideRectangle(GetRelativeMousePos(MousePos), new RectangleF(new PointF(), CurrentElement.Rect.Size)))
                CurrentElement = null;
            if (CurrentElement != null && CurrentElement.HoverEvent != null)
            {
                CurrentElement.HoverEvent.Invoke(CurrentElement, GetRelativeMousePos(MousePos), ButtonStatus);
            }
            
            if (PreviousElement != null && PreviousElement != CurrentElement && PreviousElement.HoverEndEvent != null)
            {
                PreviousElement.HoverEndEvent.Invoke(PreviousElement, GetRelativeMousePos(MousePos), ButtonStatus);
            }
            PreviousElement = CurrentElement;
            MousePosPrev = MousePos;
            PrevButton = ButtonStatus;
            if (CurrentElement != null&&CurrentElement.ShowHoverCursor)
            {
                cursorType = CursorTypes.Hand;
            }
            if(!cursorChanged)
            {
                cursorType = CursorTypes.Default;
            }
            if (cursorTypeOld!= cursorType)
            {
                formCursor = GetCussorType(cursorType);
                cursorTypeOld = cursorType;
            }
            cursorChanged = false;
        }
        Cursor GetCussorType(CursorTypes type)
        {
            switch (type)
            {
                case CursorTypes.Default: return Cursors.Default;
                case CursorTypes.Hand:return Cursors.Hand;
                case CursorTypes.ArrowHorizontal: return Cursors.SizeWE;
                case CursorTypes.ArrowVertical: return Cursors.SizeNS;
                default:return Cursors.Default;
            }
        }

        public void UpdateCurrentElement()
        {
            RelativeMouseOffset = new PointF();
            LocalRelativeMouseOffset = new PointF();
            CurrentElement = null;
            RelativeMousePosStack.Clear();
            UpdateCurrentElementRecursive(Elements);
        }
        void UpdateCurrentElementRecursive(List<GuiElement> List)
        {
            foreach (var E in List)
            {
                if (E.Enabled)
                {
                    PointF Pos = GetRelativeMousePos(MousePos, LocalRelativeMouseOffset);
                    RelativeMousePosStack.Push(LocalRelativeMouseOffset);
                    if (IsMouseInsideRectangle(Pos, E.Rect))
                    {

                        LocalRelativeMouseOffset.X += E.Rect.X;
                        LocalRelativeMouseOffset.Y += E.Rect.Y;
                        if (E.CaptureEvents)
                        {
                            CurrentElement = E;
                            RelativeMouseOffset = LocalRelativeMouseOffset;
                        }
                        if (E.ChildElements.Count > 0)
                        {
                            UpdateCurrentElementRecursive(E.ChildElements);
                        }
                    }
                    LocalRelativeMouseOffset = RelativeMousePosStack.Pop();
                }
            }

        }
        void MouseDown(MouseButtons B)
        {
            if (CurrentElement != null && CurrentElement.MouseDownEvent != null)
                CurrentElement.MouseDownEvent.Invoke(CurrentElement, GetRelativeMousePos(MousePos), B);
        }
        void MouseUp(MouseButtons B)
        {
            if (CurrentElement != null && CurrentElement.MouseUpEvent != null)
                CurrentElement.MouseUpEvent.Invoke(CurrentElement, GetRelativeMousePos(MousePos), B);
        }
        void MouseClick(MouseButtons B)
        {
            if (CurrentElement != null && CurrentElement.ClickEvent != null)
                CurrentElement.ClickEvent.Invoke(CurrentElement, GetRelativeMousePos(MousePos), B);
        }
        void MouseDrag(PointF StartPos, PointF DeltaPos, MouseButtons B)
        {
            if (CurrentElement != null && CurrentElement.DragEvent != null)
                CurrentElement.DragEvent.Invoke(CurrentElement, GetRelativeMousePos(MousePos), GetRelativeMousePos(StartPos), DeltaPos, B);
        }
        public void UpdateScroll(int Direction)
        {
            if (CurrentElement != null && CurrentElement.ScrollEvent != null)
                CurrentElement.ScrollEvent.Invoke(CurrentElement, GetRelativeMousePos(MousePos), Direction);
        }
        bool IsMouseInsideRectangle(PointF MousePos, RectangleF Rect)
        {
            return Rect.Contains(MousePos);
        }
        PointF GetRelativeMousePos(PointF Pos)
        {
            if (CurrentElement != null)
            {
                return new PointF(Pos.X - RelativeMouseOffset.X, Pos.Y - RelativeMouseOffset.Y);
            }
            else
            {
                return Pos;
            }
        }
        PointF GetRelativeMousePos(PointF Pos, PointF Offset)
        {
            if (CurrentElement != null)
            {
                return new PointF(Pos.X - Offset.X, Pos.Y - Offset.Y);
            }
            else
            {
                return Pos;
            }
        }
        private void SetPerspectiveProjection(int width, int height, float FOV)
        {
            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI * (FOV / 180f), width / (float)height, 0.2f, 1024.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projectionMatrix); // this replaces the old matrix, no need for GL.LoadIdentity()
        }

        private void SetOrthographicProjection(int x, int y, int w, int h)
        {
            projectionMatrix = Matrix4.Identity;
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity(); // reset matrix
            GL.Ortho(x, w, h, y, 1000f, -1000f);
        }

        private void SetLookAtCamera(Vector3 position, Vector3 target, Vector3 up)
        {
            modelViewMatrix = Matrix4.LookAt(position, target, up);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelViewMatrix);
        }
    }
}
