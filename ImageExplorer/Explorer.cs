using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public struct TileInfo
    {
        public Point position;
        public float scale;
        public Quality quality;
        public bool isWaitingRendering;

        public TileKey getTileKey() { 
            return new TileKey(this.position, this.scale); 
        }
    };

    public struct TileKey
    {
        public TileKey(Point position, float scale)
        {
            this.position = position;
            this.scale = scale;
        }

        public Point position;
        public float scale;
    };

    public struct TileData
    {
        public TileInfo meta;
        public BitmapSource data;
    };

    public enum Quality {
        UltraFast,
        Fast,
        Normal,
        Best,
        VeryBest
    };

    public partial class Explorer : FrameworkElement
    {
        protected int nbZoomChange = 0;
        Quality currentQuality;

        Dictionary<TileKey, TileData> tileCache = new Dictionary<TileKey, TileData>();

        Size tileSize = new Size(256, 256);
        Point centerOfImage = new Point(0, 0);
        
        ITileGenerator tileGen;
        int numberOfTileInGeneration;

        BitmapSource missingImage;

        AddImageDelegate addToCache;

        protected float currentScale = 0.01f;

        public delegate void AddImageDelegate(TileInfo inf, byte[] data);

        public Point? posMouseDown = null;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            posMouseDown = new Point( e.GetPosition(this) );
        }

        public string getStats()
        {
            string data = "Number of tiles in cache, " + this.tileCache.Count + Environment.NewLine;
            data += "Number Of Tile In Generation: " + numberOfTileInGeneration + Environment.NewLine;
            data += "Number of zoom change, " + nbZoomChange + Environment.NewLine;
            return data + tileGen.getStats();
        }

        protected Point getScreenSize()
        {
            return new Point((int)this.ActualWidth, (int)this.ActualHeight);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            nbZoomChange++;

            this.centerOfImage -= getScreenSize() / 2 - new Point(e.GetPosition(this));
            this.centerOfImage *= (e.Delta > 0 ? 2f : 0.5f);
            this.currentScale  *= (e.Delta > 0 ? 0.5f : 2.0f);
            this.InvalidateVisual();
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
                difference = nouveauPoint - posMouseDown.Value;
                posMouseDown = nouveauPoint;
                this.centerOfImage -= difference;
                this.InvalidateVisual();
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }


        public Explorer()
        {
            tileGen = new Mandelbrot(this.tileSize, new ImageDone(onImageGenerated));
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
            // cast to int : truncate
            int drawingAreaWidth = (int)(this.RenderSize.Width / tileSize.Width) + 2;
            int drawingAreaHeight = (int)(this.RenderSize.Width / tileSize.Width) + 2;
            Rect rectangle= new Rect();
            BitmapSource toDraw;
            TileInfo tileInf;
            TileData tileDat;
            TileKey tk;
            Point upperLeftImage = this.centerOfImage - new Point((int)this.ActualWidth, (int)this.ActualHeight) / 2;
            // TODO: Needs heavy refactoring, code cleaning
            for (int i = -1; i < drawingAreaWidth + 1; i++)
            {
                for (int j = -1; j < drawingAreaHeight + 1; j++)
                {

                    Point upperLeftImageTile = new Point(upperLeftImage.X + i * tileSize.Width, upperLeftImage.Y + j * tileSize.Height);
                    
                    tileInf = new TileInfo();
                    tileInf.position = new Point(tileSize.Width * (int)(upperLeftImageTile.X / tileSize.Width), tileSize.Height * (int)(upperLeftImageTile.Y / tileSize.Height));
                    tileInf.scale = this.currentScale;
                    tileInf.quality = this.currentQuality;
                    tk = tileInf.getTileKey();

                    if (tileCache.ContainsKey(tk))
                    {
                        if (tileCache[tk].meta.isWaitingRendering == false && tileCache[tk].meta.quality < this.currentQuality)
                        {
                            TileData data = tileCache[tk];
                            data.meta.isWaitingRendering = true;
                            tileCache[tk] = data;
                            tileGen.generateBitmap(tileInf);
                            numberOfTileInGeneration++;
                        }
                        toDraw = tileCache[tk].data;
                    }
                    else
                    {
                        tileInf.isWaitingRendering = true;
                        tileGen.generateBitmap(tileInf);
                        numberOfTileInGeneration++;
                        tileDat = new TileData();
                        tileDat.meta = tileInf;
                        tileDat.data = this.missingImage;
                        tileCache[tk] = tileDat;
                        toDraw = this.missingImage;
                    }
                    rectangle.Location = new System.Windows.Point(tileInf.position.X - upperLeftImage.X, tileInf.position.Y - upperLeftImage.Y);
                    rectangle.Width = tileSize.Width;
                    rectangle.Height = tileSize.Height;
                    context.DrawImage(toDraw, rectangle);
                }
            }


            DateTime end = DateTime.Now;

            FormattedText formattedText = new FormattedText(
                numberOfTileInGeneration.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Verdana"),
                20,
                Brushes.Blue
            );
            context.DrawText(formattedText, new System.Windows.Point(200, 20));


        }

        
        void addImageToCache(TileInfo info, byte[] rawImage)
        {
            // Create a BitmapSource.
            PixelFormat pf = PixelFormats.Bgr32;
            int rawStride = (this.tileSize.Width * pf.BitsPerPixel) / 8;
            BitmapSource bitmap = BitmapSource.Create(this.tileSize.Width, this.tileSize.Height,
                96, 96, pf, null,
                rawImage, rawStride);
            TileData td;
            info.isWaitingRendering = false;
            td.meta = info;
            td.data = bitmap;
            tileCache[info.getTileKey()] = td;
            this.InvalidateVisual();
        }

        void onImageGenerated(TileInfo info, byte[] rawImage)
        {
            numberOfTileInGeneration--;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, this.addToCache, info, rawImage);
        }

        public Quality quality
        {
            set
            {
                if (value != currentQuality)
                    this.currentQuality = value;
                this.InvalidateVisual();
            }
            get
            {
                return this.currentQuality;
            }
        }
    }
}
