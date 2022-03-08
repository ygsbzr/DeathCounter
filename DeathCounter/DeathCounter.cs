using Modding;
using HutongGames.PlayMaker.Actions;
using System.Linq;
using System;
using UnityEngine;
using System.Reflection;
using System.IO;
using DeathCounter.Extensions;
using System.Collections.Generic;
using System.Collections;
using Vasi;
using EXutil = DeathCounter.Extensions.FsmUtil;
namespace DeathCounter
{
    public class DeathCounter : Mod, ILocalSettings<SaveSettings>, IGlobalSettings<GlobalSettings>, IMenuMod
    {
        public static DeathCounter Instance;

        public override string GetVersion() => "UNKNOWN-blu.3";

        private SaveSettings _settings = new SaveSettings();
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

        private Texture2D[] _textures;

        public static GlobalSettings GlobalSettings { get; set; } = new GlobalSettings();
        public void OnLoadGlobal(GlobalSettings globalSettings) => GlobalSettings = globalSettings;
        public GlobalSettings OnSaveGlobal() => GlobalSettings;

        public override void Initialize()
        {
            Instance = this;

            ModHooks.TakeHealthHook += TakeHealth;

            ModHooks.NewGameHook += OnNewGame;
            ModHooks.AfterSavegameLoadHook += OnGameSaveLoad;

            _textures = LoadTextures().ToArray();

            _damageSprite = Sprite.Create(_textures[0], new Rect(0, 0, _textures[0].width, _textures[0].height), new Vector2(0.5f, 0.5f));
            _deathSprite = Sprite.Create(_textures[1], new Rect(0, 0, _textures[1].width, _textures[1].height), new Vector2(0.5f, 0.5f));

            ModHooks.LanguageGetHook += OnLangGet;
            On.DisplayItemAmount.OnEnable += OnDisplayAmount;
            On.UIManager.UIClosePauseMenu += OnUnpause;
        }

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            return new List<IMenuMod.MenuEntry>
            {
                new IMenuMod.MenuEntry()
            {
                Name = "Show Counters:",
                Description = string.Empty,
                Values = new string[] { "True", "False" },
                Saver = opt => GlobalSettings.ShowCounters = opt == 0,
                Loader = () => GlobalSettings.ShowCounters ? 0 : 1
            },
            new IMenuMod.MenuEntry
                {
                    Name = "Display Position:",
                    Description = "Toggle where to display the counters",
                    Values = new string[] { "Beside Geo Count", "Under Geo Count" },
                    Saver = SetDisplayState,
                    Loader = GetDisplayState
                }
            };
        }

        public void SetDisplayState(int i)
        {
            GlobalSettings.BesideGeoCount = i == 0;
            GlobalSettings.UnderGeoCount = i == 1;
        }

        public int GetDisplayState()
        {
            if (GlobalSettings.BesideGeoCount) return 0;
            else if (GlobalSettings.UnderGeoCount) return 1;
            return 0;
        }

