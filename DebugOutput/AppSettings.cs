using Microsoft.VisualStudio.PlatformUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DebugOutput
{
    public class OutputViewColumnInfo : ICloneable
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public double Width { get; set; }

        public object Clone()
        {
            return MemberwiseClone() as OutputViewColumnInfo;
        }
    }
    public class OutputViewSettings : ICloneable
    {
        public Visibility HeaderVisibility { get; set; } = Visibility.Visible;
        public List<OutputViewColumnInfo> Columns { get; set; }

        public static readonly OutputViewSettings Default = new OutputViewSettings
        {
            Columns = new List<OutputViewColumnInfo>
            {
                new OutputViewColumnInfo { Name = "DateTime", Order = 0, Width = 100 },
                new OutputViewColumnInfo { Name = "Level", Order = 1, Width = 50 },
                new OutputViewColumnInfo { Name = "Thread", Order = 2, Width = 50 },
                new OutputViewColumnInfo { Name = "Category", Order = 3, Width = 50 },
                new OutputViewColumnInfo { Name = "Text", Order = 4, Width = 300 },
                new OutputViewColumnInfo { Name = "File", Order = 5, Width = 100 },
                new OutputViewColumnInfo { Name = "Line", Order = 6, Width = 50 },
            }
        };
        public static readonly string DefaultSerialized = JsonConvert.SerializeObject(Default);

        public object Clone()
        {
            var obj = MemberwiseClone() as OutputViewSettings;
            obj.Columns = Columns.Select(x => x.Clone() as OutputViewColumnInfo).ToList();
            return obj;
        }
        public void Validate()
        {
            if (Columns == null)
            {
                Columns = new List<OutputViewColumnInfo>();
            }
            foreach (var colName in Default.Columns.Select(x => x.Name))
            {
                var exists = Columns.Where(x => x.Name == colName);
                if (!exists.Any())
                {
                    var remainSlots = FindRemainOrderSlots();
                    if (remainSlots.Any())
                    {
                        var newCol = Default.Columns.Where(x => x.Name == colName).First().Clone() as OutputViewColumnInfo;
                        newCol.Order = remainSlots.First();
                        Columns.Add(newCol);
                    }
                }
                else if (exists.Count() > 1)
                {
                    var remain = exists.First();
                    Columns.RemoveAll(x => x.Name == colName && x != remain);
                }
            }

            if (FindRemainOrderSlots().Length > 0)
            {
                var allocateSlots = Default.Columns.Select(x => x.Order).ToArray();
                foreach (var col in Columns)
                {
                    if (col.Order < 0 || col.Order >= allocateSlots.Length || allocateSlots[col.Order] < 0)
                    {
                        col.Order = FindRemainOrderSlots().First();
                    }
                }
            }
        }
        public int[] FindRemainOrderSlots()
        {
            var remainSlots = Enumerable.Range(0, Default.Columns.Count()).ToArray();
            foreach (var col in Columns)
            {
                if (col.Order >= 0 && col.Order < remainSlots.Length)
                {
                    remainSlots[col.Order] = -1;
                }
            }
            return remainSlots.Where(x => x >= 0).ToArray();
        }
    }

    public class RegexCaptureOrderItem : ICloneable
    {
        public string Name { get; set; }
        public int Order { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    public enum DebugOutputColorType
    {
        Default,
        Custom,
    }
    public class LogLevelItem : ICloneable, INotifyPropertyChanged
    {
        string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Name"));
                }
            }
        }

        string _Match;
        public string Match
        {
            get
            {
                return _Match;
            }
            set
            {
                if (_Match != value)
                {
                    _Match = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Match"));
                }
            }
        }

        DebugOutputColorType _ColorType;
        public DebugOutputColorType ColorType
        {
            get { return _ColorType; }
            set
            {
                if (_ColorType != value)
                {
                    _ColorType = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ColorType"));
                    OnPropertyChanged(new PropertyChangedEventArgs("ActiveColor"));
                }
            }
        }

        string _ColorValue;
        public string ColorValue
        {
            get
            {
                return _ColorValue;
            }
            set
            {
                if (_ColorValue != value)
                {
                    _ColorValue = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("ColorValue"));
                    OnPropertyChanged(new PropertyChangedEventArgs("ActiveColor"));
                }
            }
        }
        public string ActiveColor
        {
            get
            {
                if (ColorType == DebugOutputColorType.Default)
                {
                    var c = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
                    return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
                }
                else
                {
                    return ColorValue;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class LogSettings : ICloneable
    {
        public string CaptureRegex { get; set; }
        public List<RegexCaptureOrderItem> TypeOrders { get; set; }
        public List<LogLevelItem> CustomLevels { get; set; }
        public string FontFamily { get; set; }
        public int FontSize { get; set; }
        public int OrderTime => GetTypeOrder("DateTime");
        public int OrderLevel => GetTypeOrder("Level");
        public int OrderThread => GetTypeOrder("Thread");
        public int OrderCategory => GetTypeOrder("Category");
        public int OrderText => GetTypeOrder("Text");
        public int OrderFile => GetTypeOrder("File");
        public int OrderLine => GetTypeOrder("Line");
        int GetTypeOrder(string typeName) => TypeOrders.IndexOf(TypeOrders.Where(x => x.Name == typeName).FirstOrDefault());

        public static readonly LogSettings Default = new LogSettings
        {
            CaptureRegex = @"\[(.+?)\] \[(.+?)\] \[(.+?)\] \[(.+?)\] (.+) (.+?)\(([\d]+)\)",
            TypeOrders = new List<RegexCaptureOrderItem>
            {
                new RegexCaptureOrderItem{ Name="DateTime", Order=0},
                new RegexCaptureOrderItem{ Name="Level", Order=1},
                new RegexCaptureOrderItem{ Name="Thread", Order=2},
                new RegexCaptureOrderItem{ Name="Category", Order=3},
                new RegexCaptureOrderItem{ Name="Text", Order=4},
                new RegexCaptureOrderItem{ Name="File", Order=5},
                new RegexCaptureOrderItem{ Name="Line", Order=6},
            },
            CustomLevels = new List<LogLevelItem>
            {
                new LogLevelItem{ Name="Fatal", Match="FTL|FATAL|Ftl|Fatal|ftl|fatal", ColorType=DebugOutputColorType.Custom, ColorValue="#FF0000" },
                new LogLevelItem{ Name="Error", Match="ERR|ERROR|Err|Error|err|error", ColorType=DebugOutputColorType.Custom, ColorValue="#FF4040" },
                new LogLevelItem{ Name="Warn", Match="WARN|WARNING|Warn|Warning|warn|warning", ColorType=DebugOutputColorType.Custom, ColorValue="#FFFF00" },
                new LogLevelItem{ Name="Info", Match="INFO|INFORMATION|Info|Information|info|information", ColorType=DebugOutputColorType.Custom, ColorValue="#00FF00" },
                new LogLevelItem{ Name="Debug", Match="DBG|DEBUG|Dbg|Debug|dbg|debug", ColorType=DebugOutputColorType.Default, ColorValue="#FFFFFF" },
            },
            FontFamily = "Cascadia Mono",
            FontSize = 12,
        };
        public static readonly string DefaultSerialized = JsonConvert.SerializeObject(Default);

        public object Clone()
        {
            var newSettings = MemberwiseClone() as LogSettings;
            newSettings.TypeOrders = TypeOrders.Select(x => x.Clone() as RegexCaptureOrderItem).ToList();
            newSettings.CustomLevels = CustomLevels.Select(x => x.Clone() as LogLevelItem).ToList();
            return newSettings;
        }
        public void Validate()
        {
            if (TypeOrders == null)
            {
                TypeOrders = new List<RegexCaptureOrderItem>();
            }
            foreach (var colName in Default.TypeOrders.Select(x => x.Name))
            {
                var exists = TypeOrders.Where(x => x.Name == colName);
                if (!exists.Any())
                {
                    var remainSlots = FindRemainOrderSlots();
                    if (remainSlots.Any())
                    {
                        var newCol = Default.TypeOrders.Where(x => x.Name == colName).First().Clone() as RegexCaptureOrderItem;
                        newCol.Order = remainSlots.First();
                        TypeOrders.Add(newCol);
                    }
                }
                else if (exists.Count() > 1)
                {
                    var remain = exists.First();
                    TypeOrders.RemoveAll(x => x.Name == colName && x != remain);
                }
            }

            if (FindRemainOrderSlots().Length > 0)
            {
                var allocateSlots = Default.TypeOrders.Select(x => x.Order).ToArray();
                foreach (var col in TypeOrders)
                {
                    if (col.Order < 0 || col.Order >= allocateSlots.Length || allocateSlots[col.Order] < 0)
                    {
                        col.Order = FindRemainOrderSlots().First();
                    }
                }
            }
            if (string.IsNullOrEmpty(FontFamily))
            {
                FontFamily = Default.FontFamily;
            }
            if (FontSize <= 0)
            {
                FontSize = Default.FontSize;
            }
        }
        public int[] FindRemainOrderSlots()
        {
            var remainSlots = Enumerable.Range(0, Default.TypeOrders.Count()).ToArray();
            foreach (var col in TypeOrders)
            {
                if (col.Order >= 0 && col.Order < remainSlots.Length)
                {
                    remainSlots[col.Order] = -1;
                }
            }
            return remainSlots.Where(x => x >= 0).ToArray();
        }
    }
}
