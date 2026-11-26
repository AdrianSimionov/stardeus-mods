using System;
using System.Collections.Generic;
using System.IO;
using Game;
using Game.CodeGen;
using Game.Constants;
using Game.Data;
using Game.Systems;
using Game.Utils;
using KL.Utils;
using Newtonsoft.Json.Linq;

namespace ReplenishingDeposits {
    /// <summary>
    /// Slowly restores the depth of idle, depth-based resource deposits. This lets
    /// mining continue after a drill or shuttle has reached its maximum depth.
    /// </summary>
    public sealed class ReplenishingDepositsSys : GameSystem {
        public const string SysId = "ReplenishingDepositsSys";
        public const string ModId = "ReplenishingDeposits";

        private const float MiningEpsilon = 0.0001f;

        public override string Id => SysId;
        public override bool SkipInSandbox => false;

        [SelfInit]
        private static void Register() {
            GameSystems.Register(SysId, () => new ReplenishingDepositsSys());
        }

        private sealed class DepositState {
            public float LastTotalMined;
            public int IdleHours;
        }

        private sealed class ModConfig {
            // How much depth (in km) is restored per in-game day while a deposit is idle.
            public float RefillKmPerDay = 2f;
            // How many in-game hours must pass without any mining before refill starts.
            // Set to 0 to refill continuously, even while the deposit is being mined.
            public int IdleHoursBeforeRefill = 6;
            // Restore the deposit's original mining difficulty once it has fully refilled.
            public bool ResetDifficultyOnFullRefill = true;
        }

        private readonly Dictionary<int, DepositState> tracked = new();
        private readonly HashSet<int> seen = new();
        private readonly List<int> removeBuffer = new();
        private ModConfig config = new();

        protected override void OnInitialize() {
            UnityEngine.Debug.Log("Replenishing Deposits: system initialized");
            S.Sig.AfterLoadState.AddListener(OnLoadSave);
        }

        private void OnLoadSave(GameState state) {
            if (state?.Clock == null) {
                return;
            }
            state.Clock.OnTick.RemoveListener(OnTick);
            state.Clock.OnTick.AddListener(OnTick);
            tracked.Clear();
            LoadConfig();
            UnityEngine.Debug.Log(string.Format(
                "Replenishing Deposits: refill {0} km/day after {1}h idle",
                config.RefillKmPerDay, config.IdleHoursBeforeRefill));
        }

        private void LoadConfig() {
            config = new ModConfig();
            var info = The.ModLoader?.FindModById(ModId);
            if (info == null) {
                return;
            }
            var path = info.ModFile("Config.json");
            if (!File.Exists(path)) {
                return;
            }
            try {
                var json = JObject.Parse(File.ReadAllText(path));
                config.RefillKmPerDay = ReadFloat(json, "RefillKmPerDay",
                    config.RefillKmPerDay);
                config.IdleHoursBeforeRefill = ReadInt(json, "IdleHoursBeforeRefill",
                    config.IdleHoursBeforeRefill);
                config.ResetDifficultyOnFullRefill = ReadBool(json,
                    "ResetDifficultyOnFullRefill", config.ResetDifficultyOnFullRefill);
                if (config.RefillKmPerDay < 0f) {
                    config.RefillKmPerDay = 0f;
                }
                if (config.IdleHoursBeforeRefill < 0) {
                    config.IdleHoursBeforeRefill = 0;
                }
            } catch (Exception ex) {
                D.Err("Replenishing Deposits: failed to read Config.json: {0}",
                    ex.Message);
            }
        }

        private static float ReadFloat(JObject json, string key, float fallback) {
            var token = json[key];
            if (token == null) {
                return fallback;
            }
            try {
                return token.Value<float>();
            } catch {
                return fallback;
            }
        }

        private static int ReadInt(JObject json, string key, int fallback) {
            var token = json[key];
            if (token == null) {
                return fallback;
            }
            try {
                return token.Value<int>();
            } catch {
                return fallback;
            }
        }

        private static bool ReadBool(JObject json, string key, bool fallback) {
            var token = json[key];
            if (token == null) {
                return fallback;
            }
            try {
                return token.Value<bool>();
            } catch {
                return fallback;
            }
        }

        private void OnTick(long ticks) {
            if (ticks % Consts.TicksPerHour != 0) {
                return;
            }
            if (S?.Universe?.ObjectsById == null || S.Sys?.Planets == null) {
                return;
            }
            try {
                RefillDeposits();
            } catch (Exception ex) {
                D.Err("Replenishing Deposits: tick failed: {0}", ex.Message);
            }
        }

        private void RefillDeposits() {
            seen.Clear();
            foreach (var so in S.Universe.ObjectsById.Values) {
                if (so == null || !so.HasData || so.Type == null) {
                    continue;
                }
                if (so.Type.IdH != SpaceObjectTypeIdH.Resource &&
                        so.Type.IdH != SpaceObjectTypeIdH.ResourceUnknown) {
                    continue;
                }
                if (!S.Sys.Planets.TryGetResourceForUpdate(so, out var idx,
                        out var resource, out var mt)) {
                    continue;
                }
                if (mt?.Deposit == null || !mt.Deposit.IsWithDepth ||
                        resource.MiningDepth < 0f || resource.Id == 0) {
                    continue;
                }

                var id = resource.Id;
                seen.Add(id);

                if (!tracked.TryGetValue(id, out var state)) {
                    // Start tracking after this tick; refill begins once the deposit
                    // has been observed idle for the configured number of hours.
                    tracked[id] = new DepositState {
                        LastTotalMined = resource.TotalMined,
                        IdleHours = 0
                    };
                    continue;
                }

                if (resource.TotalMined > state.LastTotalMined + MiningEpsilon) {
                    state.IdleHours = 0;
                } else {
                    state.IdleHours++;
                }
                state.LastTotalMined = resource.TotalMined;

                if (state.IdleHours < config.IdleHoursBeforeRefill ||
                        config.RefillKmPerDay <= 0f) {
                    continue;
                }

                var depthBefore = resource.MiningDepth;
                var depthAfter = Math.Max(0f, depthBefore -
                    config.RefillKmPerDay / 24f);
                var changed = false;

                if (depthAfter < depthBefore) {
                    resource.MiningDepth = depthAfter;
                    changed = true;
                }
                if (depthAfter <= 0f && config.ResetDifficultyOnFullRefill) {
                    var baseDifficulty = mt.Deposit.MiningDifficulty;
                    if (Math.Abs(resource.MiningDifficulty - baseDifficulty) > 0.001f) {
                        resource.MiningDifficulty = baseDifficulty;
                        changed = true;
                    }
                }
                if (changed) {
                    S.Sys.Planets.UpdateResource(idx, in resource);
                }
            }
            PruneTracking();
        }

        private void PruneTracking() {
            if (tracked.Count == seen.Count) {
                return;
            }
            removeBuffer.Clear();
            foreach (var id in tracked.Keys) {
                if (!seen.Contains(id)) {
                    removeBuffer.Add(id);
                }
            }
            for (var i = 0; i < removeBuffer.Count; i++) {
                tracked.Remove(removeBuffer[i]);
            }
        }

        public override void Unload() {
            if (S?.Clock != null) {
                S.Clock.OnTick.RemoveListener(OnTick);
            }
            tracked.Clear();
        }
    }
}
