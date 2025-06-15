using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using Syncfusion.Maui.Core.Hosting;
using LocalizationTabii.Pages;
using LocalizationTabii.PageModels;
using LocalizationTabii.Services;
using System.IO;


namespace LocalizationTabii
{
    public static class MauiProgram
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public static MauiApp CreateMauiApp()
        {
            // DEBUG: Uygulama başlangıç testi
            try
            {
                var logPath = Path.Combine("/Users/selimcepni/Documents/GitHub/LocalizationTabii", "debug_log.txt");
                var logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] 🚀 UYGULAMA BAŞLADI - MauiProgram.CreateMauiApp\n";
                File.AppendAllText(logPath, logMessage);
                Console.WriteLine("🚀 UYGULAMA BAŞLADI - Debug dosyası yazıldı");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 Debug dosyası yazma hatası: {ex.Message}");
            }

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureSyncfusionCore()

                .ConfigureMauiHandlers(handlers =>
                {
#if IOS || MACCATALYST
    				handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Roboto-Medium.ttf", "Roboto-Medium");
                    fonts.AddFont("Roboto-Regular.ttf", "Roboto-Regular");
                    fonts.AddFont("UIFontIcons.ttf", "FontIcons");
                    fonts.AddFont("Dashboard.ttf", "DashboardFontIcons");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });
			//Register Syncfusion license https://help.syncfusion.com/common/essential-studio/licensing/how-to-generate
			//Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR LICENSE KEY");

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            builder.Services.AddSingleton<ProjectRepository>();
            builder.Services.AddSingleton<TaskRepository>();
            builder.Services.AddSingleton<CategoryRepository>();
            builder.Services.AddSingleton<TagRepository>();
            builder.Services.AddSingleton<SeedDataService>();
            builder.Services.AddSingleton<ModalErrorHandler>();
            builder.Services.AddSingleton<SRTCleaningService>();
            
            // Prompt servisleri
            builder.Services.AddSingleton<IPromptStorageService, PromptStorageService>();
            
            builder.Services.AddSingleton<MainPageModel>();
            builder.Services.AddSingleton<ProjectListPageModel>();
            builder.Services.AddSingleton<ManageMetaPageModel>();
            builder.Services.AddSingleton<SettingsPageModel>();
            
            // Pages ve PageModels
            builder.Services.AddSingleton<PromptsManagementPage>();
            builder.Services.AddSingleton<PromptsManagementPageModel>();

            builder.Services.AddTransientWithShellRoute<ProjectDetailPage, ProjectDetailPageModel>("project");
            builder.Services.AddTransientWithShellRoute<TaskDetailPage, TaskDetailPageModel>("task");
            builder.Services.AddTransientWithShellRoute<TranslatePage, TranslatePageModel>("translate");
            builder.Services.AddTransientWithShellRoute<SRTToolsPage, SRTToolsPageModel>("srttools");
            builder.Services.AddTransientWithShellRoute<SettingsPage, SettingsPageModel>("settings");

            var app = builder.Build();
            ServiceProvider = app.Services;
            return app;
        }
    }
}
