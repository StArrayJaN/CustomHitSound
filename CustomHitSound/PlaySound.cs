using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

namespace CustomHitSound
{
    public class PlaySound : ffxPlaySound
    {
        public string filePath { get; set; }
        public bool enableCustomHitSound { get; set; }
        private AudioClip _audioClip;
        
        public override void StartEffect()
        {
            if (enableCustomHitSound)
            {
                
                string path = Path.Combine(Path.GetDirectoryName(levelPath), filePath);
                if (_audioClip == null) _audioClip = MainClass.AudioDownloader.DownloadAudioClip(path);
                double num = ffxPlusBase.conductor.dspTimeSongPosZero + startTime / ffxPlusBase.conductor.song.pitch;
                double value;
                MainClass.Logger.Log("play:" + (_audioClip == null));
                ffxPlusBase.gc.hitSoundOffsets.TryGetValue(hitSound, out value);
                Tools.PlayAudioClip(_audioClip, 
                    RDUtils.GetMixerGroup(MixerGroup.ConductorPlaySound), 
                    volume,
                    num - value);
            }
            else
            {
                base.StartEffect();
            }
        }
        
        public IEnumerator LoadAudioClip(string filePath)
        {
            // 使用 "file://" 前缀来指定本地文件路径
            string url = "file://" + filePath;

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    _audioClip = DownloadHandlerAudioClip.GetContent(www);
                }
            }
        }
    }
}