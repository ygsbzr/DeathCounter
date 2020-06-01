using Modding;
using ModCommon;
using ModCommon.Util;
using HutongGames.PlayMaker.Actions;
using System.Linq;
using System;
using UnityEngine;
using System.Reflection;
using System.IO;
using DeathCounter.Extensions;
using System.Collections.Generic;
using TMPro;
using System.Collections;

namespace DeathCounter
{
    public class DeathCounter : Mod
    {
        public static DeathCounter Instance;

        private SaveSettings _settings = new SaveSettings();

        public override ModSettings SaveSettings
        {
            get => _settings;
            set => _settings = value as SaveSettings;
        }

        private Sprite _deathSprite;
        private Sprite _damageSprite;

        private GameObject _huddamage;
        private GameObject _huddeath;
        private GameObject _death;
        private GameObject _damage;

        private Texture2D[] _textures;

        public override void Initialize()
        {
            Instance = this;
           
            ModHooks.Instance.AfterTakeDamageHook += OnTakeDamage;

            ModHooks.Instance.AfterSavegameLoadHook += OnGameSaveLoad;

            ModHooks.Instance.AfterPlayerDeadHook += OnDeath;

            _textures = LoadTextures().ToArray();

            _damageSprite = Sprite.Create(_textures[0], new Rect(0, 0, _textures[0].width, _textures[0].height), new Vector2(0.5f, 0.5f));
            _deathSprite = Sprite.Create(_textures[1], new Rect(0, 0, _textures[1].width, _textures[1].height), new Vector2(0.5f, 0.5f));
           
            ModHooks.Instance.LanguageGetHook += OnLangGet;
            On.DisplayItemAmount.OnEnable += OnDisplayAmount;
        }

     
        public override List<(string, string)> GetPreloadNames()
            => new List<(string, string)>() { ("Tutorial_01", "_Props/Cave Spikes (1)") };

        

        private IEnumerable<Texture2D> LoadTextures()
        {
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.EndsWith(".png")).OrderBy(x => x);
            foreach (var resource in resources)
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

        private string OnLangGet(string key, string sheetTitle)
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

            return Language.Language.GetInternal(key, sheetTitle);
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


        private void OnGameSaveLoad(SaveGameData data)
        {
            var inventoryFSM = GameManager.instance.inventoryFSM;

            var invCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("Inv");
            var uiControl = invCanvas.LocateMyFSM("UI Inventory");
            var prefab = inventoryFSM.gameObject.FindGameObjectInChildren("Geo");

            var hudCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("HudCamera").FindGameObjectInChildren("Hud Canvas");

            _damage = CreateStatObject("damage", _settings.TotalDamage.ToString(), prefab, invCanvas.transform, _damageSprite, new Vector3(10.5f, 0, 0));
            _death = CreateStatObject("death", _settings.Deaths.ToString(), prefab, invCanvas.transform, _deathSprite, new Vector3(6.5f, 0, 0));

            _huddeath = CreateStatObject("death", _settings.TotalDamage.ToString(), prefab, hudCanvas.transform, _deathSprite, new Vector3(2.2f, 11.4f));
            _huddeath.FindGameObjectInChildren("Geo Amount").transform.position -= new Vector3(0.3f, 0, 0);
            _huddamage = CreateStatObject("damage", _settings.TotalDamage.ToString(), prefab, hudCanvas.transform, _damageSprite, new Vector3(4.3f, 11.4f));
            _huddamage.FindGameObjectInChildren("Geo Amount").transform.position -= new Vector3(0.3f, 0, 0);

            var deathState = FsmUtil.CopyState(uiControl, "Geo", "Death");
            uiControl.GetAction<SetFsmGameObject>("Death", 0).setValue = _death;
            uiControl.GetAction<SetFsmString>("Death", 3).setValue = "INV_NAME_DEATH";
            uiControl.GetAction<SetFsmString>("Death", 4).setValue = "INV_DESC_DEATH";

            var damageState = FsmUtil.CopyState(uiControl, "Death", "Damage");
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


        private GameObject CreateStatObject(string name, string text, GameObject prefab, Transform parent, Sprite sprite, Vector3 postoAdd)
        {
            var go = UnityEngine.Object.Instantiate(prefab, parent, true);
            go.transform.position += postoAdd;
            go.GetComponent<DisplayItemAmount>().playerDataInt = name;
            go.GetComponent<DisplayItemAmount>().textObject.text = text;
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

        private void OnDeath()
        {
            _settings.Deaths += 1;
            GameManager.instance.StartCoroutine(PlayBad(_huddeath.GetComponent<SpriteRenderer>()));
            _huddeath.GetComponent<DisplayItemAmount>().textObject.text = _settings.Deaths.ToString();
            Log(_settings.Deaths);
        }

        private int OnTakeDamage(int hazardType, int damageAmount)
        {
            if (damageAmount == 9999)
                _settings.TotalDamage += PlayerData.instance.health + PlayerData.instance.healthBlue;
            else
                _settings.TotalDamage += damageAmount;
            GameManager.instance.StartCoroutine(PlayBad(_huddamage.GetComponent<SpriteRenderer>()));
            _huddamage.GetComponent<DisplayItemAmount>().textObject.text = _settings.TotalDamage.ToString();
            return damageAmount;
        }

        IEnumerator PlayBad(SpriteRenderer s)
        {
            s.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            s.material.color = Color.white;
        }
    }
}
