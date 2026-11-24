I'm continuing my project "StudioDesk" from an earlier Claude session. Below is EVERYTHING from that session: my preferences, every question and answer, every decision, the exact technical design, what was built, what is still open, and the known issues. Treat it as the full history. Do not change any decision without asking me.

============================================================
1. ABOUT ME AND HOW I WANT YOU TO WORK
============================================================
- My name / Windows username: Piush_Mandal. Brand / company name: PipoZa.
- My PC: Windows. Project folder: D:\Projects\StudioDesk
- Explain things to me in simple English, step by step, with each command in its own block.
- If I ask you to do work (organize folders, open apps, set things up), do it yourself where possible, like a human would (you may open and view things in a browser like Chrome). Only ask me when something truly needs me (approval pop-ups, passwords, secrets).
- I want 0 issues and 0 bugs. Test everything you can, and tell me honestly what was and wasn't tested.
- The software must be highly personalized for each user.
- I will tell you the features later. Don't invent features.

============================================================
2. QUESTIONS AND ANSWERS FROM THE EARLIER SESSION (in order)
============================================================
Q/A 1. Me: "We will make new software that runs on Windows desktop and Android mobile. Each uses its native language. Features later."
  -> Decided stack: Windows = C# (.NET) + WinUI 3 (Windows App SDK). Android = Kotlin + Jetpack Compose.
  -> Repo layout: desktop/ (Windows), android/ (Android), docs/, README.md.
  -> No code is shared between the two apps (different languages).

Q/A 2. Me: "Desktop files saved both locally and on GitHub so users can download my .exe. Extremely simple, professional and expressive. Both apps extremely secure, impossible to hack or reverse engineer."
  -> Answer given: nothing is truly impossible to hack or reverse engineer. The goal is to make it very hard and keep nothing valuable in the app.
  -> Windows: .NET Native AOT (native machine code instead of decompilable .NET IL), code-signed exe (recommended, not yet set up).
  -> Android (planned): R8 shrinking + obfuscation, Android Keystore, Play Integrity API, encrypted storage.
  -> The .exe goes on GitHub Releases, built by GitHub Actions.
  -> Warning given: my GitHub repo Piush-Mandal/StudioDesk is PUBLIC, so the source code is readable by anyone. Making it private was suggested. I HAVE NOT ANSWERED THIS YET (open question).

