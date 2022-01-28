using Newtonsoft.Json;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.PlatformUI;
using System.Diagnostics;

namespace DebugOutput
{
    public partial class LogSettingControl : UserControl
    {
        DebugOutputPackage MyPackage => DebugOutputPackage.Instance;
        DebugOutputSettingViewDataContext MyDataContext => DataContext as DebugOutputSettingViewDataContext;

        public LogSettingControl()
        {
            this.InitializeComponent();

            ApplyToolSettings(MyPackage.SettingsLog);
        }

        bool ApplyToolSettings(LogSettings settings)
        {
            MyDataContext.FontFamily = settings.FontFamily;
            MyDataContext.FontSize = settings.FontSize;

            textBox.Text = settings.CaptureRegex;

            listBox.Items.Clear();

            var dic = new SortedDictionary<int, string>();
            foreach (var o in settings.TypeOrders)
            {
                dic.Add(o.Order, o.Name);
            }
            foreach (var p in dic)
            {
                listBox.Items.Add(p.Value);
            }

            MyDataContext.LevelItems.Assign(settings.CustomLevels);

            return true;
        }

        void SaveUserSettings()
        {
            var settings = RetrieveLogSettings();
            SaveUserSettings(settings);
        }

        void SaveUserSettings(LogSettings settings)
        {
            MyPackage.SaveUserSettings(settings);
        }

        LogSettings RetrieveLogSettings()
        {
            var orderList = new List<RegexCaptureOrderItem>();
            foreach (string item in listBox.Items)
            {
                orderList.Add(new RegexCaptureOrderItem
                {
                    Name = item,
                    Order = listBox.Items.IndexOf(item),
                });
            }

            return new LogSettings
            {
                CaptureRegex = textBox.Text,
                TypeOrders = orderList,
                CustomLevels = dataGrid.Items.OfType<LogLevelItem>().ToList(),
                FontFamily = textFontFamily.Text,
                FontSize = (int)sldFontSize.Value,
            };
        }

        private void GuiEvent_ClickApply(object sender, RoutedEventArgs e)
        {
            var settings = RetrieveLogSettings();
            SaveUserSettings(settings);
            var window = MyPackage.FindToolWindow(typeof(DebugOutputWindow), 0, true);
            if ((null != window) && (null != window.Frame))
            {
                var ctrl = window.Content as DebugOutputControl;
                ctrl.ApplyLogSettings(settings);
            }
        }
        private void GuiEvent_ClickCancel(object sender, RoutedEventArgs e)
        {
            ApplyToolSettings(MyPackage.SettingsLog);
        }
        private void GuiEvent_ClickDefault(object sender, RoutedEventArgs e)
        {
            ApplyToolSettings(LogSettings.Default);
            SaveUserSettings(LogSettings.Default);
            var window = MyPackage.FindToolWindow(typeof(DebugOutputWindow), 0, true);
            if ((null != window) && (null != window.Frame))
            {
                var ctrl = window.Content as DebugOutputControl;
                ctrl.ApplyLogSettings(LogSettings.Default);
            }
        }

        private void GuiEvent_ClickUp(object sender, RoutedEventArgs e)
        {
            var selectedIndex = listBox.SelectedIndex;
            if (listBox.SelectedItem == null || selectedIndex <= 0)
                return;

            var itemToMoveUp = listBox.Items[selectedIndex];
            listBox.Items.RemoveAt(selectedIndex);
            listBox.Items.Insert(selectedIndex - 1, itemToMoveUp);
            listBox.SelectedIndex = selectedIndex - 1;
        }

        private void GuiEvent_ClickDown(object sender, RoutedEventArgs e)
        {
            var selectedIndex = listBox.SelectedIndex;
            if (listBox.SelectedItem == null || selectedIndex < 0 || selectedIndex + 1 >= listBox.Items.Count)
                return;

            var itemToMoveDown = listBox.Items[selectedIndex];
            listBox.Items.RemoveAt(selectedIndex);
            listBox.Items.Insert(selectedIndex + 1, itemToMoveDown);
            listBox.SelectedIndex = selectedIndex + 1;
        }
    }

    public class LogLevelList : ObservableCollection<LogLevelItem>
    {
        public void Assign(List<LogLevelItem> newList)
        {
            if (Count != newList.Count)
            {
                Clear();

                foreach (var e in newList)
                {
                    Add(e.Clone() as LogLevelItem);
                }
            }
            else
            {
                for (int i = 0; i < Count; i++)
                {
                    var src = newList[i];
                    var dst = this[i];
                    dst.Name = src.Name;
                    dst.Match = src.Match;
                    dst.ColorType = src.ColorType;
                    dst.ColorValue = src.ColorValue;
                }
            }
        }

    }

    public class DebugOutputSettingViewDataContext : ObservableObject
    {
        public DebugOutputSettingViewDataContext()
        {

        }

        public LogLevelList LevelItems { get; set; } = new LogLevelList();
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
    }
}