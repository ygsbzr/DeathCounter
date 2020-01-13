using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeathCounter
{
    public class SaveSettings : ModSettings
    {
        public int Deaths
        {
            get => GetInt();
            set => SetInt(value);
        }

        public int TotalDamage
        {
            get => GetInt();
            set => SetInt(value);
        }
    }
}
