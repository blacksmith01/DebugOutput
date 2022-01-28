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
    [ProvideToolWindow(typeof(LogSettingWindow))]
    public sealed partial class DebugOutputPackage : AsyncPackage
    {
        public const string PackageGuidString = "eaa8b0e1-f5f0-4fe3-8cb9-792622c32b0b";
        public static Guid PackageGuid = new Guid(PackageGuidString);

        static DebugOutputPackage _instance;
        public static DebugOutputPackage Instance { get => _instance; private set => _instance = value; }
        
        readonly Dictionary<Type, object> _commands = new Dictionary<Type, object>();

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Instance = this;

            InitializeDTE();
            InitializeSettings();

            await AddToolbarCommandAsync<OutputWindowCommand>();
            await AddToolbarCommandAsync<SettingWindowCommand>();
            await AddToolbarCommandAsync<OpenSettingsCommand>();
            await AddToolbarCommandAsync<ClearListCommand>();
            await AddToolbarCommandAsync<CollapseHeaderCommand>();
            await AddToolbarCommandAsync<GoToBottomCommand>();
        }

        async Task AddToolbarCommandAsync<T>() where T: CommandBase, new()
        {
            var command = new T();

            await command.InitializeAsync(this);

            _commands[typeof(T)] = command;
        }

        public Task<ToolWindowPane> ShowToolWindowAsync<T>()
        {
            return ShowToolWindowAsync(typeof(T), 0, true, DisposalToken);
        }
    }
}
