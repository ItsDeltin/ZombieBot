using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Deltin.CustomGameAutomation;

namespace ZombieBot
{
    partial class Program
    {
        private static readonly Map[] ElimMaps = new Map[]
        {
            Map.ELIM_Ayutthaya,
            Map.ELIM_Ilios_Well,
            Map.ELIM_Ilios_Ruins,
            Map.ELIM_Ilios_Lighthouse,
            Map.ELIM_Lijiang_ControlCenter,
            Map.ELIM_Lijiang_Garden,
            Map.ELIM_Nepal_Sanctum,
            Map.ELIM_Nepal_Shrine,
            Map.ELIM_Nepal_Village,
            Map.ELIM_Oasis_CityCenter,
        };
        private static readonly Map[] TdmMaps = new Map[]
        {
            Map.TDM_Dorado,
            Map.TDM_Eichenwalde,
            Map.TDM_Hanamura,
            Map.TDM_Hollywood,
            Map.TDM_HorizonLunarColony,
            Map.TDM_KingsRow,
            Map.TDM_TempleOfAnubis,
            Map.TDM_VolskayaIndustries,
            Map.TDM_Ilios_Well,
            Map.TDM_Ilios_Ruins
        };

        private static CustomGame cg;
        private static Task ZombieBotTask;
        private static bool Initialized = false;

        static void Main(string[] args)
        {
            string header = "Zombiebot - https://github.com/ItsDeltin/Overwatch-Custom-Game-Automation";
            Console.Title = header;
            Console.WriteLine(header);

            var ts = new CancellationTokenSource();

            Config config = Config.ParseConfig();

            while (true)
            {
                const string START =        "start [--local] [--skipsetup]  Starts the bot.";
                const string STOP =         "stop                           Stops the bot.";
                const string START_GAME =   "startgame                      Starts Overwatch.";
                const string SEND =         "send <text...>                 Sends a message to the chat.";
                const string INVITE =       "invite <battletag>             Invites a player to the game.";
                const string MOVE =         "move <0-26> <0-26>             Swaps players' slots.";
                const string PLAYER_COUNT = "playercount                    Gets the player count.";

                const string NOT_INITIALIZED = "ZombieBot not started. Run the start command to start.";

                Console.Write(">");
                string input = Console.ReadLine();
                string[] inputSplit = input.Split(' ');

                switch(inputSplit[0].ToLower())
                {
                    #region help Command
                    case "help":
                        Console.WriteLine(string.Join("\n    ", "Usage:", START, STOP, START_GAME, SEND, INVITE, MOVE, PLAYER_COUNT));
                        break;
                    #endregion

                    #region clear Command
                    case "clear":
                    case "cls":
                        Console.Clear();
                        Console.WriteLine(header);
                        break;
                    #endregion

                    #region start Command
                    case "start":
                        if (Initialized)
                        {
                            Console.WriteLine("ZombieBot already started.");
                            break;
                        }

                        config.Local = config.Local || FindOption(input, "--local");
                        bool skipSetup = FindOption(input, "--skipsetup");
                        bool debug = FindOption(input, "--debug");

                        Start(config, skipSetup, debug, ts.Token);
                        break;
                    #endregion

                    #region stop Command
                    case "stop":
                        if (!Initialized)
                        {
                            Console.WriteLine(NOT_INITIALIZED);
                            break;
                        }

                        Console.WriteLine("Stopping bot...");
                        ts.Cancel();
                        ZombieBotTask.Wait();
                        ts = new CancellationTokenSource();

                        break;
                    #endregion

                    #region startgame Command
                    case "startgame":
                        if (CustomGame.GetOverwatchProcess() != null)
                        {
                            Console.WriteLine("Overwatch already started.");
                            break;
                        }

                        Console.WriteLine("Starting Overwatch...");

                        bool createCustomGame = FindOption(input, "--createcustomgame", "-ccg");

                        try
                        {
                            CustomGame.StartOverwatch(new OverwatchInfoAuto()
                            {
                                AutomaticallyCreateCustomGame = createCustomGame,
                                CloseOverwatchProcessOnFailure = false,
                                ScreenshotMethod = config.ScreenshotMethod,
                                BattlenetExecutableFilePath = config.BattlenetExecutable,
                                OverwatchSettingsFilePath = config.OverwatchSettingsFile
                            });
                            Console.WriteLine("Overwatch started.");
                        }
                        catch (OverwatchStartFailedException ex)
                        {
                            Console.WriteLine(ex);
                        }
                        break;
                    #endregion

                    #region send Command
                    case "send":
                        if (!Initialized)
                        {
                            Console.WriteLine(NOT_INITIALIZED);
                            break;
                        }

                        if (inputSplit.Length >= 2)
                            cg.Chat.SendChatMessage(string.Join(" ", inputSplit, 1, inputSplit.Length - 1));
                        else
                            Console.WriteLine(SEND);
                        break;
                    #endregion

                    #region invite Command
                    case "invite":
                        if (!Initialized)
                        {
                            Console.WriteLine(NOT_INITIALIZED);
                            break;
                        }

                        string invitePlayer = inputSplit.ElementAtOrDefault(1);
                        if (invitePlayer != null)
                            cg.InvitePlayer(invitePlayer, Team.BlueAndRed);
                        else
                            Console.WriteLine(INVITE);
                        break;
                    #endregion

                    #region move Command
                    case "move":
                        if (!Initialized)
                        {
                            Console.WriteLine(NOT_INITIALIZED);
                            break;
                        }

                        if (int.TryParse(inputSplit.ElementAtOrDefault(1), out int slot1) && int.TryParse(inputSplit.ElementAtOrDefault(2), out int slot2))
                            cg.Interact.Move(slot1, slot2);
                        else
                            Console.WriteLine(MOVE);
                        break;
                    #endregion

                    #region playercount Command
                    case "playercount":
                        if (!Initialized)
                        {
                            Console.WriteLine(NOT_INITIALIZED);
                            break;
                        }

                        Console.WriteLine(PlayingCount - cg.GetInvitedCount());
                        break;
                    #endregion

                    default:
                        break;
                }
            }
        } // Main()

