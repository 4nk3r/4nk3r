using System.Threading.Tasks;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using EcoSurvival.Core;

namespace Eco.EcoSurvival
{
    /// <summary>
    /// Server plugin entry point.
    ///
    /// Status: the rules in EcoSurvival.Core are complete and tested. This class loads and validates
    /// the rules and reports status. The hooks that apply the rules to live animals are not wired yet,
    /// see README.md, section "Open items". Until they are wired the mod changes nothing in game.
    /// </summary>
    public class EcoSurvivalPlugin : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin
    {
        private string status = "Not started.";

        /// <summary>Active rules. Replaced only by configuration, never mid-tick.</summary>
        public static SurvivalConfig Rules { get; private set; } = new SurvivalConfig();

        /// <summary>Returns the plugin status shown in the server UI.</summary>
        public string GetStatus() => this.status;

        /// <summary>Validates the rules and marks the plugin ready.</summary>
        public void Initialize(TimedTask timer)
        {
            Rules.Validate();
            this.status = "Ready. Animal hooks not wired yet (see README open items).";
        }

        /// <summary>Marks the plugin as stopped.</summary>
        public Task ShutdownAsync()
        {
            this.status = "Stopped.";
            return Task.CompletedTask;
        }
    }
}
