using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SplitBillApp.Data;
using SplitBillApp.Services;
using SplitBillApp.ViewModels;
using SplitBillApp.Views;

namespace SplitBillApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                // Plus Jakarta Sans — the design system's typeface, one alias per weight
                fonts.AddFont("PlusJakartaSans-Regular.ttf", "PJSans");
                fonts.AddFont("PlusJakartaSans-Medium.ttf", "PJSansMedium");
                fonts.AddFont("PlusJakartaSans-SemiBold.ttf", "PJSansSemibold");
                fonts.AddFont("PlusJakartaSans-Bold.ttf", "PJSansBold");
                fonts.AddFont("PlusJakartaSans-ExtraBold.ttf", "PJSansExtra");
            });

        // ---- Dependency Injection ----
        // Shared SQLite-backed data layer (offline-first) + in-memory bill draft
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<DraftService>();

        // ViewModels
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<CreateBillViewModel>();
        builder.Services.AddTransient<ResultViewModel>();
        builder.Services.AddTransient<CategoryViewModel>();

        // Pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<CreateBillPage>();
        builder.Services.AddTransient<ResultPage>();
        builder.Services.AddTransient<CategoryPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
