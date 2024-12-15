using System.Collections.Generic;
using ADOFAI;
using UnityEngine;

namespace CustomHitSound
{
    public class Misc
    {
        public static List<HitSoundData> hitSoundDatas = new();
        public static List<HoldSoundData> holdSoundDatas = new();
        public static Dictionary<int,LevelEvent> hitSounds = new();
        public static int usedEventCount;

        public static int hitSoundIndex;
        public static int holdSoundIndex;
    }

    public struct HitSoundData
    {
        public HitSound hitSound;
        public double time;
        public float volume;
        public bool played;
        public AudioClip clip;

        public HitSoundData(HitSound hitSound, double time, float volume,AudioClip clip =null)
        {
            this.hitSound = hitSound;
            this.time = time;
            this.volume = volume;
            played = false;
            this.clip = clip;
        }

        public override string ToString()
        {
            return $"HitSoundData: {hitSound} at {time} with volume {volume}";
        }
    }
    public struct HoldSoundData
    {
        public string name;
        public double time;
        public double endTime;
        public float volume;
        public bool played;

        public HoldSoundData(string name, double time, double endTime, float volume)
        {
            this.name = name;
            this.time = time;
            this.endTime = endTime;
            this.volume = volume;
            this.played = false;
        }
    }

}