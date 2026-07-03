using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SplitBillApp.Data;
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
            });

        // ---- Dependency Injection ----
        // Single shared SQLite-backed data layer (offline-first)
        builder.Services.AddSingleton<DatabaseService>();

        // ViewModels
        builder.Services.AddSingleton<GroupsViewModel>();
        builder.Services.AddTransient<GroupDetailViewModel>();
        builder.Services.AddTransient<AddExpenseViewModel>();

        // Pages
        builder.Services.AddSingleton<GroupsPage>();
        builder.Services.AddTransient<GroupDetailPage>();
        builder.Services.AddTransient<AddExpensePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
