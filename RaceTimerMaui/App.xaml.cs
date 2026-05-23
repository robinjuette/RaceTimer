using Microsoft.Maui.Controls;

namespace RaceTimerMaui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new MainPage();
    }
}
