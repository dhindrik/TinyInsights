using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;

#if WINDOWS
using Windows.Storage;
using Windows.UI.Xaml;
#endif

namespace TinyInsightsLib.ApplicationInsights;
    public class ApplicationInsightsProvider : ITinyInsightsProvider
    {
        private const string userIdKey = nameof(userIdKey);

        private const string crashLogFilename = "crashes.tinyinsights";
#if IOS || MACCATALYST || ANDROID
        private readonly string logPath = FileSystem.CacheDirectory;

#endif

        private TelemetryClient client;

        public bool IsTrackErrorsEnabled { get; set; } = true;
        public bool IsTrackPageViewsEnabled { get; set; } = true;
        public bool IsTrackEventsEnabled { get; set; } = true;
        public bool IsTrackDependencyEnabled { get; set; } = true;

#if IOS|| MACCATALYST || ANDROID
        public ApplicationInsightsProvider(string connectionString)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            var configuration = new TelemetryConfiguration()
            {
                ConnectionString = connectionString
            };

            try
            {
                client = new TelemetryClient(configuration);

                AddMetaData();
            }
            catch (Exception)
            {
            }

            Task.Run(SendCrashes);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleCrash(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleCrash((Exception)e.ExceptionObject);
        }

#elif WINDOWS
        public ApplicationInsightsProvider(Application app, string key)
        {
            app.UnhandledException += App_UnhandledException;

            client = new TelemetryClient();
            client.InstrumentationKey = key;

            AddMetaData();

            Task.Run(SendCrashes);
        }

        private void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            HandleCrash(e.Exception);
        }
#endif

        private void AddMetaData()
        {
            client.Context.Device.OperatingSystem = DeviceInfo.Platform.ToString();
            client.Context.Device.Model = DeviceInfo.Model;
            client.Context.Device.Type = DeviceInfo.Idiom.ToString();

            //Role name will show device name if we don't set it to empty and we want it to be so anonymous as possible.
            client.Context.Cloud.RoleName = string.Empty;
            client.Context.Cloud.RoleInstance = string.Empty;

            if (Preferences.ContainsKey(userIdKey))
            {
                var userId = Preferences.Get(userIdKey, string.Empty);

                client.Context.User.Id = userId;
            }
            else
            {
                var userId = Guid.NewGuid().ToString();
                Preferences.Set(userIdKey, userId);

                client.Context.User.Id = userId;
            }

            client.Context.GlobalProperties.Add("Language", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            client.Context.GlobalProperties.Add("Manufacturer", DeviceInfo.Manufacturer);
            client.Context.GlobalProperties.Add("AppVersion", AppInfo.VersionString);
            client.Context.GlobalProperties.Add("AppBuildNumber", AppInfo.BuildString);
        }

        private async Task SendCrashes()
        {
            try
            {
                var crashes = ReadCrashes();

                if (crashes != null)
                {
                    var properties = new Dictionary<string, string>();
                    properties.Add("IsCrash", "true");

                    foreach (var crash in crashes)
                    {
                        await TrackErrorAsync(crash, properties);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private List<Exception> ReadCrashes()
        {
            try
            {
#if IOS || MACCATALYST || ANDROID
                var path = Path.Combine(logPath, crashLogFilename);

                if (!File.Exists(path))
                {
                    return new List<Exception>();
                }

                var json = File.ReadAllText(path);
#elif WINDOWS
            
            var fileTask = ApplicationData.Current.LocalCacheFolder.CreateFileAsync(crashLogFilename, CreationCollisionOption.OpenIfExists).AsTask<StorageFile>();
            fileTask.Wait();
            var file = fileTask.Result;

            var readTask = FileIO.ReadTextAsync(file).AsTask<string>();
            readTask.Wait();
            var json = readTask.Result;
#endif
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<Exception>();
                }

                return JsonConvert.DeserializeObject<List<Exception>>(json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

            }
            catch (Exception)
            {
            }

            return new List<Exception>();
        }

        private void HandleCrash(Exception ex)
        {
            try
            {
                var crashes = ReadCrashes();

                crashes.Add(ex);

                var json = JsonConvert.SerializeObject(crashes, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

#if IOS|| MACCATALYST || ANDROID
                var path = Path.Combine(logPath, crashLogFilename);

                if (!File.Exists(path))
                {
                    File.Create(path);
                }

                System.IO.File.WriteAllText(path, json);
#elif WINDOWS
             var fileTask = ApplicationData.Current.LocalCacheFolder.CreateFileAsync(crashLogFilename, CreationCollisionOption.OpenIfExists).AsTask<StorageFile>();
            fileTask.Wait();
            var file = fileTask.Result;

            var writeTask = FileIO.WriteTextAsync(file, json).AsTask();
            writeTask.Wait();
#endif
            }
            catch (Exception)
            {
            }
        }

        public async Task TrackErrorAsync(Exception ex, Dictionary<string, string> properties = null)
        {
            try
            {
                client.TrackException(ex, properties);
                client.Flush();
            }
            catch (Exception)
            {
            }
        }

        public async Task TrackEventAsync(string eventName, Dictionary<string, string> properties = null)
        {
            try
            {
                client.TrackEvent(eventName, properties);
                client.Flush();
            }
            catch (Exception ex)
            {
                _ = TinyInsights.TrackErrorAsync(ex);
            }
        }

        public async Task TrackPageViewAsync(string viewName, Dictionary<string, string> properties = null)
        {
            try
            {
                client.TrackPageView(viewName);
                client.Flush();
            }
            catch (Exception ex)
            {
                _ = TinyInsights.TrackErrorAsync(ex);
            }
        }

        public async Task TrackDependencyAsync(string dependencyType, string dependencyName, DateTimeOffset startTime, TimeSpan duration, bool success, int resultCode = 0, Exception exception = null)
        {
            try
            {
                var dependency = new DependencyTelemetry(dependencyType, dependencyName, startTime, duration, success)
                {
                    ResultCode = resultCode.ToString()
                };

                if (exception != null)
                {
                    var properties = new Dictionary<string, string>();
                    properties.Add("Exception message", exception.Message);
                    properties.Add("StackTrace", exception.StackTrace);

                    if (exception.InnerException != null)
                    {
                        properties.Add("Inner exception message", exception.InnerException.Message);
                        properties.Add("Inner exception stackTrace", exception.InnerException.StackTrace);
                    }
                }

                client.TrackDependency(dependency);
            }
            catch (Exception ex)
            {
                _ = TinyInsights.TrackErrorAsync(ex);
            }
        }
    }