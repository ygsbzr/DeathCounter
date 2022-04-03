using Modding;
using HutongGames.PlayMaker.Actions;
using System.Linq;
using UnityEngine;
using System;
using System.Reflection;
using System.IO;
using DeathCounter.Extensions;
using System.Collections.Generic;
using System.Collections;
using Satchel;
using EXutil = DeathCounter.Extensions.FsmUtil;
using System.ComponentModel;

namespace DeathCounter
{
    public class DeathCounter : Mod, ILocalSettings<SaveSettings>, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        public static DeathCounter Instance;

        public override string GetVersion() => "1.5.78-8";

        public static SaveSettings _settings = new SaveSettings();

        public void OnLoadLocal(SaveSettings s)
        {
            _settings = s;
        }

        public SaveSettings OnSaveLocal()
        {
            return _settings;
        }

        public bool ToggleButtonInsideMenu => false;
        private Sprite _deathSprite;
        private Sprite _damageSprite;

        private GameObject _huddamage;
        private GameObject _huddeath;
        private GameObject _death;
        private GameObject _damage;
        private Vector3 origpos;
        private Texture2D[] _textures;

        public static GlobalSettings GlobalSettings { get; set; } = new GlobalSettings();
        public void OnLoadGlobal(GlobalSettings globalSettings) => GlobalSettings = globalSettings;
        public GlobalSettings OnSaveGlobal() => GlobalSettings;
        public MenuScreen GetMenuScreen(MenuScreen lastmenu, ModToggleDelegates? delegates)
        {
            return ModMenu.GetMenu(lastmenu);
        }

        public override void Initialize()
        {
            Instance = this;

            ModHooks.TakeHealthHook += TakeHealth;

            On.HeroController.Awake += Awake;

            _textures = LoadTextures().ToArray();

            _damageSprite = Sprite.Create(_textures[0], new Rect(0, 0, _textures[0].width, _textures[0].height), new Vector2(0.5f, 0.5f));
            _deathSprite = Sprite.Create(_textures[1], new Rect(0, 0, _textures[1].width, _textures[1].height), new Vector2(0.5f, 0.5f));

            ModHooks.LanguageGetHook += OnLangGet;
            On.DisplayItemAmount.OnEnable += OnDisplayAmount;
            On.UIManager.UIClosePauseMenu += OnUnpause;

            GlobalSettings.PropertyChanged += GlobalSettings_PropertyChanged;
        }

        private void Awake(On.HeroController.orig_Awake orig, HeroController self)
        {
            orig(self);

            Log($"Deaths: {_settings.Deaths}");
            Log($"Damage: {_settings.TotalDamage}");

            var inventoryFSM = GameManager.instance.inventoryFSM;

            var invCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("Inv");
            var uiControl = invCanvas.LocateMyFSM("UI Inventory");
            var prefab = inventoryFSM.gameObject.FindGameObjectInChildren("Geo");
            origpos = prefab.transform.position;
            var hudCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("HudCamera").FindGameObjectInChildren("Hud Canvas");
            DrawHudDeath(prefab, hudCanvas);
            DrawHudDamage(prefab, hudCanvas);
            if (!GlobalSettings.ShowDeathCounter)
            {
                _huddeath?.SetActive(false);
            }
            if (!GlobalSettings.ShowHitCounter)
            {
                _huddamage?.SetActive(false);
            }

            _death = CreateStatObject("death", _settings.Deaths.ToString(), prefab, invCanvas.transform, _deathSprite, new Vector3(6.5f, 0, 0));
            _damage = CreateStatObject("damage", _settings.TotalDamage.ToString(), prefab, invCanvas.transform, _damageSprite, new Vector3(10.5f, 0, 0));

            EXutil.CopyState(uiControl, "Geo", "Death");
            uiControl.GetAction<SetFsmGameObject>("Death", 0).setValue = _death;
            uiControl.GetAction<SetFsmString>("Death", 3).setValue = "INV_NAME_DEATH";
            uiControl.GetAction<SetFsmString>("Death", 4).setValue = "INV_DESC_DEATH";

            EXutil.CopyState(uiControl, "Death", "Damage");
            uiControl.GetAction<SetFsmGameObject>("Damage", 0).setValue = _damage;
            uiControl.GetAction<SetFsmString>("Damage", 3).setValue = "INV_NAME_DAMAGE";
            uiControl.GetAction<SetFsmString>("Damage", 4).setValue = "INV_DESC_DAMAGE";

            uiControl.ChangeTransition("Geo", "UI RIGHT", "Death");
            uiControl.ChangeTransition("Death", "UI RIGHT", "Damage");
            uiControl.ChangeTransition("Death", "UI LEFT", "Geo");
            uiControl.ChangeTransition("Death", "UI UP", "Trinket 1");

            uiControl.ChangeTransition("Damage", "UI RIGHT", "Trinket 4");
            uiControl.ChangeTransition("Damage", "UI LEFT", "Death");
            uiControl.ChangeTransition("Damage", "UI UP", "Trinket 4");

            uiControl.AddTransition("Trinket 1", "UI DOWN", "Death");
            uiControl.AddTransition("Trinket 2", "UI DOWN", "Death");
            uiControl.AddTransition("Trinket 3", "UI DOWN", "Death");
            uiControl.AddTransition("Trinket 4", "UI DOWN", "Death");
        }

