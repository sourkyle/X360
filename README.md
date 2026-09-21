# Le Fluffie, DLC packaging branch

This branch keeps DJ Shepherd's X360 library and Le Fluffie GUI, and points the program at packaging downloadable content. The original source is an archive of the build that used to be posted at skunkiebutt.com. It is GPL-3.0. The license text is in `X360/READ ME.txt`. This branch was modified on 2026-09-21.

Fallout: New Vegas on Xbox 360 is title ID `425307E0`. A `.LIVE` file from this tool is an STFS container whose header magic is the four bytes `LIVE`. The extension is only a file name. The console reads the header.

## What changed

- Removed the in-app FATX drive browser: `FATXBrowser`, `FATXViewer`, `Drive Selector`, `FileProp`, and `GoToDialog`, and the File → FATX Explorer command. Copy packages on and off the drive with FATXplorer. Opening a FATX image in this program now says that, and does not mount the image.
- Left the X360 `FATX` library in place. `DJsIO` still has FATX stream helpers, and file-type detection still recognizes a FATX image. Nothing in the GUI browses a drive anymore.
- Package creation for STFS now starts on package type `MarketPlace` (the on-disk content type `00000002`), title ID `425307E0`, display name `New Vegas mod`, and **Dev LIVE**. Change the title ID if the package is for a different game. The Title ID box is hexadecimal.
- Dev LIVE packaging no longer requires `KV.bin`. CON signing still uses `KV.bin` next to the executable, and that choice is disabled when the file is absent. This branch does not create or extract a keyvault.
- Fixed the header editor that saved the display title into the description field. Creation also copies the display title and description into the header before it writes the package.
- Startup no longer calls skunkiebutt.com for updates, the RSS feed, or the chat page. That host is offline. Check For Updates and Refresh Chat explain that instead of hanging.

Profile editing is still in the program. Open a profile package and the same profile, achievement, and gamertag screens are there. They are not required to build a New Vegas mod package.

Disc tools are still there too: SVOD, GDF images, the music viewer, and the multi-STFS fixer. They are separate from marketplace DLC.

## What the packer actually does

Le Fluffie does not read `.esp`, `.esm`, `.bsa`, textures, or audio. `CreateSTFS` stores the files you add, writes an STFS header, and signs it.

From `X360/X360/STFS/STFSStuff.cs`:

- `PackageType.MarketPlace` is enum value `2`, which the content folder shows as `00000002`.
- `PackageMagic.LIVE` is ASCII `LIVE`. Choosing Dev LIVE uses the strong-signed LIVE key already compiled into the library (`RSAParams(StrongSigned.LIVE)`).
- A new header starts with one unlocked license entry (`STFSLicense` id `-1`). That is what lets a dev-signed marketplace package show up as owned content on a console that is not checking Xbox Live.

`Create.cs` caps an STFS package at `0xFE7DA` blocks for type 0 and `0xFD00B` blocks for type 1. Blocks are 4 KiB, so the ceiling is about 4 GiB including hash blocks. A normal plugin fits. A huge texture dump may not.

Adding files does not import a folder tree. Files dropped on the root are stored under their file name. A subfolder has to be created in the package tree first, then files added while that folder is selected. Source subfolders are not preserved automatically.

After a package is signed, do not rebuild it. Rebuild writes the header again.

## Installing a New Vegas package

The game directory from a drive backup is the title install. A typical Fallout: New Vegas folder contains `$SystemUpdate`, `Data`, `AvatarAssetPack`, `default.xex`, `Fallout.ini`, and `nxeart`. `Data` holds `FalloutNV.esm`, `update.esp`, `final_master_xml.dat`, and the retail BSA archives (`Fallout - Meshes.bsa` and the sound, texture, and voice archives), plus `Music`, `Shaders`, and `Video`.

That folder is not where a `.LIVE` mod goes. `update.esp` in that folder is the retail title update. Leave the retail ESM, title update, and BSA archives as they are.

On the content partition, copy the package to:

```text
Content\0000000000000000\425307E0\00000002\
```

