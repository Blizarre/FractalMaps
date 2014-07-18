using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageExplorer
{
    class LowPriorityThreadPool
    {
        List<Thread> threadpool;

        
        public LowPriorityThreadPool(int number, ThreadStart start)
        {
            this.threadpool = new List<Thread>();
            for(int i = 0; i < number; i++)
            {
                threadpool.Add(new Thread(start));
            }
        }


    }
}
