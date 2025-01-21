using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static SharedLibrary.util.Util;
namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        public async Task<bool> LetTheMagicHappen(DateOnly date)
        {
            var result = await ReadAndErase(date);




            return false;
        }
        private async Task<string> ReadAndErase(DateOnly date)
        {
            var allDayFiles = GetYearDayFiles(date);

            return await HandleDayFiles(date);

        }

        async Task<ConcurrentBag<BlobFile>> GetYearDayFiles(DateOnly date)
        {

            var daysFiles = await GetYear_DayFilesAsync(date);

            if (await DeleteAllYearFilesExceptDays(date))
            {
                await HandleTheRest(daysFiles);
            }

            return new ConcurrentBag<BlobFile>();
        }


        async Task HandleTheRest(ConcurrentBag<BlobFile> day)
        {


            return;
        }

        async Task<bool> DeleteAllYearFilesExceptDays(DateOnly date)
        {

            try
            {



                return true;
            }
            catch (Exception e)
            {
                LogError("Could not delete all year files, year:" + date.Year);
                LogError(e.Message);
                return false;
                throw;
            }
        }

    }


    public class BlobFile
    {
        public FileType FileType { get; set; }

        public DateOnly Date { get; set; }

        public string DataJson { get; set; }

    }
}
