using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

// TODO: Rename this namespace to your mod's name.
namespace CustomHitSound
{
    public static class Main
    {
        public static Languages language;

        public static bool IsEnabled { get; private set; }

        public static AudioDownloader AudioDownloader = new();
        
        public static UnityModManager.ModEntry.ModLogger Logger { get; private set; }

        private static Harmony harmony;

        internal static Settings settings;
        
        public class Settings: UnityModManager.ModSettings , IDrawable 
        {
            public bool enableBPMLimiter;
            public float BPMLimit = 20000;
        
            public void OnGUI(UnityModManager.ModEntry modEntry)
            {
                enableBPMLimiter = GUILayout.Toggle(enableBPMLimiter,language.enableBPMLimiter);
                if (enableBPMLimiter)
                { 
                    GUILayout.Label(language.BPMLimit);
                    string minBPM = GUILayout.TextField(BPMLimit.ToString(CultureInfo.CurrentCulture),5,GUILayout.ExpandWidth(true),GUILayout.Width(42)); 
                    float.TryParse(minBPM,out BPMLimit);
                }
                settings.Draw(modEntry);
            }

            public void OnChange()
            {
                
            }

            public void OnSaveGUI(UnityModManager.ModEntry modEntry)
            {
                settings.Save(modEntry);
            }

            public void Save(UnityModManager.ModEntry modEntry)
            {
                Save(this,modEntry);
            }
        }
        internal static void Setup(UnityModManager.ModEntry modEntry) {
            Logger = modEntry.Logger;
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            // Add hooks to UMM event methods
            modEntry.OnGUI = settings.OnGUI;
            modEntry.OnSaveGUI = settings.OnSaveGUI;
            modEntry.OnToggle = OnToggle;
        }
        
        public static void log(object o) => Tools.log(o);

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
            IsEnabled = value;
            if (value) {
                StartMod(modEntry);
            } else {
                StopMod(modEntry);
            }
            ADOStartup.SetupLevelEventsInfo();
            if (!GCS.sceneToLoad.IsNullOrEmpty()) SceneManager.LoadScene(GCS.sceneToLoad);
            return true;
        }

        public static void InitLanguage()
        {
            switch (RDString.language)
            {
                case SystemLanguage.ChineseSimplified:
                    language = new Languages.Chinese();
                    break;
                case SystemLanguage.ChineseTraditional:
                    language = new Languages.Chinese();
                    break;
                case SystemLanguage.Korean:
                    language = new Languages.Korean();
                    break;
                case SystemLanguage.English:
                    language = new Languages.English();
                    break;
            }
        }
        private static void StartMod(UnityModManager.ModEntry modEntry) {
            // Patch everything in this assembly
            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void StopMod(UnityModManager.ModEntry modEntry) {
            // Unpatch everything
            harmony.UnpatchAll(modEntry.Info.Id);
        }
    }
    
}
