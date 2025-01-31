using SharedLibrary.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using static SharedLibrary.util.Util;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;

namespace SharedLibrary.Azure
{
    public partial class AzureBlobCtrl
    {
        private ConcurrentBag<Inverter> _inverters = new ConcurrentBag<Inverter>();

        private ConcurrentBag<Inverter> Inverters
        {
            get => _inverters;
        }

        private static async Task<ConcurrentBag<Inverter>> ExtractInverters(ProductionDto production)
        {
            var inverters = new ConcurrentBag<Inverter>(production.Inverters);
            return await Task.FromResult(inverters);
        }

        private ConcurrentBag<Inverter> ExtractInverters(IEnumerable<Inverter> inverters)
        {
            var updatedInverters = new ConcurrentBag<Inverter>();
            Parallel.ForEach(inverters, inverter =>
            {
                updatedInverters.Add(new Inverter
                                     {
                                         Id = inverter.Id,
                                         Production = new List<DataPoint>(),
                                     });
            });
            return updatedInverters;
        }

        private async Task<ConcurrentBag<Inverter>> GetInverters()
        {
            if (!Inverters.Any())
            {
                return await InitInverters();
            }

            return ExtractInverters(Inverters);
        }

        async Task<ConcurrentBag<Inverter>> InitInverters()
        {
            var production = ProductionDto.FromJson(await ReadBlobFile("pt"));
            var invs = await ExtractInverters(production);
            this._inverters = invs;
            return _inverters;
        }
        private static int ExtractYearFromFileName(string fileName)
        {
            if (fileName.Length < 6)
            {
                throw new ArgumentException("fileName must be at least 6 characters");
            }

            return Convert.ToInt32(fileName.Substring(2, 4));
        }

        private static int ExtractMonthFromFileName(string fileName)
        {
            if (fileName.Length < 8)
            {
                throw new ArgumentException("fileName must be at least 8 characters");
            }

            var result = (fileName.Substring(6, 2));
            var formated = $"{result:D2}";
            return Convert.ToInt32(formated);
        }

        private static int ExtractDayFromFileName(string fileName)
        {
            if (fileName.Length < 10)
            {
                throw new ArgumentException("fileName must be at least 10 characters");
            }

            return Convert.ToInt32(fileName.Substring(8, 2));
        }

        private DateOnly ExtractDateFromFileName(string fileName)
        {
            int year = ExtractYearFromFileName(fileName);
            int month = ExtractMonthFromFileName(fileName);
            int day = 01;
            if (fileName.Length > 8)
                day = ExtractDayFromFileName(fileName);

            DateOnly date = new DateOnly(year, month, day);
            return date;
        }

        public string ValidateFileName(string fileName, string? sn)
        {
            if (string.IsNullOrEmpty(sn) && !string.IsNullOrEmpty(InstallationId))
            {
                sn = InstallationId;
            }
            else if (string.IsNullOrEmpty(sn) && string.IsNullOrEmpty(InstallationId))
            {
                throw new ArgumentException("Either 'InstallationId' or'sn' parameter must be provided.");
            }

            return sn;
        }
    }
}