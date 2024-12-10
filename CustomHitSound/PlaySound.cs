using System.IO;
using UnityEngine;

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
                string path = Path.Combine(Path.GetDirectoryName(levelPath) ?? string.Empty, filePath);
                if (_audioClip == null) _audioClip = Main.AudioDownloader.DownloadAudioClip(path);
                double num = conductor.dspTimeSongPosZero + startTime / conductor.song.pitch;
                gc.hitSoundOffsets.TryGetValue(hitSound, out var value);
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
    }
}