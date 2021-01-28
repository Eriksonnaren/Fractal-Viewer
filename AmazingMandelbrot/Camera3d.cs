using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using System.Drawing;
using OpenTK.Graphics.OpenGL;

namespace AmazingMandelbrot
{
    class Camera3d
    {
        public GuiElement Parent;
        public Vector3 CameraCenter = new Vector3();
        public Vector3 CameraVector;
        //public Vector4 TranslateVector;
        public RectangleF Rect;
        public float Fov = 45;
        public Matrix4 ProjectMatrix = new Matrix4();
        public Matrix4 ViewMatrix = new Matrix4();
        public float FarZ = 8000;
        public float NearZ = 0.5f;
        public Camera3d(Vector3 cameraVector, RectangleF R, GuiElement Parent)
        {
            this.Parent = Parent;
            Rect = R;
            CameraVector = cameraVector;
        }
        public void Prepare3d()
        {
            GL.PushMatrix();
            GL.Enable(EnableCap.DepthTest);
            Vector4 Trans = Parent.TranslateVector;
            int TransX = (int)(Main.Size.Width * (1 + Trans.X) / 2);
            int TransY = (int)(Main.Size.Height * (1 - Trans.Y) / 2);
            int TransX2 = TransX;
            int TransY2 = TransY;
            if (TransX2 > Main.Size.Width / 2)
                TransX2 -= (int)Rect.Width / 2;
            if (TransY2 > Main.Size.Height / 2)
                TransY2 -= (int)Rect.Height / 2;
            Vector4 Trans2 = new Vector4(-1 + 2 * (int)(TransX2 / (float)Main.Size.Width), 1 - 2 * (int)(TransY2 / (float)Main.Size.Height), 0, 0);
            SetPerspectiveProjection(Trans2, 1, 1, Rect.Width / (float)Rect.Height, Fov);
            SetLookAtCamera(CameraVector+ CameraCenter, CameraCenter, -Vector3.UnitZ);
            //GL.GetFloat(GetPName.ModelviewMatrix, out ViewMatrix);
            //GL.GetFloat(GetPName.ProjectionMatrix, out ProjectMatrix);

            GL.Viewport(TransX, Main.Size.Height - (int)Rect.Height - TransY, (int)Rect.Width, (int)Rect.Height);

            //GL.Translate(Offset3d);

        }
        public void End3d()
        {
            
            GL.Disable(EnableCap.DepthTest);
            
            GL.Viewport(0, 0, Main.Size.Width, Main.Size.Height);
            
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            GL.MatrixMode(MatrixMode.Projection);
            //SetOrthographicProjection(0,0, Main.Size.Width, Main.Size.Height);
            GL.PopMatrix();
        }
        public void SetPerspectiveProjection(Vector4 TranslateVector, float width, float height, float aspect, float FOV)
        {
            ProjectMatrix = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI * (FOV / 180f), aspect, NearZ, FarZ);
            GL.MatrixMode(MatrixMode.Projection);
            ProjectMatrix *= Matrix4.CreateScale(width, height, 1);
            ProjectMatrix *= Matrix4.CreateTranslation(TranslateVector.X + width, TranslateVector.Y - height, 0);

            GL.LoadMatrix(ref ProjectMatrix); // this replaces the old matrix, no need for GL.LoadIdentity()
        }
        public void SetOrthographicProjection(int x, int y, int w, int h)
        {
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            ProjectMatrix = Matrix4.CreateOrthographicOffCenter(x, w, h, y, 1f, -1f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref ProjectMatrix);
        }

        public void SetLookAtCamera(Vector3 position, Vector3 target, Vector3 up)
        {
            ViewMatrix = Matrix4.LookAt(position, target, up);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref ViewMatrix);
        }
    }
}
