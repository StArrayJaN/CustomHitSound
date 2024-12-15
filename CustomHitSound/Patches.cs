using System;
using System.Collections.Generic;
using System.IO;
using ADOFAI;
using ADOFAI.Common.Platform;
using HarmonyLib;
using LightJson;
using UnityEngine;

namespace CustomHitSound
{
    public class Patches
    {
        [HarmonyPatch(typeof(ADOStartup), "SetupLevelEventsInfo")]
        private static class Patch_ADOStartup_SetupLevelEventsInfo
        {
            public static bool Prefix()
            {
                Dictionary<string, object> dictionary =
                    Json.Deserialize(InitEvent().ToString()) as Dictionary<string, object>;
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
                        Misc.hitSounds[num1] = evnt;
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
                HitSound hitSound1 = conductor.hitSound;
                List<scrFloor> listFloors = ADOBase.lm.listFloors;
                bool useMidspinHitSound = false;
                HitSound midspinHitSound = hitSound1;
                float volume1 = 1;
                double num4 = Tools.GetPrivateField<double>(conductor,"dspTimeSong") + conductor.addoffset / conductor.song.pitch;
                int num3 = GCS.checkpointNum < listFloors.Count ? GCS.checkpointNum + 1 : 1;
                int index3 = GCS.practiceMode ? GCS.checkpointNum + GCS.practiceLength : listFloors.Count - 1;
                LevelEvent currentEvent = null;
                Misc.hitSoundIndex = 0;
                Misc.hitSoundDatas = new();
                for (int i = 1; i < listFloors.Count; ++i)
                {
                    scrFloor scrFloor1 = listFloors[i];
                    ffxSetHitsound setHitsound = scrFloor1.setHitsound;
                    
                    if (setHitsound != null)
                    {
                        if (setHitsound.gameSound == GameSound.Midspin)
                        {
                            useMidspinHitSound = true;
                            midspinHitSound = setHitsound.hitSound;
                        }
                        else
                        {
                            hitSound1 = setHitsound.hitSound;
                            var hasSetMidspinHitsound =
                                Tools.GetPrivateField<bool>(conductor, "hasSetMidspinHitsound");
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
                        ADOBase.gc.hitSoundOffsets.TryGetValue(
                            !(scrFloor2 != null) || !scrFloor2.midSpin || useMidspinHitSound
                                ? hitSound1
                                : midspinHitSound, out num5);
                        double time1 = conductor.dspTimeSongPosZero + scrFloor1.entryTimePitchAdj - num5;
                        
                        if (i >= num3 && i <= index3 && time1 > conductor.dspTime && !scrFloor1.midSpin &&
                            hitSound1 != HitSound.None)
                        {
                            HitSound hitSound2 = !(scrFloor2 != null) || !scrFloor2.midSpin || !useMidspinHitSound
                                ? hitSound1
                                : midspinHitSound;
                            if (scrFloor1.tapsNeeded > 1)
                                hitSound2 = midspinHitSound;
                            
                            if (Misc.hitSounds.TryGetValue(i, out var setHitSound2))
                            {
                                currentEvent = setHitSound2;
                            }
                            
                            if (currentEvent != null && currentEvent.GetBool("customHitSound"))
                            {
                                if (!string.IsNullOrEmpty(currentEvent.GetString("selectAudioFile")))
                                {
                                    string audioUrl =
                                        Path.Combine(Path.GetDirectoryName(scnEditor.instance.customLevel.levelPath),
                                            currentEvent.GetString("selectAudioFile"));
                                    AudioClip clip = Main.AudioDownloader.DownloadAudioClip(audioUrl);
                                    clip.name = currentEvent.GetString("selectAudioFile");
                                    Misc.hitSoundDatas.Add(new HitSoundData(HitSound.None, time1, volume1, clip));
                                    Tools.log("add clip:" + clip.name);
                                }
                            }
                            else
                            {
                                Misc.hitSoundDatas.Add(new HitSoundData(hitSound2, time1, volume1));
                            }
                        }
                    }
                }
                return true;
            }
            /*public static void Postfix(scrConductor __instance)
            {
                var holdSounds = Tools.GetPrivateField<List<object>>(__instance, "holdSoundsData");
                Misc.holdSoundDatas.Clear();
                foreach (var holdSound in holdSounds)
                {
                    var name = Tools.GetField<string>(holdSound, "name");
                    var time = Tools.GetField<double>(holdSound, "time");
                    var endTime = Tools.GetField<double>(holdSound, "endTime");
                    var volume = Tools.GetField<float>(holdSound, "volume");
                    Misc.holdSoundDatas.Add(new HoldSoundData(name, time, endTime, volume));
                }
            }*/
        }

