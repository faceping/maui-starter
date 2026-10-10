using FacePing.Sdk.Maui;

namespace FaceCheck;

public partial class MainPage : ContentPage
{
	static readonly Color Ink = Color.FromArgb("#0F172A"), Green = Color.FromArgb("#047857"), Red = Color.FromArgb("#B91C1C");

	readonly IFacePing faceping;
	bool busy;

	public MainPage(IFacePing faceping)
	{
		InitializeComponent();
		this.faceping = faceping;
		Camera.CameraFailed += (_, message) => Show("Camera unavailable", message, Ink);
		Connectivity.ConnectivityChanged += (_, _) => MainThread.BeginInvokeOnMainThread(ShowNetwork);
		ShowNetwork();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		// A face enrolled earlier may still be on the phone (sandbox faces are deleted 24 hours after enrolment).
		try { await faceping.SyncAsync(); }
		catch (FacePingSetupException ex) { Show("Sandbox key problem", ex.Message, Red); }
		catch (FacePingModelException ex) { Show("Face check unavailable", ex.Message, Red); }
		SetEnrolled(faceping.EnrolledIds.Contains("me"));
	}

	async void OnEnrol(object sender, EventArgs e)
	{
		await Run(async () =>
		{
			Show("Hold still", "Taking the photo…", Ink);
			Camera.GuideState = FaceGuideState.Working;
			var photo = await Camera.CaptureAsync();
			var result = await faceping.EnrolAsync("me", photo);
			Camera.GuideState = FaceGuideState.Ready;
			if (result.Outcome == EnrolOutcome.Enrolled)
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

	async void OnVerify(object sender, EventArgs e)
	{
		await Run(async () =>
		{
			Show("Turn your head", "Liveness check…", Ink);
			// A live check: matches your face, asks you to turn your head (liveness), then matches again.
			var result = await faceping.VerifyLiveAsync("me", Camera);
			var time = $"{result.Elapsed.TotalSeconds:0.00} s";
			if (result.IsMatch)
				Show("Match", $"Live face · {time} · {(Offline ? "no network used" : "on the phone")}", Green);
			else
				Show(Title(result.Outcome), "Tap Verify to try again.", Red);
		});
	}

	async Task Run(Func<Task> action)
	{
		if (busy)
			return;
		busy = true;
		Enable(EnrolButton, false);
		Enable(VerifyButton, false);
		try
		{
			await action();
		}
		catch (FacePingSetupException ex)
		{
			// The sandbox key is missing or revoked, or the very first run had no connection.
			Camera.GuideState = FaceGuideState.Ready;
			Show("Sandbox key problem", ex.Message, Red);
		}
		catch (FacePingModelException ex)
		{
			// The face models couldn't load on this phone: send people to your normal way in.
			Camera.GuideState = FaceGuideState.Ready;
			Show("Face check unavailable", ex.Message, Red);
		}
		finally
		{
			busy = false;
			Enable(EnrolButton, true);
			Enable(VerifyButton, faceping.EnrolledIds.Contains("me"));
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
		EnrolButton.Text = enrolled ? "Enrol again" : "Enrol my face";
	}

	void Show(string title, string note, Color colour)
	{
		ResultTitle.Text = title;
		ResultTitle.TextColor = colour;
		ResultNote.Text = note;
	}

	static bool Offline => Connectivity.Current.NetworkAccess != NetworkAccess.Internet;

	void ShowNetwork()
	{
		NetworkText.Text = Offline ? "Offline" : "Online";
		NetworkText.TextColor = Color.FromArgb(Offline ? "#B45309" : "#475569");
		NetworkPill.BackgroundColor = Color.FromArgb(Offline ? "#FFF4E5" : "#EAECF0");
	}

	static string Title(VerifyOutcome outcome) => outcome switch
	{
		VerifyOutcome.NoMatch => "Not a match",
		VerifyOutcome.NotEnrolled => "Not enrolled",
		VerifyOutcome.NoFace => "No face seen",
		VerifyOutcome.LivenessFailed => "Not a live face",
		VerifyOutcome.ChallengeFailed => "Head turn not seen",
		VerifyOutcome.Expired => "Enrolment expired",
		_ => outcome.ToString()
	};

	static string Describe(EnrolOutcome outcome) => outcome switch
	{
		EnrolOutcome.NoFace => "No face seen. Look at the camera.",
		EnrolOutcome.MultipleFaces => "More than one face. Just you, please.",
		EnrolOutcome.FaceTooSmall => "Move a little closer.",
		EnrolOutcome.NotLive => "That looked like a photo or a screen.",
		EnrolOutcome.LimitReached => "The sandbox is full on this phone.",
		_ => outcome.ToString()
	};
}
