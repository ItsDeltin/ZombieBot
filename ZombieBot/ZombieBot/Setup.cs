using System;
using System.Threading;
using System.Linq;
using Deltin.CustomGameAutomation;

namespace ZombieBot
{
    partial class Program
    {
        public static void Setup(Abyxa abyxa, bool serverBrowser, CustomGame cg, Map[] maps, string preset, string name)
        {
            cg.AI.RemoveAllBotsAuto();

            if (abyxa != null)
                cg.Settings.JoinSetting = Join.InviteOnly;

            if (preset != null)
                cg.Settings.LoadPreset(preset);

            int moderatorSlot = cg.PlayerInfo.ModeratorSlot();
            if (moderatorSlot != -1)
            {
                if (moderatorSlot != 12)
                    cg.Interact.Move(moderatorSlot, 12);
            }
            else
            {
                var allSlots = cg.AllSlots;
                if (allSlots.Count == 1 && allSlots[0] != 12)
                    cg.Interact.Move(allSlots[0], 12);
            }

            cg.ToggleMap(ToggleAction.EnableAll);
            Thread.Sleep(500);

            cg.StartGame();
        }
    }
}
