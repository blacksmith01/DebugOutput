using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

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
    [Guid(DebugOutputSettingWindow.GUID)]
    public class DebugOutputSettingWindow : ToolWindowPane
    {
        public const string GUID = "8d3d588b-926d-4e8d-bfd2-98ed751a1b3e";
        /// <summary>
        /// Initializes a new instance of the <see cref="DebugOutputSettingWindow"/> class.
        /// </summary>
        public DebugOutputSettingWindow() : base(null)
        {
            this.Caption = "DebugOutputSettingWindow";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new DebugOutputSettingWindowControl();
        }
    }
}
