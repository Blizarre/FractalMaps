using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ImageExplorer
{
    interface ITileGenerator
    {
        // Async generation
        void generateBitmap(TileInfo tInfo);

        // misc. stats, nothing formal right now
        string getStats();
    }

}