`0000000000000000` is the shared offline profile directory used for dev-signed content. `425307E0` is New Vegas. `00000002` is marketplace content. Reboot, start the game, and enable the package under Downloadable Content. Use a new save when trying a plugin.

The same layout is what [frankischilling/nv360-mods](https://github.com/frankischilling/nv360-mods) documents for packages built with Le Fluffie (STFS, Marketplace, title `425307E0`, Dev LIVE).

## How far New Vegas mods can go

This was checked against the packer source and against public console-mod writeups. It was not run on a console in this branch.

**Plugin data the retail game already understands.** A `.esp` or `.esm` made in the GECK, using only vanilla functions, records, and masters the console already has, is the kind of payload this container is for. The two packages in nv360-mods (Mick and Ralph's Kid Crucified, Casino UnBanner) are NVSE-free plugins of that kind, and that repo marks them working on RGH. The game lists the package as downloadable content and loads the plugin.

**NVSE is outside this tool.** NVSE and xNVSE are PC hook libraries. They are not Xbox executables, and this program never patches `default.xex`. A plugin that calls NVSE, JIP, JohnnyGuitar, MCM, or any other extender script will not gain those functions by being wrapped in a LIVE package. nv360-mods states the same limit.

**Masters.** If the plugin masters `FalloutNV.esm` only, the retail game is enough. If it masters `update.esp` or an official add-on such as Dead Money, that master has to be the console version and it has to be installed. A PC master file is a different file.

**Textures, meshes, and sounds.** The packer will store them byte for byte. The Xbox game will not treat a PC loose-file replacer as a drop-in override of `Fallout - Textures.bsa`.

- PC `.nif` meshes and PC `.dds` textures are a different asset build from the Xbox BSA archives in `Data`. Community reports split on this. nv360-mods unpacks BSA archives and puts loose `.dds` files (DXT1, DXT3, or DXT5) inside the LIVE package. A Nexus conversion of Cheat Terminal reports that simple GECK edits were the reliable case and that custom textures and meshes were not. A Willow conversion kept extra assets in the game folder and put an ESP plus a BSA in the content package, and it needed Xbox `xma2` audio rather than PC wav.
- Because those reports disagree, a texture, mesh, or sound replacer is unproven until it is enabled on the console. Plan on a plugin-only package working, and plan on asset replacements needing a format pass and a hardware test.
- Sound files the PC game plays as wav, mp3, or ogg have to be converted to the Xbox XMA2 / xWMA data the 360 uses before they are added. This repository does not include that encoder.
- Replacing a file inside `Fallout - Meshes.bsa` or the other retail archives is an edit of the game install in FATXplorer. It is a different job from building a LIVE package, and this program does not open those archives.

**What you can put in one package**

| Payload | What this branch can do with it |
| --- | --- |
| NVSE-free `.esp` / `.esm` using vanilla records | Store it at the package root and sign it as marketplace DLC |
| Plugin that needs an official add-on master | Same container, only if that console DLC is already installed |
| NVSE, script extenders, ENB, menu frameworks | No path in this program |
| Loose textures or meshes | Stored as-is; the game may ignore them until the assets are Xbox-format and the paths match what that plugin loads |
| PC wav / mp3 / ogg | Stored as-is; convert to XMA2 before packing if the game is supposed to play them |
| Retail BSA set from the backup | Leave it in the game `Data` folder |

## Build

`X360/X360.sln` builds with Mono xbuild (`Release`). That library is the packer. It compiled on this branch with two pre-existing warnings and no errors.

Le Fluffie targets .NET Framework 3.5 and references `DevComponents.DotNetBar2`, which is not in this repository. The project file points at a local Visual Studio 2008 path. On a Windows machine with that assembly, open `Le Fluffie/Le Fluffie.sln`.

The GUI was not compiled here. Mono also stops while embedding `Properties/Resources.resx`, because resgen treats a libpng profile warning in the bundled images as a failure. The C# sources were type-checked against the built `X360.dll`; the only missing types were that DotNetBar assembly. This branch was not run on a console.
