using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FractalMaps
{
    // Heavily borrowed from http://msdn.microsoft.com/fr-fr/library/dd287147%28v=vs.110%29.aspx
    public class ProducerConsumerStack<T>:IProducerConsumerCollection<T>
    {
        Object m_sync = new Object();
        Stack<T> m_stack = new Stack<T>();


        public void CopyTo(T[] array, int index)
        {
            lock (m_sync)
                m_stack.CopyTo(array, index);
        }

        public T[] ToArray()
        {
            T[] rval = null;
            lock (m_sync) rval = m_stack.ToArray();
            return rval;
        }

        public bool TryAdd(T item)
        {
            lock (m_sync)
                m_stack.Push(item);

            return true;
        }

        public bool TryTake(out T item)
        {
            bool rval = true;
            lock (m_sync)
            {
                if (m_stack.Count == 0) 
                { 
                    item = default(T); 
                    rval = false; 
                }
                else item = m_stack.Pop();
            }
            return rval;
        }

        public IEnumerator<T> GetEnumerator()
        {
            Stack<T> stackCopy = null;
            lock (m_sync) 
                stackCopy = new Stack<T>(m_stack);
            return stackCopy.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            lock (m_sync) 
                ((ICollection)m_stack).CopyTo(array, index);
        }

        public int Count
        {
            get { 
                return m_stack.Count; 
            }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get { return m_sync; }
        }
    }

    class ThreadedQueue<T>: IDisposable
    {
        List<Thread> threadpool;
        // TODO: Better names
        ProducerConsumerStack<T> pcs;
        BlockingCollection<T> bc;

        public ThreadedQueue(int number, ParameterizedThreadStart start, ThreadPriority priority = ThreadPriority.Normal)
        {
            pcs = new ProducerConsumerStack<T>();
            bc = new BlockingCollection<T>(pcs);

            this.threadpool = new List<Thread>();
            for(int i = 0; i < number; i++)
            {
                Thread t = new Thread(start);
                threadpool.Add(t);
                t.Priority = priority;
                t.Start((object)bc);
            }
        }

        public void Dispose()
        {
            foreach(Thread t in threadpool)
            {
                t.Abort();
            }
            bc.Dispose();
            bc = null;
        }

        public void addElement(T obj)
        {
            bc.Add(obj);
        }


        public int getStackCount()
        {
            return bc.Count;
        }
    }
}
