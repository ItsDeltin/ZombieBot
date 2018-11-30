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

        static void Main(string[] args)
        {
            string header = "Zombiebot - https://github.com/ItsDeltin/Overwatch-Custom-Game-Automation";
            Console.Title = header;
            Console.WriteLine(header);

            while (true)
            {
                const string START = "start [--local] [--skipsetup] - Starts the bot.";
                const string SEND = "send <text...> - Sends a message to the chat.";
                const string INVITE = "invite <battletag> - Invites a player to the game.";
                const string MOVE = "move <0-26> <0-26> - Swaps players' slots.";
                const string PLAYER_COUNT = "playercount - Gets the player count.";

                const string NOT_INITIALIZED = "ZombieBot not started. Run the start command to start.";

                Console.Write(">");
                string input = Console.ReadLine();
                string[] inputSplit = input.Split(' ');

                switch(inputSplit[0].ToLower())
                {
                    #region Help Command
                    case "help":
                        Console.WriteLine(string.Join("\n    ", "Usage:", START, SEND, INVITE, MOVE, PLAYER_COUNT));
                        break;
                    #endregion

                    #region Clear Command
                    case "clear":
                        Console.Clear();
                        Console.WriteLine(header);
                        break;
                    #endregion

                    #region Start Command
                    case "start":
                        if (cg != null)
                        {
                            Console.WriteLine("ZombieBot already started.");
                            break;
                        }

                        Config config = Config.ParseConfig();

                        config.Local = config.Local || FindOption(input, "--local");
                        bool skipStartup = FindOption(input, "--skipstartup");

                        Start(config, skipStartup);
                        break;
                    #endregion

                    #region Send Command
                    case "send":
                        if (cg == null)
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

                    #region Invite Command
                    case "invite":
                        if (cg == null)
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

                    #region Move Command
                    case "move":
                        if (cg == null)
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

                    #region PlayerCount Command
                    case "playercount":
                        if (cg == null)
                        {
                            Console.WriteLine(NOT_INITIALIZED);
                            break;
                        }

                        Console.WriteLine(PlayingCountIngame);
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

        private static void Start(Config config, bool skipStartup)
        {
            Abyxa abyxa = null;
            if (config.DefaultMode == "abyxa")
            {
                abyxa = new Abyxa(config.Name, config.Region, config.Local);
                abyxa.ZombieServer.MinimumPlayerCount = config.MinimumPlayers;
            }
            bool serverBrowser = config.DefaultMode == "serverbrowser";

            Task.Run(() =>
            {
                try
                {
                    cg = new CustomGame(new CustomGameBuilder()
                    {
                        OverwatchProcess = CustomGame.GetOverwatchProcess() ?? CustomGame.CreateOverwatchProcessAutomatically(),
                        ScreenshotMethod = config.ScreenshotMethod
                    });
                    cg.Commands.Listen = true;

                    cg.ModesEnabled = config.Version == 0 ? Gamemode.Elimination : Gamemode.TeamDeathmatch;
                    cg.CurrentEvent = config.OverwatchEvent;

                    Map[] maps = config.Version == 0 ? ElimMaps : TdmMaps;

                    if (!skipStartup)
                        Setup(abyxa, serverBrowser, cg, maps, config.Preset, config.Name);

                    while (true)
                    {
                        if (!Pregame(abyxa, serverBrowser, cg, maps, config.MinimumPlayers))
                            break;
                        if (!Ingame(abyxa, serverBrowser, cg, config.Version))
                            break;
                    }
                }
                catch (OverwatchClosedException)
                {

                }

                cg.Dispose();
                cg = null;

                Log("Overwatch closed or was disconnected. Stopping bot.");
            });
        }

        private static string UpdateMap(Abyxa abyxa, CustomGame cg)
        {
            string currentMap = cg.GetCurrentMap()?.FirstOrDefault()?.ShortName;
            if (currentMap != null && abyxa != null)
            {
                abyxa.ZombieServer.Map = currentMap;
                abyxa.Update();
            }
            return currentMap;
        }

        public static void Log(string text)
        {
            Console.WriteLine("[ZombieBot] " + text);
        }
    }
}