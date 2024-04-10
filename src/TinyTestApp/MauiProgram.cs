using Microsoft.Extensions.Logging;
using TinyInsightsLib;
using TinyInsightsLib.ApplicationInsights;

namespace TinyTestApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
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

#if WINDOWS
        TinyInsights.Configure(new ApplicationInsightsProvider(MauiWinUIApplication.Current, "InstrumentationKey=8b51208f-7926-4b7b-9867-16989206b950;IngestionEndpoint=https://swedencentral-0.in.applicationinsights.azure.com/;ApplicationId=0c04d3a0-9ee2-41a5-996e-526552dc730f"));
#else
        TinyInsights.Configure(new ApplicationInsightsProvider("InstrumentationKey=8b51208f-7926-4b7b-9867-16989206b950;IngestionEndpoint=https://swedencentral-0.in.applicationinsights.azure.com/;ApplicationId=0c04d3a0-9ee2-41a5-996e-526552dc730f"));
#endif
        return builder.Build();
    }
}