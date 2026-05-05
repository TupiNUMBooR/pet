# Klip

<image src="docs/itch-picture.png" height="200">

Klip is a small desktop pet for Windows that follows your cursor and remembers your clipboard.

![Release](https://github.com/TupiNUMBooR/pet/actions/workflows/release.yml/badge.svg)
![Latest Release](https://img.shields.io/github/release/TupiNUMBooR/pet)
![Release Date](https://img.shields.io/github/release-date/TupiNUMBooR/pet)

![Top Lang](https://img.shields.io/github/languages/top/TupiNUMBooR/pet?logo=csharp)
![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![WinForms](https://img.shields.io/badge/UI-WinForms-blue)
![itch.io](https://img.shields.io/badge/deploy-itch.io-blue?logo=itchdotio)

## Features

- Follows the cursor
- Shows clipboard text or images
- Lets you edit clipboard text (plain text only)
- Lives in the system tray
- Wobbles

![](docs/memory-screenshot-2.png)

## Usage

### Download

Prebuilt portable version available on [itch.io](https://tupinumboor.itch.io/klip)

### Controls

| Action | Result |
|--------|--------|
| LMB | Pushes the pet away from the cursor |
| RMB | Opens clipboard memory |
| MMB | Exits the app |

## Development

Klip is written in C# and WinForms

Requirements for building or modifying Klip:

- Windows
- .NET 8 SDK
  `winget install Microsoft.DotNet.SDK.8`

### Run

`dotnet run --project src/Klip`

### Build

`dotnet publish -c Release`

Output:

`src/Klip/bin/Release/net8.0-windows/win-x64/publish/`

## License

Free to modify.

Cat Purr Sound Effect by <a href="https://pixabay.com/users/dragon-studio-38165424/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=482870">DRAGON-STUDIO</a> from <a href="https://pixabay.com//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=482870">Pixabay</a>
