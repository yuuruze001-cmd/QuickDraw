using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using QuickDraw.Activation;
using QuickDraw.Contracts.Services;
using QuickDraw.Services;
using QuickDraw.ViewModels;
using QuickDraw.Views;
using System;
using System.IO;
using System.Reflection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public IHost Host
    {
        get;
    }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        // TODO: Move to App SDK 1.8 (currently syncfusion doesn't work with that version)
        try
        {
            string resourceName = "syncfusion.license";

            Assembly assembly = Assembly.GetExecutingAssembly();

            if (assembly != null)
            {
                using Stream? rsrcStream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Assets." + resourceName);

                if (rsrcStream != null)
                {
                    using StreamReader streamReader = new(rsrcStream);

                    string key = streamReader.ReadToEnd();

                    if (key != "")
                    {
                        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(key);
                    }    
                }
            }
        }
        catch { };

        this.InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices(services =>
            {
                // Default Activation Handler
                services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

                // Other Activation Handlers

                // Services
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();

                // Views and ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<MainPage>();
            }).
            Build();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        /*            Settings.ReadSettings();


                    var presenter = OverlappedPresenter.Create();

                    presenter.PreferredMinimumWidth = 512;
                    presenter.PreferredMinimumHeight = 312;
                    Window.AppWindow.SetPresenter(presenter);
                    Window.Activate();*/

        await App.GetService<IActivationService>().ActivateAsync(args);
    }


    public static MainWindow Window { get; } = new();
}
