using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using MagicCursor.ViewModels;
using MagicCursor.Views;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MagicCursor;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static MainWindow AppWindow { get; private set; } = null!;
    public static IServiceProvider Services { get; private set; } = null!;

#if DEBUG
    public static ChatWindow? PreviewChatWindow { get; private set; }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);
#endif

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        // Configure dependency injection
        var services = new ServiceCollection();
        services.AddSingleton<ChatViewModel>();
        Services = services.BuildServiceProvider();

        // Global crash handler
        this.UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true; // Attempt to keep it alive to write the log
        Log.Error("Unhandled exception", e.Exception);
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
#if DEBUG
        if (Environment.CommandLine.Contains("--preview-chat", StringComparison.OrdinalIgnoreCase))
        {
            LaunchPreviewChat();
            return;
        }
#endif
        AppWindow = new MainWindow();
        AppWindow.Activate();
    }

#if DEBUG
    private void LaunchPreviewChat()
    {
        var chatWindow = new ChatWindow();
        PreviewChatWindow = chatWindow;

        var appWindow = chatWindow.AppWindow;
        var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }

        var primaryDisplay = Microsoft.UI.Windowing.DisplayArea.Primary;
        var workArea = primaryDisplay != null ? primaryDisplay.WorkArea : new Windows.Graphics.RectInt32(0, 0, 1920, 1080);

        var hWnd = chatWindow.GetWindowHandle();
        uint dpi = GetDpiForWindow(hWnd);
        if (dpi == 0) dpi = 96;

        double scale = dpi / 96.0;
        int widthPx = (int)Math.Round(460.0 * scale);
        int x = workArea.X + workArea.Width - widthPx;
        int y = workArea.Y;
        int heightPx = workArea.Height;

        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, widthPx, heightPx));
        chatWindow.Activate();
    }
#endif
}
