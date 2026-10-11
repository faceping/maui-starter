# FacePing MAUI starter

**FaceCheck**: a small .NET MAUI app that enrols your face and verifies it live, on the phone, with a head-turn liveness check. Switch on airplane mode and it still matches.

It's a fuller version of the app you build in [Get started with .NET MAUI](https://docs.faceping.ai/start/maui/) (it also runs as a check-in device and shows what each sync found), built on [FacePing.Sdk.Maui](https://www.nuget.org/packages/FacePing.Sdk.Maui).

## Run it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) with the MAUI workload (`dotnet workload install maui`), and an Android phone (Android 7 or later, 64-bit) or an iPhone (iOS 15.1 or later) with a cable. Use a real phone: iOS simulators have no front camera, and an Android emulator only has one if you map your computer's webcam to it.

1. **Get a free sandbox key** at [app.faceping.ai/signup](https://app.faceping.ai/signup) (no card, no approval). In the dashboard: **Developers → Sandbox keys → Create sandbox key**.
2. **Paste it** into `MauiProgram.cs`, replacing `fp_test_…`.
3. **Run a Release build** on your phone (Debug builds are much slower at face checks):

   ```bash
   # Android
   dotnet build -t:Run -f net10.0-android -c Release
   ```

   On iOS, change `ApplicationId` in `FaceCheck.csproj` to your own, then run from Visual Studio or Rider, or see [Run it on your phone](https://docs.faceping.ai/start/maui/#run-it-on-your-phone) in the guide for the command line.

4. Tap **Enrol my face**, then **Verify**: you should see **Match**. Now switch on airplane mode and tap **Verify** again.

## Try it as a check-in device

Once you've tried the server side with your sandbox key (a practice check-in list, an enrolment link, a device), this app can be the check-in device:

1. In the dashboard, open your practice list's **Devices** tab and add a device (or call `POST /v1/lists/{listId}/devices`). Copy the `fpd_` token.
2. In `MauiProgram.cs`, use it instead of the sandbox key: `.UseFacePing(o => o.DeviceToken = "fpd_…")`.
3. In `MainPage.xaml.cs`, set `PersonId` to the `personId` you enrolled on the practice list (through an enrolment link or from your server).
4. Run it. The app downloads the list, hides **Enrol** (a check-in device gets its people from the server) and opens **Verify** once that person is on the phone.

See [Devices and offline sync](https://docs.faceping.ai/devices/).

## What's where

| File | What it does |
|---|---|
| `MauiProgram.cs` | Turns FacePing on with your sandbox key (`UseFacePing`). |
| `MainPage.xaml` | The camera view (`FaceCameraView`) and the two buttons. |
| `MainPage.xaml.cs` | `PersonId`, `EnrollAsync` and `VerifyLiveAsync`, and what each result means. |

## The sandbox

Free, for building and testing: up to 25 faces per device, each deleted 24 hours after it was enrolled. Faces you enrol in the app never leave the phone. People on your practice list (see above) are held by FacePing, each for 24 hours. The app needs a connection the first time it runs, to activate your key; after that it works offline for 30 days.

To use FacePing with real people, go live from your [FacePing dashboard](https://app.faceping.ai). Your verify code doesn't change.

## Learn more

- [How the SDK works](https://docs.faceping.ai/sdk/): liveness, storage, sandbox and production
- [MAUI reference](https://docs.faceping.ai/sdk/maui/): every type and method
- [Security](https://docs.faceping.ai/security/): the Rust core, encryption on the device
- Questions: support@faceping.ai

## Licence

This starter app is MIT licensed (see [LICENSE](LICENSE)): copy any of it into your own app. The FacePing SDK it uses has its own licence, included in the NuGet package.
