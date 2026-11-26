using System;
using Game.Data;
using Game.Systems.Policies;
using KL.Utils;

namespace NoAutoForbid {
    /// <summary>
    /// The five options of the mod. A true value means the game forbids the thing
    /// automatically. Defaults match the mod's purpose: only "moved by player" keeps the
    /// base game behaviour. NoAutoForbidSys saves and loads the values per save.
    /// </summary>
    public static class NoForbidSettings {
        public const string Dropped = "dropped";
        public const string Stripped = "stripped";
        public const string SpaceLoot = "spaceloot";
        public const string Derelict = "derelict";
        public const string Moved = "moved";

        public static readonly string[] AllOptions =
            { Dropped, Stripped, SpaceLoot, Derelict, Moved };

        public static bool ForbidDropped;
        public static bool ForbidStripped;
        public static bool ForbidSpaceLoot;
        public static bool ForbidDerelict;
        public static bool ForbidMoved = true;

        public static void ResetDefaults() {
            ForbidDropped = false;
            ForbidStripped = false;
            ForbidSpaceLoot = false;
            ForbidDerelict = false;
            ForbidMoved = true;
        }

        public static bool IsValid(string option) {
            return Array.IndexOf(AllOptions, option) >= 0;
        }

        public static bool Get(string option) {
            switch (option) {
                case Dropped: return ForbidDropped;
                case Stripped: return ForbidStripped;
                case SpaceLoot: return ForbidSpaceLoot;
                case Derelict: return ForbidDerelict;
                case Moved: return ForbidMoved;
                default: return false;
            }
        }

        public static void Set(string option, bool value) {
            switch (option) {
                case Dropped: ForbidDropped = value; break;
                case Stripped: ForbidStripped = value; break;
                case SpaceLoot: ForbidSpaceLoot = value; break;
                case Derelict: ForbidDerelict = value; break;
                case Moved: ForbidMoved = value; break;
            }
            Apply();
        }

        /// <summary>
        /// Pushes the four tunable-backed options into the game's tunables, which the game
        /// reads when the action happens. "Moved" is no tunable, MovedPatches reads it.
        /// Failures are logged and never thrown: this runs while a save is loading.
        /// </summary>
        public static void Apply() {
            try {
                Tunable.SetBool("AutoForbidDroppedWeapons", ForbidDropped);
                Tunable.SetBool("AutoForbidStrippedEquipment", ForbidStripped);
                Tunable.SetBool("AutoForbidSpaceBodyLoot", ForbidSpaceLoot);
                Tunable.SetBool("AutoForbidDerelictResources", ForbidDerelict);
                // The Equipment policy default "Forbid Dropped Weapon" is cached by the
                // game from the first tunable, so rebuild the cache.
                EquipmentPolicyDefaults.Reset();
            } catch (Exception e) {
                D.Err("NoAutoForbid: could not apply options: " + e);
            }
        }
    }
}
