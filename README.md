# Alice Magatroid — Slay the Spire 2 Mod

A playable Alice Magatroid character mod for **Slay the Spire 2**, built with Godot 4 + C#.

## Requirements

- Windows 10/11
- [Slay the Spire 2](https://store.steampowered.com/app/2581800/Slay_the_Spire_2/) installed via Steam
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- PowerShell 5.1+ (built-in on Windows)

Godot is included in this repository as a portable build (`_godot/`) — no separate installation needed.

## Quick Start

1. Clone the repository:
   ```bash
   git clone --recursive https://github.com/elabalice-code/sts2_alicemagatroid_elabalice.git
   cd sts2_alicemagatroid_elabalice
   ```

2. Copy `user_settings.example.json` to `user_settings.json` and edit it with your paths:
   ```json
   {
     "GameRoot": "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Slay the Spire 2",
     "BaseLibPath": "<full-path-to-BaseLib.dll>"
   }
   ```

3. Run the build script:
   ```powershell
   .\build.ps1
   ```

4. The mod will be deployed to your game's `Mods/` directory automatically.

## License

This project is licensed under [GPL v3](LICENSE).

**Non-commercial, fan-made mod. Not affiliated with Mega Crit Studios or Steam.**
