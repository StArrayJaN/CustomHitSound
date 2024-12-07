using System.Collections.Generic;
using UnityEngine;

namespace CustomHitSound
{
    public static class Misc
    {
        public static List<HitSoundsData> hitSounds = new();
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
            this.played = false;
            this.clip = clip;
        }
    }
}