using Microsoft.VisualStudio.PlatformUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DebugOutput
{
    public class OutputViewItem
    {
        public string FullText { get; set; }
        public string Time { get; set; }
        public string Level { get; set; }
        public string Text { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
    }
    public class OutputViewList : ObservableCollection<OutputViewItem>
    {

    }

    public class OutputViewColumnInfo : ICloneable
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public double Width { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    public class OutputViewSettings : ICloneable
    {
        public List<OutputViewColumnInfo> Columns { get; set; }

        public static readonly OutputViewSettings Default = new OutputViewSettings
        {
            Columns = new List<OutputViewColumnInfo>
            {
                new OutputViewColumnInfo { Name = "DateTime", Order = 0, Width = 100 },
                new OutputViewColumnInfo { Name = "Level", Order = 1, Width = 50 },
                new OutputViewColumnInfo { Name = "Text", Order = 2, Width = 300 },
                new OutputViewColumnInfo { Name = "File", Order = 3, Width = 100 },
                new OutputViewColumnInfo { Name = "Line", Order = 4, Width = 50 },
            }
        };
        public static readonly string DefaultSerialized = JsonConvert.SerializeObject(Default);

        public object Clone()
        {
            var obj = MemberwiseClone() as OutputViewSettings;
            obj.Columns = Columns.Select(x => x.Clone() as OutputViewColumnInfo).ToList();
            return obj;
        }
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
