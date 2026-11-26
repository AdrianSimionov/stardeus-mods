using System;
using System.Collections.Generic;
using Game;
using Game.Commands;
using Game.Data;
using Game.Systems;
using Game.Systems.Copters;
using Game.Utils;
using KL.Utils;

namespace AutoRebuildMiner {
    /// <summary>
    /// Watches miner deployment missions. Once the miner tile has been picked up
    /// (EntityId becomes 0), queues a normal blueprint back at the old spot.
    /// This avoids Harmony, which cannot patch on native Apple Silicon macOS.
    /// </summary>
    public sealed class AutoRebuildMinerSys : GameSystem {
        public const string SysId = "AutoRebuildMinerSys";

        private readonly HashSet<int> processedMissionIds = new();

        public override string Id => SysId;
        public override bool SkipInSandbox => false;

        [SelfInit]
        private static void Register() {
            GameSystems.Register(SysId, () => new AutoRebuildMinerSys());
        }

        protected override void OnInitialize() {
            UnityEngine.Debug.Log("Auto Rebuild Miner: system initialized");
            S.Sig.AfterLoadState.AddListener(OnLoadSave);
        }

        private void OnLoadSave(GameState state) {
            if (state?.Clock == null) {
                return;
            }
            state.Clock.OnTick.RemoveListener(OnTick);
            state.Clock.OnTick.AddListener(OnTick);
            processedMissionIds.Clear();
            UnityEngine.Debug.Log("Auto Rebuild Miner: watching copter missions");
        }

        private void OnTick(long ticks) {
            if (A.InstantBuild) {
                return;
            }

            var copters = S?.Sys?.Copters;
            var missions = copters?.Missions;
            if (missions == null) {
                return;
            }

            for (var i = 0; i < missions.Count; i++) {
                var mission = missions[i];
                if (mission.MissionType != CopterMissionType.DeployMiner) {
                    continue;
                }

                var miner = mission.Miner;
                if (miner.EntityId != 0 ||
                        string.IsNullOrEmpty(miner.MinerDefId) ||
                        miner.PosOnShip == 0) {
                    continue;
                }

                if (!processedMissionIds.Add(mission.Id)) {
                    continue;
                }

                try {
                    QueueRebuild(mission);
                } catch (Exception ex) {
                    D.Err("Auto Rebuild Miner: rebuild failed for mission {0}: {1}",
                        mission.Id, ex);
                }
            }
        }

        private void QueueRebuild(CopterMissionData mission) {
            var miner = mission.Miner;
            var proto = EntityUtils.CreatePrototype<Tile>(miner.MinerDefId);
            if (proto == null) {
                D.Err("Auto Rebuild Miner: could not create prototype for {0}",
                    miner.MinerDefId);
                return;
            }

            proto.Transform.Rotation = miner.RotOnShip;
            var layer = proto.Definition.LayerId;
            if (S.Map.Grids[layer].Get(miner.PosOnShip) != null) {
                // The spot is already occupied by a tile or blueprint.
                return;
            }

            S.CmdQ.Enqueue(new CmdPlaceTile(
                miner.PosOnShip,
                proto,
                warn: false,
                instant: false,
                skipValidation: true,
                afterPlace: (t) => {
                    var goal = t.Goal;
                    if (goal != null) {
                        goal.WithPriority(A.AutoRebuildPriority);
                    }
                }));

            UnityEngine.Debug.Log(
                "Auto Rebuild Miner: queued rebuild for mission " + mission.Id);
        }

        public override void Unload() {
            if (S?.Clock != null) {
                S.Clock.OnTick.RemoveListener(OnTick);
            }
            processedMissionIds.Clear();
        }
    }
}
