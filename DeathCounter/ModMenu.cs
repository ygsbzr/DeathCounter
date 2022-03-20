using Satchel.BetterMenus;

namespace DeathCounter
{
    public static class ModMenu
    {
        public static Menu menu = null;

        public static MenuScreen GetMenu(MenuScreen lastmenu)
        {
            if (menu == null)
            {
                menu = PrepareMenu();
            }
            return menu.GetMenuScreen(lastmenu);
        }

        public static Menu PrepareMenu()
        {
            return new Menu("Death Counter",
                new Element[] {
                    new HorizontalOption(
                        "Show Death Counter:",
                        "Deaths will still be tracked in the background",
                        new string[] { "True", "False" },
                        (opt) => DeathCounter.GlobalSettings.ShowDeathCounter = opt == 0,
                        () => DeathCounter.GlobalSettings.ShowDeathCounter ? 0 : 1),
                    new HorizontalOption(
                        "Show Hit Counter:",
                        "Damage will still be tracked in the background",
                        new string[] { "True", "False" },
                        (opt) => DeathCounter.GlobalSettings.ShowHitCounter = opt == 0,
                        () => DeathCounter.GlobalSettings.ShowHitCounter ? 0 : 1),
                    new HorizontalOption(
                        "Display Position:",
                        "Toggle where to display the counters",
                        new string[] { "Beside Geo", "Under Geo", "Further Under Geo" },
                        i => SetDisplayState(i),
                        () => GetDisplayState()),
                    new MenuButton("Reset Death Count",
                        "Click button to reset Death",
                        (_) => DeathCounter._settings.Deaths = 0),
                    new MenuButton("Reset Damage Count",
                        "Click button to reset Damage",
                        (_) => DeathCounter._settings.TotalDamage = 0)
                });
        }

        public static void SetDisplayState(int i)
        {
            DeathCounter.GlobalSettings.BesideGeoCount = i == 0;
            DeathCounter.GlobalSettings.UnderGeoCount = i == 1;
            DeathCounter.GlobalSettings.FurtherUnderGeoCount = i == 2;
        }

        public static int GetDisplayState()
        {
            if (DeathCounter.GlobalSettings.BesideGeoCount) return 0;
            else if (DeathCounter.GlobalSettings.UnderGeoCount) return 1;
            else if (DeathCounter.GlobalSettings.FurtherUnderGeoCount) return 2;
            return 0;
        }
    }
}
