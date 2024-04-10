using TinyInsightsLib;

namespace TinyTestApp;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();

    }


    private async void PageViewButton_OnClicked(object? sender, EventArgs e)
    {
        await TinyInsights.TrackPageViewAsync("MainPage");
    }

    private async void EventButton_OnClicked(object? sender, EventArgs e)
    {
        await TinyInsights.TrackEventAsync("EventButton");
    }

    private async void ExceptionButton_OnClicked(object? sender, EventArgs e)
    {
        try
        {
            throw new Exception("Test excception");
        }
        catch (Exception ex)
        {
            await TinyInsights.TrackErrorAsync(ex);
        }
    }

    private async void TrackHttpButton_OnClicked(object? sender, EventArgs e)
    {
        var client = new HttpClient(new TinyInsightsMessageHandler());

        for (int i = 0; i < 10; i++)
        {
            _ = await client.GetAsync("https://google.se");
        }
    }

    private void CrashButtom_OnClicked(object? sender, EventArgs e)
    {
        throw new Exception("Crash Boom Bang!");
    }

    private async void TrackFailedHttpButton_Clicked(object sender, EventArgs e)
    {
        var client = new HttpClient(new TinyInsightsMessageHandler());

        for (int i = 0; i < 10; i++)
        {
            try
            {
                var response = await client.GetAsync("https://google.s");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                await TinyInsights.TrackErrorAsync(ex);
            }
        }

    }
}