using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FractalMaps
{
    interface ITileGenerator:IDisposable
    {
        // Async generation
        void generateBitmap(TileInfo tInfo);

        // misc. stats, nothing formal right now
        string getStats();
    }

}
