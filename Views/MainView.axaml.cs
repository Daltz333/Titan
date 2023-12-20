using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Serilog;
using Titan.ViewModels;

namespace Titan.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        Log.Information("Initialized application.");
        InitializeComponent();
    }

    private void TextBox_TextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is TextBox textbox)
        {
            var items = vm.Records;
            var query = textbox?.Text ?? string.Empty;

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(query))
                {
                    item.IsVisible = true;
                } else if (item.Name.Contains(query, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    item.IsVisible = true;
                } else
                {
                    item.IsVisible = false;
                }
            }
        }
    }
}