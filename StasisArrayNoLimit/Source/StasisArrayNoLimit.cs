using Game.Data;
using Game.Procgen;
using Game.Systems.Story;
using Game.Utils;
using KL.Randomness;

namespace StasisArrayNoLimit {
    public sealed class UnlimitedDerelictShipWithStasisArrayStoryEvent : BaseDerelictShipStoryEvent {
        public const string EventId = "DerelictShipWithStasisArray";

        public override string Id => EventId;

        protected override float chanceOfSmallShip => 0f;

        public override bool CheckConditions(StoryContext ctx, StoryEventDef def) {
            return true;
        }

        protected override void AddPostStructureStages(ShipFactory factory, Rng rng) {
            factory.AddStage(new StasisAreaGenStage(5, 10, 0, 2, 3, 3, minIntegrity: 0.2f,
                doSomeDamageToTheArea: true));
        }

        protected override void AddDestructionStages(ShipFactory factory, Rng rng) {
            factory.AddStage(new SplitStasisAndCPUGenStage());
        }
    }

    public static class StasisArrayNoLimitInit {
        [SelfInit(SelfInit.Stage.AfterMods)]
        private static void ReplaceStoryEvent() {
            var original = Registries.StoryEvents.Get(
                UnlimitedDerelictShipWithStasisArrayStoryEvent.EventId, false);
            if (original == null) {
                return;
            }
            var replacement = new UnlimitedDerelictShipWithStasisArrayStoryEvent {
                IdH = original.IdH
            };
            Registries.StoryEvents.All[original.IdH] = replacement;
        }
    }
}
