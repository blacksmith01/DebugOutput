using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugOutput
{
    public class StaticObject<T> where T : new()
    {
        public static T Instance = new T();
    }

    public static class WindowsExtensions
    {
        public static IVsWritableSettingsStore GetWritableSettingStore()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsManager = Package.GetGlobalService(typeof(SVsSettingsManager)) as IVsSettingsManager;
            settingsManager.GetWritableSettingsStore((uint)__VsSettingsScope.SettingsScope_UserSettings, out var settingsStore);
            return settingsStore;
        }

        public static T GetPackage<T>() where T : Package
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var vsShell = (IVsShell)ServiceProvider.GlobalProvider.GetService(typeof(IVsShell));
            if (vsShell == null)
            {
                return default;
            }

            if (vsShell.IsPackageLoaded(new Guid(DebugOutputPackage.PackageGuidString), out var pkg) != Microsoft.VisualStudio.VSConstants.S_OK)
            {
                return default;
            }

            return pkg as T;
        }

        public static ToolWindowPane FindToolWindow<TPackage, TWindow>() where TPackage : Package
        {
            var package = WindowsExtensions.GetPackage<TPackage>();
            var window = package.FindToolWindow(typeof(TWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                return null;
            }
            return window;
        }

        public static void ShowToolWindow<TPackage, TWindow>() where TPackage : Package
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var package = WindowsExtensions.GetPackage<TPackage>();
            var window = package.FindToolWindow(typeof(TWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
