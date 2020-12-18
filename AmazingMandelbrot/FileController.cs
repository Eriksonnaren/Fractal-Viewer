using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;

namespace AmazingMandelbrot
{
    class FileController
    {
        public readonly string programPath;
        public readonly string screenshotFolder;

        public FileController()
        {
            programPath = Application.StartupPath;
            screenshotFolder = Path.Combine(programPath, "Screenshots");
            if (!Directory.Exists(screenshotFolder))
            {
                Directory.CreateDirectory(screenshotFolder);
            }
        }
        public void SaveBitmap(Bitmap bitmap)
        {
            const string filetype = ".png";
            string filename = DateTime.Today.Year.ToString() + "_" + DateTime.Today.Month.ToString() + "_" + DateTime.Today.Day.ToString();
            filename = Path.Combine(screenshotFolder, filename);
            if (File.Exists(filename + filetype))
            {
                var ExistingImage = Image.FromFile(filename + filetype);
                int n = 1;
                while (File.Exists(filename + "(" + n.ToString() + ")" + filetype))
                {
                    n++;
                }
                filename += "(" + n.ToString() + ")";
            }
            //FileStream stream = new FileStream(filename + ".png",  FileMode.CreateNew);
            
            var prop = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            prop.Id = 37510;
            prop.Type = 2;
            prop.Value = Encoding.UTF8.GetBytes("Hello World!");
            prop.Len = prop.Value.Length;
            bitmap.SetPropertyItem(prop);
            bitmap.Save(filename + filetype);
            BitmapMetadata data;
            
            //FileStream stream = new FileStream(filename + filetype, FileMode.Create);
            //BitmapMetadata myBitmapMetadata = new BitmapMetadata("jpeg");
            //myBitmapMetadata.Comment = "this is a comment";
            //var encoder3 = new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            
            Stream pngStream = new FileStream(filename + filetype, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            PngBitmapDecoder pngDecoder = new PngBitmapDecoder(pngStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            BitmapFrame pngFrame = pngDecoder.Frames[0];
            InPlaceBitmapMetadataWriter pngInplace = pngFrame.CreateInPlaceBitmapMetadataWriter();
            if (pngInplace.TrySave() == true)
            { pngInplace.SetQuery("/Text/Description", "Have a nice day."); }
            pngStream.Close();
            //ImageMetadata metadata = new ImageMetadata("blah","erik","fractal","this is a comment");
            //SaveEXIFMetadata(bitmap, metadata, filename + ".jpeg");
            //SaveEXIFMetadataProperty(bitmap,"hello", filename + ".png");


        }
        /*private void SaveEXIFMetadataProperty(Image image, string propertyValue, string filepath)
        {
            PropertyItem propertyItem = CreatePropertyItem();
            propertyItem.Id = 40092;//comments
            // Type=1 means Array of Bytes.
            propertyItem.Type = 2;
            propertyItem.Len = propertyValue.Length;
            //propertyItem.Value = Encoding.Unicode.GetBytes(propertyValue)
            propertyItem.Value = Encoding.UTF8.GetBytes(propertyValue);
            image.SetPropertyItem(propertyItem);
            image.Save(filepath);
        }
        private PropertyItem CreatePropertyItem()
        {
            System.Reflection.ConstructorInfo ci = typeof(PropertyItem).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
            return (PropertyItem)ci.Invoke(null);
        }*/

    }
    public static class Extensions
    {
        public enum MetaProperty
        {
            Title = 40091,
            Comment = 40092,
            Author = 40093,
            Keywords = 40094,
            Subject = 40095,
            Copyright = 33432,
            Software = 11,
            DateTime = 36867
        }
        public static Bitmap SetMetaValue(this Bitmap sourceBitmap, MetaProperty property, string value)
        {
            PropertyItem prop;
            if (sourceBitmap.PropertyItems.Length==0)
            {
                prop= (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            }
            else
            {
                prop = sourceBitmap.PropertyItems[0];
            }
             
            int iLen = value.Length + 1;
            byte[] bTxt = new Byte[iLen];
            for (int i = 0; i < iLen - 1; i++)
                bTxt[i] = (byte)value[i];
            bTxt[iLen - 1] = 0x00;
            prop.Id = (int)property;
            prop.Type = 2;
            prop.Value = bTxt;
            prop.Len = iLen;
            sourceBitmap.SetPropertyItem(prop);
            return sourceBitmap;
        }

        public static string GetMetaValue(this Bitmap sourceBitmap, MetaProperty property)
        {
            PropertyItem[] propItems = sourceBitmap.PropertyItems;
            var prop = propItems.FirstOrDefault(p => p.Id == (int)property);
            if (prop != null)
            {
                return Encoding.UTF8.GetString(prop.Value);
            }
            else
            {
                return null;
            }
        }

    }

}
