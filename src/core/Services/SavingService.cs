using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Mitheti.Core.Services
{
    public class SavingService : ISavingService, IDisposable
    {
        public const string RecordDelayConfigKey = "database:delay";
        public const int RecordDelayDefault = 1;
        public const int MillisecondsInMinute = 60 * 1000;
        public const int StopWait = 500;

        private readonly IDatabaseService _database;

        private readonly object _lock = new object();

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly List<AppTimeModel> _records = new List<AppTimeModel>();
        private readonly Task _savingTask;

        public SavingService(IConfiguration config, IDatabaseService database)
        {
            _database = database;

            var delayMinutes = config.GetValue(RecordDelayConfigKey, RecordDelayDefault);
            _savingTask = SavingTask(_tokenSource.Token, delayMinutes * MillisecondsInMinute);
        }

        public void Add(AppTimeModel data)
        {
            lock (_lock)
            {
                var sameRecord = _records.Find(data.Equals);

                if (sameRecord == null)
                {
                    _records.Add(data);
                }
                else
                {
                    sameRecord.Duration += data.Duration;
                }
            }
        }

        private async Task SavingTask(CancellationToken stoppingToken, int delay)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                SaveToDatabase();

                await Task.Delay(delay, stoppingToken); //.ContinueWith(Extensions.NoErrorOnCancellation);
            }
        }

        private void SaveToDatabase()
        {
            // достаточно лочить _records, а не _lock
            // и лочить только там где используется _records
            lock (_lock) // этот лок убрать
            {
                //lock(_records)
                //{
                if (_records.Count == 0)
                {
                    return;
                }
                //}

                using var context = _database.GetContext();

                //lock(_records)
                //{
                context.AddRange(_records);
                //}

                context.SaveChanges();

                //lock(_records)
                //{
                _records.Clear();
                //}
            }
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _savingTask.WaitCancelled(StopWait);
            _tokenSource.Dispose();

            //? save leftovers;
            SaveToDatabase();
        }
    }
}
