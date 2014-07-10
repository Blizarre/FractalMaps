using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageExplorer
{
    public struct Size
    {
        public Size(int w, int h)
        {
            this.Width = w;
            this.Height = h;
        }

        public String toString()
        {
            return "Width:" + this.Width + ", Height:" + this.Height;
        }
        
        public bool Equals(Size s)
        {
            return (this.Width == s.Width && this.Height == s.Height);
        }

        public int Width;
        public int Height;
    }

    public struct Point
    {
        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public Point(System.Windows.Point point)
        {
            this.X = (int)point.X;
            this.Y = (int)point.Y;
        }


        public String toString()
        {
            return "X:" + this.X + ", X:" + this.Y;
        }

        public int X;
        public int Y;
    }


    class RandomImageGenerator
    {
        Size m_s;

        public RandomImageGenerator(Size size)
        {
            m_s = size;
        }

        public BitmapSource generate(Point origin)
        {
            // Define parameters used to create the BitmapSource.
            PixelFormat pf = PixelFormats.Bgr32;
            int width = this.m_s.Width;
            int height = this.m_s.Height;
            int rawStride = (width * pf.BitsPerPixel) / 8;
            byte[] rawImage = new byte[rawStride * height];

            // Initialize the image with data.
            for (int i = 0; i < rawImage.Length; i++)
            {
                rawImage[i] = (byte)((Math.Sin(origin.X) * 255 + Math.Sin(origin.Y) * 255) % Byte.MaxValue);
            }
            
            // Create a BitmapSource.
            BitmapSource bitmap = BitmapSource.Create(width, height,
                96, 96, pf, null,
                rawImage, rawStride);

            return bitmap;
        }

    }
}
