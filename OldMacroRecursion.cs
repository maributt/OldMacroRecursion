using System;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;

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
                macroCallHook = Svc.GameInteropProvider.HookFromAddress<MacroCallDelegate>(Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 4D 28"), new MacroCallDelegate(MacroCallDetour));
                macroCallHook?.Enable();

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

        private delegate void MacroCallDelegate(IntPtr a, IntPtr b);
        private Hook<MacroCallDelegate> macroCallHook;
        private IntPtr macroBasePtr = IntPtr.Zero;
        private IntPtr macroDataPtr = IntPtr.Zero;

        private int CurrentMacroLine {
            set {
                if (macroBasePtr != IntPtr.Zero) Marshal.WriteInt32(macroBasePtr, 0x2C0, value);
            }
        }

        private bool MacroLock {
            set {
                byte v = 0;
                if (value) v = 1;
                if (macroBasePtr != IntPtr.Zero) Marshal.WriteByte(macroBasePtr, 0x2B3, v);
            }
        }

        public void Dispose() {
            Svc.Commands.RemoveHandler("/runmacro");
            macroCallHook?.Disable();
            macroCallHook?.Dispose();
        }

        private void MacroCallDetour(IntPtr a, IntPtr b) {
            macroCallHook?.Original(a, b);

            macroBasePtr = IntPtr.Zero;
            macroDataPtr = IntPtr.Zero;
            try {
                // Hack-y search for first macro lol
                var scanBack = b;
                var limit = 200;
                while (limit-- >= 0) {
                    var macroDatHeaderCheck = Marshal.ReadInt64(scanBack, -40);
                    if (macroDatHeaderCheck == 0x41442E4F5243414D) {
                        macroDataPtr = scanBack;
                        macroBasePtr = a;
                        return;
                    }

                    scanBack -= 0x688;
                }

                Svc.PluginLog.Error("Failed to find Macro[0]");
            } catch (Exception ex) {
                Svc.PluginLog.Error(ex.ToString());
            }
        }

        public void OnMacroCommandHandler(string command, string args) {
            try {
                if (macroBasePtr != IntPtr.Zero && macroDataPtr != IntPtr.Zero) {
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
                                int.TryParse(arg, out startingLine);
                                break;
                            }
                        }
                    }

                    if (shared) num += 100;
                    
                    var macroPtr = macroDataPtr + 0x688 * num;
                    Svc.PluginLog.Debug($"Executing Macro #{num} @ {macroPtr}");
                    MacroLock = false;
                    macroCallHook.Original(macroBasePtr, macroPtr);

                    if (startingLine > 0 && startingLine <= 15) {
                        CurrentMacroLine = startingLine - 1;
                    }
                } else {
                    Svc.Chat.PrintError("OldMacroRecursion is not ready.\nExecute a macro to finish setup.");
                }
            } catch (Exception ex) {
                Svc.PluginLog.Error(ex.ToString());
            }
        }
    }
}
