using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using LightJson;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace CustomHitSound
{
    public class Patches
    {
        public static double realBPM = 0;
        [HarmonyPatch(typeof(ADOStartup), "SetupLevelEventsInfo")]
        private static class Patch_ADOStartup_SetupLevelEventsInfo
        {
            public static bool Prefix()
            {
                Dictionary<string, object> dictionary = Json.Deserialize(InitEvent().ToString()) as Dictionary<string, object>;
                GCS.levelEventsInfo = ADOStartup.DecodeLevelEventInfoList(dictionary["levelEvents"] as List<object>);
                GCS.settingsInfo = ADOStartup.DecodeLevelEventInfoList(dictionary["settings"] as List<object>);
                ADOStartup.DecodeLevelEventCategoryList(dictionary["categories"] as List<object>);
                LevelEventType[] values = (LevelEventType[])Enum.GetValues(typeof(LevelEventType));
                GCS.levelEventTypeString = new Dictionary<LevelEventType, string>();
                foreach (LevelEventType key in values)
                    GCS.levelEventTypeString.Add(key, key.ToString());
                return false;
            }

            private static JsonObject InitEvent()
            {
                var jsonObject = JsonValue.Parse(Resources.Load<TextAsset>("LevelEditorProperties").text).AsJsonObject;
                JsonArray array = jsonObject["levelEvents"].AsJsonArray;
                // Add custom hit sound event
                JsonArray setHitSound = Tools.GetEvent(array, LevelEventType.SetHitsound)["properties"];
                JsonObject jsonObject2 = setHitSound[1];
                jsonObject2.Add("enableIf", new JsonArray()
                    .Add("customHitSound")
                    .Add(false));
                JsonObject customHitSound = new JsonObject()
                    .Add("name", "customHitSound")
                    .Add("type", "Bool")
                    .Add("default", false)
                    .Add("key", "editor.enableCustomHitSound");
                setHitSound.Add(customHitSound);
                JsonObject audioFile = new JsonObject()
                    .Add("name", "selectAudioFile")
                    .Add("type", "File")
                    .Add("fileType", "audio")
                    .Add("default", "")
                    .Add("key", "editor.selectAudioFile")
                    .Add("disableIf", new JsonArray()
                        .Add("customHitSound")
                        .Add(false));
                setHitSound.Add(audioFile);
                // Add custom hit sound play event
                JsonArray playSound = Tools.GetEvent(array, LevelEventType.PlaySound)["properties"];
                JsonObject jsonObject3 = playSound[0];
                jsonObject3.Add("enableIf", new JsonArray()
                    .Add("customHitSound")
                    .Add(false));
                playSound.Add(customHitSound);
                playSound.Add(audioFile);
                return jsonObject;
            }
        }
        
        [HarmonyPatch(typeof(RDString), "Get")]
        private static class Patch_RDString_Get
        {
            public static bool Prefix(string key, ref string __result)
            {
                switch (key)
                {
                    case "editor.enableCustomHitSound":
                        __result = Main.language.enableHitSoundType;
                        return false;
                    case "editor.selectAudioFile":
                        __result = Main.language.selectAudioFile;
                        return false;
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(RDString), "ChangeLanguage")]
        private static class Patch_RDString_ChangeLanguage
        {
            public static void Postfix()
            {
                Main.InitLanguage();
            }
        }
        [HarmonyPatch(typeof(RDString), "Setup")]
        private static class Patch_RDString_Setup
        {
            public static void Postfix()
            {
                Main.InitLanguage();
            }
        }
        
        [HarmonyPatch(typeof(AudioManager), nameof(AudioManager.Play))]
        public static class AudioManager_Play_Patch
        {
            public static void Prefix(string snd)
            {
                
            }
        }

        [HarmonyPatch(typeof(scnGame), nameof(scnGame.ApplyEvent))]
        private static class Patch_scnGame_ApplyEvent
        {
            public static bool Prefix(ref ffxPlusBase __result,
                LevelEvent evnt,
                float bpm,
                float pitch,
                List<scrFloor> floors,
                float offset = 0.0f,
                int? customFloorID = null
            )
            {
                if (evnt.eventType != LevelEventType.PlaySound && evnt.eventType != LevelEventType.SetHitsound)
                    return true;
                
                int num1 = customFloorID ?? evnt.floor;
                scrFloor floor = floors[num1];
                GameObject gameObject = floor.gameObject;
                ffxPlusBase ffxPlusBase = null;
                switch (evnt.eventType)
                {
                    case LevelEventType.SetHitsound:
                        var ffxSetHitsound = gameObject.AddComponent<ffxSetHitsound>();
                        ffxSetHitsound.gameSound = (GameSound)evnt.data["gameSound"];
                        ffxSetHitsound.hitSound = (HitSound)evnt.data["hitsound"];
                        ffxSetHitsound.volume = evnt.GetInt("hitsoundVolume") / 100f;
                        if (evnt.GetBool("customHitSound")) Misc.usedEventCount++;
                        Misc.hitSounds.Add(ffxSetHitsound,evnt);
                        floor.setHitsound = ffxSetHitsound;
                        break;
                    case LevelEventType.PlaySound:
                        var ffxPlaySound = gameObject.AddComponent<PlaySound>();
                        ffxPlusBase = ffxPlaySound;
                        ffxPlaySound.hitSound = (HitSound)evnt.data["hitsound"];
                        ffxPlaySound.volume = evnt.GetInt("hitsoundVolume") / 100f;
                        ffxPlaySound.enableCustomHitSound = evnt.GetBool("customHitSound");
                        if (evnt.GetBool("customHitSound")) Misc.usedEventCount++;
                        ffxPlaySound.filePath = evnt.GetString("selectAudioFile");
                        __result = ffxPlusBase;
                        break;
                }

                if (ffxPlusBase == null)
                {
                    return true;
                }
                floors[num1].plusEffects.Add(ffxPlusBase);
                ffxPlusBase.SetStartTime(bpm, evnt.GetFloat("angleOffset") + offset);
                string str = evnt.GetString("eventTag") ?? "";
                if (!str.IsNullOrEmpty())
                {
                    string[] tags = str.Split(' ');
                    foreach (string key in tags)
                    {
                        if (scrDecorationManager.instance != null)
                        {
                            List<scrDecoration> scrDecorationList;
                            if (scrDecorationManager.instance.hitboxEventTagDecorations.TryGetValue(key,
                                    out scrDecorationList))
                            {
                                foreach (scrDecoration scrDecoration in scrDecorationList)
                                    scrDecoration.hitboxEvents.Add(ffxPlusBase);
                            }
                            else
                                continue;
                        }

                        ffxPlusBase.runManually = true;
                    }
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(scrConductor), nameof(scrConductor.PlayHitTimes))]
        public class scrConductor_PlayHitTimes_Patch
        {
            public static bool Prefix(scrConductor __instance)
            {
                if (Misc.usedEventCount == 0) return true;
                scrConductor conductor = __instance;
                List<scrFloor> listFloors = ADOBase.lm.listFloors;
                bool useMidspinHitSound = false;
                HitSound midspinHitSound = HitSound.Kick;
                float volume1 = 1;
                int num3 = GCS.checkpointNum < listFloors.Count ? GCS.checkpointNum + 1 : 1;
                int index3 = GCS.practiceMode ? GCS.checkpointNum + GCS.practiceLength : listFloors.Count - 1;
                ffxSetHitsound setHitsound = null;
                
                for (int i = 1; i < listFloors.Count; ++i)
                {
                    scrFloor scrFloor1 = listFloors[i];
                    setHitsound = scrFloor1.setHitsound;
                    
                    HitSound hitSound1 = conductor.hitSound;
                    if ( setHitsound !=  null)
                    {
                        
                        if (setHitsound.gameSound == GameSound.Midspin)
                        {
                            useMidspinHitSound = true;
                            midspinHitSound = setHitsound.hitSound;
                        }
                        else
                        {
                            hitSound1 = setHitsound.hitSound;
                            var hasSetMidspinHitsound = Tools.GetPrivateField<bool>(conductor,"hasSetMidspinHitsound");
                            Tools.log(hasSetMidspinHitsound);
                            if (!hasSetMidspinHitsound)
                            {
                                hasSetMidspinHitsound = true;
                                midspinHitSound = hitSound1;
                            }
                        }
                        volume1 = setHitsound.volume;
                    }
                    if (ADOBase.lm.listFloors[i].holdLength <= -1 && 
                        ADOBase.lm.listFloors[i - 1].holdLength <= -1 &&
                        (!ADOBase.lm.listFloors[i - 1].midSpin ||
                         i < 2 ||
                         ADOBase.lm.listFloors[i - 2].holdLength <= -1))
                    {
                        scrFloor scrFloor2 = listFloors[i - 1];
                        double num5 = 0.0;
                        ADOBase.gc.hitSoundOffsets.TryGetValue(!( scrFloor2 !=  null) || !scrFloor2.midSpin || useMidspinHitSound ? hitSound1 : midspinHitSound, out num5);
                        double time1 = conductor.dspTimeSongPosZero + scrFloor1.entryTimePitchAdj - num5;
                        Tools.log(222);
                        
                        if (i >= num3 && i <= index3 && time1 > conductor.dspTime && !scrFloor1.midSpin && hitSound1 != HitSound.None)
                        {
                            HitSound hitSound2 = !( scrFloor2 != null) || !scrFloor2.midSpin || !useMidspinHitSound ? hitSound1 : midspinHitSound;
                            if (scrFloor1.tapsNeeded > 1)
                                hitSound2 =midspinHitSound;
                            
                            Tools.log("get setHitsound");
                            LevelEvent setHitSound2 = Misc.hitSounds[setHitsound];
                            //TODO:not execute!                         ↑
                            if (setHitSound2.GetBool("customHitSound"))
                            { 
                                string audioUrl = Path.Combine(Path.GetDirectoryName(scnEditor.instance.customLevel.levelPath), setHitSound2.GetString("selectAudioFile"));
                                AudioClip clip = Main.AudioDownloader.DownloadAudioClip(audioUrl);
                                Misc.hitSoundDatas.Add(new HitSoundsData(HitSound.None, time1, volume1, clip));
                                Tools.log(6);
                            }
                            else
                            {
                                Misc.hitSoundDatas.Add(new HitSoundsData(hitSound2, time1, volume1));
                            }
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
        public class scrPlanet_MoveToNextFloor
        {
            public static void Prefix(scrPlanet __instance,scrFloor floor)
            {
                if (!ADOBase.isLevelEditor || !Main.settings.enableBPMLimiter) return;
                if (floor.nextfloor != null)
                {
                    double entryTime = floor.entryTime;
                    double nextEntryTime = floor.nextfloor.entryTime;
                    float pitch = scnEditor.instance.customLevel.levelData.pitch / 100f;
                    float playbackRate = scnEditor.instance.playbackSpeed;
                    realBPM = 60 / (nextEntryTime - entryTime) * pitch * playbackRate;
                }
                if (realBPM >= Main.settings.BPMLimit - 1 && Misc.usedEventCount > 0)
                {
                    scnEditor.instance.SwitchToEditMode();
                    scnEditor.instance.ShowNotification(Main.language.bpmTooHighWarning, Color.red,
                        Tools.calculateDelayTime());
                }
            }
        }

        [HarmonyPatch(typeof(scnEditor), "Play")]
        public class scnEditor_Play
        {
            public static void Postfix(scnEditor __instance)
            {
                if (Main.settings.enableBPMLimiter)
                {
                    realBPM = 0;
                    if (__instance.customLevel.levelData.bpm >= Main.settings.BPMLimit - 1 && Misc.usedEventCount > 0)
                    {
                        __instance.SwitchToEditMode();
                        __instance.ShowNotification(Main.language.bpmTooHighWarning, Color.red,
                            Tools.calculateDelayTime());
                    }
                }
            }
        }
        [HarmonyPatch(typeof(scnEditor), "OnGUI")]
        public class scnEditor_OnGUI 
        {
            private static void Postfix()
            {
                GUI.Label(new (100,100,100,100),"BPM:" +realBPM);
            }
        }
        
        [HarmonyPatch(typeof(scnEditor),"SwitchToEditMode")]
        public static class scnEditor_SwitchToEditMode
        {
            public static void Postfix()
            {
                Misc.usedEventCount = 0;
            }
        }
    }
}