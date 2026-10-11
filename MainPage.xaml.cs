using FacePing.Sdk.Maui;

namespace FaceCheck;

public partial class MainPage : ContentPage
{
	static readonly Color Ink = Color.FromArgb("#0F172A"), Green = Color.FromArgb("#047857"), Red = Color.FromArgb("#B91C1C");

	// Who this app enrols and checks. In the sandbox that's you ("me"). As a check-in device (a DeviceToken in
	// MauiProgram.cs instead of the SandboxKey), set it to the personId you enrolled on your practice list.
	const string PersonId = "me";

	readonly IFacePing faceping;
	bool busy;

	public MainPage(IFacePing faceping)
	{
		InitializeComponent();
		this.faceping = faceping;
		Camera.CameraFailed += (_, message) => Show("Camera unavailable", message, Ink);
		Connectivity.ConnectivityChanged += (_, _) => MainThread.BeginInvokeOnMainThread(ShowNetwork);
		ShowNetwork();
		// A check-in device gets its people from the server: no enrolling here, and Verify opens once PersonId has synced.
		EnrollButton.IsVisible = faceping.IsSandbox;
		faceping.Synced += (_, _) => MainThread.BeginInvokeOnMainThread(() => SetEnrolled(faceping.EnrolledIds.Contains(PersonId)));
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		// Activates the sandbox key (or, on a check-in device, downloads its list). A face enrolled earlier may still be on
		// the phone (sandbox faces are deleted 24 hours after enrolment).
		try { await faceping.SyncAsync(); }
		catch (FacePingSetupException ex) { Show(faceping.IsSandbox ? "Sandbox key problem" : "Device token problem", ex.Message, Red); }
		catch (FacePingModelException ex) { Show("Face check unavailable", ex.Message, Red); }
		SetEnrolled(faceping.EnrolledIds.Contains(PersonId));
	}

	async void OnEnroll(object? sender, EventArgs e)
	{
		await Run(async () =>
		{
			Show("Hold still", "Taking the photo…", Ink);
			Camera.GuideState = FaceGuideState.Working;
			var photo = await Camera.CaptureAsync();
			var result = await faceping.EnrollAsync(PersonId, photo);
			Camera.GuideState = FaceGuideState.Ready;
			if (result.Outcome == EnrollOutcome.Enrolled)
			{
				SetEnrolled(true);
				Show("Enrolled", Offline ? "No network. Tap Verify." : "Now tap Verify.", Ink);
			}
			else
			{
				Show("Try again", Describe(result.Outcome), Red);
			}
		});
	}

	async void OnVerify(object? sender, EventArgs e)
	{
		await Run(async () =>
		{
			Show("Turn your head", "Liveness check…", Ink);
			// A live check: matches your face, asks you to turn your head (liveness), then matches again.
			var result = await faceping.VerifyLiveAsync(PersonId, Camera);
			var time = $"{result.Elapsed.TotalSeconds:0.00} s";
			if (result.IsMatch)
				Show("Match", $"Live face · {time} · {(Offline ? "no network used" : "on the phone")}", Green);
			else
				Show(TitleFor(result.Outcome), "Tap Verify to try again.", Red);
		});
	}

	async Task Run(Func<Task> action)
	{
		if (busy)
			return;
		busy = true;
		Enable(EnrollButton, false);
		Enable(VerifyButton, false);
		try
		{
			await action();
		}
		catch (FacePingSetupException ex)
		{
			// The sandbox key is missing or revoked, or the very first run had no connection.
			Camera.GuideState = FaceGuideState.Ready;
			Show(faceping.IsSandbox ? "Sandbox key problem" : "Device token problem", ex.Message, Red);
		}
		catch (FacePingModelException ex)
		{
			// The face models couldn't load on this phone: send people to your normal way in.
			Camera.GuideState = FaceGuideState.Ready;
			Show("Face check unavailable", ex.Message, Red);
		}
		catch (InvalidOperationException ex)
		{
			// For example the camera didn't deliver a picture, or enrolling on a check-in device (device token):
			// devices get their people from the server.
			Camera.GuideState = FaceGuideState.Ready;
			Show("Try again", ex.Message, Red);
		}
		finally
		{
			busy = false;
			Enable(EnrollButton, true);
			Enable(VerifyButton, faceping.EnrolledIds.Contains(PersonId));
		}
	}

	// Fixed button colours override MAUI's disabled look, so fade them instead (as in the docs' picture).
	static void Enable(Button button, bool on)
	{
		button.IsEnabled = on;
		button.Opacity = on ? 1 : 0.45;
	}

	void SetEnrolled(bool enrolled)
	{
		Enable(VerifyButton, enrolled);
		EnrollButton.Text = enrolled ? "Enrol again" : "Enrol my face";
	}

	void Show(string title, string note, Color color)
	{
		ResultTitle.Text = title;
		ResultTitle.TextColor = color;
		ResultNote.Text = note;
	}

	static bool Offline => Connectivity.Current.NetworkAccess != NetworkAccess.Internet;

	void ShowNetwork()
	{
		NetworkText.Text = Offline ? "Offline" : "Online";
		NetworkText.TextColor = Color.FromArgb(Offline ? "#B45309" : "#475569");
		NetworkPill.BackgroundColor = Color.FromArgb(Offline ? "#FFF4E5" : "#EAECF0");
	}

	static string TitleFor(VerifyOutcome outcome) => outcome switch
	{
		VerifyOutcome.NoMatch => "Not a match",
		VerifyOutcome.NotEnrolled => "Not enrolled",
		VerifyOutcome.NoFace => "No face seen",
		VerifyOutcome.LivenessFailed => "Not a live face",
		VerifyOutcome.ChallengeFailed => "Head turn not seen",
		VerifyOutcome.Expired => "Enrolment expired",
		_ => outcome.ToString()
	};

	static string Describe(EnrollOutcome outcome) => outcome switch
	{
		EnrollOutcome.NoFace => "No face seen. Look at the camera.",
		EnrollOutcome.MultipleFaces => "More than one face. Just you, please.",
		EnrollOutcome.FaceTooSmall => "Move a little closer.",
		EnrollOutcome.NotLive => "That looked like a photo or a screen.",
		EnrollOutcome.LimitReached => "The sandbox is full on this phone.",
		_ => outcome.ToString()
	};
}
