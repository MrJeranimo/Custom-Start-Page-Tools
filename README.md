# Custom Start Page Tools

This mod replaces the original KSA start screen with a custom start page to provide a better starting experience.

This includes, checking and updating your Mod Manifest from the start screen, loading a Save from the start screen, Seeing a Save's info when selected, Warnings for mismatches when loading a save, and more to come.

This mod also replaces the in-game Save/Load window with a custom one that is similar to the one on the start screen. It provides extra warnings about if the Save you are about to load has a different Celestial System and/or different Game Type than what is currently loaded.

**License:** GNU General Public License v3

**Required Mod Loader:** (StarMap)[https://github.com/StarMapLoader/StarMap/releases/tag/0.4.5]

## Installation

If you install the Release, extract the `Custom Start Page Tools.zip` to `Documents\My Games\Kitten Space Agency\mods` and make sure to add

```toml
[[mods]]
id="Custom Start Page Tools"
enabled=true
```

to `\Documents\My Games\Kitten Space Agency\manifest.toml`.

## Adding to the SaveInfoWindow

There is a function called `DrawModHook()` in the `StartStagePatch` class in this mod that is intended to allow for other mods to add their own Save Info into the prebuilt window. Just use a Harmony Patch to get the `SelectedSave` from `StartStagePatch` and to patch into the method.
