using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeathCounter
{
    public class SaveSettings : IModSettings
    {
        public int DeathCounter
        {
            get => GetInt();
            set => SetInt(value);
        }
    }
}
