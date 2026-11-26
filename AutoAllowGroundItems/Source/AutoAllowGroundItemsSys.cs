using Game;
using Game.CodeGen;
using Game.Components;
using Game.Constants;
using Game.Data;
using Game.Systems;
using Game.Systems.Items;
using Game.Utils;
using KL.Utils;

namespace AutoAllowGroundItems {
    public sealed class AutoAllowGroundItemsSys : GameSystem {
        public const string SysId = "AutoAllowGroundItemsSys";
        private const long ScanIntervalTicks = 60;

        public override string Id => SysId;
        public override bool SkipInSandbox => false;

        [SelfInit]
        private static void Register() {
            GameSystems.Register(SysId, () => new AutoAllowGroundItemsSys());
        }

        protected override void OnInitialize() {
            S.Clock.OnTick.AddListener(OnTick);
        }

        public override void Unload() {
            S.Clock.OnTick.RemoveListener(OnTick);
        }

        private void OnTick(long ticks) {
            if (ticks % ScanIntervalTicks != 0) { return; }

            AllowItems();
            AllowPiles();
        }

        private void AllowItems() {
            var items = S.Components.AllByType<StorableObjComp>();
            if (items == null) { return; }

            foreach (var comp in items) {
                if (!(comp is StorableObjComp item) || !item.IsForbidden) { continue; }
                if (!IsOnGround(item.Obj)) { continue; }
                item.IsForbidden = false;
            }
        }

        private void AllowPiles() {
            var piles = S.Components.AllByType<UnstoredMatComp>();
            if (piles == null) { return; }

            foreach (var comp in piles) {
                if (!(comp is UnstoredMatComp pile) || !pile.IsForbidden) { continue; }
                if (!IsOnGround(pile.Obj)) { continue; }
                pile.IsForbidden = false;
            }
        }

        private static bool IsOnGround(Obj obj) {
            if (obj == null || !obj.IsActive || obj.IsCarriedByAny) { return false; }
            var data = obj.Data;
            if (data == null || data.StorageId != 0) { return false; }
            return data.State == ObjState.Active;
        }
    }
}
