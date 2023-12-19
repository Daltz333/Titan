using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Models
{
    public partial class LogItem(string name, bool isSelected) : ObservableObject
    {
        public string Name { get; set; } = name;
        public bool IsSelected { get; set; } = isSelected;

        [ObservableProperty]
        private bool _IsVisible = true;

        [ObservableProperty]
        private string _SelectedRecordType = "None";

        public List<string> AvailableSignalTypes { get; private set; } = new()
        {
            "None",
            "Position",
            "Velocity",
            "Voltage",
        };
    }
}
