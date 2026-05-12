using Microsoft.Extensions.DependencyInjection;
#if WINDOWS
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
#endif

namespace NecroPanel
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

#if WINDOWS

            window.Width = 420;
            window.Height = 900;

            window.X = 500;
            window.Y = 40;

            window.Created += (s, e) =>
            {
                var nativeWindow = window.Handler.PlatformView;

                IntPtr hWnd = WindowNative.GetWindowHandle(nativeWindow);

                var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsResizable = false;
                    presenter.IsMaximizable = false;
                    presenter.IsMinimizable = true;
                }
            };
#endif
                return window;
            }
    }
}