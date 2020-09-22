﻿using System;
using System.Threading.Tasks;
using Humanizer;

namespace SIBR.Storage.Ingest
{
    public abstract class IntervalWorker : BaseWorker
    {
        public TimeSpan Interval { get; set; }
        

        protected abstract Task RunInterval();

        protected virtual Task BeforeStart() => Task.CompletedTask;

        protected override async Task Run()
        {
            await BeforeStart();
            while (true)
            {
                async Task Inner()
                {           
                    try
                    {
                        _logger.Debug("Running interval worker {WorkerType} (interval of {Interval})", GetType().Name, Interval.Humanize());
                        await RunInterval();
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Error running worker {WorkerType}", GetType().Name);
                    }
                }

                var _ = Inner();

                // (arbitrary, just for tick alignment mostly)
                var epoch = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
                var waitTime = TimeSpan.FromTicks(Interval.Ticks - (DateTimeOffset.UtcNow - epoch).Ticks % Interval.Ticks);
                if (waitTime > TimeSpan.Zero)
                    await Task.Delay(waitTime);
            }
        }

        protected IntervalWorker(IServiceProvider services) : base(services)
        {
        }
    }
}