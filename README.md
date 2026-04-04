# Klip

![](src/Klip/assets/pet.png)

Klip is a desktop pet for Windows.

- Follows the cursor
- Remembers clipboard text
- Lives in the system tray
- Wobbles
- Written in C# and WinForms

![](docs/memory-screenshot.png)

## Usage

### Download

Download the latest portable version from GitHub Releases:

https://github.com/tupinumboor/pet/releases

### Controls

| Action | Result |
|--------|--------|
| LMB | Pushes the pet away from the cursor |
| RMB | Opens clipboard memory |
| MMB | Exits the app |

## Development

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
