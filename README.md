![](assets/pet.png)

# Klip

Klip is a desktop pet for Windows

- Follows cursor
- Remembers clipboard text
- Lives in tray
- Wobbles
- Written in C# + WinForms

![](memory-screenshot.png)

## Controls

| Action | Result |
|--------|--------|
| LMB | Push pet away from cursor |
| RMB | Open clipboard memory |
| MMB | Exit |

## Requirements

- Windows
- .NET 8 SDK (development only)
  `winget install Microsoft.DotNet.SDK.8`

## Run

`dotnet run --project src/Klip`

## Build

`dotnet publish -c Release`

Output:
`src/Klip/bin/Release/net8.0-windows/win-x64/publish/`

## License

Free to modify.
