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

namespace ImageExplorer
{
    public partial class Explorer : FrameworkElement
    {

        Dictionary<Point, BitmapSource> tileCache = new Dictionary<Point, BitmapSource>();
        Size tileSize = new Size(128, 128);
        Point currentPosition = new Point(0, 0);
        RandomImageGenerator rm;
        int numberOfRendering = 0;

        public Point? posMouseDown = null;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            posMouseDown = new Point( e.GetPosition(this) );
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
            rm = new RandomImageGenerator(this.tileSize);
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

            for(int i = -1; i < drawingAreaWidth; i++)
            {
                for(int j = -1; j < drawingAreaHeight; j++)
                {
                    
                    Point upperLeftPoint = new Point(currentPosition.X + i * tileSize.Width, currentPosition.Y + j * tileSize.Height);
                    Point upperLeftPointNormalized = new Point(tileSize.Width * (int)(upperLeftPoint.X / tileSize.Width), tileSize.Height * (int)(upperLeftPoint.Y / tileSize.Height));

                    if(tileCache.ContainsKey(upperLeftPointNormalized))
                    {
                        toDraw = tileCache[upperLeftPointNormalized];
                    }
                    else
                    {
                        toDraw = rm.generate(upperLeftPointNormalized);
                        tileCache[upperLeftPointNormalized] = toDraw;
                    }
                    rectangle.Location = new System.Windows.Point(upperLeftPointNormalized.X - this.currentPosition.X, upperLeftPointNormalized.Y - this.currentPosition.Y);
                    rectangle.Width = tileSize.Width;
                    rectangle.Height = tileSize.Height;
                    context.DrawImage(toDraw, rectangle);
                }
            }

            DateTime end = DateTime.Now;

            FormattedText formattedText = new FormattedText(
                "RenderingTime:" + (end - begin).Milliseconds + "ms.",
                CultureInfo.GetCultureInfo("fr-fr"),
                FlowDirection.LeftToRight,
                new Typeface("Verdana"),
                20,
                Brushes.Blue
            );
            context.DrawText(formattedText, new System.Windows.Point(20, 20));

        }

    }
}
