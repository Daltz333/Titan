using Avalonia.Controls;
using System.ComponentModel;

namespace Titan
{
    public partial class MainWindow : Window
    {
        private static Window? _Instance;
        public static Window? Instance
        {
            get => _Instance;
        }

        public MainWindow()
        {
            InitializeComponent();

            _Instance = this;
        }
    }
}