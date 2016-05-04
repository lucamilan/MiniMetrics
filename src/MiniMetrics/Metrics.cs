﻿using System;
using MiniMetrics.Extensions;
using MiniMetrics.Net;

namespace MiniMetrics
{
    public class Metrics
    {
        private static IMetricsClient MetricsClient = new NullMetricsClient();

        private static readonly Object Sync = new Object();

        public static void StartFromConfig()
        {
            Start(MetricsOptions.CreateFromConfig());
        }

        public static void Start(MetricsOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            lock (Sync)
            {
                StopInternal();
                TcpMetricsClient.StartAsync(OutbountChannel.From(options.HostName, options.Port).Build())
                                .ContinueWith(_ => MetricsClient = _.Result)
                                .Wait();
            }
        }

        public static void StartAutoRecoverable(MetricsOptions options, TimeSpan recoverySlice)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            lock (Sync)
            {
                if (MetricsClient != null)
                    return;

                StopInternal();
                TcpMetricsClient.StartAsync(OutbountChannel.From(options.HostName, options.Port).BuildAutoRecoverable(recoverySlice))
                                .ContinueWith(_ => MetricsClient = _.Result)
                                .Wait();
            }
        }

        public static void Stop()
        {
            lock (Sync)
            {
                if (MetricsClient == null)
                    return;

                StopInternal();
            }
        }

        public static void Report<TValue>(String key, TValue value)
        {
            lock (Sync)
            {
                if (MetricsClient == null)
                    throw new InvalidOperationException("client has to be started by calling 'Start' method");

                MetricsClient.Report(key, value);
            }
        }

        public static IDisposable ReportTimer(String key, Func<IStopwatch> stopWatchFactory = null)
        {
            lock (Sync)
            {
                if (MetricsClient == null)
                    throw new InvalidOperationException("client has to be started by calling 'Start' method");

                return MetricsClient.ReportTimer(key, stopWatchFactory ?? SimpleStopwatch.StartNew);
            }
        }

        private static void StopInternal()
        {
            MetricsClient?.Dispose();
            MetricsClient = null;
        }
    }
}