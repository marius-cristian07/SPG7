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
                    Console.WriteLine("APP START");

                    var dataManager = new SourceDataManager();
                    Console.WriteLine("DATA LOADED");

                    var assetManager = new AssetManager();
                    Console.WriteLine("ASSETS LOADED");

                    new Optimizer(dataManager, assetManager);
                    Console.WriteLine("OPTIMIZER CREATED");

                    desktop.MainWindow = new MainWindow();
                    Console.WriteLine("MAIN WINDOW CREATED");
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