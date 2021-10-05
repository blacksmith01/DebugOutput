using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DebugOutput
{
    /// <summary>
    /// Interaction logic for DebugOutputWindowControl.
    /// </summary>
    /// 
    public partial class DebugOutputWindowControl : UserControl
    {
        DebugOutputPackage MyPackage => DebugOutputPackage.Instance;
        DebugOutputViewDataContext MyDataContext => DataContext as DebugOutputViewDataContext;
        IVsWritableSettingsStore SettingsStore => MyPackage.SettingsStore;

        bool loadedState = false;
        int loaedCount = 0;
        string captureRegex;
        int orderTime;
        int orderLevel;
        int orderText;
        int orderFile;
        int orderLine;

        Dictionary<string, SolidColorBrush> levelColors = new Dictionary<string, SolidColorBrush>();

        public DebugOutputWindowControl()
        {
            this.InitializeComponent();

            ApplyViewSettings(MyPackage.SettingsView);
            ApplyLogSettings(MyPackage.SettingsLog);
        }

        bool ApplyViewSettings(OutputViewSettings settings)
        {
            var gridView = logListView.View as GridView;
            foreach (var col in settings.Columns)
            {
                var newIndex = settings.Columns.IndexOf(col);
                var viewCol = gridView.Columns.Where(x => x.Header.ToString() == col.Name).FirstOrDefault();
                var curIndex = gridView.Columns.IndexOf(viewCol);
                if (curIndex != newIndex)
                {
                    gridView.Columns.Move(curIndex, newIndex);
                }
                viewCol.Width = col.Width;
            }

            return true;
        }

        public void ApplyLogSettings(LogSettings logSettings)
        {
            captureRegex = logSettings.CaptureRegex;

            orderTime = logSettings.OrderTime;
            orderLevel = logSettings.OrderLevel;
            orderText = logSettings.OrderText;
            orderFile = logSettings.OrderFile;
            orderLine = logSettings.OrderLine;

            levelColors = logSettings.CustomLevels.ToDictionary(x => x.Match, y => new SolidColorBrush((Color)ColorConverter.ConvertFromString(y.ActiveColor)));

            for (int i = 0; i < MyDataContext.Items.Count; i++)
            {
                var item = MyDataContext.Items[i];

                var result = Regex.Match(item.FullText, captureRegex);
                if (result.Success && result.Groups.Count == 6)
                {
                    item.Time = result.Groups[1 + orderTime].Value;
                    item.Level = result.Groups[1 + orderLevel].Value;
                    item.Text = result.Groups[1 + orderText].Value;
                    item.File = result.Groups[1 + orderFile].Value;
                    item.Line = int.Parse(result.Groups[1 + orderLine].Value);

                    var listViewItem = logListView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                    if (listViewItem != null)
                    {
                        UpdateTextColor_ByLogLevelType(listViewItem, item);
                    }
                }
            }
        }

        void UpdateTextColor_ByLogLevelType(ListViewItem listviewItem, OutputViewItem viewItem)
        {
            foreach (var p in levelColors)
            {
                if (Regex.Match(viewItem.Level, p.Key).Success)
                {
                    listviewItem.Foreground = p.Value;
                    return;
                }
            }
        }

        void SaveUserSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var columns = (logListView.View as GridView).Columns;
            var settings = new OutputViewSettings
            {
                Columns = columns.Select(x => new OutputViewColumnInfo
                {
                    Name = x.Header.ToString(),
                    Width = x.Width,
                    Order = columns.IndexOf(x)
                }).ToList()
            };

            var json = JsonConvert.SerializeObject(settings);

            SettingsStore.CollectionExists("UserSettings", out var exists);
            if (exists == 0)
            {
                SettingsStore.CreateCollection("UserSettings");
            }
            var result2 = SettingsStore.SetString("UserSettings", nameof(OutputViewSettings), json);
        }

        void ClearItems()
        {
            MyDataContext.Items.Clear();
            MyDataContext.LastEndLine = 0;
        }

        void UpdateItems(OutputWindowPane pane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Debug.WriteLine($"[UpdateItems]");

            try
            {
                TextDocument document = pane.TextDocument;

                var lineStart = document.StartPoint.Line;
                var lineEnd = document.EndPoint.Line;

                string addText = string.Empty;
                if (MyDataContext.LastEndLine >= lineEnd)
                {
                    Debug.WriteLine($"[UpdateItems] {MyDataContext.LastEndLine}>={lineEnd}");
                    return;
                }
                else if (MyDataContext.LastEndLine == 0)
                {
                    EditPoint point = document.StartPoint.CreateEditPoint();
                    addText = point.GetText(document.EndPoint);
                }
                else
                {
                    EditPoint point = document.StartPoint.CreateEditPoint();
                    addText = point.GetLines(MyDataContext.LastEndLine, lineEnd);
                }
                MyDataContext.LastEndLine = lineEnd;

                Debug.WriteLine($"[UpdateItems] {addText}");


                var logSettings = MyPackage.SettingsLog;

                var lines = addText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Where(a => !string.IsNullOrEmpty(a));
                //AddToOutput(_outputWindowContent[CurrentWindow].Where(z => z.MatchesFilter).Select(z => z.Text), true);
                foreach (var l in lines)
                {
                    var result = Regex.Match(l, captureRegex);
                    if (result.Success && result.Groups.Count == 6)
                    {
                        var newItem = new OutputViewItem
                        {
                            FullText = l,
                            Time = result.Groups[1 + orderTime].Value,
                            Level = result.Groups[1 + orderLevel].Value,
                            Text = result.Groups[1 + orderText].Value,
                            File = result.Groups[1 + orderFile].Value,
                            Line = int.Parse(result.Groups[1 + orderLine].Value),
                        };
                        MyDataContext.Items.Add(newItem);

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "{0}", ex.ToString()), "Error");
            }
        }

        public void ToggleHeaderVisibility()
        {
            MyDataContext.ToggleVisibility();
        }

        void DteEvent_OutputClearing(OutputWindowPane pane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pane.Guid != VSConstants.OutputWindowPaneGuid.DebugPane_string)
            {
                return;
            }

            ClearItems();
        }

        void DteEvent_OutputUpdated(OutputWindowPane pane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pane.Guid != VSConstants.OutputWindowPaneGuid.DebugPane_string)
            {
                return;
            }

            if (loadedState)
            {
                UpdateItems(pane);
            }
        }

        private void DteEvent_SolutionBeforeClosing()
        {
            SaveUserSettings();
        }

        void GuiEvent_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var item = ((ListViewItem)sender).Content as OutputViewItem;
                //dte.ItemOperations.OpenFile(item.File);
                MyPackage.dte.ExecuteCommand("File.OpenFile", item.File);
                ((TextSelection)MyPackage.dte.ActiveDocument.Selection).GotoLine(item.Line, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "{0}", ex.ToString()), "Error");
            }

        }

        private void GuiEvent_Loaded(object sender, RoutedEventArgs e)
        {
            MyPackage.outputWindowEvents.PaneUpdated += DteEvent_OutputUpdated;
            MyPackage.outputWindowEvents.PaneClearing += DteEvent_OutputClearing;
            MyPackage.solutionEvents.BeforeClosing += DteEvent_SolutionBeforeClosing;
            loadedState = true;
            loaedCount++;

            if (MyPackage.IsDebugging)
            {
                Debug.WriteLine($"[GuiEvent_Loaded] clear by debugging");

                ClearItems();
                UpdateItems(MyPackage.DebugOutputPane);
            }
            else
            {
                if (loaedCount > 1)
                {
                    UpdateItems(MyPackage.DebugOutputPane);
                }
                Debug.WriteLine($"[GuiEvent_Loaded] not debugging");
            }
        }

        private void GuiEvent_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[GuiEvent_Unloaded]");

            MyPackage.outputWindowEvents.PaneUpdated -= DteEvent_OutputUpdated;
            MyPackage.outputWindowEvents.PaneClearing -= DteEvent_OutputClearing;
            MyPackage.solutionEvents.BeforeClosing -= DteEvent_SolutionBeforeClosing;
            loadedState = false;
        }

        private void GuiEvent_ListViewItemLoaded(object sender, RoutedEventArgs e)
        {
            var viewItem = sender as ListViewItem;
            int index = logListView.ItemContainerGenerator.IndexFromContainer(viewItem);

            if (index < 0 || index >= MyDataContext.Items.Count)
                return;

            UpdateTextColor_ByLogLevelType(viewItem, MyDataContext.Items[index]);
        }
    }
}