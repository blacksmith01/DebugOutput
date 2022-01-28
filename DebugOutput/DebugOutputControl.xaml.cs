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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DebugOutput
{
    /// <summary>
    /// Interaction logic for DebugOutputWindowControl.
    /// </summary>
    /// 
    public partial class DebugOutputControl : UserControl
    {
        DebugOutputPackage MyPackage => DebugOutputPackage.Instance;
        DebugOutputViewDataContext MyDataContext => DataContext as DebugOutputViewDataContext;

        Dictionary<string, string> _currentFilters = new Dictionary<string, string>();
        List<OutputViewItem> _fullItemList = new List<OutputViewItem>();
        ScrollViewer _sv = null;
        IVsWritableSettingsStore SettingsStore => MyPackage.SettingsStore;

        bool loadedState = false;
        int loaedCount = 0;
        string captureRegex;
        int orderTime;
        int orderLevel;
        int orderThread;
        int orderCategory;
        int orderText;
        int orderFile;
        int orderLine;

        Dictionary<string, SolidColorBrush> levelColors = new Dictionary<string, SolidColorBrush>();

        double _scrollVerticalOffset;
        double _scrollExtentHeight;
        double _scrollViewportHeight;


        public DebugOutputControl()
        {
            this.InitializeComponent();

            ApplyViewSettings(MyPackage.SettingsView);
            ApplyLogSettings(MyPackage.SettingsLog);

            ((INotifyCollectionChanged)logListView.Items).CollectionChanged += DebugOutputControl_CollectionChanged;
            logGrid.Loaded += LogGrid_Loaded;
        }

        private void LogGrid_Loaded(object sender, RoutedEventArgs e)
        {
            _sv = GetScrollViewer(logGrid) as ScrollViewer;
        }

        DependencyObject GetScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer)
            {
                return o;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;
        }

        private void DebugOutputControl_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_sv != null && IsStickedToBottom())
            {
                GoToBottom();
            }
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

            if (MyDataContext.HeaderVisibility != settings.HeaderVisibility)
            {
                MyDataContext.ToggleVisibility();
            }

            return true;
        }

        public void ApplyLogSettings(LogSettings logSettings)
        {
            MyDataContext.FontFamily = logSettings.FontFamily;
            MyDataContext.FontSize = logSettings.FontSize;

            captureRegex = logSettings.CaptureRegex;

            orderTime = logSettings.OrderTime;
            orderLevel = logSettings.OrderLevel;
            orderThread = logSettings.OrderThread;
            orderCategory = logSettings.OrderCategory;
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
                    item.Thread = result.Groups[1 + orderThread].Value;
                    item.Category = result.Groups[1 + orderCategory].Value;
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
                HeaderVisibility = MyDataContext.HeaderVisibility,
                Columns = columns.Select(x => new OutputViewColumnInfo
                {
                    Name = x.Header.ToString(),
                    Width = x.Width,
                    Order = columns.IndexOf(x)
                }).ToList()
            };

            MyPackage.SaveUserSettings(settings);
        }

        void ClearItems()
        {
            MyDataContext.Items.Clear();
            MyDataContext.LastEndLine = 0;
        }

        void UpdateItems()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            UpdateItems((MyPackage.dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput).Object as EnvDTE.OutputWindow).OutputWindowPanes.Item("Debug"));
        }

        void UpdateItems(OutputWindowPane pane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (pane == null)
                {
                    return;
                }

                var document = pane.TextDocument;
                var lineStart = document.StartPoint.Line;
                var lineEnd = document.EndPoint.Line;

                string addText = string.Empty;
                if (MyDataContext.LastEndLine >= lineEnd)
                {
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

                var logSettings = MyPackage.SettingsLog;

                var lines = addText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Where(a => !string.IsNullOrEmpty(a));
                //AddToOutput(_outputWindowContent[CurrentWindow].Where(z => z.MatchesFilter).Select(z => z.Text), true);
                foreach (var l in lines)
                {
                    var result = Regex.Match(l, captureRegex);
                    if (result.Success && result.Groups.Count == LogSettings.Default.TypeOrders.Count + 1)
                    {
                        var newItem = new OutputViewItem
                        {
                            FullText = l,
                            Time = result.Groups[1 + orderTime].Value,
                            Level = result.Groups[1 + orderLevel].Value,
                            Thread = result.Groups[1 + orderThread].Value,
                            Category = result.Groups[1 + orderCategory].Value,
                            Text = result.Groups[1 + orderText].Value,
                            File = result.Groups[1 + orderFile].Value,
                            Line = int.Parse(result.Groups[1 + orderLine].Value),
                        };

                        if (_currentFilters.Any())
                        {
                            _fullItemList.Add(newItem);
                            if (CheckItemFilter(newItem))
                            {
                                MyDataContext.Items.Add(newItem);
                            }
                        }
                        else
                        {
                            MyDataContext.Items.Add(newItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(
                //string.Format(System.Globalization.CultureInfo.CurrentUICulture, "{0}", ex.ToString()), "Error");
            }
        }

        public void ToggleHeaderVisibility()
        {
            MyDataContext.ToggleVisibility();
        }
        public void GoToBottom()
        {
            if (_sv != null)
            {
                int maxCount = logListView.Items.Count;
                if (maxCount > 0)
                {
                    _sv?.ScrollToBottom();
                }
            }
        }

        public void SetFilter(Dictionary<string, string> newFilters)
        {
            if (!newFilters.Any())
            {
                return;
            }

            _currentFilters = new Dictionary<string, string>(newFilters);
            if (!_fullItemList.Any())
            {
                _fullItemList.Clear();
                foreach (var item in MyDataContext.Items)
                {
                    _fullItemList.Add(item);
                }
            }
            MyDataContext.Items.Clear();
            foreach (var item in _fullItemList)
            {
                if (CheckItemFilter(item))
                {
                    MyDataContext.Items.Add(item);
                }
            }
        }
        public void ClearFilter()
        {
            if (_currentFilters.Any())
            {
                _currentFilters.Clear();
                MyDataContext.Items.Clear();
                foreach (var item in _fullItemList)
                {
                    MyDataContext.Items.Add(item);
                }
                _fullItemList.Clear();
            }
        }

        bool CheckItemFilter(OutputViewItem item)
        {
            if (_currentFilters.ContainsKey("Level") && item.Level.IndexOf(_currentFilters["Level"], StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (_currentFilters.ContainsKey("Text") && item.Text.IndexOf(_currentFilters["Text"], StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (_currentFilters.ContainsKey("Thread") && item.Thread.IndexOf(_currentFilters["Thread"], StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (_currentFilters.ContainsKey("Category") && item.Category.IndexOf(_currentFilters["Category"], StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (_currentFilters.ContainsKey("File") && item.File.IndexOf(_currentFilters["File"], StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            return true;
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
                ClearItems();
                UpdateItems();
            }
            else
            {
                if (loaedCount > 1)
                {
                    UpdateItems();
                }
            }
        }

        private void GuiEvent_Unloaded(object sender, RoutedEventArgs e)
        {
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

        bool IsScrolledBottom() => (_scrollVerticalOffset - (_scrollExtentHeight - _scrollViewportHeight) >= -0.0000001);
        bool IsStickedToBottom() => IsScrolledBottom();
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //e.VerticalChange;
            _scrollVerticalOffset = e.VerticalOffset;
            _scrollViewportHeight = e.ViewportHeight;
            _scrollExtentHeight = e.ExtentHeight;
        }
    }

    public class OutputViewItem
    {
        public string FullText { get; set; }
        public string Time { get; set; }
        public string Level { get; set; }
        public string Thread { get; set; }
        public string Category { get; set; }
        public string Text { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
    }
    public class OutputViewList : ObservableCollection<OutputViewItem>
    {

    }

    public class DebugOutputViewDataContext : ObservableObject
    {
        public DebugOutputViewDataContext()
        {
        }

        public OutputViewList Items { get; set; } = new OutputViewList();
        public Visibility HeaderVisibility { get; set; } = Visibility.Visible;

        public string LastTextAll;
        public int LastEndLine { get; set; }
        public string FontFamily
        {
            get
            {
                return _FontFamily;
            }
            set
            {
                _FontFamily = value;
                NotifyPropertyChanged("FontFamily");
            }
        }
        public int FontSize
        {
            get
            {
                return _FontSize;
            }
            set
            {
                _FontSize = value;
                NotifyPropertyChanged("FontSize");
            }
        }

        public string _FontFamily = LogSettings.Default.FontFamily;
        public int _FontSize = LogSettings.Default.FontSize;
        public void ToggleVisibility()
        {
            if (HeaderVisibility == Visibility.Visible)
            {
                HeaderVisibility = Visibility.Collapsed;
            }
            else
            {
                HeaderVisibility = Visibility.Visible;
            }
            NotifyPropertyChanged(nameof(HeaderVisibility));
        }
    }
}