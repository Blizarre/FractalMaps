using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageExplorer
{
    struct Complex
    {
        public Complex(double real, double imag) { this._real = real; this._imag = imag; }
        public double AbsSq() { return this._real * this._real + this._imag * this._imag; }
        public double _real;
        public double _imag;
    }



    public delegate void ImageDone(TileInfo info, byte[] bm);

    struct Pixel
    {
        public Pixel(byte a) { b = g = r = 0;  this.a = 255; }
        public Pixel(byte _r, Byte _g, Byte _b) { r = _r; g = _g; b = _b; a = 255; }
        public byte b;
        public byte g;
        public byte r;
        public byte a;
    };

    class Mandelbrot : ITileGenerator
    {
        protected Size m_s;
        protected ImageDone callback;
        int[] histo = new int[1000];
        Pixel[] lut = new Pixel[255];
        int nbOverflow = 0;

        public Mandelbrot(Size size, ImageDone onImDone)
        {
            this.m_s = size;
            this.callback = onImDone;
            generateLUT();
            ThreadPool.SetMaxThreads(1, 1); // 1 < number of core => number of threads will be set at number of core, see msdn
        }

        // copy/paste from wikipedia   
        public double mandelbrotFunction(double x0, double y0, int maxIter, double N, bool smooth)
        {
            int iter = 0;
            double retValue;
            double x = 0.0f;
            double y = 0.0f;
            double tmp;
            while (x * x + y * y < N * N && iter < maxIter)
            {
                tmp = x*x - y*y + x0;
                y = 2*x*y + y0;
                x = tmp;
                iter++;
            }

            if (iter == maxIter)
                retValue = 0;
            else
                retValue = (double)iter;

            //Smoothing :
            // realResult = n - log_p(log(|z_n|/log(N))
            // p = 2 (exponent of z in the julia formulae)
            // n = iteration number achieved during bailout detection
            // N = bailout, bigger number, better results.


            if(smooth)
                retValue -= (double)Math.Log(Math.Log(Math.Sqrt(x*x + y*y) / Math.Log(N), 2));

            if (double.IsNaN(retValue))
                retValue = 0;

            return retValue;
        }

        public string getStats()
        {
            string data = "Mandlebrot overflow: " + this.nbOverflow + Environment.NewLine;
            data += "Mandelbrot values histogram:" + Environment.NewLine;
            data += string.Join(", ", Enumerable.Range(0, histo.Length)) + Environment.NewLine;
            data += string.Join(", ", histo) + Environment.NewLine;
            return data;
        }

        public void generateLUT()
        {
            Pixel p = new Pixel();
            p.a = 255;
            for(int i = 0; i< lut.Length; i++)
            {
                p.r = (byte)(255 * (Math.Sqrt(i + 1) / Math.Sqrt(255)));
                p.g = (byte)((2*i > 255 ? 255 : 2 * i ));
                p.b = (byte)(255 * (i * i / 255 * 255)); 
                this.lut[i] = p;
            }
        }

        // object[] o = { Point origin, double scale, Quality q};
        public void ThreadPoolGenerateBitmap(object o)
        {
            TileInfo info = (TileInfo)o;
            double N = 2.0f;
            int maxIter = 100;
            bool smooth =false;

            int renderWidth = this.m_s.Width;
            int renderHeight = this.m_s.Height;

            // replace with global quality constants
            switch (info.quality)
            {
                case Quality.UltraFast:
                /*    info.scale /= 2;
                    renderWidth /= 2;
                    renderHeight /= 2;*/
                    goto case Quality.Fast;
                case Quality.Fast:
                    N = 2.0f;
                    maxIter = 200;
                    smooth = false;
                    break;
                case Quality.Normal:
                    N = 4.0f;
                    maxIter = 1000;
                    smooth = true;
                    break;
                case Quality.Best:
                case Quality.VeryBest:
                    N = 8.0f;
                    maxIter = 2000;
                    smooth = true;
                    break;
            }

            byte[] rawData = new byte[4 * renderWidth * renderHeight];
            Pixel p = new Pixel(255);

            uint value;
            int position = 0;

            for (int j = info.position.Y; j < info.position.Y + renderHeight; j++)
            {
                for (int i = info.position.X; i < info.position.X + renderWidth; i++)
                {
                    value = (uint)mandelbrotFunction(i * info.scale, j * info.scale, maxIter, N, smooth);

                    if (value < histo.Length)
                        histo[value]++;

                    if (value > this.lut.Length - 1)
                    {
                        this.nbOverflow++;
                        value = (uint)(this.lut.Length - 1);
                    }

                    p = this.lut[value];

                    rawData[position++] = p.b;
                    rawData[position++] = p.g;
                    rawData[position++] = p.r;
                    rawData[position++] = p.a;
                }
            }

            this.callback(info, rawData);
        }

        public void generateBitmap(TileInfo tInfo)
        {
            // TODO : Create a specialized thread pool with reduced priority
            ThreadPool.QueueUserWorkItem(ThreadPoolGenerateBitmap, tInfo);

        }

    }
}
