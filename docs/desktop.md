# StudioDesk for Windows: developer guide

## Where files go (for every user)

| What | Location | Protection |
|---|---|---|
| App | `C:\Program Files\PipoZa\StudioDesk\StudioDesk.exe` | Native machine code (AOT), no debug symbols. Read-only for normal users |
| App data | `C:\Program Files\PipoZa\StudioDesk\Data\*.sdpak` | AES-256-GCM encrypted packs (file names hidden too), decrypted in memory only |
| User data | `%LOCALAPPDATA%\PipoZa\StudioDesk\UserData\` (`Keys`, `Settings`, `Logs`, `Content`, `Cache`) | Every file AES-256-GCM encrypted; key wrapped with Windows DPAPI for that Windows account only |
| Exports | `Documents\StudioDesk` by default. Changeable in **Settings → Export folder** | Normal files the user owns |

The Documents folder is resolved through Windows' Known Folder API, so it works for any username and follows OneDrive automatically:
- OneDrive backing up Documents: `C:\Users\<name>\OneDrive\Documents\StudioDesk`
- No OneDrive: `C:\Users\<name>\Documents\StudioDesk`

If the chosen export folder is missing or not writable (deleted, USB removed), StudioDesk falls back to the default and tells the user. Exports never overwrite: `Report.pdf`, `Report (2).pdf`, ...

Uninstalling removes the app and `Data\`, and keeps each user's UserData and exports.

## Project layout

```
desktop/
├── src/StudioDesk.Core/      encryption, storage, settings, logs, export paths (testable on any OS)
├── src/StudioDesk.App/       WinUI 3 app (Windows only)
├── tools/StudioDesk.Packer/  build tool: makes the data key, encrypts assets into Data packs
├── tests/StudioDesk.Core.Tests/
├── assets/<pack>/            plain source files; each folder becomes Data\<pack>.sdpak
├── installer/StudioDesk.iss  Inno Setup script -> StudioDesk-Setup-x.y.z.exe
└── secrets/                  data.key lives here locally. NEVER committed
```

## One-time setup on your PC

Install Visual Studio 2022 or newer with the **.NET desktop development** and **Windows application development** workloads, plus the .NET 10 SDK.

From the `desktop` folder, create the secret data key:

```powershell
./tools/new-data-key.ps1
```

Then on GitHub: **Settings → Secrets and variables → Actions → New repository secret**, name `STUDIODESK_DATA_KEY`, value = the contents of `desktop\secrets\data.key`. Back the key up somewhere safe.

## Run locally

Open `desktop\StudioDesk.sln`, set **StudioDesk.App** as startup project, platform **x64**, press F5. The build embeds the key and creates `Data\` next to the exe automatically.

## Release a new version

```bash
git tag v0.1.0
git push origin v0.1.0
```

GitHub Actions tests, builds the native exe, encrypts the Data packs, builds the installer, and attaches `StudioDesk-Setup-0.1.0.exe` to a GitHub Release.

## Security notes

- Never commit `secrets/` or put passwords or keys in code. CI reads the key from the GitHub secret.
- Encrypted files are bound to their path or name. Editing, swapping or renaming them makes StudioDesk refuse to load them.
- Limits: because StudioDesk is offline, the Data key has to ship inside the exe (split and hidden in native code). That stops casual inspection, but a skilled, determined attacker could still recover it. User data is stronger, since its key is protected by the user's Windows login.
- Recommended before public release: a code-signing certificate, so Windows SmartScreen trusts the installer.
