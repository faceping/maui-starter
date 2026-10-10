using Microsoft.Extensions.DependencyInjection;

namespace FaceCheck;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Light; // matches the app pictured in the FacePing docs
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}