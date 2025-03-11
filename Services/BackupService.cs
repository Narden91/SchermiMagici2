using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WpfApp1.Services
{
    /// <summary>
    /// Provides backup functionality for pen data during task execution
    /// </summary>
    public class BackupService
    {
        private readonly string _backupFolder;
        private readonly SynchronizationContext _synchronizationContext;
        private readonly TimeSpan _backupInterval = TimeSpan.FromSeconds(30);
        private CancellationTokenSource _backupCts;
        private Task _backupTask;
        private string _currentTaskName;

        /// <summary>
        /// Initializes a new instance of the BackupService class
        /// </summary>
        /// <param name="experimentFolder">The folder where backups will be stored</param>
        public BackupService(string experimentFolder)
        {
            _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
            _backupFolder = Path.Combine(experimentFolder, "Backup");

            // Create backup directory if it doesn't exist
            if (!Directory.Exists(_backupFolder))
            {
                try
                {
                    Directory.CreateDirectory(_backupFolder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BackupService] Failed to create backup directory: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Starts periodic backup for the current task
        /// </summary>
        /// <param name="taskName">Name of the current task</param>
        /// <param name="getData">Function that retrieves the current pen data</param>
        public void StartBackup(string taskName, Func<List<Wacom.Devices.RealTimePointReceivedEventArgs>> getData)
        {
            // Cancel any existing backup task
            StopBackup();

            _currentTaskName = taskName;
            _backupCts = new CancellationTokenSource();

            _backupTask = Task.Run(async () =>
            {
                try
                {
                    while (!_backupCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(_backupInterval, _backupCts.Token);

                        if (_backupCts.Token.IsCancellationRequested)
                            break;

                        // Create backup
                        CreateBackup(taskName, getData());
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BackupService] Backup process encountered an error: {ex.Message}");
                }
            }, _backupCts.Token);
        }

        /// <summary>
        /// Stops the backup process
        /// </summary>
        public void StopBackup()
        {
            if (_backupCts != null)
            {
                _backupCts.Cancel();
                _backupCts.Dispose();
                _backupCts = null;
            }

            _backupTask = null;
        }

        /// <summary>
        /// Creates a backup of the current pen data
        /// </summary>
        /// <param name="taskName">Name of the current task</param>
        /// <param name="penData">Collection of pen data points</param>
        private void CreateBackup(string taskName, List<Wacom.Devices.RealTimePointReceivedEventArgs> penData)
        {
            if (penData == null || penData.Count == 0)
                return;

            try
            {
                string backupFileName = $"{taskName}_backup.csv";
                string backupFilePath = Path.Combine(_backupFolder, backupFileName);

                using (var stream = new StreamWriter(backupFilePath, false, Encoding.UTF8))
                {
                    // Write CSV header
                    stream.WriteLine("Timestamp,PointX,PointY,Phase,Pressure,PointDisplayX,PointDisplayY,PointRawX,PointRawY,PressureRaw,TimestampRaw,Sequence,Rotation,Azimuth,Altitude,TiltX,TiltY,PenId");

                    // Write pen data
                    StringBuilder sb = new StringBuilder(200);
                    foreach (var item in penData)
                    {
                        sb.Clear();
                        sb.Append($"{item.Timestamp.ToString("O")},{item.Point.X,6},{item.Point.Y,6},{item.Phase.ToString(),-11}");
                        sb.Append(item.Pressure.HasValue ? $",{item.Pressure.Value,9}" : ",");
                        sb.Append(item.PointDisplay.HasValue ? $",{item.PointDisplay.Value.X,6},{item.PointDisplay.Value.Y,6}" : ",,");
                        sb.Append(item.PointRaw.HasValue ? $",{item.PointRaw.Value.X,6},{item.PointRaw.Value.Y,6}" : ",,");
                        sb.Append(item.PressureRaw.HasValue ? $",{item.PressureRaw.Value,6}" : ",");
                        sb.Append(item.TimestampRaw.HasValue ? $",{item.TimestampRaw.Value,8}" : ",");
                        sb.Append(item.Sequence.HasValue ? $",{item.Sequence.Value,8}" : ",");
                        sb.Append(item.Rotation.HasValue ? $",{item.Rotation.Value,9}" : ",");
                        sb.Append(item.Azimuth.HasValue ? $",{item.Azimuth.Value,9}" : ",");
                        sb.Append(item.Altitude.HasValue ? $",{item.Altitude.Value,9}" : ",");
                        sb.Append(item.Tilt.HasValue ? $",{item.Tilt.Value.X,9},{item.Tilt.Value.Y,9}" : ",,");
                        sb.Append(item.PenId.HasValue ? $",0x{item.PenId.Value:x8}" : ",");
                        stream.WriteLine(sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BackupService] Error creating backup: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the backup for a specific task if it exists
        /// </summary>
        /// <returns>Path to the backup file, or null if no backup exists</returns>
        public string GetBackupFile()
        {
            if (string.IsNullOrEmpty(_currentTaskName))
                return null;

            string backupFileName = $"{_currentTaskName}_backup.csv";
            string backupFilePath = Path.Combine(_backupFolder, backupFileName);

            return File.Exists(backupFilePath) ? backupFilePath : null;
        }

        /// <summary>
        /// Cleans up backup files for the current task
        /// </summary>
        public void CleanupBackup()
        {
            if (string.IsNullOrEmpty(_currentTaskName))
                return;

            string backupFileName = $"{_currentTaskName}_backup.csv";
            string backupFilePath = Path.Combine(_backupFolder, backupFileName);

            if (File.Exists(backupFilePath))
            {
                try
                {
                    File.Delete(backupFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BackupService] Error deleting backup file: {ex.Message}");
                }
            }
        }
    }
}