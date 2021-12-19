using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DebugOutput
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid(LogSettingWindow.GUID)]
    public class LogSettingWindow : ToolWindowPane
    {
        public const string GUID = "8d3d588b-926d-4e8d-bfd2-98ed751a1b3e";
        /// <summary>
        /// Initializes a new instance of the <see cref="LogSettingWindow"/> class.
        /// </summary>
        public LogSettingWindow() : base(null)
        {
            this.Caption = "LogSettings";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new LogSettingControl();
        }
    }

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("57dca8bd-52cb-4742-b527-bb701387112e")]
    public class DebugOutputWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DebugOutputWindow"/> class.
        /// </summary>
        public DebugOutputWindow() : base(null)
        {
            this.Caption = "DebugOutput";
            this.ToolBar = new CommandID(new Guid("a6d24558-6c2f-4d54-afcb-3870618a6daf"), 0x0100);

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new DebugOutputControl();
        }

        public DebugOutputControl Control => (Content as DebugOutputControl);
        public override bool SearchEnabled => true;
        public override IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
        {
            if (pSearchQuery == null || pSearchCallback == null)
                return null;
            return new OutputFilterTask(dwCookie, pSearchQuery, pSearchCallback, this);
        }

        public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
        {
            Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchWatermarkProperty.Name, "Level:, Text:, Thread:, File:");
            Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchStartTypeProperty.Name, (uint)VSSEARCHSTARTTYPE.SST_ONDEMAND);
        }

        public override void ClearSearch()
        {
            Control.ClearFilter();
        }
    }

    public class OutputFilterTask : VsSearchTask
    {
        readonly string[] _filterNames = new string[] { "Level", "Text", "Thread", "File" };
        DebugOutputWindow _window;
        public OutputFilterTask(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback, DebugOutputWindow window) : base(dwCookie, pSearchQuery, pSearchCallback)
        {
            _window = window;
        }

        int FindFilterIndex(string text)
        {
            var len = _filterNames.Length;
            for (int i = 0; i < len; i++)
            {
                if (text.Equals(_filterNames[i], StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        protected override async void OnStartSearch()
        {
            var searchString = SearchQuery.SearchString;
            if (string.IsNullOrEmpty(searchString))
            {
                _window.Control.Dispatcher.Invoke(() =>
                {
                    _window.Control.ClearFilter();
                });
                return;
            }

            var inputFilters = new Dictionary<string, string>();
            var splited = searchString.Split(',');
            foreach (var str in splited)
            {
                var kv = str.Split(':');
                if (kv.Count() != 2)
                    continue;
                var idx = FindFilterIndex(kv[0].Trim());
                if (idx < 0)
                    continue;
                inputFilters[_filterNames[idx]] = kv[1];
            }

            _window.Control.Dispatcher.Invoke(() =>
            {
                if(!inputFilters.Any())
                {
                    inputFilters.Add("Text", searchString);
                }
                _window.Control.SetFilter(inputFilters);
            });

            base.OnStartSearch();
        }

        protected override void OnStopSearch()
        {
            base.OnStopSearch();
        }
    }
}
