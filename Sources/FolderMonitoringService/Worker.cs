using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FolderMonitoringService
{
    public class Worker : BackgroundService
    {
        public IConfiguration Configuration { get; }

        private readonly ILogger<Worker> _logger;
        private List<string> _foldersToScanList = new List<string>();

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            Configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MonitorFolders();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Error occurs when MonitorFolders");
                }
                finally
                {
                    await Task.Delay(10000, stoppingToken);
                }
            }
        }

        private async Task MonitorFolders()
        {
            _foldersToScanList = Configuration.GetSection("FoldersToScan").Value.Split(';').ToList();
            var monitorTasks = new List<Task>();

            foreach (var folderToScan in _foldersToScanList)
            {
                monitorTasks.Add(MonitorFolder(folderToScan));
            }

            await Task.WhenAll(monitorTasks);
        }

        private Task MonitorFolder(string folderPath)
        {
            try
            {
                var folderExist = Directory.Exists(@folderPath);
                _logger.LogInformation(folderExist == true ? $" { folderPath } exist " : $" { folderPath } not exist ");
                if (folderExist)
                {
                    var folderSize = GetDirectorySize(@folderPath);
                    _logger.LogInformation(folderPath + $" size is { folderSize } byte");
                }                    
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error occurs when MonitorFolder({ folderPath })");
            }

            return Task.CompletedTask;
        }

        private static long GetDirectorySize(string folderPath)
        {
            DirectoryInfo di = new DirectoryInfo(folderPath);
            return di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }
    }
}
