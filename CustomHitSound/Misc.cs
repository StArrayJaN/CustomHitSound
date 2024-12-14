using System.Collections.Generic;
using ADOFAI;
using UnityEngine;

namespace CustomHitSound
{
    public class Misc
    {
        public static List<HitSoundsData> hitSoundDatas = new();
        public static Dictionary<int,LevelEvent> hitSounds = new();
        public static int usedEventCount;
    }

    public struct HitSoundsData
    {
        public HitSound hitSound;
        public double time;
        public float volume;
        public bool played;
        public AudioClip clip;

        public HitSoundsData(HitSound hitSound, double time, float volume,AudioClip clip =null)
        {
            this.hitSound = hitSound;
            this.time = time;
            this.volume = volume;
            played = false;
            this.clip = clip;
        }
    }
}