        private static bool FindOption(string text, params string[] variants)
        {
            return Regex.IsMatch(text, @"(?<= )(" + string.Join("|", variants) + @")\b");
        }

        private static void Start(Config config, bool skipSetup, bool debug, CancellationToken cs)
        {
            Initialized = true;

            Abyxa abyxa = null;
            if (config.DefaultMode == "abyxa")
            {
                abyxa = new Abyxa(config.Name, config.Region, config.Local);
                abyxa.ZombieServer.MinimumPlayerCount = config.MinimumPlayers;
            }
            bool serverBrowser = config.DefaultMode == "serverbrowser";

            ZombieBotTask = Task.Run(() =>
            {
                OperationResult operationResult = OperationResult.Success;

                try
                {
                    cg = new CustomGame(new CustomGameBuilder()
                    {
                        OverwatchProcess = CustomGame.GetOverwatchProcess() ?? CustomGame.StartOverwatch(new OverwatchInfoAuto()
                        {
                            BattlenetExecutableFilePath = config.BattlenetExecutable,
                            OverwatchSettingsFilePath = config.OverwatchSettingsFile,
                            ScreenshotMethod = config.ScreenshotMethod
                        }),
                        ScreenshotMethod = config.ScreenshotMethod
#if DEBUG
                        , DebugMode = debug
#endif
                    });
                    cg.Commands.Listen = true;

                    cg.ModesEnabled = config.Version == 0 ? Gamemode.Elimination : Gamemode.TeamDeathmatch;
                    cg.CurrentEvent = config.OverwatchEvent;

                    Map[] maps = config.Version == 0 ? ElimMaps : TdmMaps;

                    if (cs.IsCancellationRequested)
                        operationResult = OperationResult.Canceled;
                    else
                    {
                        if (!skipSetup)
                            Setup(abyxa, serverBrowser, cg, maps, config.Preset, config.Name);

                        if (cs.IsCancellationRequested)
                            operationResult = OperationResult.Canceled;
                        else
                            while (!cs.IsCancellationRequested)
                            {
                                if ((operationResult = Pregame(abyxa, serverBrowser, cg, maps, config.MinimumPlayers, cs)) != OperationResult.Success)
                                    break;
                                if ((operationResult = Ingame(abyxa, serverBrowser, cg, config.Version, cs)) != OperationResult.Success)
                                    break;
                            }
                    }
                }
                catch (OverwatchClosedException)
                {
                    operationResult = OperationResult.Exited;
                }
                catch (OverwatchStartFailedException)
                {
                    operationResult = OperationResult.StartFailed;
                }

                cg.Dispose();
                cg = null;

                if (abyxa != null)
                    abyxa.Kill();

                if (operationResult == OperationResult.Canceled)
                    Log("Bot stopped.");
                else if (operationResult == OperationResult.Disconnected)
                    Log("Overwatch disconnected, bot stopped.");
                else if (operationResult == OperationResult.Exited)
                    Log("Overwatch exited, bot stopped.");
                else if (operationResult == OperationResult.StartFailed)
                    Log("Could not start Overwatch, bot stopped.");

                Initialized = false;
            });
        }

        private static string UpdateMap(Abyxa abyxa, CustomGame cg)
        {
            string currentMap = cg.GetCurrentMap()?.FirstOrDefault()?.ShortName;
            if (currentMap != null)
            {
                if (abyxa != null)
                {
                    abyxa.ZombieServer.Map = currentMap;
                    abyxa.Update();
                }
            }
            else
                Log("Could not detect current map.");
            return currentMap;
        }

        public static void Log(string text)
        {
            Console.WriteLine("[ZombieBot] " + text);
        }
    }

    public enum OperationResult
    {
        Disconnected,
        Exited,
        Canceled,
        StartFailed,
        Success,
    }
}