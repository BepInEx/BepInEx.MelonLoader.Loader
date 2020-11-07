## BepInEx.MelonLoader.Loader

Loader for BepInEx to be able to use MelonLoader plugins.

IL2CPP only, as I am not aware of any games that are Mono-based and still use
MelonLoader

Right now this is the only way you can run MelonLoader mods/plugins on 32-bit games 🤠

#### Notes

- TLS currently does not work
- modprefs.ini is now using the BepInEx TOML config system, and is located under `BepInEx/config/MelonLoader.cfg`
- The Mods and Plugins folders have been moved to `MelonLoader/Mods` and `MelonLoader/Plugins` folders respectively

### Installation instructions

1. Install the IL2CPP preview of BepInEx from here ([server](https://discord.gg/MpFEDAg) and [message](https://discord.com/channels/623153565053222947/754333645199900723/774723954258083860) (pick the correct bitness for your game).
2. Download the [latest release](https://github.com/BepInEx/BepInEx.MelonLoader.Loader/releases) into the `BepInEx/plugins` folder (create the folder if it doesn't exist)
3. (Optional but recommended) Download the dependencies for your game's unity version [here](https://github.com/HerpDerpinstine/MelonLoader/tree/master/BaseLibs/UnityDependencies) (you can figure out the unity version by right clicking your game's .exe and checking the properties -> details -> file version)
     Extract them into your `BepInEx/unhollowed/base` folder (create it if it doesn't exist). If you've opened your game before this point, delete the `assembly-hash.txt` file in your `BepInEx/unhollowed` directory and it'll regenerate correctly.
     
### Licensing

This repo uses code adapted from MelonLoader itself, which is under the Apache 2.0 license (included in the repo)

As per licensing instructions, the changes included in this repository are:
- All references to native libraries / code removed
- Cleaned up some code
- Hollowed out some classes to act as wrappers for greater implementations in BepInEx
- BuildInfo information changed
- Removed unused libraries
- Harmony now uses a managed detour creator instead of the native Detours library

Third-party libraries used as source code or bundled in binary form:
- [Harmony](https://github.com/pardeike/Harmony) (MIT license)
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) (MIT license)