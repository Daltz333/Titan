using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.ViewModels
{
    public partial class SharedViewModel : ObservableObject
    {
        private SharedViewModel() { }

        private static readonly Lazy<SharedViewModel> lazy = new(() => new());
        public static SharedViewModel Instance => lazy.Value;

        /// <summary>
        /// Whether or not the global application busy indicator should be shown
        /// </summary>
        [ObservableProperty]
        private bool _IsGlobalBusy = false;
    }
}
