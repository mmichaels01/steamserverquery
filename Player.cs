using System;
using System.Collections.Generic;
using System.Text;

namespace SteamServerQuery
{
    public class Player
    {
        public virtual float Duration { get; set; }
        public virtual string Name { get; set; }
        public virtual int Score { get; set; }
    }
}
