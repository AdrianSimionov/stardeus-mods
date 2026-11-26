using System.Text;
using KL.Console;
using Game.Utils;
using NoAutoForbid;

namespace Game.Console {
    /// <summary>
    /// Console fallback for the settings page: "noforbid [option] [on|off]".
    /// "on" means the game forbids that thing automatically.
    /// </summary>
    public class NoForbidCommand : ConsoleCommand {
        [SelfInit]
        private static void Load() {
            Register(new NoForbidCommand());
        }

        public override void Initialize() {
            Name = "noforbid";
            HelpLine = "No Auto Forbid mod: noforbid [dropped|stripped|spaceloot|derelict|moved] [on|off]";
        }

        public override ConsoleCommandResult Execute(ConsoleCommandArguments args) {
            if (The.R?.State.IsLoaded != true) {
                return Error("This command doesn't work in main menu");
            }
            if (!args.HasArgument(1)) {
                return OK(Describe());
            }
            var option = args.GetString(1);
            if (!NoForbidSettings.IsValid(option)) {
                return Error("Unknown option: {0}. Options: {1}", option,
                    string.Join(", ", NoForbidSettings.AllOptions));
            }
            if (!args.HasArgument(2)) {
                return OK("{0}: {1}", option, NoForbidSettings.Get(option) ? "on" : "off");
            }
            var value = args.GetString(2);
            if (value != "on" && value != "off") {
                return Error("Use 'on' (the game forbids it) or 'off' (it does not)");
            }
            NoForbidSettings.Set(option, value == "on");
            return OK(Describe());
        }

        private static string Describe() {
            var sb = new StringBuilder("No Auto Forbid (on = the game forbids it):");
            foreach (var o in NoForbidSettings.AllOptions) {
                sb.Append('\n').Append(o).Append(": ")
                    .Append(NoForbidSettings.Get(o) ? "on" : "off");
            }
            return sb.ToString();
        }
    }
}
