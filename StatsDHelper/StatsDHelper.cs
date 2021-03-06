﻿using System.Configuration;
using System.Diagnostics;
using StatsdClient;

namespace StatsDHelper
{
    public class StatsDHelper : IStatsDHelper
    {
        private readonly IPrefixProvider _prefixProvider;
        private readonly IStatsd _statsdClient;
        private string _prefix;

        private static readonly object Padlock = new object();
        private static IStatsDHelper _instance;

        internal StatsDHelper(IPrefixProvider prefixProvider, IStatsd statsdClient)
        {
            _prefixProvider = prefixProvider;
            _statsdClient = statsdClient;
        }

        public IStatsd StatsdClient
        {
            get{return _statsdClient;}
        }

        public void LogCount(string name, int count = 1)
        {
            _statsdClient.LogCount(string.Format("{0}.{1}", GetStandardPrefix, name), count);
        }

        public void LogGauge(string name, int value)
        {
            _statsdClient.LogGauge(string.Format("{0}.{1}", GetStandardPrefix, name), value);
        }

        public void LogTiming(string name, long milliseconds)
        {
            _statsdClient.LogTiming(string.Format("{0}.{1}", GetStandardPrefix, name), milliseconds);
        }

        public void LogSet(string name, int value)
        {
            _statsdClient.LogSet(string.Format("{0}.{1}", GetStandardPrefix, name), value);
        }

        public string GetStandardPrefix
        {
            get
            {
                if (string.IsNullOrEmpty(_prefix))
                {
                    _prefix = _prefixProvider.GetPrefix();
                }
                return _prefix;
            }
        }

        public static IStatsDHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Padlock)
                    {
                        if (_instance == null)
                        {
                            var host = ConfigurationManager.AppSettings["StatsD.Host"];
                            var port = ConfigurationManager.AppSettings["StatsD.Port"];
                            var applicationName = ConfigurationManager.AppSettings["StatsD.ApplicationName"];

                            if (string.IsNullOrEmpty(host)
                                || string.IsNullOrEmpty(port)
                                || string.IsNullOrEmpty(applicationName))
                            {
                                Debug.WriteLine(
                                    "One or more StatsD Client Settings missing. This is designed to fail silently. Ensure an application name, host and port are set or no metrics will be sent. Set Values: Host={0} Port={1}",
                                    host, port);
                                return new NullStatsDHelper();
                            }

                            _instance = new StatsDHelper(new PrefixProvider(new HostPropertiesProvider()), new Statsd(host, int.Parse(port)));
                        }
                    }
                }
                return _instance;
            }
        }
    }
}