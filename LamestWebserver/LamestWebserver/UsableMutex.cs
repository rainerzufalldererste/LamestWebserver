using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A regular Mutex, but disposable if Lock() is called.
/// This let's you use it within a using statement
/// 
/// Syntax:
/// 
/// using (usableMutex.Lock())
/// {
///     // Your Code.
/// }
/// 
/// </summary>
namespace LamestWebserver
{
    public class UsableMutex
    {
        private Mutex innerMutex;

        public UsableMutex()
        {
            innerMutex = new Mutex();
        }

        public UsableMutex(bool initiallyOwned)
        {
            innerMutex = new Mutex(initiallyOwned);
        }

        public UsableMutex(bool initiallyOwned, string name)
        {
            innerMutex = new Mutex(initiallyOwned, name);
        }

        public UsableMutex(bool initiallyOwned, string name, out bool createdNew)
        {
            innerMutex = new Mutex(initiallyOwned, name, out createdNew);
        }

        public UsableMutex(bool initiallyOwned, string name, out bool createdNew, System.Security.AccessControl.MutexSecurity mutexSecurity)
        {
            innerMutex = new Mutex(initiallyOwned, name, out createdNew, mutexSecurity);
        }

        public bool WaitOne()
        {
            return innerMutex.WaitOne();
        }

        public bool WaitOne(int millisecondsTimeout)
        {
            return innerMutex.WaitOne(millisecondsTimeout);
        }

        public bool WaitOne(TimeSpan timeout)
        {
            return innerMutex.WaitOne(timeout);
        }

        public bool WaitOne(int millisecondsTimeout, bool exitContext)
        {
            return innerMutex.WaitOne(millisecondsTimeout, exitContext);
        }

        public bool WaitOne(TimeSpan timeout, bool exitContext)
        {
            return innerMutex.WaitOne(timeout, exitContext);
        }

        public void ReleaseMutex()
        {
            innerMutex.ReleaseMutex();
        }

        public UsableMutexLocker Lock()
        {
            return new UsableMutexLocker(innerMutex);
        }
    }
}