        [HarmonyPatch(typeof(scnEditor), "Play")]
        public class scnEditor_Play
        {
            public static void Postfix(scnEditor __instance)
            {
                if (Main.settings.enableBPMLimiter)
                {
                    if (__instance.customLevel.highestBPM >= Main.settings.BPMLimit - 1 && Misc.usedEventCount > 0)
                    {
                        __instance.SwitchToEditMode();
                        __instance.ShowNotification(Main.language.bpmTooHighWarning, Color.red,
                            2);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(scnEditor), "OpenLevelCo")]
        public static class scnEditor_OpenLevel
        {
            public static void Postfix()
            {
                Misc.usedEventCount = 0;
                Misc.hitSoundDatas.Clear();
                Misc.holdSoundDatas.Clear();
                Misc.hitSoundIndex = 0;
                Misc.holdSoundIndex = 0;
            }
        }

        [HarmonyPatch(typeof(scnEditor), "SwitchToEditMode")]
        public static class scnEditor_SwitchToEditMode
        {
            public static void Postfix()
            {
                Misc.usedEventCount = 0;
            }
        }

        [HarmonyPatch(typeof(scrConductor), "Update")]
        public class scrConductor_Update
        {
            static int nextHitSoundToSchedule = Tools.GetPrivateField<int>(scrConductor.instance, "nextHitSoundToSchedule");
            public static bool Prefix(scrConductor __instance)
            {
                if (Misc.usedEventCount == 0) return true;
                try
                {
                    nextHitSoundToSchedule = Tools.GetPrivateField<int>(scrConductor.instance, "nextHitSoundToSchedule");
                    scrConductor conductor = __instance;
                    if (scrConductor.isAudioOutputDeviceChanged)
                    {
                        scrController.CheckForAudioOutputChange();
                        scrConductor.isAudioOutputDeviceChanged = false;
                    }

                    PlatformHelper.Instance.Update();
                    double previousFrameTime = Tools.GetPrivateField<double>(conductor, "previousFrameTime");
                    double lastReportedPlayheadPosition =
                        Tools.GetPrivateField<double>(conductor, "lastReportedPlayheadPosition");
                    if (!AudioListener.pause && Application.isFocused &&
                        (double)Time.unscaledTime - previousFrameTime < 0.10000000149011612)
                    {
                        conductor.dspTime += (double)Time.unscaledTime - previousFrameTime;
                        if (AsyncInputManager.isActive)
                            AsyncInputManager.dspTime +=
                                (double)Time.unscaledTime - AsyncInputManager.previousFrameTime;
                    }

                    Tools.SetPrivateField(conductor, "previousFrameTime", Time.unscaledTime);
                    if (AudioSettings.dspTime != lastReportedPlayheadPosition)
                    {
                        conductor.dspTime = AudioSettings.dspTime;
                        Tools.SetPrivateField(conductor, "lastReportedPlayheadPosition", AudioSettings.dspTime);
                    }

                    double dspTimeSong = Tools.GetPrivateField<double>(conductor, "dspTimeSong");
                    if (AsyncInputManager.isActive)
                    {
                        AsyncInputManager.prevFrameTick = AsyncInputManager.currFrameTick;
                        AsyncInputManager.currFrameTick = (ulong)DateTime.Now.Ticks;
                        if (!AudioListener.pause && Application.isFocused &&
                            (double)Time.unscaledTime - AsyncInputManager.previousFrameTime < 0.1)
                            AsyncInputManager.dspTime +=
                                (double)Time.unscaledTime - AsyncInputManager.previousFrameTime;
                        AsyncInputManager.previousFrameTime = (double)Time.unscaledTime;
                        if (AudioSettings.dspTime - AsyncInputManager.lastReportedDspTime != 0.0)
                        {
                            AsyncInputManager.lastReportedDspTime = AudioSettings.dspTime;
                            AsyncInputManager.dspTime = AudioSettings.dspTime;
                            AsyncInputManager.offsetTick = AsyncInputManager.currFrameTick -
                                                           (ulong)(AsyncInputManager.dspTime * 10000000.0);
                            AsyncInputManager.offsetTickUpdated = true;
                        }

                        AsyncInputManager.dspTimeSong = dspTimeSong;
                        if (ADOBase.controller != (UnityEngine.Object)null &&
                            !ADOBase.controller.paused)
                            ADOBase.controller.UpdateInput();
                    }

                    if (conductor.hasSongStarted && conductor.isGameWorld)
                    {
                        switch (ADOBase.controller.state)
                        {
                            case States.Fail:
                            case States.Fail2:
                                break;
                            default:
                                int nextExtraTickToSchedule =
                                    Tools.GetPrivateField<int>(conductor, "nextExtraTickToSchedule");
                                int nextHoldSoundToSchedule =
                                    Tools.GetPrivateField<int>(conductor, "nextHoldSoundToSchedule");
                                for (;
                                     nextExtraTickToSchedule < conductor.extraTicksCountdown.Count;
                                     ++nextExtraTickToSchedule)
                                {
                                    Tools.SetPrivateField(conductor, "nextExtraTickToSchedule",
                                        nextExtraTickToSchedule);
                                    double time = conductor.extraTicksCountdown[nextExtraTickToSchedule].time;
                                    if (conductor.dspTime + 5.0 > time)
                                        AudioManager.Play("sndHat", time, conductor.hitSoundGroup,
                                            conductor.hitSoundVolume,
                                            10);
                                    else
                                        break;
                                }

                                for (; nextHoldSoundToSchedule < Misc.holdSoundDatas.Count; ++nextHoldSoundToSchedule)
                                {
                                    HoldSoundData holdSoundsData = Misc.holdSoundDatas[nextHoldSoundToSchedule];
                                    if (conductor.dspTime + 5.0 > holdSoundsData.time)
                                    {
                                        if (holdSoundsData.endTime > 0.0)
                                            conductor.PlayWithEndTime(holdSoundsData.name, holdSoundsData.time,
                                                holdSoundsData.endTime, holdSoundsData.volume);
                                        else
                                            AudioManager.Play(holdSoundsData.name, holdSoundsData.time,
                                                conductor.hitSoundGroup, holdSoundsData.volume);
                                    }
                                    else
                                        break;
                                }
                                for (; Misc.hitSoundIndex < Misc.hitSoundDatas.Count; ++Misc.hitSoundIndex)
                                {
                                    HitSoundData hitSoundsData = Misc.hitSoundDatas[Misc.hitSoundIndex];
                                    if (hitSoundsData.time < conductor.dspTime + 5.0)
                                    {
                                        if (hitSoundsData.clip == null)
                                        {
                                            AudioManager.Play("snd" + hitSoundsData.hitSound, hitSoundsData.time, conductor.hitSoundGroup, hitSoundsData.volume);
                                        }
                                        else
                                        {
                                            Tools.PlayAudioClip(hitSoundsData.clip,conductor.hitSoundGroup,hitSoundsData.volume, hitSoundsData.time);
                                        }
                                    }
                                    else
                                        break;
                                    
                                }
                                break;
                        }
                    }
                    conductor.crotchetAtStart = 60.0 / (double)conductor.bpm;
                    double songpositionMinusi1 = conductor.songposition_minusi;
                    conductor.songposition_minusi = GCS.d_oldConductor || GCS.d_webglConductor
                        ? (double)(conductor.song.time - scrConductor.calibration_i) -
                          conductor.addoffset / (double)conductor.song.pitch
                        : (double)((float)(conductor.dspTime - dspTimeSong) - scrConductor.calibration_i) *
                        (double)conductor.song.pitch - conductor.addoffset;
                    conductor.deltaSongPos = conductor.songposition_minusi - songpositionMinusi1;
                    conductor.deltaSongPos = Math.Max(conductor.deltaSongPos, 0.0);
                    double nextBeatTime = Tools.GetPrivateField<double>(conductor, "nextBeatTime");
                    if (conductor.songposition_minusi > nextBeatTime)
                    {
                        conductor.OnBeat();
                        nextBeatTime += conductor.crotchetAtStart;
                        Tools.SetPrivateField(conductor, "nextBeatTime", nextBeatTime);
                        ++conductor.beatNumber;
                    }

                    double nextBarTime = Tools.GetPrivateField<double>(conductor, "nextBarTime");
                    int crotchetsPerBar = Tools.GetPrivateField<int>(conductor, "crotchetsPerBar");
                    if (conductor.songposition_minusi > nextBarTime)
                    {
                        nextBarTime += conductor.crotchetAtStart * (double)crotchetsPerBar;
                        Tools.SetPrivateField(conductor, "nextBarTime", nextBarTime);
                        ++conductor.barNumber;
                    }

                    if (!conductor.getSpectrum || GCS.lofiVersion)
                        return false;
                    AudioSource audioSource = conductor.song;
                    if (conductor.CLSComponent != null)
                    {
                        PreviewSongPlayer previewSongPlayer = conductor.CLSComponent.previewSongPlayer;
                        if (previewSongPlayer.playing)
                            audioSource = previewSongPlayer.audioSource;
                    }

                    audioSource.GetSpectrumData(conductor.spectrum, 0, FFTWindow.BlackmanHarris);
                    return false;
                }
                catch (Exception e)
                {
                    Tools.log(e);
                    return true;
                }
            }
        }
    }
}