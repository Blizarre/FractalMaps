using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageExplorer
{
    class ThreadedQueue
    {
        List<Thread> threadpool;
        Stack syncStack;

        public ThreadedQueue(int number, ThreadStart start, ThreadPriority priority = ThreadPriority.Normal)
        {
            syncStack = Stack.Synchronized(new Stack());
            this.threadpool = new List<Thread>();
            for(int i = 0; i < number; i++)
            {
                Thread t = new Thread(start);
                threadpool.Add(t);
                t.Priority = priority;
                t.Start((object)syncStack);
            }
        }

        public void addElement(object obj)
        {
            syncStack.Push(obj);
        }

        public int getStackCount()
        {
            return syncStack.Count;
        }
    }
}