        private int TakeHealth(int damageAmount)
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
            return damageAmount;
        }

        private string OnLangGet(string key, string sheetTitle, string orig)
        {
            switch (key)
            {
                case "INV_NAME_DEATH":
                    return "Death";
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

        IEnumerator epic()
        {
            yield return new WaitWhile(() => CharmIconList.Instance == null);
            var invTexture = _textures[2];
            Modding.Logger.Log(invTexture.name);
            var orig = CharmIconList.Instance.spriteList[7];
            Modding.Logger.Log(orig.rect);
            foreach (var u in orig.uv)
                Modding.Logger.Log(new Vector2(ConvertUVToPixelCoordinates(u, orig.texture.width, orig.texture.height).x, orig.texture.height - ConvertUVToPixelCoordinates(u, orig.texture.width, orig.texture.height).y));

            var coords = orig.uv.Select(v => ConvertUVToPixelCoordinates(v, orig.texture.width, orig.texture.height));
            var x = coords.Min(v => v.x);
            var y = coords.Min(v => v.y);
            var texture = orig.texture;
            Modding.Logger.Log(new Vector2(x, y));
            var newSprite = Sprite.Create(invTexture, new Rect(x, y, orig.rect.width, orig.rect.height), new Vector2(0.5f, 0.5f));

            CharmIconList.Instance.spriteList[7] = newSprite;
        }

        private Vector2 ConvertUVToPixelCoordinates(Vector2 uv, int width, int height)
            => new Vector2(uv.x * width, uv.y * height);

        private void OnNewGame()
        {
            OnGameSaveLoad(null);
        }

        private const float HudDeathX = 2.2f;
        private const float HudDamageX = 4.3f;

        private const float BesideGeoCountY = 11.3f;
        private const float UnderGeoCountY = 10.55f;
        private float GetGeoCountHeight() => GlobalSettings.BesideGeoCount
            ? BesideGeoCountY
            : GlobalSettings.UnderGeoCount
                ? UnderGeoCountY
                : BesideGeoCountY;

        private void OnGameSaveLoad(SaveGameData _)
        {
            var inventoryFSM = GameManager.instance.inventoryFSM;

            var invCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("Inv");
            var uiControl = invCanvas.LocateMyFSM("UI Inventory");
            var prefab = inventoryFSM.gameObject.FindGameObjectInChildren("Geo");

            var hudCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("HudCamera").FindGameObjectInChildren("Hud Canvas");
            DrawHudDeathAndDamage(prefab, hudCanvas);
            if (!GlobalSettings.ShowCounters)
            {
                _huddamage?.Recycle();
                _huddeath?.Recycle();
                _huddamage = null;
                _huddeath = null;
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

            uiControl.AddTransition("Trinket 1", "UI DOWN", "Death", false);
            uiControl.AddTransition("Trinket 2", "UI DOWN", "Death", false);
            uiControl.AddTransition("Trinket 3", "UI DOWN", "Death", false);
            uiControl.AddTransition("Trinket 4", "UI DOWN", "Death", false);
        }

        private void DrawHudDeathAndDamage(GameObject prefab, GameObject hudCanvas)
        {
            _huddeath = CreateStatObject("death", _settings.Deaths.ToString(), prefab, hudCanvas.transform, _deathSprite, new Vector3(HudDeathX, GetGeoCountHeight()));
            _huddeath.FindGameObjectInChildren("Geo Amount").transform.position -= new Vector3(0.3f, 0, 0);
            _huddamage = CreateStatObject("damage", _settings.TotalDamage.ToString(), prefab, hudCanvas.transform, _damageSprite, new Vector3(HudDamageX, GetGeoCountHeight()));
            _huddamage.FindGameObjectInChildren("Geo Amount").transform.position -= new Vector3(0.3f, 0, 0);
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

            _huddeath?.Recycle();
            _huddamage?.Recycle();
            if (!GlobalSettings.ShowCounters)
            {
                _huddamage = null;
                _huddeath = null;
                return;
            }

            var inventoryFSM = GameManager.instance.inventoryFSM;
            var prefab = inventoryFSM.gameObject.FindGameObjectInChildren("Geo");
            var hudCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("HudCamera").FindGameObjectInChildren("Hud Canvas");
            DrawHudDeathAndDamage(prefab, hudCanvas);
        }

        public void Unload()
        {
            _settings.Deaths = 0;
            _settings.TotalDamage = 0;
            if (_huddeath != null) _huddeath.GetComponent<DisplayItemAmount>().textObject.text = _settings.Deaths.ToString();
            if (_huddamage != null) _huddamage.GetComponent<DisplayItemAmount>().textObject.text = _settings.TotalDamage.ToString();
            ModHooks.TakeHealthHook -= TakeHealth;

            ModHooks.NewGameHook -= OnNewGame;
            ModHooks.AfterSavegameLoadHook -= OnGameSaveLoad;

            ModHooks.LanguageGetHook -= OnLangGet;
            On.DisplayItemAmount.OnEnable -= OnDisplayAmount;
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
