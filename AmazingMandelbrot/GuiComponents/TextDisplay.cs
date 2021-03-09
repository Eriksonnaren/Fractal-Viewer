using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace AmazingMandelbrot.GuiComponents
{
    class TextDisplay : GuiElement
    {
        Bitmap bmp;
        public Graphics gfx;
        int texture = -1;
        static Font font = new Font("Arial Black", 10);
        BitmapData data;
        StringFormat SF = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        public TextDisplay(RectangleF Rect) : base(Rect)
        {
            bmp = new Bitmap((int)Rect.Width, (int)Rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            gfx = Graphics.FromImage(bmp);
            gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            //GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
            //GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
            //GL.PolygonMode(MaterialFace.Front,PolygonMode.Fill);
            //GL.Enable(EnableCap.PointSmooth);
            //GL.Enable(EnableCap.LineSmooth);
            GL.Enable(EnableCap.Blend);

            //GL.ShadeModel(ShadingModel.Smooth);
            //GL.Enable(EnableCap.AutoNormal);
            texture = GL.GenTexture();
        }
        public override void Update()
        {

        }
        public override void Show(Main D)
        {
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0,
            OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, data.Scan0);
            GL.Color3(1.0, 1.0, 1.0);
            //GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);

            //GL.MatrixMode(MatrixMode.Modelview);


            

            GL.Begin(PrimitiveType.Quads);
            float realWidth = Rect.Width;
            float realHeight = Rect.Height;
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex2(0f, 0f);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex2(realWidth, 0f);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex2(realWidth, realHeight);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex2(0f, realHeight);
            GL.End();
            GL.Disable(EnableCap.Texture2D);
        }
        public void PrepareDraw()
        {
            data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }
        public void PrepareWrite()
        {
            if (data != null)
                bmp.UnlockBits(data);
            gfx.Clear(Color.FromArgb(0, 0, 0, 0));
        }
        public void DrawCenteredText(string s,Brush col,int fontsize)
        {
            gfx.DrawString(s, new Font("Arial Black", fontsize), col, Rect.Width / 2, Rect.Height / 2, SF);
        }
    }
}
