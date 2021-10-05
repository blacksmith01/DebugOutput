using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
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
    [Guid("57dca8bd-52cb-4742-b527-bb701387112e")]
    public class DebugOutputWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DebugOutputWindow"/> class.
        /// </summary>
        public DebugOutputWindow() : base(null)
        {
            this.Caption = "DebugOutputWindow";
            this.ToolBar = new CommandID(new Guid("a6d24558-6c2f-4d54-afcb-3870618a6daf"), 0x0100);

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new DebugOutputWindowControl();
        }
    }
}
