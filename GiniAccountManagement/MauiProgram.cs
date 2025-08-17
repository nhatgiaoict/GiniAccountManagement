using GiniAccountManagement.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Serilog;
using Serilog.Events;

namespace GiniAccountManagement
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Load cấu hình trước khi tạo app
            AppConfig.Load();
            // Khởi tạo Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
#if RELEASE
            .MinimumLevel.Information()
#endif
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Gắn Serilog vào hệ thống logging của MAUI
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            builder.ConfigureLifecycleEvents(events =>
            {
#if ANDROID
            events.AddAndroid(android => android
                .OnStop((a) => Chrome.CloseAllSafe())
                .OnDestroy((a) => Chrome.CloseAllSafe()));
#endif
#if WINDOWS
            events.AddWindows(win => win
                .OnClosed((app, args) => Chrome.CloseAllSafe()));
#endif
#if IOS
            events.AddiOS(ios => ios
                .WillTerminate((app) => Chrome.CloseAllSafe()));
#endif
            });

            return builder.Build();
        }
    }
}
