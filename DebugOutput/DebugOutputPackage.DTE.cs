using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugOutput
{
    public sealed partial class DebugOutputPackage
    {
        public DTE dte;
        public Events dteEvents;
        public OutputWindowEvents outputWindowEvents;
        public SolutionEvents solutionEvents;
        public DebuggerEvents debuggerEvents;
        public bool IsDebugging { get; private set; }

        void InitializeDTE()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            dte = (DTE)Package.GetGlobalService(typeof(SDTE));

            dteEvents = dte.Events;
            outputWindowEvents = dteEvents.OutputWindowEvents;
            solutionEvents = dteEvents.SolutionEvents;
            debuggerEvents = dteEvents.DebuggerEvents;

            debuggerEvents.OnEnterRunMode += DebuggerEvents_OnEnterRunMode;
            debuggerEvents.OnEnterDesignMode += DebuggerEvents_OnEnterDesignMode;
        }

        private void DebuggerEvents_OnEnterRunMode(dbgEventReason Reason)
        {
            IsDebugging = true;
        }

        private void DebuggerEvents_OnEnterDesignMode(dbgEventReason Reason)
        {
            IsDebugging = false;
        }
    }
}
