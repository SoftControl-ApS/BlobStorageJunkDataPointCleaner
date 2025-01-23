using Microsoft.WindowsAzure.Storage.Blob;
using SharedLibrary.Azure;
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

        public static string GetFileName(CloudBlockBlob blob)
        {

            var fileName = Path.GetFileName(blob.Name);

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

        public static string GetFileName(DateOnly date, FileType filetype)
        {

            var fileName = string.Empty;
            switch (filetype)
            {
                case FileType.Day:
                    fileName = $"pd{date.Year}{date.Month:D2}{date.Day:D2}";
                    break;
                case FileType.Month:
                    fileName = $"pm{date.Year}{date.Month:D2}";
                    break;
                case FileType.Year:
                    fileName = $"py{date.Year}";
                    break;
                case FileType.Total:
                    fileName = $"pt";
                    break;
            }
            return fileName;
        }
    }
}
