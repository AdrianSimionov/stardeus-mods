using Game;
using Game.CodeGen;
using Game.Components;
using Game.Data;
using Game.Systems;
using Game.Utils;
using KL.Utils;

namespace AutoUnlockDoors {
    /// <summary>
    /// Stardeus keeps door locks as plain DoorComp.State changes - no Harmony needed.
    /// This system sweeps every game tick and immediately unlocks any locked door.
    /// Covers player locks, the mental-breakdown "Lockdown" action, and copied configs.
    /// </summary>
    public sealed class AutoUnlockDoorsSys : GameSystem {
        public const string SysId = "AutoUnlockDoorsSys";
        private const long ScanIntervalTicks = 60;

        public override string Id => SysId;

        [SelfInit]
        private static void Register() {
            GameSystems.Register(SysId, () => new AutoUnlockDoorsSys());
        }

        protected override void OnInitialize() {
            S.Clock.OnTick.AddListener(OnTick);
        }

        public override void Unload() {
            S.Clock.OnTick.RemoveListener(OnTick);
        }

        private void OnTick(long ticks) {
            if (ticks % ScanIntervalTicks != 0) { return; }
            var doors = S.Components.AllByType<DoorComp>();
            if (doors == null) { return; }

            foreach (var comp in doors) {
                if (comp is DoorComp door && door.IsLocked && door.Tile != null) {
                    door.Unlock(false);
                }
            }
        }
    }
}
