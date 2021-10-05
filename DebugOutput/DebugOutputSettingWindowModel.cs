using Microsoft.VisualStudio.PlatformUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugOutput
{
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

    }

    public class LogSettings : ICloneable
    {
        public string CaptureRegex { get; set; }
        public List<RegexCaptureOrderItem> TypeOrders { get; set; }
        public List<LogLevelItem> CustomLevels { get; set; }

        public int OrderTime => TypeOrders.IndexOf(TypeOrders.Where(x => x.Name == "DateTime").FirstOrDefault());
        public int OrderLevel => TypeOrders.IndexOf(TypeOrders.Where(x => x.Name == "Level").FirstOrDefault());
        public int OrderText => TypeOrders.IndexOf(TypeOrders.Where(x => x.Name == "Text").FirstOrDefault());
        public int OrderFile => TypeOrders.IndexOf(TypeOrders.Where(x => x.Name == "File").FirstOrDefault());
        public int OrderLine => TypeOrders.IndexOf(TypeOrders.Where(x => x.Name == "Line").FirstOrDefault());


        public static readonly LogSettings Default = new LogSettings
        {
            CaptureRegex = @"\[(.+?)\] \[(.+?)\] (.+) (.+?)\(([\d]+)\)",
            TypeOrders = new List<RegexCaptureOrderItem>
            {
                new RegexCaptureOrderItem{ Name="DateTime", Order=0},
                new RegexCaptureOrderItem{ Name="Level", Order=1},
                new RegexCaptureOrderItem{ Name="Text", Order=2},
                new RegexCaptureOrderItem{ Name="File", Order=3},
                new RegexCaptureOrderItem{ Name="Line", Order=4},
            },
            CustomLevels = new List<LogLevelItem>
            {
                new LogLevelItem{ Name="Fatal", Match="FTL|FATAL|Ftl|Fatal|ftl|fatal", ColorType=DebugOutputColorType.Custom, ColorValue="#FF0000" },
                new LogLevelItem{ Name="Error", Match="ERR|ERROR|Err|Error|err|error", ColorType=DebugOutputColorType.Custom, ColorValue="#FF4040" },
                new LogLevelItem{ Name="Warn", Match="WARN|WARNING|Warn|Warning|warn|warning", ColorType=DebugOutputColorType.Custom, ColorValue="#FFFF00" },
                new LogLevelItem{ Name="Info", Match="INFO|INFORMATION|Info|Information|info|information", ColorType=DebugOutputColorType.Custom, ColorValue="#00FF00" },
                new LogLevelItem{ Name="Debug", Match="DBG|DEBUG|Dbg|Debug|dbg|debug", ColorType=DebugOutputColorType.Default, ColorValue="#FFFFFF" },
            }
        };
        public static readonly string DefaultSerialized = JsonConvert.SerializeObject(Default);

        public object Clone()
        {
            var newSettings = MemberwiseClone() as LogSettings;
            newSettings.TypeOrders = TypeOrders.Select(x => x.Clone() as RegexCaptureOrderItem).ToList();
            newSettings.CustomLevels = CustomLevels.Select(x => x.Clone() as LogLevelItem).ToList();
            return newSettings;
        }
    }

}
