using Game;
using Game.Components;
using Game.Data;
using Game.Systems;
using Game.Systems.AI;
using Game.Utils;

namespace NoAutoForbid {
    /// <summary>
    /// Native replacement for the Harmony-based No Auto Forbid mod.
    /// The four tunable-backed options are applied in OnInitialize; the fifth ("moved")
    /// is handled by listening to the game's own AIAddGoal signal instead of patching.
    /// </summary>
    public sealed class NoAutoForbidSys : GameSystem, ISaveable {
        public const string SysId = "NoAutoForbidSys";
        private const string MoveIcon = "Icons/Color/Move";

        public override string Id => SysId;
        public override bool SkipInSandbox => false;

        [SelfInit]
        private static void Register() {
            GameSystems.Register(SysId, () => new NoAutoForbidSys());
        }

        public NoAutoForbidSys() {
            NoForbidSettings.ResetDefaults();
        }

        protected override void OnInitialize() {
            NoForbidSettings.Apply();
            S.Sig.AIAddGoal.AddListener(OnGoalAdded);
        }

        public override void Unload() {
            S.Sig.AIAddGoal.RemoveListener(OnGoalAdded);
            NoForbidSettings.ResetDefaults();
            NoForbidSettings.Apply();
        }

        private static void OnGoalAdded(AIGoal goal) {
            if (NoForbidSettings.ForbidMoved) {
                return;
            }
            if (goal == null || !goal.IsByPlayer || goal.IconId != MoveIcon) {
                return;
            }
            if (!(goal.Target is MovableComp movable)) {
                return;
            }
            var obj = movable.Obj;
            if (obj == null) {
                return;
            }
            if (obj.StorableObj != null) {
                obj.StorableObj.IsForbidden = false;
            }
            if (obj.Pile != null) {
                obj.Pile.IsForbidden = false;
            }
        }

        public string GetName() {
            return Id;
        }

        private ComponentData data;

        public ComponentData Save() {
            if (data == null) { data = new ComponentData(0, SysId); }
            data.SetBool("ForbidDropped", NoForbidSettings.ForbidDropped);
            data.SetBool("ForbidStripped", NoForbidSettings.ForbidStripped);
            data.SetBool("ForbidSpaceLoot", NoForbidSettings.ForbidSpaceLoot);
            data.SetBool("ForbidDerelict", NoForbidSettings.ForbidDerelict);
            data.SetBool("ForbidMoved", NoForbidSettings.ForbidMoved);
            return data;
        }

        public void Load(ComponentData data) {
            this.data = data;
            NoForbidSettings.ForbidDropped = data.GetBool("ForbidDropped", false);
            NoForbidSettings.ForbidStripped = data.GetBool("ForbidStripped", false);
            NoForbidSettings.ForbidSpaceLoot = data.GetBool("ForbidSpaceLoot", false);
            NoForbidSettings.ForbidDerelict = data.GetBool("ForbidDerelict", false);
            NoForbidSettings.ForbidMoved = data.GetBool("ForbidMoved", true);
            NoForbidSettings.Apply();
        }
    }
}
