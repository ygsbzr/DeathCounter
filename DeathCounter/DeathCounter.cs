using Modding;
using ModCommon;
using ModCommon.Util;
using HutongGames.PlayMaker.Actions;
using System.Linq;
using System;
using UnityEngine;
using System.Reflection;
using System.IO;

namespace DeathCounter
{
    public class DeathCounter : Mod<SaveSettings>
    {
        public static DeathCounter Instance;



        private Sprite _deathSprite;
        private GameObject _death;

        public override void Initialize()
        {
            Instance = this;
            ModHooks.Instance.AfterPlayerDeadHook += OnDeath;

            ModHooks.Instance.AfterSavegameLoadHook += OnGameSaveLoad;


            Texture2D deathTexture = null;
            var resource = Assembly.GetExecutingAssembly().GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(".png"));
            using (Stream res = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                byte[] buffer = new byte[res.Length];
                res.Read(buffer, 0, buffer.Length);


                deathTexture = new Texture2D(1, 1);
                deathTexture.LoadImage(buffer, true);
            }
            _deathSprite = Sprite.Create(deathTexture, new Rect(0, 0, deathTexture.width, deathTexture.height), new Vector2(0.5f, 0.5f));

            ModHooks.Instance.LanguageGetHook += OnLangGet;
            On.DisplayItemAmount.OnEnable += OnDisplayAmount;
        }

        private string OnLangGet(string key, string sheetTitle)
        {
            switch (key)
            {
                case "INV_NAME_DEATH":
                    return "Death";
                case "INV_DESC_DEATH":
                    return "number of times you have failed smh.";
            }

            return Language.Language.GetInternal(key, sheetTitle);
        }

        private void OnGameSaveLoad(SaveGameData data)
        {
            try
            {
                var inventoryFSM = GameManager.instance.inventoryFSM;
                var invCanvas = GameObject.Find("_GameCameras").FindGameObjectInChildren("Inv");
                var uiControl = invCanvas.LocateMyFSM("UI Inventory");

                _death = UnityEngine.Object.Instantiate(inventoryFSM.gameObject.FindGameObjectInChildren("Geo"), invCanvas.transform, true);
                _death.transform.SetPositionX(_death.transform.GetPositionX() + 6.5f);
                _death.GetComponent<DisplayItemAmount>().playerDataInt = "death";
                _death.GetComponent<DisplayItemAmount>().textObject.text = Settings.DeathCounter.ToString();
                _death.GetComponent<SpriteRenderer>().sprite = _deathSprite;
                _death.SetActive(true);
                _death.GetComponent<BoxCollider2D>().size = new Vector2(1.5f, 1f);
                _death.GetComponent<BoxCollider2D>().offset = new Vector2(0.5f, 0f);

                var deathState = FsmutilExt.CopyState(uiControl, "Geo", "Death");
                uiControl.GetAction<SetFsmGameObject>("Death", 0).setValue = _death;
                uiControl.GetAction<SetFsmString>("Death", 3).setValue = "INV_NAME_DEATH";
                uiControl.GetAction<SetFsmString>("Death", 4).setValue = "INV_DESC_DEATH";


                uiControl.ChangeTransition("Geo", "UI RIGHT", "Death");
                uiControl.ChangeTransition("Death", "UI RIGHT", "To Equip");
                uiControl.ChangeTransition("Death", "UI LEFT", "Geo");
                uiControl.ChangeTransition("Death", "UI UP", "Trinket 1");
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        private void OnDisplayAmount(On.DisplayItemAmount.orig_OnEnable orig, DisplayItemAmount self)
        {
            orig(self);
            if (self.playerDataInt == "death")
                self.textObject.text = Settings.DeathCounter.ToString();
        }

        private void OnDeath()
        {
            Log("PLAYER DIED");
            Settings.DeathCounter += 1;
            Log(Settings.DeathCounter);

        }
    }
}
