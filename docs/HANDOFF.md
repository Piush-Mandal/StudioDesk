# StudioDesk: project handoff

Paste this into a new Claude session to continue the project.

## The project
- **StudioDesk** by **PipoZa**: one app for **Windows desktop** and **Android**, each in its own native language.
  - Windows: C# · .NET 10 · WinUI 3 · Native AOT (compiled to machine code, harder to reverse engineer)
  - Android: Kotlin · Jetpack Compose (not started yet)
- **Fully offline.** No server, no accounts.
- Style: extremely simple, professional, expressive.
- Security: as strong as practical. Everything the app stores is encrypted and only readable through the app. Nothing is truly impossible to crack, and the user has been told that.
- **Features are not decided yet.** The user will provide them.

## Decisions already made
- **Distribution:** a single installer `StudioDesk-Setup-x.y.z.exe` (Inno Setup, compressed) on **GitHub Releases**, built by GitHub Actions when a tag `vX.Y.Z` is pushed. Also built locally.
- **Install location (option A, chosen by the user):**
  - App: `C:\Program Files\PipoZa\StudioDesk\StudioDesk.exe`
  - App data: `C:\Program Files\PipoZa\StudioDesk\Data\*.sdpak`, AES-256-GCM encrypted packs (file names hidden), decrypted in memory only. The key is split into two parts and hidden inside the exe at build time.
  - User data: `%LOCALAPPDATA%\PipoZa\StudioDesk\UserData\` with subfolders `Keys`, `Settings`, `Logs`, `Content`, `Cache`. Every file is AES-256-GCM encrypted and bound to its path; the master key is protected by Windows DPAPI for that Windows account. It lives here, not in Program Files, because Program Files is read-only for normal users.
- **Default export folder:** `Documents\StudioDesk` for the current Windows user, found through the Windows Known Folder API, so it works for any username and follows OneDrive automatically (e.g. `C:\Users\Piush_Mandal\OneDrive\Documents\StudioDesk`). Users can change it in Settings. It falls back to the default if the chosen folder is unavailable. Exports are not encrypted and never overwrite existing files ("Name (2).ext").
- Uninstall keeps UserData and exports.
- The data key (`desktop/secrets/data.key`) is **never committed**. CI reads it from the GitHub secret `STUDIODESK_DATA_KEY`.

## What is built (repo Piush-Mandal/StudioDesk, branch `claude/cool-rubin-h0jzkh`)
- `desktop/src/StudioDesk.Core`: encryption (AesGcmCipher, MasterKey + DPAPI, DataPack), EncryptedStore, settings, encrypted logs, ExportLocation. 12 passing tests in `desktop/tests`.
- `desktop/src/StudioDesk.App`: WinUI 3 app with Mica window, Home page (greets the user) and Settings page (theme, export folder, privacy). **Not yet compiled.** It needs a Windows machine; the first real build is still to come.
- `desktop/tools/StudioDesk.Packer`: build tool that creates the key, encrypts `desktop/assets/*` into Data packs, and embeds the key.
- `desktop/installer/StudioDesk.iss`: Inno Setup script.
- `.github/workflows/desktop-release.yml`: test, publish, build installer, GitHub Release.
- Full guide: `docs/desktop.md`

## Where we stopped
- The user's PC folder `D:\Projects\StudioDesk` was not a git clone. The cloud session couldn't use the PC through Remote Control: folders showed "not_available", probably because the Windows Claude app needs an update.
- Still to do on the PC: clone the repo, install the .NET 10 SDK and Visual Studio, create the key (`desktop/tools/new-data-key.ps1`), add the GitHub secret `STUDIODESK_DATA_KEY`, then build and run.
- Next: the user sends the features, then start the Android app.

## User preferences
- Simple English explanations, step by step.
- The user wants Claude to do the work, opening apps and running things itself where possible.