        private int TakeHealth(int damageAmount)
        {
            try
            {
                if (damageAmount >= PlayerData.instance.health + PlayerData.instance.healthBlue)
                {
                    _settings.Deaths++;
                    if (_huddeath != null)
                    {
                        GameManager.instance.StartCoroutine(PlayBad(_huddeath.GetComponent<SpriteRenderer>()));
                        _huddeath.GetComponent<DisplayItemAmount>().textObject.text = _settings.Deaths.ToString();
                    }
                }
                if (damageAmount == 9999)
                    _settings.TotalDamage += PlayerData.instance.health + PlayerData.instance.healthBlue;
                else
                    _settings.TotalDamage += damageAmount;

                if (_huddamage != null)
                {
                    GameManager.instance.StartCoroutine(PlayBad(_huddamage.GetComponent<SpriteRenderer>()));
                    _huddamage.GetComponent<DisplayItemAmount>().textObject.text = _settings.TotalDamage.ToString();
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogError(e.StackTrace);
            }

            return damageAmount;
        }

        private string OnLangGet(string key, string sheetTitle, string orig)
        {
            switch (key)
            {
                case "INV_NAME_DEATH":
                    return "Deaths";
                case "INV_DESC_DEATH":
                    return "Imagine dying.";
                case "INV_NAME_DAMAGE":
                    return "Damage";
                case "INV_DESC_DAMAGE":
                    return "Imagine taking damage.";
            }
            return orig;
        }

        public override List<(string, string)> GetPreloadNames()
            => new List<(string, string)>() { ("Tutorial_01", "_Props/Cave Spikes (1)") };

        private IEnumerable<Texture2D> LoadTextures()
        {
            foreach (var resource in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.EndsWith(".png")).OrderBy(x => x))
            {
                using (Stream res = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    byte[] buffer = new byte[res.Length];
                    res.Read(buffer, 0, buffer.Length);

                    var tex = new Texture2D(1, 1);
                    tex.LoadImage(buffer, true);
                    yield return tex;
                }
            }
        }

        public class CounterCoordinate
        {
            public float X;
            public float Y;
        }

        public class CounterLocation
        {
            public CounterCoordinate Death;
            public CounterCoordinate Damage;
        }

        private Dictionary<string, CounterLocation> CounterLocations = new Dictionary<string, CounterLocation>
        {
            { "Beside Geo", new CounterLocation { Death = new CounterCoordinate { X = 2.2f, Y = 11.3f }, Damage = new CounterCoordinate { X = 4.3f, Y = 11.3f } } },
            { "Under Geo", new CounterLocation { Death = new CounterCoordinate { X = 2.2f, Y = 10.55f }, Damage = new CounterCoordinate { X = 4.3f, Y = 10.55f } } },
            { "Beside Essence", new CounterLocation { Death = new CounterCoordinate { X = 1.0f, Y = 9.65f }, Damage = new CounterCoordinate { X = 3.1f, Y = 9.65f } } },
            { "Under Essence", new CounterLocation { Death = new CounterCoordinate { X = -1.1f, Y = 9.05f }, Damage = new CounterCoordinate { X = 1.0f, Y = 9.05f } } },
            { "On Screen Edge", new CounterLocation { Death = new CounterCoordinate { X = -5.0f, Y = 10.4f }, Damage = new CounterCoordinate { X = -5.0f, Y = 9.5f } } },
            { "Above Masks", new CounterLocation { Death = new CounterCoordinate { X = -0.15f, Y = 13.5f }, Damage = new CounterCoordinate { X = 2.05f, Y = 13.5f } } },
        };

        private CounterLocation GetPositionOption()
        {
            return GlobalSettings.BesideGeoCount
                ? CounterLocations["Beside Geo"]
                : GlobalSettings.UnderGeoCount
                ? CounterLocations["Under Geo"]
                : GlobalSettings.BesideEssenceCount
                ? CounterLocations["Beside Essence"]
                : GlobalSettings.UnderEssenceCount
                ? CounterLocations["Under Essence"]
                : GlobalSettings.OnLeftEdge
                ? CounterLocations["On Screen Edge"]
                : GlobalSettings.AboveMasks
                ? CounterLocations["Above Masks"]
                : CounterLocations["Above Masks"];
        }

