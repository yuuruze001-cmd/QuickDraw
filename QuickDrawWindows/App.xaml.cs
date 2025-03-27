using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using QuickDraw.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Path = System.IO.Path;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QuickDraw
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
#if DEBUG
            try
            {
                var executingAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                if (executingAssemblyPath != null)
                {
                    var syncfusionKey = System.IO.File.ReadAllText(Path.Combine(executingAssemblyPath, @"..\..\..\..\..\devlics\syncfusion.devlic"));
                    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);
                }
            }
            catch { };
#else
            // Do whatever I should do with the actual license key
#endif
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {

            Settings.ReadSettings();


            var presenter = OverlappedPresenter.Create();

            presenter.PreferredMinimumWidth = 512;
            presenter.PreferredMinimumHeight = 312;
            Window.AppWindow.SetPresenter(presenter);
            Window.Activate();
        }

        public static readonly MainWindow Window = new();
        public MFSettings Settings { get; set; } = new();
    }
}
