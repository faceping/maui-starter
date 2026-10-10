using FacePing.Sdk.Maui;
using Microsoft.Extensions.Logging;

namespace FaceCheck;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			// Your sandbox key: app.faceping.ai → Developers → Sandbox keys → Create sandbox key.
			.UseFacePing(o => o.SandboxKey = "fp_test_…")
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddTransient<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
