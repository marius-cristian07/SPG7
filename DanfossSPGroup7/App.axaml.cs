using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DanfossSPGroup7.UI.ViewModels;
using DanfossSPGroup7.UI.Views;
using DanfossSPGroup7.Data;
using DanfossSPGroup7.Domain;
using System;

namespace DanfossSPGroup7
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {

                    // Create the main objects used by the app
                    var dataManager = new SourceDataManager();
                    var assetManager = new AssetManager();
                    var optimizer = new Optimizer(dataManager, assetManager);
                    var mainViewModel = new MainViewModel(optimizer, assetManager);

                    // Open the main window with its view model
                    desktop.MainWindow = new MainWindow(mainViewModel);
                }

                base.OnFrameworkInitializationCompleted();
            }
            catch (Exception ex)
            {
                Console.WriteLine("FATAL ERROR:");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}