        private void DrawHudDeath(GameObject prefab, GameObject hudCanvas)
        {
            try
            {
                var deaths = _settings.Deaths.ToString();
                var deathPosition = GetPositionOption().Death;
                _huddeath = CreateStatObject("death", deaths, prefab, hudCanvas.transform, _deathSprite, new Vector3(deathPosition.X, deathPosition.Y));
                _huddeath.FindGameObjectInChildren("Geo Amount").transform.position -= new Vector3(0.3f, 0, 0);
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogError(e.StackTrace);
            }
        }
        private void DrawHudDamage(GameObject prefab, GameObject hudCanvas)
        {
            try
            {
                var damage = _settings.TotalDamage.ToString();
                var damagePosition = GlobalSettings.ShowDeathCounter ? GetPositionOption().Damage : GetPositionOption().Death;
                _huddamage = CreateStatObject("damage", damage, prefab, hudCanvas.transform, _damageSprite, new Vector3(damagePosition.X, damagePosition.Y));
                _huddamage.FindGameObjectInChildren("Geo Amount").transform.position -= new Vector3(0.3f, 0, 0);
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogError(e.StackTrace);
            }
        }

        private GameObject CreateStatObject(string name, string text, GameObject prefab, Transform parent, Sprite sprite, Vector3 postoAdd)
        {
            var go = UnityEngine.Object.Instantiate(prefab, parent, true);
            go.transform.position += postoAdd;
            go.GetComponent<DisplayItemAmount>().playerDataInt = name;
            go.GetComponent<DisplayItemAmount>().textObject.text = text;
            go.GetComponent<DisplayItemAmount>().textObject.fontSize = 4;
            go.GetComponent<SpriteRenderer>().sprite = sprite;
            go.SetActive(true);
            go.GetComponent<BoxCollider2D>().size = new Vector2(1.5f, 1f);
            go.GetComponent<BoxCollider2D>().offset = new Vector2(0.5f, 0f);
            return go;
        }


        private void OnDisplayAmount(On.DisplayItemAmount.orig_OnEnable orig, DisplayItemAmount self)
        {
            orig(self);
            switch (self.playerDataInt)
            {
                case "death":
                    self.textObject.text = _settings.Deaths.ToString();
                    break;
                case "damage":
                    self.textObject.text = _settings.TotalDamage.ToString();
                    break;
            }
        }

        private void OnUnpause(On.UIManager.orig_UIClosePauseMenu origUIClosePauseMenu, UIManager self)
        {
            origUIClosePauseMenu(self);
            RedrawCounters();
        }

        private void GlobalSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Log($"{e.PropertyName} changed");
            RedrawCounters();
        }

        private void RedrawCounters()
        {
            try
            {
                var position = GetPositionOption();
                var deathPosition = position.Death;
                var damagePosition = GlobalSettings.ShowDeathCounter ? position.Damage : position.Death;

                if (!GlobalSettings.ShowDeathCounter)
                {
                    _huddeath?.SetActive(false);
                }
                else if (_huddeath != null)
                {
                    _huddeath.SetActive(true);
                    _huddeath.transform.position = origpos + new Vector3(deathPosition.X, deathPosition.Y);
                }

                if (!GlobalSettings.ShowHitCounter)
                {
                    _huddamage?.SetActive(false);
                }
                else if (_huddamage != null)
                {
                    _huddamage.SetActive(true);
                    _huddamage.transform.position = origpos + new Vector3(damagePosition.X, damagePosition.Y);
                }
            }
            catch (Exception e)
            {

                LogError(e.Message);
                LogError(e.StackTrace);
            }
        }

        public void Unload()
        {
            _settings.Deaths = 0;
            _settings.TotalDamage = 0;
            if (_huddeath != null)
                _huddeath.GetComponent<DisplayItemAmount>().textObject.text = _settings.Deaths.ToString();
            if (_huddamage != null)
                _huddamage.GetComponent<DisplayItemAmount>().textObject.text = _settings.TotalDamage.ToString();
            ModHooks.TakeHealthHook -= TakeHealth;
            ModHooks.LanguageGetHook -= OnLangGet;
            On.DisplayItemAmount.OnEnable -= OnDisplayAmount;
            On.HeroController.Awake -= Awake;
            On.UIManager.UIClosePauseMenu -= OnUnpause;
        }

        IEnumerator PlayBad(SpriteRenderer s)
        {
            s.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            s.material.color = Color.white;
        }
    }
}
