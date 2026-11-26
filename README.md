# Stardeus Mods

A small collection of native, Harmony-free mods for **Stardeus**, plus a fix
that restores Harmony patching on native Apple Silicon macOS.

**Target game version: Stardeus 0.15.23** (built and tested against it).
These are unofficial mods. Back up your saves before installing.

## Mods

| Mod | Version | Summary |
| --- | --- | --- |
| [AutoAllowGroundItems](#autoallowgrounditems) | 1.0.2 | Clears the Forbid flag on loose items and material piles. |
| [AutoRebuildMiner](#autorebuildminer) | 1.0.1 | Queues a normal rebuild blueprint when an Autodrill Rig is picked up. |
| [AutoUnlockDoors](#autounlockdoors) | 1.0.2 | Unlocks any locked door, including lockdown-locked doors. |
| [NoAutoForbid](#noautoforbid) | 1.3.1 | Stops the game auto-forbidding dropped/stripped/derelict/space loot. |
| [QuietWarp](#quietwarp) | 1.0.1 | Removes the "wait for Copters" hyperjump warning. |
| [ReplenishingDeposits](#replenishingdeposits) | 1.0.1 | Resource deposits slowly refill depth while idle. |
| [StasisArrayNoLimit](#stasisarraynolimit) | 1.0.1 | Removes the 15-sleeper cap from the Stasis Array derelict event. |
| [HarmonyAppleSiliconFix](#harmonyapplesiliconfix) | 1.0.0 | Restores Harmony mods on native Apple Silicon (Unity 6000.6+). |

## Installation

1. **Close Stardeus.**
2. Open your user Mods folder:
   - **macOS:** `~/Library/Application Support/com.Kodo-Linija.Stardeus/Mods/`
   - **Windows / Linux:** in game, go to **Main Menu → Mods → About Mods →
     Open User Mods Directory**. This is the folder next to your Saves.
   - Do **not** install into the game's `Stardeus.app/Contents/Resources/...
     /StreamingAssets/Mods` folder. That one is for official mods only.
3. Download the release zip(s) for the mods you want from the
   [Releases](../../releases) page.
4. Extract each zip so the mod folder sits **directly** under `Mods/`, for
   example:

   ```
   Mods/
     AutoAllowGroundItems/
       ModInfo.json
       Libraries/
         AutoAllowGroundItems.dll
   ```

   Do not leave a second nested folder.
5. Start Stardeus, open **Main Menu → Mods**, and **enable** the mods you
   installed. New mods are disabled by default.
6. Load your save.

### Which file do I need?

Each release asset is a self-contained mod zip. Download only the mods you
want:

| Download | Contains |
| --- | --- |
| `AutoAllowGroundItems.zip` | `AutoAllowGroundItems/ModInfo.json`, `Libraries/AutoAllowGroundItems.dll` |
| `AutoRebuildMiner.zip` | `AutoRebuildMiner/ModInfo.json`, `Libraries/AutoRebuildMiner.dll` |
| `AutoUnlockDoors.zip` | `AutoUnlockDoors/ModInfo.json`, `Libraries/AutoUnlockDoors.dll` |
| `NoAutoForbid.zip` | `NoAutoForbid/ModInfo.json`, `Libraries/NoAutoForbidNative.dll`, `Patches/Tunables/Resources_NoAutoForbid.json` |
| `QuietWarp.zip` | `QuietWarp/ModInfo.json`, `Libraries/QuietWarp.dll` |
| `ReplenishingDeposits.zip` | `ReplenishingDeposits/ModInfo.json`, `Config.json`, `Libraries/ReplenishingDeposits.dll` |
| `StasisArrayNoLimit.zip` | `StasisArrayNoLimit/ModInfo.json`, `Libraries/StasisArrayNoLimit.dll` |
| `HarmonyAppleSiliconFix.zip` | `HarmonyAppleSiliconFix/ModInfo.json`, `Libraries/` (3 files), `LICENSE`, `THIRD-PARTY-NOTICES.md` |

If you use any third-party Harmony-based mod on an Apple Silicon Mac with
Stardeus 0.15.14 or newer, also install **HarmonyAppleSiliconFix**. The seven
other mods in this repository do not need it.

## Mod descriptions

### AutoAllowGroundItems

Automatically clears the **Forbid** flag on items and material piles lying on
the ground, so they can be hauled. Checks every 60 game ticks (one game hour).
Does not touch items in storage or being carried.

### AutoRebuildMiner

When an **Autodrill Rig** is picked up for deployment, automatically queues a
normal rebuild blueprint at the same spot. The replacement costs the normal
materials and builder work.

### AutoUnlockDoors

Every 60 game ticks, automatically unlocks any door that is locked, including
doors locked by the mental-breakdown **Lockdown** action.

### NoAutoForbid

Stops Stardeus from auto-forbidding:

- weapons dropped by incapacitated beings
- stripped equipment
- asteroid/meteor body loot
- derelict resources
- (optionally) items you move yourself

Console command: `noforbid [dropped|stripped|spaceloot|derelict|moved]
[on|off]`. `on` means the game forbids that thing. Settings are saved per game.

### QuietWarp

Removes the "wait for Copters" warning from the warp action. Warp is still
queued immediately and starts automatically once copter missions (including
mining-drill deployments) finish. Other warp blockers still show their
warnings.

### ReplenishingDeposits

Resource deposits slowly restore their depth while idle, so mining does not
permanently stop when a drill or shuttle reaches maximum depth. Configure it in
`ReplenishingDeposits/Config.json`:

```json
{
    "RefillKmPerDay": 5.0,
    "IdleHoursBeforeRefill": 6,
    "ResetDifficultyOnFullRefill": true
}
```

Set `RefillKmPerDay` to `0` to disable refilling.

### StasisArrayNoLimit

Removes the 15-sleeper cap from the **Derelict Ship With Stasis Array** story
event, so the event can still appear after you already have 15 or more
sleepers. All other requirements are unchanged.

### HarmonyAppleSiliconFix

Stardeus 0.15.14 upgraded to Unity 6000.6, whose Mono runtime keeps JIT code in
`MAP_JIT` pages. Harmony/MonoMod's old `mprotect`-based detours cannot write
those pages (`mprotect returned EACCES`), so every Harmony-based mod fails on
native Apple Silicon.

This mod applies the [AppleSiliconHarmony](https://github.com/anatawa12/AppleSiliconHarmony)
W^X patch at startup, before any mod uses Harmony. It routes detour writes
through a native `pthread_jit_write_protect_np` helper.

- **Platform:** macOS on Apple Silicon (arm64) only. It does nothing under
  Rosetta or on Intel/Windows/Linux.
- **Files:** keep all three files in `Libraries/`:
  `HarmonyAppleSiliconFix.dll`, `AppleSiliconHarmony.dll`,
  `libapple_silicon_harmony_native.dylib`.
- **Remove it:** delete the folder or disable the mod. Then Harmony-based mods
  will fail again on native arm64.

## Building from source

The build scripts target macOS with Stardeus installed in
`/Applications/Stardeus.app` and a .NET SDK at `~/.dotnet/dotnet`:

```sh
./AutoAllowGroundItems/build.sh
./HarmonyAppleSiliconFix/build.sh
# ...and so on
```

Set `MANAGED` to override the game's Managed assemblies folder. Output DLLs go
to each mod's `Libraries/` folder.

Source is provided for every mod so it can be audited and rebuilt.

## License

- All original mods in this repository are licensed under the **MIT License**
  (see [LICENSE](LICENSE)).
- **HarmonyAppleSiliconFix** is licensed under the **Mozilla Public License
  2.0** (see [HarmonyAppleSiliconFix/LICENSE](HarmonyAppleSiliconFix/LICENSE))
  because it redistributes AppleSiliconHarmony binaries. See
  [HarmonyAppleSiliconFix/THIRD-PARTY-NOTICES.md](HarmonyAppleSiliconFix/THIRD-PARTY-NOTICES.md)
  for attribution and source locations.

Stardeus and its assets belong to Kodo Linija. These mods contain no Stardeus
game code or assets and are not endorsed by Kodo Linija.
