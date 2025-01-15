using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.util
{
    public partial class Util
    {
        public static string GetFileName(string blobName)
        {
            var fileName = Path.GetFileName(blobName);

            if (fileName.EndsWith(".zip"))
            {
                fileName = fileName.Substring(0, fileName.Length - 4);
            }
            else if (fileName.EndsWith(".json"))
            {
                fileName = fileName.Substring(0, fileName.Length - 5);
            }

            return fileName;
        }
    }
}
