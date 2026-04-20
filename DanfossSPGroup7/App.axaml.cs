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

                    var dataManager = new SourceDataManager();

                    var assetManager = new AssetManager();

                    new Optimizer(dataManager, assetManager);

                    desktop.MainWindow = new MainWindow();
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