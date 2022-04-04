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
                        new string[] { "Beside Geo", "Under Geo", "Beside Essence", "Under Essence", "On Screen Edge", "Above Masks" },
                        i => SetDisplayState(i),
                        () => GetDisplayState()),
                    new MenuButton("Reset Death Count",
                        "Click button to reset Death",
                        (_) =>
                        {
                            DeathCounter._settings.Deaths=0;
                            DeathCounter._huddeath.GetComponent<DisplayItemAmount>().textObject.text =DeathCounter. _settings.Deaths.ToString();
                        }),
                    new MenuButton("Reset Damage Count",
                        "Click button to reset Damage",
                        (_) =>
                        {
                            DeathCounter._settings.TotalDamage = 0;
                           DeathCounter._huddamage.GetComponent<DisplayItemAmount>().textObject.text =DeathCounter. _settings.TotalDamage.ToString();
                        })
                });
        }

        public static void SetDisplayState(int i)
        {
            DeathCounter.GlobalSettings.BesideGeoCount = i == 0;
            DeathCounter.GlobalSettings.UnderGeoCount = i == 1;
            DeathCounter.GlobalSettings.BesideEssenceCount = i == 2;
            DeathCounter.GlobalSettings.UnderEssenceCount = i == 3;
            DeathCounter.GlobalSettings.OnLeftEdge = i == 4;
            DeathCounter.GlobalSettings.AboveMasks = i == 5;
        }

        public static int GetDisplayState()
        {
            if (DeathCounter.GlobalSettings.BesideGeoCount) return 0;
            else if (DeathCounter.GlobalSettings.UnderGeoCount) return 1;
            else if (DeathCounter.GlobalSettings.BesideEssenceCount) return 2;
            else if (DeathCounter.GlobalSettings.UnderEssenceCount) return 3;
            else if (DeathCounter.GlobalSettings.OnLeftEdge) return 4;
            else if (DeathCounter.GlobalSettings.AboveMasks) return 5;
            return 0;
        }
    }
}
