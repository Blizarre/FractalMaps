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

namespace FractalMaps
{
    public struct TileInfo
    {
        public Point position;
        public double scale;
        public Quality quality;
        public bool isWaitingRendering;

        public TileKey getTileKey() { 
            return new TileKey(this.position, this.scale); 
        }
    };

    // TODO: Warning - Replace scale by integer
    public struct TileKey
    {
        public TileKey(Point position, double scale)
        {
            this.position = position;
            this.scale = scale;
        }

        public Point position;
        public double scale;
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

        Size m_tileSize = new Size(256, 256);
        Point m_centerOfImage = new Point(0, 0);
        ITileGenerator m_tileGen;
        int numberOfTileInGeneration;

        BitmapSource m_missingImage;

        AddImageDelegate addToCache;

        // TODO: Implement as Integer
        protected double m_currentScale = 0.01f;

        public delegate void AddImageDelegate(TileInfo inf, byte[] data);

        public Point? posMouseDown = null;
        public bool m_generateTiles = true;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            posMouseDown = new Point( e.GetPosition(this) );
        }

        public string getStats()
        {
            StringBuilder data = new StringBuilder();
            data.AppendLine("Current View: " + this.m_centerOfImage + ", scale: " + this.m_currentScale);
            data.AppendLine("Number of tiles in cache: " + this.tileCache.Count);
            data.AppendLine("Number Of Tile awaiting generation: " + numberOfTileInGeneration);
            data.AppendLine("Number of zoom change: " + nbZoomChange);
            data.AppendLine("Cached Images :");
            foreach( TileKey k in tileCache.Keys)
            {
                data.AppendLine(" - " + k.position + ", scale " + k.scale + ", quality: " + tileCache[k].meta.quality);
            }
            data.Append(m_tileGen.getStats());
            return data.ToString();
        }

        protected Point getScreenSize()
        {
            return new Point((int)this.ActualWidth, (int)this.ActualHeight);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            nbZoomChange++;

            this.m_centerOfImage -= getScreenSize() / 2 - new Point(e.GetPosition(this));
            this.m_centerOfImage *= (e.Delta > 0 ? 2f : 0.5f);
            this.m_currentScale  *= (e.Delta > 0 ? 0.5f : 2.0f);
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
                this.m_centerOfImage -= difference;
                this.InvalidateVisual();
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }


        public Explorer()
        {
            m_tileGen = new Mandelbrot(this.m_tileSize, new ImageDone(onImageGenerated));
            this.m_missingImage = getMissingImage();
            this.addToCache = new AddImageDelegate(addImageToCache);
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            this.m_tileGen.Dispose();
        }

