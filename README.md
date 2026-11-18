# AoE4 Stats

Unofficial Windows desktop app for looking up a public [AoE4 World](https://aoe4world.com/) player, then showing seasonal rating, rank, wins, losses, win rate, and recent matches.

AoE4 Stats is not affiliated with, endorsed by, or sponsored by Microsoft, World's Edge, Relic Entertainment, or AoE4 World. Age of Empires IV is a trademark of its owners. The app does not ship Age of Empires artwork. Logos under `src/AoE4Stats/Assets` and `brand/mark.svg` are an original bar-chart mark.

This is a separate product from any interval timer. Do not reuse Store product ID `9N2SMSC169CL`. The package identity in source control is a development placeholder only.

Replay parsing is not implemented. The Replays page explains why a build-order parser is not the same thing as engine-based unit positions. Two MIT-licensed projects are named there as later references only: [exoque/aoe4replayanalyzer](https://github.com/exoque/aoe4replayanalyzer) and [BigManJohn000/aoe4-replay-viewer](https://github.com/BigManJohn000/aoe4-replay-viewer). Their code is not included.

## What the app does

- Search `GET /api/v0/players/search?query=` after 3 characters. Nothing is sent while you type.
- Load `GET /api/v0/players/{id}` and `GET /api/v0/players/{id}/games`.
- Show loading, error, and empty states. Missing ratings stay missing. Unranked ladders are labeled Unranked.
- Cache search for 1 minute, profiles for 5 minutes, and match pages for 2 minutes. A refresh inside 15 seconds reuses that payload.
- Save the selected profile id, name, and country code in `%LOCALAPPDATA%\AoE4Stats\selected-profile.json`. Packaged runs use the package's virtualized local folder.
- User-Agent: `AoE4Stats/0.1 (Windows desktop; public AoE4 World lookup; non-bulk; +https://aoe4world.com/api)`. It is not a browser string.

The public API does not require a key. This app does not send `api_key`, does not walk every page by itself, and does not call the bulk games feed.

## Layout

```
AoE4Stats.sln          Visual Studio 2026 solution
AoE4Stats.slnx         .NET 10 XML solution (same projects)
src/AoE4Stats.Core     net10.0 client, cache, formatting, view models
src/AoE4Stats          WinUI 3 single-project MSIX app
tests/AoE4Stats.Core.Tests
brand/mark.svg         original logo source
tools/render_logo.py   regenerates the PNG and ICO assets
```

Open `AoE4Stats.slnx` or `AoE4Stats.sln`. Startup project: **AoE4Stats**. Solution platform: **x64** on a typical desktop. ARM64 is also configured. x86 is not a target.

## Versions

| Piece | Version | Why |
| --- | --- | --- |
| .NET SDK | 10.0.403, roll forward to later 10.0 patches | `global.json`. Matches the SDK on the build PC. |
| App TFM | `net10.0-windows10.0.26100.0` | Windows 11 SDK family already installed (10.0.26100.0). Minimum OS is 10.0.17763.0. |
| `Microsoft.Windows.SDK.NET.Ref` | 10.0.26100.57 | The ref package .NET SDK 10.0.403 selects for this TFM. This is not the Windows SDK installer revision. |
| `Microsoft.Windows.SDK.BuildTools` | 10.0.26100.9457 | Current stable 26100-series build tools on NuGet (September 2026 SDK). |
| `Microsoft.Windows.SDK.BuildTools.WinApp` | 0.8.0 | Stable package. `dotnet run` registers a loose MSIX layout through the bundled `winapp` tool. |
| `Microsoft.WindowsAppSDK` | 2.6.5 | Current stable channel (2 November 2026). WinUI 3 comes from this package. Experimental 2.6.6 is not used. |
| `CommunityToolkit.Mvvm` | 8.4.2 | View models and commands. |
| xUnit / test SDK | xunit 2.9.3, runner 3.1.5, Microsoft.NET.Test.Sdk 18.11.0 | Core tests. |
| Package identity | Name `AoE4Stats.Development`, Publisher `CN=AoE4Stats Development`, Version `0.1.0.0` | Local placeholder. Not a Store identity. |

`PublishTrimmed` is false. The JSON client uses source generation, but trimming stays off until a Windows build proves the packaged app still deserializes.

## Prerequisites on the Windows PC

1. Windows 10 version 1809 or later. Windows 11 is the expected desktop.
2. [.NET SDK 10.0.403](https://dotnet.microsoft.com/download/dotnet/10.0) or a later 10.0 patch. Check with `dotnet --version`.
3. Visual Studio Community 2026 18.11.3 with the **Windows application development** workload. Single-project MSIX packaging tools are built into Visual Studio 2026. The .NET desktop workload is useful and not a substitute for the Windows workload.
4. Windows SDK 10.0.26100 (already installed as 10.0.26100.0 is enough for the targeting tools; the NuGet packages above supply the compiler references). SDK 10.0.28000.0 can stay installed. This project does not target it.
5. [Windows App Runtime 2.6.5](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads), the stable x64 (or ARM64) installer. Framework-dependent runs need this runtime. Visual Studio often prompts on first launch if it is missing.
6. Developer Mode: Settings → System → For developers → Developer Mode. Required to register the local MSIX layout.

No API key, Store certificate, or Partner Center secret belongs in this repo.

## Open, build, and run in Visual Studio 2026

1. Copy the sources so the solution file is `C:\Projects\AoE4Stats\AoE4Stats.sln`.
2. Start Visual Studio 2026.
3. **File → Open → Project/Solution** and choose `C:\Projects\AoE4Stats\AoE4Stats.slnx` (or `AoE4Stats.sln`).
4. Wait for restore. Package source is nuget.org only (`nuget.config`).
5. In the toolbar, set the solution configuration to **Debug** and the platform to **x64**.
6. In Solution Explorer, confirm the startup project is **AoE4Stats** (the WinUI project, not the class library or the test project). Right-click it and choose **Set as Startup Project** if it is not bold.
7. **Build → Build Solution**.
8. Turn on Developer Mode if Windows asks.
9. If deploy asks for a signing certificate, stop and follow [Local signing](#local-signing) before F5. A compile that does not package does not need a certificate: `AppxPackageSigningEnabled` is false in the project file.
10. Press **F5**. The launch profile is `MsixPackage` (`src/AoE4Stats/Properties/launchSettings.json`). The solution marks Deploy for the app project.

Command-line build from a Developer PowerShell or a normal terminal, in `C:\Projects\AoE4Stats`:

```powershell
dotnet build .\AoE4Stats.sln -c Debug -p:Platform=x64
dotnet run --project .\src\AoE4Stats\AoE4Stats.csproj -p:Platform=x64
```

`dotnet run` uses `Microsoft.Windows.SDK.BuildTools.WinApp` to build, register a loose package, and launch with package identity. It still requires Developer Mode and the Windows App Runtime. This command was not executed here.

Core tests, which do not build the WinUI app:

```powershell
dotnet test .\tests\AoE4Stats.Core.Tests\AoE4Stats.Core.Tests.csproj -c Release
```

An optional live call to AoE4 World (one exact search, one profile, one match page) is off unless you set `AOE4STATS_LIVE_API=1` for that test process. Leave it unset for ordinary runs.

## Local signing

No certificate, password, or thumbprint is in this repository. Do not generate one on a build server and commit it.

The manifest publisher is exactly:

```text
CN=AoE4Stats Development
```

The package name is `AoE4Stats.Development`. A test certificate must use that subject or installation fails the publisher check.

In a PowerShell window on the development PC, create a certificate and trust it for the current user. This creates a private key on that PC. Do not copy the `.pfx` into the project.

```powershell
$cert = New-SelfSignedCertificate `
  -Type Custom `
  -Subject "CN=AoE4Stats Development" `
  -FriendlyName "AoE4Stats Development" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -KeyUsage DigitalSignature `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

Export-Certificate -Cert $cert -FilePath "$env:TEMP\AoE4Stats-dev.cer" | Out-Null
Import-Certificate -FilePath "$env:TEMP\AoE4Stats-dev.cer" -CertStoreLocation "Cert:\CurrentUser\TrustedPeople" | Out-Null
$cert.Thumbprint
```

Put the thumbprint only in `src\AoE4Stats\AoE4Stats.csproj.user`, which is gitignored and imported automatically:

```xml
<Project>
  <PropertyGroup>
    <PackageCertificateThumbprint>PASTE_THUMBPRINT</PackageCertificateThumbprint>
    <AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>
  </PropertyGroup>
</Project>
```

Visual Studio may also offer to create a temporary test certificate the first time you package. If you use that prompt, check that the subject matches `CN=AoE4Stats Development`. If it does not, packaging will not install.

Signing on is required for a normal local install of an `.msix`. Leaving signing off still lets the project compile. `dotnet run` through the WinApp tooling registers a development layout and may handle its own dev identity; if it reports a publisher mismatch, use the certificate above.

## Release package

Framework-dependent, unsigned layout (proves the package builds; Windows will not install an unsigned package for normal sideload):

```powershell
dotnet publish .\src\AoE4Stats\AoE4Stats.csproj -c Release -p:Platform=x64 -p:RuntimeIdentifier=win-x64 -p:SelfContained=false -p:PublishTrimmed=false -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false -p:AppxBundle=Never
```

Signed sideload build, after the certificate exists. Replace the thumbprint. Do not put the thumbprint in the committed project file.

```powershell
dotnet publish .\src\AoE4Stats\AoE4Stats.csproj -c Release -p:Platform=x64 -p:RuntimeIdentifier=win-x64 -p:SelfContained=false -p:PublishTrimmed=false -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=true -p:PackageCertificateThumbprint=PASTE_THUMBPRINT -p:AppxBundle=Never
```

Single-project MSIX does not produce an `.msixbundle` by itself (`AppxBundle` is `Never`). Publish profiles for x64 and ARM64 are in `src/AoE4Stats/Properties/PublishProfiles`. The output folder is under `src\AoE4Stats\AppPackages` or `src\AoE4Stats\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\`. Confirm the path on the first successful package build. That path was not verified here.

The Windows App SDK build adds the framework dependency on the Windows App Runtime when the app is not self-contained. Install runtime 2.6.5 on machines that sideload this package, or let the Store dependency do that after publication.

ARM64 is the same command with `-p:Platform=ARM64 -p:RuntimeIdentifier=win-arm64`.

## Store, later

Do this only when you are ready to publish. Nothing in this repo uploads a package or reserves a name.

1. Do not associate this app with Store product `9N2SMSC169CL`. Reserve a new name in Partner Center.
2. In Visual Studio, use **Publish → Associate App with the Store** on the AoE4Stats project. That step replaces `Name` and `Publisher` in `Package.appxmanifest`. The development values `AoE4Stats.Development` and `CN=AoE4Stats Development` must not ship as the Store identity.
3. Association changes the package family name. Local saved state from development builds will not follow the Store package.
4. Store signing uses the Partner Center certificate. Do not create a production certificate in this repo.
5. Capabilities in the manifest are only `runFullTrust` (required for packaged WinUI 3 desktop) and `internetClient` (HTTPS to AoE4 World). Do not add broad file system, private network, or device capabilities for this MVP.
6. A network-connected Store app needs a privacy policy URL, an age-rating questionnaire, and screenshots. Host the policy yourself before submission. A draft you can edit is below.
7. Add a contact address to the User-Agent before a public release. AoE4 World asks automated clients to identify themselves and not to spoof a browser.
8. Complete the Store listing as an unofficial fan app. Do not upload Age of Empires logos.

### Privacy policy draft

AoE4 Stats does not create an account and does not run its own analytics or ads.

When you search, the name you submit is sent to `aoe4world.com`. Opening a player sends that public profile id to the same site. Match lists are requested the same way. The app stores the selected profile id, display name, and country code on this PC only.

The app does not sell data. Windows, Visual Studio, and the Microsoft Store may have their own diagnostics outside this application.

Replace this draft with a hosted page and a contact email before Partner Center will accept the submission.

## API behavior worth knowing

Search rows use `leaderboards`. Profile documents use `modes`. Unranked blocks omit `rating` and `rank`. `rating_history` is ignored on purpose; it is large and this version does not chart it. Match `mmr_diff` can be null. `per_page` may be a number or a numeric string.

Player profile links open only when the host is `aoe4world.com`.

## Validation already done

On a Linux agent, with .NET SDK 10.0.403:

- `dotnet test tests/AoE4Stats.Core.Tests/AoE4Stats.Core.Tests.csproj -c Release` passed 11 tests.
- One opted-in live smoke (`AOE4STATS_LIVE_API=1`) called search, profile, and a one-game page and passed. The default test run does not call the network.
- `dotnet restore src/AoE4Stats/AoE4Stats.csproj -p:EnableWindowsTargeting=true -p:Platform=x64` restored Windows App SDK 2.6.5, BuildTools 10.0.26100.9457, WinApp 0.8.0, and the 10.0.26100.57 ref pack.
- `dotnet build` of the WinUI project then failed because `XamlCompiler.exe` from `Microsoft.WindowsAppSDK.WinUI` 2.6.5 is a Windows executable (`Exec format error`). The compiler did not run, so XAML was not checked.

## Not validated on Linux

These need the Windows PC. Do not treat them as done:

- WinUI 3 XAML compile and C# code-behind compile (the markup compiler never started).
- MSIX manifest schema check, PRI resource generation, and `makeappx` / `signtool`.
- F5 deploy, Developer Mode registration, and the Windows App Runtime.
- Visual Studio 2026 designer, solution load, and the Package and Publish menu.
- Layout at desktop or narrow window sizes, including the title bar and navigation pane.
- Real certificate creation and a signed install.
- Store upload, certification, or WACK.

A Windows build failure in the XAML compiler, a missing `InitializeComponent`, or a manifest schema error still needs a source fix. The core library and the package versions above are the part that was exercised here.
