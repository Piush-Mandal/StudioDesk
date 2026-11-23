using Microsoft.UI.Xaml.Controls;

namespace StudioDesk.App.Pages;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        var hour = DateTime.Now.Hour;
        var part = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
        var name = Environment.UserName;
        Greeting.Text = string.IsNullOrWhiteSpace(name) ? part : $"{part}, {name.Replace('_', ' ')}";
        Tagline.Text = App.Services.Tagline;
    }
}
