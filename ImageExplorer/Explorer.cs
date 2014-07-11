using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ImageExplorer
{
    public partial class Explorer : FrameworkElement
    {

        protected int nbZoomChange = 0;
        
        Dictionary<Point, BitmapSource> tileCache = new Dictionary<Point, BitmapSource>();
        Size tileSize = new Size(128, 128);
        Point currentPosition = new Point(0, 0);
        ITileGenerator rm;
        BitmapSource missingImage;
        AddImageDelegate addToCache;

        public delegate void AddImageDelegate(Point p, byte[] data);

        public Point? posMouseDown = null;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            posMouseDown = new Point( e.GetPosition(this) );
        }

        public string getStats()
        {
            string data = "Number of tiles in cache, " + this.tileCache.Count + Environment.NewLine;
            data += "Number of zoom change, " + nbZoomChange + Environment.NewLine;
            return data + rm.getStats();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            nbZoomChange++;
            Point p = new Point(e.GetPosition(this));

            if (e.Delta > 0)
            {
                this.rm.zoom();
            }
            else
            {
                this.rm.unZoom();
            }
            resetAllTiles();
        }

        protected void resetAllTiles()
        {
            this.tileCache.Clear(); 
            this.InvalidateVisual();
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (e.ClickCount > 2)
            {
                this.rm.unZoom();
                resetAllTiles();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            posMouseDown = null;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Released)
                posMouseDown = null;
        }


        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && posMouseDown != null)
            {
                Point nouveauPoint = new Point(e.GetPosition(this));
                Point difference = new Point();
                difference.X = nouveauPoint.X - posMouseDown.Value.X;
                difference.Y = nouveauPoint.Y - posMouseDown.Value.Y;
                posMouseDown = nouveauPoint;
                this.currentPosition.X -= difference.X;
                this.currentPosition.Y -= difference.Y;
                this.InvalidateVisual();
                //posMouseDown = nouveauPoint; // int truncating will likely cause some problems
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }


        public Explorer()
        {
            //rm = new RandomImageGenerator(this.tileSize);
            rm = new Mandelbrot(this.tileSize, 0.01f, new ImageDone(onImageGenerated));
            this.missingImage = getMissingImage();
            this.addToCache = new AddImageDelegate(addImageToCache);
        }

        private BitmapSource getMissingImage()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            byte[] rawImage = new byte[4 * this.tileSize.Width * this.tileSize.Height];
            return BitmapSource.Create(this.tileSize.Width, this.tileSize.Height, 96, 96, pf, null, rawImage, 4 * this.tileSize.Width);
        }

        protected override void OnRender(DrawingContext context)
        {
            DateTime begin = DateTime.Now;
            System.Windows.Size current = this.RenderSize;
            // cast to int : truncate
            int drawingAreaWidth = (int)(current.Width / tileSize.Width) + 2;
            int drawingAreaHeight = (int)(current.Width / tileSize.Width) + 2;
            Rect rectangle= new Rect();
            BitmapSource toDraw;

            for(int i = -1; i < drawingAreaWidth + 1; i++)
            {
                for(int j = -1; j < drawingAreaHeight + 1; j++)
                {
                    
                    Point upperLeftPoint = new Point(this.currentPosition.X + i * tileSize.Width, this.currentPosition.Y + j * tileSize.Height);
                    Point upperLeftPointNormalized = new Point(tileSize.Width * (int)(upperLeftPoint.X / tileSize.Width), tileSize.Height * (int)(upperLeftPoint.Y / tileSize.Height));

                    if(tileCache.ContainsKey(upperLeftPointNormalized))
                    {
                        toDraw = tileCache[upperLeftPointNormalized];
                    }
                    else
                    {
                        rm.generateBitmap(upperLeftPointNormalized);
                        tileCache[upperLeftPointNormalized] = this.missingImage;
                        toDraw = this.missingImage;
                    }
                    rectangle.Location = new System.Windows.Point(upperLeftPointNormalized.X - this.currentPosition.X, upperLeftPointNormalized.Y - this.currentPosition.Y);
                    rectangle.Width = tileSize.Width;
                    rectangle.Height = tileSize.Height;
                    context.DrawImage(toDraw, rectangle);
                }
            }
           
            DateTime end = DateTime.Now;

            FormattedText formattedText = new FormattedText(
                "RenderingTime:" + (int)(end - begin).TotalMilliseconds + "ms.",
                CultureInfo.GetCultureInfo("fr-fr"),
                FlowDirection.LeftToRight,
                new Typeface("Verdana"),
                20,
                Brushes.Blue
            );
            context.DrawText(formattedText, new System.Windows.Point(20, 20));

        }

        
        void addImageToCache(Point p, byte[] rawImage)
        {
            // Create a BitmapSource.
            PixelFormat pf = PixelFormats.Bgr32;
            int rawStride = (this.tileSize.Width * pf.BitsPerPixel) / 8;
            BitmapSource bitmap = BitmapSource.Create(this.tileSize.Width, this.tileSize.Height,
                96, 96, pf, null,
                rawImage, rawStride);
            tileCache[p] = bitmap;
            this.InvalidateVisual();
        }

        void onImageGenerated(Point p, byte[] rawImage)
        {
            Object[] objs = new Object[2];
            objs[0] = p;
            objs[1] = rawImage;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, this.addToCache, p, rawImage);
        }


    }
}
