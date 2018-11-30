using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieBot
{
    /// <summary>
    /// Data sent to Abyxa
    /// </summary>
    public class ZombieServer
    {
        public int PlayerCount;
        public int InvitedCount;
        public int MinimumPlayerCount;
        public int Mode;
        public string Map;

        public DateTime GameStarted;
        public int Survivors;
    }

    public class QueueUser
    {
        public QueueUser(string battleTag, bool isWaiting)
        {
            BattleTag = battleTag;
            IsWaiting = isWaiting;
        }

        public string BattleTag { get; private set; }
        public bool IsWaiting { get; private set; }
    }
}
