using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TinyInsightsLib
{
    public static class TinyInsights
    {
        private static List<ITinyInsightsProvider> insightsProviders = new List<ITinyInsightsProvider>();

        public static void Configure(ITinyInsightsProvider provider)
        {
            insightsProviders.Add(provider);
        }

        public static void Configure(params ITinyInsightsProvider[] providers)
        {
            insightsProviders.AddRange(providers.ToList());
        }

        public static async Task TrackErrorAsync(Exception ex, Dictionary<string, string> properties = null)
        {
            var tasks = new List<Task>();

            foreach (var provider in insightsProviders)
            {
                var task = provider.TrackErrorAsync(ex, properties);
                tasks.Add(task);
            }

            _ = Task.WhenAll(tasks);
        }

        public static async Task TrackPageViewAsync(string viewName, Dictionary<string, string> properties = null)
        {
            var tasks = new List<Task>();

            foreach (var provider in insightsProviders)
            {
                    var task = provider.TrackPageViewAsync(viewName, properties);
                    tasks.Add(task);
            }

            _ = Task.WhenAll(tasks);
        }

        public static async Task TrackEventAsync(string eventName, Dictionary<string, string> properties = null)
        {
            var tasks = new List<Task>();

            foreach (var provider in insightsProviders)
            {
                    var task = provider.TrackEventAsync(eventName, properties);
                    tasks.Add(task);
            }

            _ = Task.WhenAll(tasks); 
        }

        public static async Task TrackDependencyAsync(string dependencyType, string dependencyName, DateTimeOffset startTime, TimeSpan duration, bool success, int resultCode = 0, Exception exception = null)
        {
            var tasks = new List<Task>();

            foreach (var provider in insightsProviders)
            {
                var task = provider.TrackDependencyAsync(dependencyType, dependencyName, startTime, duration, success, resultCode, exception);
                tasks.Add(task);
            }

            _ =  Task.WhenAll(tasks);
        }

        public static TinyDependency CreateDependencyTracker(string dependencyType, string dependencyName)
        {
            var dependency = new TinyDependency()
            {
                DependencyType = dependencyType,
                DependencyName = dependencyName
            };

            return dependency;
        }
    }
}
