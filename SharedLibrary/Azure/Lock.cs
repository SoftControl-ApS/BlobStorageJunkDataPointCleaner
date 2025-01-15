using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        private static Dictionary<string, ReaderWriterLock> Locks = new Dictionary<string, ReaderWriterLock>();
        private static object LocksLock = new object();

        private static TimeSpan LockTimeOut = TimeSpan.FromMinutes(1);

        private static ReaderWriterLock GetLock(string file, string zip, string sn)
        {
            var key = file + zip + sn;
            ReaderWriterLock foundLock;

            lock (LocksLock)
            {
                if (Locks.ContainsKey(key) == true)
                {
                    return Locks[key];
                }
                else
                {
                    var newLock = new ReaderWriterLock();

                    Locks.Add(key, newLock);

                    return newLock;
                }
            }

        }

    }
}
