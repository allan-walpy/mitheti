﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Mitheti.Core.Services
{
    public class StatisticDayOfWeekService : IStatisticDayOfWeekService
    {
        public const int DaysOfWeekCount = 7;

        private readonly IDatabaseService _database;

        public StatisticDayOfWeekService(IDatabaseService database)
        {
            _database = database;
        }

        public List<int> GetTotal(TimeInterval interval) => GetList(interval);

        public List<int> GetTotal() => GetTotal(TimeInterval.All);

        public List<double> GetPercentage(TimeInterval interval)
        {
            var result = Enumerable.Repeat((double) 0, DaysOfWeekCount).ToList();
            var list = GetList(interval);
            var max = list.Max();
            if (max == 0)
            {
                //? avoiding 0/0 resulting in NaN;
                max = 1;
            }

            for (var i = 0; i < DaysOfWeekCount; i++)
            {
                result[i] = (double) list[i] / max;
            }

            return result;
        }

        public List<double> GetPercentage() => GetPercentage(TimeInterval.All);

        private List<int> GetList(TimeInterval interval)
        {
            using var context = _database.GetContext();
            var result = Enumerable.Repeat(0, DaysOfWeekCount).ToList();

            // TODO: define `==` also;
            if (!interval.Equals(TimeInterval.All))
            {
                // TODO: check if this works;
                context.AppTimes.Where(interval.Equals).ToList()
                    .ForEach(timeSpan => result[(int) timeSpan.Time.DayOfWeek] += timeSpan.Duration);
            }
            else
            {
                context.AppTimes
                    .ForEachAsync(timeSpan => result[(int) timeSpan.Time.DayOfWeek] += timeSpan.Duration).Wait();
            }

            return result;
        }
    }
}