Q/A 3. Me: "Users download an .exe with compressed files. Running it installs the app to C:\Program Files\PipoZa\StudioDesk\Data (files needed to run, HIGHLY ENCRYPTED) and C:\Program Files\PipoZa\StudioDesk\UserData (settings, things, logs, etc.). Other subfolders OK, but encrypted and only accessible through the software."
  Me: "It will work offline only."
  -> Installer: Inno Setup 6, single compressed StudioDesk-Setup-x.y.z.exe.
  -> Fully offline: no server, no accounts, no internet.
  -> Problem explained: Program Files is read-only for normal users, so UserData can't live there without admin.
  -> Option A: app + Data in Program Files, UserData in %LOCALAPPDATA%\PipoZa\StudioDesk\UserData (per Windows user).
  -> Option B: UserData in Program Files with write permission for all users (shared, worse).
  -> Limit explained: because the app is offline, the Data key must ship inside the exe, so a skilled attacker could eventually extract it. Admins can still delete or copy files (but can't read them).

Q/A 4. Me: "I choose Option A."

Q/A 5. Me: "Default export folder should be C:\Users\Piush_Mandal\OneDrive\Documents\StudioDesk. Check whether OneDrive is enabled and the username too. Use locations properly for every user, and it must be changeable."
  -> Default export folder = [user's Documents]\StudioDesk, where Documents comes from the Windows Known Folder API (Environment.SpecialFolder.MyDocuments). This handles every username, OneDrive backup on or off, and redirected Documents automatically. No hard-coded paths.
     OneDrive on: C:\Users\<name>\OneDrive\Documents\StudioDesk
     OneDrive off: C:\Users\<name>\Documents\StudioDesk
  -> If Windows returns nothing, fall back to %USERPROFILE%\Documents\StudioDesk.
  -> Changeable in Settings (Change, Open folder, Reset to default). A chosen folder is checked by actually writing a test file. If it later becomes unavailable (deleted, USB removed, no permission), exports go to the default and the user is warned.
  -> Exports are normal unencrypted files the user owns. They never overwrite: "Name.ext", then "Name (2).ext", and so on. Invalid filename characters are replaced with "_".

Q/A 6. After code was pushed, I tried "git fetch" in D:\Projects\StudioDesk and got "not a git repository". The folder was never cloned. I was told to clone instead:
     cd D:\Projects
     git clone -b claude/cool-rubin-h0jzkh https://github.com/Piush-Mandal/StudioDesk.git StudioDesk
  (If the folder isn't empty: Rename-Item StudioDesk StudioDesk-old first.) Use normal PowerShell, not Administrator.

Q/A 7. Remote Control (to let the cloud session work on my PC): I turned on "Connect new sessions to Remote Control" and "Let your phone and claude.ai start sessions here", pinned D:\Projects\StudioDesk, and turned Worktree OFF. All my folders still showed "not_available" to cloud sessions, probably because the Windows Claude app needs an update. Not solved. Workaround: start a Claude Code session in the desktop app, opened in D:\Projects\StudioDesk.

============================================================
3. FINAL FILE LOCATIONS (decided)
============================================================
- App:        C:\Program Files\PipoZa\StudioDesk\StudioDesk.exe   (admin-installed, read-only for users)
- App data:   C:\Program Files\PipoZa\StudioDesk\Data\*.sdpak      (encrypted)
- User data:  %LOCALAPPDATA%\PipoZa\StudioDesk\UserData\           (encrypted, per Windows user)
                 Keys\  Settings\  Logs\  Content\  Cache\
- Exports:    [Documents]\StudioDesk (changeable), not encrypted
- Uninstall removes the app and Data\. Each user's UserData and exports are KEPT.

============================================================
4. EXACT TECHNICAL DESIGN (rebuild exactly like this)
============================================================
Versions:
- .NET 10 (LTS). Library TFM net10.0. App TFM net10.0-windows10.0.19041.0, TargetPlatformMinVersion 10.0.17763.0.
- Microsoft.WindowsAppSDK 1.8.260921001 (stable 1.8 line, used on purpose instead of 2.x).
- Microsoft.Windows.SDK.BuildTools 10.0.26100.8249
- System.Security.Cryptography.ProtectedData 10.0.15
- Tests: xunit 2.9.3, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.11.1
- Directory.Build.props: Company PipoZa, Product StudioDesk, Version 0.1.0, Nullable enable, ImplicitUsings enable, TreatWarningsAsErrors true, Deterministic true, DebugType none in Release (no .pdb shipped).

Folder layout:
desktop/
  StudioDesk.sln
  Directory.Build.props
  .gitignore (bin/ obj/ out/ publish/ *.user .vs/)
  secrets/.gitignore (ignores everything except itself; data.key lives here, NEVER committed)
  assets/<packname>/...   plain source files; each top-level folder becomes Data\<packname>.sdpak
     assets/core/app.json = {"appName":"StudioDesk","tagline":"Your creative desk."}
  src/StudioDesk.Core/    (net10.0, IsAotCompatible, InternalsVisibleTo StudioDesk.Core.Tests)
  src/StudioDesk.App/     (WinUI 3 app, AssemblyName StudioDesk)
  tools/StudioDesk.Packer/ (build-time console tool, not shipped)
  tools/new-data-key.ps1
  tests/StudioDesk.Core.Tests/
  installer/StudioDesk.iss
.github/workflows/desktop-release.yml
docs/desktop.md, docs/HANDOFF.md
README.md

Encryption (Core/Security):
- AesGcmCipher: AES-256-GCM, 32-byte key, random 12-byte nonce, 16-byte tag.
  File layout: [magic 4 bytes][version 1 = 0x01][nonce 12][tag 16][ciphertext].
  AAD = first 5 header bytes + a caller "context" byte string, so tampering, wrong key, wrong context, or moving or renaming a file all make decryption fail.
- MasterKey: random 32-byte per-user key at UserData\Keys\master.key, stored only DPAPI-wrapped (DataProtectionScope.CurrentUser, entropy "PipoZa.StudioDesk.UserMasterKey.v1"). Subkeys via HKDF-SHA256, salt "StudioDesk.v1", info = purpose string. Key memory is zeroed on dispose.
- IKeyProtector interface + DpapiKeyProtector (Windows only). Tests use a fake protector so they run on any OS.
- AtomicFile: write to "<path>.tmp", then rename (no half-written files after a crash).
- DataPack: magic "SDPK", extension .sdpak. Plaintext is a ZIP (CompressionLevel.SmallestSize, files in sorted order, all timestamps set to 2000-01-01 to hide build times). Context = "pack:" + lowercase pack name, so a renamed pack fails. Decrypted only in memory. Create(sourceDir, packPath, key) is used by the packer; Open(packPath, key) + ReadBytes/ReadText(entry) are used by the app.

Storage (Core/Storage):
- EncryptedStore(userDataDir, masterKey): subkey purpose "UserData.Files", magic "SDUD". On-disk file = <relative path> + ".sdx". Context = relative path with "\" changed to "/", trimmed, lowercased. Blocks paths that escape UserData (throws UnauthorizedAccessException). Methods: Exists, WriteBytes, ReadBytes, WriteText, ReadText, Delete.
- AppSettings (System.Text.Json source generator, AOT-safe, enums as strings): Theme (System/Light/Dark, default System), Language "en", WindowWidth?, WindowHeight?, ExportDirectory? (null = default). Saved encrypted at Settings/settings.json.
- EncryptedLog: one encrypted file per session at Logs/yyyyMMdd-HHmmss-<processId>.log (UTC), keeps the newest 20, levels INFO/WARN/ERROR. Never logs passwords, keys or personal content.
- ExportLocation: described in Q/A 5. Methods: DefaultDirectory, Resolve(customDir, out usedFallback), static Validate(dir) (returns an error message or null), static UniqueFilePath(dir, fileName).
- AppPaths: InstallDir = app folder, DataDir = InstallDir\Data, UserDataDir = %LOCALAPPDATA%\PipoZa\StudioDesk\UserData, plus Keys/Settings/Logs/Content/Cache folders. EnsureUserDirectories() creates them.

Packer tool commands (dotnet run --project tools/StudioDesk.Packer -c Release -- ...):
- new-key <keyFile>: writes a random 32-byte key as base64. Refuses to overwrite.
- pack <assetsDir> <outDir> <keyFile>: each top-level folder becomes <name>.sdpak
- emit-key <keyFile> <out.cs>: generates namespace StudioDesk.App.Generated, internal static class EmbeddedKeys with two random byte arrays A and B. DataKey() returns A XOR B (the key is never stored whole).
- The key is read from env var STUDIODESK_DATA_KEY if set, otherwise from the key file.

App project (StudioDesk.App.csproj):
- WinExe, Platforms x64;ARM64, RIDs win-x64;win-arm64, UseWinUI, AllowUnsafeBlocks, WindowsPackageType None (unpackaged), WindowsAppSDKSelfContained true, SelfContained true.
- Security/size: PublishAot true, PublishTrimmed true, IlcGenerateStackTraceData false, StackTraceSupport false, UseSystemResourceKeys true, EventSourceSupport false, DebuggerSupport false in Release.
- MSBuild targets: EmbedDataKey (BeforeBuild) runs packer emit-key into obj\EmbeddedKeys.g.cs and compiles it. PackDataAfterBuild and PackDataAfterPublish run packer pack into $(OutDir)Data and $(PublishDir)Data.
- app.manifest: asInvoker (app never needs admin), PerMonitorV2 DPI, Windows 10/11 supportedOS.

App UI:
- App.xaml: XamlControlsResources. App.xaml.cs: starts AppServices. If that throws CryptographicException, FileNotFoundException or DirectoryNotFoundException, it shows a native Windows error box: "StudioDesk's files are missing or have been modified. Please reinstall StudioDesk." and exits. Unhandled exceptions are logged.
- AppServices: paths, MasterKey (DPAPI), EncryptedStore, EncryptedLog, SettingsService, Settings, ExportLocation, opens Data\core.sdpak and reads the tagline from app.json, ResolveExportDirectory(), SaveSettings(), Dispose(). Uses MessageBoxW via LibraryImport.
- MainWindow: Mica backdrop, custom title bar (48px, accent icon glyph E7F4 + "StudioDesk"), NavigationView LeftCompact with Home + built-in Settings. Remembers window size (default 1200x760, clamped to 800-4000 x 560-3000) and theme. On close: saves size, logs, disposes.
- HomePage: big greeting "Good morning/afternoon/evening, <Windows username with _ shown as a space>", the tagline, and a card: "Ready when you are. Features are on the way. Your data stays on this PC, encrypted and private."
- SettingsPage: Theme radio buttons (Use Windows setting / Light / Dark, applied instantly); Export folder card (shows the path, Change… via FolderPicker, Open folder via Explorer, Reset to default, InfoBar messages); Privacy card (offline + encrypted text, UserData path); version number.

Tests (all 12 passed on Linux with .NET 10.0.115): cipher roundtrip + tamper detection; wrong context or key rejected; store files unreadable on disk and survive a restart; swapped files detected; path escape blocked; settings roundtrip; log encrypted; DataPack roundtrip, hidden file names, rename detected; export default uses Documents; falls back to profile\Documents; custom folder used and bad one falls back; unique file path never overwrites.

Installer (installer/StudioDesk.iss, Inno Setup 6):
- AppId {6F3B2C1E-8A4D-4E2B-9C71-5D0A2F8E4B13}, AppName StudioDesk, AppPublisher PipoZa
- DefaultDirName {autopf}\PipoZa\StudioDesk, DisableDirPage yes (fixed location), PrivilegesRequired admin
- x64compatible, MinVersion 10.0.17763, Compression lzma2/ultra64, SolidCompression, WizardStyle modern
- Output ..\out\installer\StudioDesk-Setup-<version>.exe. Defines AppVersion and PublishDir (default ..\out\publish).
- Start Menu shortcut, optional desktop shortcut task, CloseApplications yes
- "Launch StudioDesk" after install uses runasoriginaluser, so the app's data and keys belong to the real user, not the admin.

Release pipeline (.github/workflows/desktop-release.yml):
- Triggers: push of a tag v* (e.g. git tag v0.1.0 then git push origin v0.1.0) or manual run. permissions: contents write. Runs on windows-latest in the desktop folder.
- Steps: checkout@v4, setup-dotnet@v4 (10.0.x), fail if secret STUDIODESK_DATA_KEY is missing, version from the tag (or 0.0.0-dev), dotnet test, dotnet publish src/StudioDesk.App -c Release -r win-x64 -p:Platform=x64 -p:Version=... -o out/publish, verify StudioDesk.exe + Data/core.sdpak exist and no .pdb files, build the installer with ISCC (choco install innosetup if missing), upload-artifact@v4, and on tags create a GitHub Release with softprops/action-gh-release@v2.

One-time setup on my PC (not done yet):
1. Clone the repo into D:\Projects\StudioDesk (see Q/A 6).
2. Install the .NET 10 SDK (x64) and Visual Studio 2022+ Community with the ".NET desktop development" and "Windows application development" workloads.
3. In D:\Projects\StudioDesk\desktop run: dotnet run --project tools/StudioDesk.Packer -c Release -- new-key secrets/data.key
4. GitHub repo, Settings, Secrets and variables, Actions, New repository secret: name STUDIODESK_DATA_KEY, value = the contents of data.key.
5. Back up data.key safely (if it's lost, make a new one; future builds just use the new key).
6. Open desktop\StudioDesk.sln, startup project StudioDesk.App, platform x64, press F5.

============================================================
5. STATUS
============================================================
- Earlier code is on GitHub: Piush-Mandal/StudioDesk, branch claude/cool-rubin-h0jzkh (2 commits). If that repo still exists, use it. If it's gone, rebuild from this spec.
- Done: Core library + 12 passing tests, packer tool, WinUI app code, installer script, workflow, docs.
- The WinUI app has NEVER BEEN COMPILED (the old session ran on Linux). The first Windows build may need small fixes. Do this first and fix any errors.
- Android app: NOT STARTED.
- Features: NOT GIVEN YET.

============================================================
6. KNOWN ISSUES TO FIX
============================================================
a) If UserData\Keys\master.key is damaged, the app says "please reinstall", but reinstalling doesn't fix UserData. It should show a clear message and offer to reset user data.
b) Code signing isn't set up. Without a certificate, Windows SmartScreen warns users about the installer.
c) The GitHub repo is public, so the source code is visible to everyone.
d) No app icon or branding images yet.
e) Only an x64 installer is built (the ARM64 project config exists, but no ARM64 installer).
f) EncryptedLog rewrites the whole session file on every line. That's fine for now, but should be improved if logs get large.
g) Remote Control folders show "not_available" for cloud sessions (Claude app on Windows probably needs an update).

============================================================
7. OPEN QUESTIONS FOR ME (ask me these)
============================================================
1. What are the features of StudioDesk?
2. Should the GitHub repo be private (to protect the source code), with releases shared publicly?
3. Do I want to buy a code-signing certificate?
4. App icon, colors and branding?
5. Languages besides English?
6. Android: same features as desktop? Where should Android exports go?
7. Should uninstall offer to delete user data, or always keep it?
