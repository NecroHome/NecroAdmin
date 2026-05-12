using Microsoft.Extensions.Logging;
using NecroPanel.ApplicationN.Interfaces;
using NecroPanel.ApplicationN.Services;

namespace NecroPanel
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
#if DEBUG
            NecroPanel.Settings.SettingsDev settings = new NecroPanel.Settings.SettingsDev();
#else
            NecroPanel.Settings.SettingsProd settings = new NecroPanel.Settings.SettingsProd();
#endif
            var builder = MauiApp.CreateBuilder();
            builder.Services.AddSingleton<ISshService, SshService>();
            builder.Services.AddSingleton<IWakeOnLanService, WakeOnLanService>();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
