using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System.IO;

namespace AmazingMandelbrot
{
    public partial class Form1 : Form
    {
        
        Timer T = new Timer();
        int ShaderId;
        Graphics gfx;
        int arrStride = 3;
        byte[] arr;
        int ProgramId;
        int Frame = 0;
        ShaderController ShaderController;
        Matrix4 projectionMatrix;
        GLControl GLcontrol;
        Main Main;
        Stopwatch stopwatch = new Stopwatch();
        public Form1()
        {
            InitializeComponent();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            
            
            MouseWheel += Form1_MouseWheel;
            //string S = Application.LocalUserAppDataPath;
            
            GraphicsMode graphicsMode = new GraphicsMode(new ColorFormat(8, 8, 8, 8), 24);
            GLcontrol = new GLControl(graphicsMode);
            GLcontrol.Dock = DockStyle.Fill;
            WindowState = FormWindowState.Maximized;
            GLcontrol.Parent = this;
            int w = GLcontrol.Width;
            int h = GLcontrol.Height;
            GL.Viewport(0, 0, w, h);
            T.Interval = 20;
            T.Tick += T_Tick;
            T.Start();
            ShaderId = GL.CreateShader(ShaderType.ComputeShader);
            //ShaderController = new ShaderController(w,h);
            ShaderController.GenreateComputeProgram();
            Main = new Main(GLcontrol.Size);
            //Console.WriteLine();
            
        }

        

        private void T_Tick(object sender, EventArgs e)
        {
            
            int w = GLcontrol.Width;
            int h = GLcontrol.Height;
            
            GLcontrol.MakeCurrent();
            GL.ClearColor(Color.Blue);
            SetOrthographicProjection(0, 0, w, h);
            //ShaderController.Compute();
            
            Main.Update();
            Main.Draw(PointToClient(MousePosition), MouseButtons, ContainsFocus);
            //ShaderController.Draw();
            GLcontrol.SwapBuffers();
            
        }
        public void SetOrthographicProjection(int x, int y, int w, int h)
        {
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            projectionMatrix = Matrix4.CreateOrthographicOffCenter(x, w, h, y, 1f, -1f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projectionMatrix);
        }
        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            Main.GuiHandler.UpdateScroll(e.Delta);
        }


        private void Form1_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            Main.TypeChar(e.KeyChar);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Main.TypeKey(e.KeyData);
        }
    }
}