        private BitmapSource getMissingImage()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            byte[] rawImage = new byte[4 * this.m_tileSize.Width * this.m_tileSize.Height];
            return BitmapSource.Create(this.m_tileSize.Width, this.m_tileSize.Height, 96, 96, pf, null, rawImage, 4 * this.m_tileSize.Width);
        }

        protected override void OnRender(DrawingContext context)
        {
            // cast to int : truncate
            int drawingAreaWidth = (int)(this.RenderSize.Width / m_tileSize.Width) + 2;
            int drawingAreaHeight = (int)(this.RenderSize.Width / m_tileSize.Width) + 2;
            Rect rectangle= new Rect();
            BitmapSource toDraw;
            TileInfo tileInf;
            TileData tileDat;
            TileKey tk;
            Point upperLeftImage = this.m_centerOfImage - new Point((int)this.ActualWidth, (int)this.ActualHeight) / 2;
            // TODO: Needs heavy refactoring, code cleaning
            for (int i = -1; i < drawingAreaWidth + 1; i++)
            {
                for (int j = -1; j < drawingAreaHeight + 1; j++)
                {

                    Point upperLeftImageTile = new Point(upperLeftImage.X + i * m_tileSize.Width, upperLeftImage.Y + j * m_tileSize.Height);

                    tileInf = new TileInfo();
                    tileInf.position = new Point(m_tileSize.Width * (int)(upperLeftImageTile.X / m_tileSize.Width), m_tileSize.Height * (int)(upperLeftImageTile.Y / m_tileSize.Height));
                    tileInf.scale = this.m_currentScale;
                    tileInf.quality = this.currentQuality;
                    
                    tk = tileInf.getTileKey();

                    if (tileCache.ContainsKey(tk))
                    {
                        // If the quality is not the right one, draw what you have and regenerate after
                        if (tileCache[tk].meta.isWaitingRendering == false && tileCache[tk].meta.quality < this.currentQuality)
                        {
                            TileData data = tileCache[tk];
                            data.meta.isWaitingRendering = true;
                            tileCache[tk] = data;
                            m_tileGen.generateBitmap(tileInf);
                            numberOfTileInGeneration++;
                        }
                        toDraw = tileCache[tk].data;
                    }
                    else
                    {
                        // Try to find a lower resolution version
                        tileDat = new TileData();
                        tileDat.meta = tileInf;
                        tileInf.quality = this.currentQuality;
                        
                        TileKey tmp;

                        // Look for a smaller image
                        tmp.scale = tileInf.scale * 2;
                        tileDat.meta.scale = tmp.scale;

                        // Position of the tile on the parent image
                        int posXParent = (int)Math.Round((tileInf.position.X / this.m_tileSize.Width - 0.5) / 2, MidpointRounding.AwayFromZero) * this.m_tileSize.Width;
                        int posYParent = (int)Math.Round((tileInf.position.Y / this.m_tileSize.Height - 0.25) / 2, MidpointRounding.AwayFromZero) * this.m_tileSize.Height;

                        int resteX = Math.Abs( (tileInf.position.X/2) % 256 );
                        int resteY = Math.Abs( (tileInf.position.Y/2) % 256 ); 
                        
                        tmp.position = new Point(posXParent, posYParent);
                        if (tileCache.ContainsKey(tmp))
                        {
                            // something like that
                            CroppedBitmap cBm = new CroppedBitmap(tileCache[tmp].data, new Int32Rect(resteX, resteY , (int)(this.m_tileSize.Width / 2), (int)(this.m_tileSize.Height / 2)));
                            TransformedBitmap tBm = new TransformedBitmap(cBm, new ScaleTransform(2, 2));

                            tileDat.data = tBm; //tileCache[tmp].data;
                        }
                        else
                        {
                            tileDat.data = this.m_missingImage;
                        }
                        tileInf.isWaitingRendering = true;
                        if(this.m_generateTiles)
                            m_tileGen.generateBitmap(tileInf);
                        numberOfTileInGeneration++;
                        tileCache[tk] = tileDat;
                        toDraw = this.m_missingImage;
                    }
                    rectangle.Location = new System.Windows.Point(tileInf.position.X - upperLeftImage.X, tileInf.position.Y - upperLeftImage.Y);
                    rectangle.Width = m_tileSize.Width;
                    rectangle.Height = m_tileSize.Height;
                    context.DrawImage(toDraw, rectangle);
                }
            }


            DateTime end = DateTime.Now;

            if(numberOfTileInGeneration > 0)
            {
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

        }

        public void ResetCache()
        {
            this.tileCache.Clear();
            this.InvalidateVisual();
        }

        void addImageToCache(TileInfo info, byte[] rawImage)
        {
            // Add small white cross at borders
            rawImage[0] = 255;
            rawImage[1] = 255;
            rawImage[2] = 255;

            rawImage[4] = 255;
            rawImage[5] = 255;
            rawImage[6] = 255;

            rawImage[(this.m_tileSize.Width-1) * 4] = 255;
            rawImage[(this.m_tileSize.Width-1) * 4 + 1] = 255;
            rawImage[(this.m_tileSize.Width-1) * 4 + 2] = 255;

            // Generate Bitmap 

            PixelFormat pf = PixelFormats.Bgr32;
            int rawStride = (this.m_tileSize.Width * pf.BitsPerPixel) / 8;
            BitmapSource bitmap = BitmapSource.Create(this.m_tileSize.Width, this.m_tileSize.Height,
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
