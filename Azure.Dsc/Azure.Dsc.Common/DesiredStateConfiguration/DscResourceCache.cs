using System;
using System.Collections.Generic;
using Sogeti.IaC.Model.DesiredStateConfiguration;

namespace Azure.Dsc.Common.DesiredStateConfiguration
{
    public sealed class DscResourceCache
    {
        private static volatile DscResourceCache _instance;
        private static readonly object SyncRoot = new object();
        private readonly Dictionary<string, DscResource> _cache;
        private DateTime LastUpdated;

        public DscResource this[string path]
        {
            get { return _cache[path]; }
            set
            {
                if (_cache.ContainsKey(path))
                {
                    _cache[path] = value;
                }
                else
                {
                    _cache.Add(path, value);
                }
            }
        }

        public IEnumerable<DscResource> Resources
        {
            get { return _cache.Values; }
        }

        private DscResourceCache()
        {
            _cache = new Dictionary<string, DscResource>();
        }

        public static DscResourceCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new DscResourceCache();
                        }
                    }
                }

                if (_instance.Count == 0 || _instance.LastUpdated.AddMinutes(15) < DateTime.UtcNow)
                {
                    _instance.Clear();

                    var resources = DscParser.GetAllDscResources();

                    foreach (var resource in resources)
                    {
                        _instance[resource.ToString()] = resource;
                    }

                    _instance.LastUpdated = DateTime.UtcNow;
                }

                return _instance;
            }
        }

        public int Count
        {
            get
            {
                return _cache.Count;
            }
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
