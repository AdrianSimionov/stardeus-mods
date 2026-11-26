using System.Text;
using Game;
using Game.Constants;
using Game.Data;
using Game.Data.Space;
using Game.Systems.Ships;
using Game.Systems.Space;
using Game.Systems.UIUX;
using Game.UI;
using Game.Utils;

namespace QuietWarp {
    /// <summary>
    /// Replaces the vanilla "Warp" starmap action with the same logic, except
    /// that the "wait for Copters" line is removed from the warning. If that
    /// was the only blocker, no popup is shown at all. The warp target is still
    /// queued immediately; the vanilla HyperjumpQueue waits for the copter
    /// missions (including mining-drill deployments) to finish and then warps.
    ///
    /// Implemented without Harmony so it works on native Apple Silicon macOS.
    /// </summary>
    public sealed class QuietWarpAction : SOAction {
        public override string Id => "HyperjumpWarp";

        public override int UIPriority => 50;

        [SelfInit(SelfInit.Stage.AfterMods, 100)]
        private static void Init() {
            Register("HyperjumpWarp", new QuietWarpAction());
            UnityEngine.Debug.Log(
                "Quiet Warp: replaced HyperjumpWarp action");
        }

        public override bool IsAvailable(SOActionContext ctx) {
            var so = ctx.SO;
            if (so == null || so == A.PlayerSector ||
                    so.Region == A.PlayerSector.Parent?.Region) {
                return false;
            }
            return SOActionHyperjumpWarp.CanWarpTo(so, out _);
        }

        public override bool AddOptions(QuickChoice qc) {
            qc.AddButton(T.TravelHere + " " + Texts.GreyParentheses(T.VerbWarp),
                WarpJump);
            return true;
        }

        private void WarpJump() {
            if (!IsAvailable(ctx)) {
                return;
            }

            var so = ctx.SO;
            var s = ctx.S;
            var layer = SOUtils.JumpLayerFor(so);
            if (!SOUtils.CheckCanQueueHyperjump(s, layer)) {
                return;
            }

            if (!s.Sys.ShipNav.CanMoveInHyperspace(true, layer,
                    out var failReason)) {
                var hadCopterTip = !string.IsNullOrEmpty(failReason) &&
                    failReason.Contains(T.HyperjumpTipCopter);
                var remaining = hadCopterTip
                    ? RemoveCopterTip(failReason)
                    : failReason;

                // Suppress the popup only when the copter wait was the sole
                // reason. Real blockers must still be reported.
                if (!hadCopterTip || !string.IsNullOrEmpty(remaining)) {
                    WPopup.Warning(T.Warning,
                        T.EnterHyperspaceFailed + ":\n" + remaining);
                }
            }

            if (A.IsInHyperspace) {
                s.Sig.ShowErrorMessage.Send(T.ExitHyperspaceFirst);
                return;
            }

            SOUtils.CheckAbandonDerelicts(delegate {
                DoEnqueue(so, s);
            });
        }

        private static string RemoveCopterTip(string failReason) {
            var sb = new StringBuilder(failReason.Length);
            foreach (var line in failReason.Split('\n')) {
                if (line.Contains(T.HyperjumpTipCopter)) {
                    continue;
                }
                if (sb.Length > 0) {
                    sb.Append('\n');
                }
                sb.Append(line);
            }
            return sb.ToString().Trim();
        }

        private static void DoEnqueue(SpaceObject so, GameState s) {
            var result = s.Query.SetWarpTarget.Ask(so.SpaceEntity);
            if (result.IsSuccess) {
                return;
            }
            if (result.FailReason != null) {
                s.Sig.ShowErrorMessage.Send(result.FailReason);
            } else {
                UISounds.PlayActionDenied();
            }
        }
    }
}
