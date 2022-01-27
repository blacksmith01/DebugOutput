using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugOutput
{
    public sealed partial class DebugOutputPackage
    {
        public IVsWritableSettingsStore SettingsStore { get; private set; }
        public OutputViewSettings SettingsView { get; private set; } = OutputViewSettings.Default.Clone() as OutputViewSettings;
        public LogSettings SettingsLog { get; private set; } = LogSettings.Default.Clone() as LogSettings;

        void InitializeSettings()
        {
            SettingsStore = WindowsExtensions.GetWritableSettingStore();

            SettingsView = LoadUserSettings(OutputViewSettings.Default);
            SettingsView.Validate();
            SettingsLog = LoadUserSettings(LogSettings.Default);
            SettingsLog.Validate();
        }

        public void SaveUserSettings<T>(string json)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var result = SettingsStore.CollectionExists("UserSettings", out var exists);
            if (exists == 0)
            {
                SettingsStore.CreateCollection("UserSettings");
            }
            SettingsStore.SetString("UserSettings", typeof(T).FullName, json);
        }

        public string LoadUserSettingsToJson<T>()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var result = SettingsStore.GetStringOrDefault("UserSettings", typeof(T).FullName, string.Empty, out string json);
            return json;
        }
        public T LoadUserSettings<T>(T defaultT) where T : class, ICloneable
        {
            try
            {
                var json = LoadUserSettingsToJson<T>();
                if (string.IsNullOrEmpty(json))
                {
                    return defaultT.Clone() as T;
                }
                else
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
            }
            catch (Exception)
            {
                return defaultT.Clone() as T;
            }
        }

        public void SaveUserSettings(LogSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            SettingsLog = settings.Clone() as LogSettings;
            SaveUserSettings<LogSettings>(json);
        }

        public void SaveUserSettings(OutputViewSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            SettingsView = settings.Clone() as OutputViewSettings;
            SaveUserSettings<OutputViewSettings>(json);
        }
    }
}
