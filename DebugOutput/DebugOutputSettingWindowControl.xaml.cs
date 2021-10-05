using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DebugOutput
{
    public partial class DebugOutputSettingWindowControl : UserControl
    {
        DebugOutputPackage MyPackage => DebugOutputPackage.Instance;
        DebugOutputSettingViewDataContext MyDataContext => DataContext as DebugOutputSettingViewDataContext;

        public DebugOutputSettingWindowControl()
        {
            this.InitializeComponent();

            ApplyToolSettings(MyPackage.SettingsLog);
        }

        bool ApplyToolSettings(LogSettings settings)
        {
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
            var settings = RetieveLogSettings();
            SaveUserSettings(settings);
        }

        void SaveUserSettings(LogSettings settings)
        {
            MyPackage.SaveUserSettings(settings);
        }

        LogSettings RetieveLogSettings()
        {
            var orderList = new List<RegexCaptureOrderItem>();
            foreach (string item in listBox.Items)
            {
                orderList.Add(new RegexCaptureOrderItem
                {
                    Name = item,
                    Order = listBox.Items.IndexOf(item)
                });
            }

            return new LogSettings
            {
                CaptureRegex = textBox.Text,
                TypeOrders = orderList,
                CustomLevels = dataGrid.Items.OfType<LogLevelItem>().ToList(),
            };
        }

        private void GuiEvent_ClickApply(object sender, RoutedEventArgs e)
        {
            var settings = RetieveLogSettings();
            SaveUserSettings(settings);
            var window = MyPackage.FindToolWindow(typeof(DebugOutputWindow), 0, true);
            if ((null != window) && (null != window.Frame))
            {
                var ctrl = window.Content as DebugOutputWindowControl;
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
                var ctrl = window.Content as DebugOutputWindowControl;
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
}