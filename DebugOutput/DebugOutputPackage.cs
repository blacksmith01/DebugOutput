using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace DebugOutput
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(DebugOutputPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(DebugOutputWindow))]
    [ProvideToolWindow(typeof(DebugOutputSettingWindow))]
    public sealed class DebugOutputPackage : AsyncPackage
    {
        public const string PackageGuidString = "eaa8b0e1-f5f0-4fe3-8cb9-792622c32b0b";
        public static Guid PackageGuid = new Guid(PackageGuidString);

        public DTE dte;
        public Events dteEvents;
        public OutputWindowEvents outputWindowEvents;
        public SolutionEvents solutionEvents;
        public DebuggerEvents debuggerEvents;
        public OutputViewSettings SettingsView { get; private set; } = OutputViewSettings.Default.Clone() as OutputViewSettings;
        public LogSettings SettingsLog { get; private set; } = LogSettings.Default.Clone() as LogSettings;
        public bool IsDebugging { get; private set; }

        private readonly Dictionary<Type, object> _commands = new Dictionary<Type, object>();

        static DebugOutputPackage _instance;
        public static DebugOutputPackage Instance { get => _instance; private set => _instance = value; }
        public IVsWritableSettingsStore SettingsStore { get; private set; }
        public OutputWindowPane DebugOutputPane => (dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput).Object as EnvDTE.OutputWindow).OutputWindowPanes.Item("Debug");

         protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Instance = this;
            SettingsStore = WindowsExtensions.GetWritableSettingStore();

            dte = (DTE)Package.GetGlobalService(typeof(SDTE));

            dteEvents = dte.Events;
            outputWindowEvents = dteEvents.OutputWindowEvents;
            solutionEvents = dteEvents.SolutionEvents;
            debuggerEvents = dteEvents.DebuggerEvents;

            debuggerEvents.OnEnterRunMode += DebuggerEvents_OnEnterRunMode;
            debuggerEvents.OnEnterDesignMode += DebuggerEvents_OnEnterDesignMode;

            SettingsView = LoadUserSettings(OutputViewSettings.Default);
            SettingsLog = LoadUserSettings(LogSettings.Default);

            await AddToolbarCommandAsync<OutputWindowCommand>();
            await AddToolbarCommandAsync<SettingWindowCommand>();
            await AddToolbarCommandAsync<OpenSettingsCommand>();
            await AddToolbarCommandAsync<ClearListCommand>();
            await AddToolbarCommandAsync<CollapseHeaderCommand>();
        }

        async Task AddToolbarCommandAsync<T>() where T: CommandBase, new()
        {
            var command = new T();

            await command.InitializeAsync(this);

            _commands[typeof(T)] = command;
        }

        private void DebuggerEvents_OnEnterRunMode(dbgEventReason Reason)
        {
            IsDebugging = true;
        }

        private void DebuggerEvents_OnEnterDesignMode(dbgEventReason Reason)
        {
            IsDebugging = false;
        }

        public Task<ToolWindowPane> ShowToolWindowAsync<T>()
        {
            return ShowToolWindowAsync(typeof(T), 0, true, DisposalToken);
        }

        public string LoadUserSettingsToJson(string key)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SettingsStore.GetStringOrDefault("UserSettings", key, string.Empty, out string json);
            return json;
        }
        public T LoadUserSettings<T>(T defaultT) where T : class, ICloneable
        {
            try
            {
                var json = LoadUserSettingsToJson(nameof(T));
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
            SaveUserSettings(nameof(LogSettings), json);
        }

        public void SaveUserSettings(OutputViewSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            SettingsView = settings.Clone() as OutputViewSettings;
            SaveUserSettings(nameof(OutputViewSettings), json);
        }

        public void SaveUserSettings(string key, string json)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SettingsStore.CollectionExists("UserSettings", out var exists);
            if (exists == 0)
            {
                SettingsStore.CreateCollection("UserSettings");
            }
            SettingsStore.SetString("UserSettings", key, json);
        }
    }
}
