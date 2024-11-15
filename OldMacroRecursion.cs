using System;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;

namespace OldMacroRecursion {

    public class OldMacroRecursion : IDalamudPlugin {

        class Svc
        {
            [PluginService] static internal IChatGui Chat { get; private set; }
            [PluginService] static internal ICommandManager Commands { get; private set; }
            [PluginService] static internal IFramework Framework { get; private set; }
            [PluginService] static internal ISigScanner SigScanner { get; private set; }
            [PluginService] static internal IGameInteropProvider GameInteropProvider { get; private set; }
            [PluginService] static internal IPluginLog PluginLog { get; private set; }
        }

        public string Name => "OldMacroRecursion";
        private readonly IDalamudPluginInterface pluginInterface;

        public OldMacroRecursion(IDalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Svc>();
            this.pluginInterface = pluginInterface;

            try
            {
                Svc.Commands.AddHandler("/runmacro", new CommandInfo(OnMacroCommandHandler)
                {
                    HelpMessage = "Execute a Macro - /runmacro ## [individual|shared] [line]",
                    ShowInHelp = true
                });
            }
            catch (Exception ex)
            {
                Svc.PluginLog.Error(ex.ToString());
            }
        }

        public void Dispose() {
            Svc.Commands.RemoveHandler("/runmacro");
        }

        public unsafe void OnMacroCommandHandler(string command, string args) {
            try {
                if (true) {
                    var argSplit = args.Split(' ');

                    var num = byte.Parse(argSplit[0]);

                    if (num > 99) {
                        Svc.Chat.PrintError("Invalid Macro number.\nShould be 0 - 99");
                        return;
                    }

                    var shared = false;
                    var startingLine = 0;
                    foreach (var arg in argSplit.Skip(1)) {
                        switch (arg.ToLower()) {
                            case "shared":
                            case "share":
                            case "s": {
                                shared = true;
                                break;
                            }
                            case "individual":
                            case "i": {
                                shared = false;
                                break;
                            }
                            default: {
                                if (int.TryParse(arg, out startingLine) && startingLine > 14)
                                {
                                        Svc.Chat.PrintError("Invalid Macro starting line number.\nShould be 0 - 14");
                                        return;
                                }
                                break;
                            }
                        }
                    }

                    RaptureShellModule.Instance()->ExecuteMacro(RaptureMacroModule.Instance()->GetMacro(shared ? 1u : 0u, num));
                    RaptureShellModule.Instance()->MacroCurrentLine = startingLine;
                }
            } catch (Exception ex) {
                Svc.PluginLog.Error(ex.ToString());
            }
        }
    }
}
