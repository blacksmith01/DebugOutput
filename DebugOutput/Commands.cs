using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Task = System.Threading.Tasks.Task;

namespace DebugOutput
{
    public abstract class CommandBase
    {
        private readonly Guid _commandSet;
        private readonly int _commandId;

        protected CommandBase(int commandId) : this(new Guid("a6d24558-6c2f-4d54-afcb-3870618a6daf"), commandId)
        {
        }

        protected CommandBase(Guid commandSet, int commandId)
        {
            _commandSet = commandSet;
            _commandId = commandId;
        }

        protected AsyncPackage Package { get; private set; }
        protected DebugOutputPackage MyPackage => Package as DebugOutputPackage;
        protected MenuCommand MenuCommand { get; private set; }

        public virtual async Task InitializeAsync(AsyncPackage package)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if(commandService == null)
                throw new Exception("!InitializeAsync");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var menuCommandId = new CommandID(_commandSet, _commandId);
            MenuCommand = new MenuCommand(Execute, menuCommandId);

            commandService.AddCommand(MenuCommand);
        }

        protected virtual Task ExecuteAsync(object sender, EventArgs e)
        {
            return Task.CompletedTask;
        }

        protected virtual void Execute(object sender, EventArgs e)
        {
            Package.JoinableTaskFactory.RunAsync(async delegate
            {
                await ExecuteAsync(sender, e);
            });
        }
    }


    public class OutputWindowCommand : CommandBase
    {
        public OutputWindowCommand() :
            base(0x0010)
        {
        }

        protected override async Task ExecuteAsync(object sender, EventArgs e)
        {
            ToolWindowPane window = await MyPackage.ShowToolWindowAsync(typeof(DebugOutputWindow), 0, true, MyPackage.DisposalToken);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
        }
    }


    public class SettingWindowCommand : CommandBase
    {
        public SettingWindowCommand() :
            base(0x0020)
        {
        }

        protected override async Task ExecuteAsync(object sender, EventArgs e)
        {
            ToolWindowPane window = await MyPackage.ShowToolWindowAsync(typeof(LogSettingWindow), 0, true, MyPackage.DisposalToken);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
        }
    }

    public class OpenSettingsCommand : CommandBase
    {
        public OpenSettingsCommand() :
            base(0x0120)
        {
        }

        protected async override Task ExecuteAsync(object sender, EventArgs e)
        {
            await MyPackage.ShowToolWindowAsync<LogSettingWindow>();
        }
    }

    public class ClearListCommand : CommandBase
    {
        public ClearListCommand() :
            base(0x0130)
        {
        }

        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                MyPackage.dte.ExecuteCommand("Edit.ClearOutputWindow");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "{0}", ex.ToString()), "Error");
            }
        }
    }

    public class CollapseHeaderCommand : CommandBase
    {
        public CollapseHeaderCommand() :
            base(0x0140)
        {
        }

        protected override void Execute(object sender, EventArgs e)
        {
            var window = MyPackage.FindToolWindow(typeof(DebugOutputWindow), 0, true);
            if ((null != window) && (null != window.Frame))
            {
                var ctrl = window.Content as DebugOutputControl;
                ctrl.ToggleHeaderVisibility();
            }
        }
    }
    public class GoToBottomCommand : CommandBase
    {
        public GoToBottomCommand() :
            base(0x0150)
        {
        }

        protected override void Execute(object sender, EventArgs e)
        {
            var window = MyPackage.FindToolWindow(typeof(DebugOutputWindow), 0, true);
            if ((null != window) && (null != window.Frame))
            {
                var ctrl = window.Content as DebugOutputControl;
                ctrl.GoToBottom();
            }
        }
    }
}
