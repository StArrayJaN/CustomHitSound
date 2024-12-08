using System;
using System.Diagnostics;
using System.Reflection;
using ADOFAI;
using LightJson;
using UnityEngine;
using UnityEngine.Audio;

namespace CustomHitSound
{
    public class Tools
    {
        public static JsonObject GetEvent(JsonArray array, LevelEventType name)
        {
            foreach (JsonValue item in array)
            {
                if (item["name"]== name.ToString()) return item.AsJsonObject;
            }
            return null;
        }

        public static AudioSource PlayAudioClip(AudioClip clip,AudioMixerGroup group,float volume = 1,double time = 0)
        {
            if (clip != null)
            {
                MainClass.Logger.Log("play:" +clip.name);
                AudioManager audioManager = AudioManager.Instance;
                Type type = audioManager.GetType();
                FieldInfo fieldInfo = type.GetField(nameof(AudioManager.audioSourcePrefab), BindingFlags.NonPublic | BindingFlags.Instance);
                GameObject gameObject = fieldInfo?.GetValue(audioManager) as GameObject;
                AudioSource audioSource = audioManager.reusableSources.Dequeue();
                if (audioSource == null)
                    audioSource = UnityEngine.Object.Instantiate(audioManager.audioSourcePrefab, gameObject?.transform);
                audioSource.gameObject.SetActive(true);
                audioSource.clip = clip;
                audioSource.pitch = 1f;
                audioSource.outputAudioMixerGroup = !( group != null) ? audioManager.fallbackMixerGroup : group;
                audioSource.volume = volume;
                audioSource.priority = 128;
                audioSource.PlayScheduled(time);
                
                float num = (bool) (UnityEngine.Object) audioSource.clip ? audioSource.clip.length : float.PositiveInfinity;
                audioManager.liveSources.Enqueue(audioSource, time + num);
                return audioSource;
            }
            return null;
        }

        public static T GetPrivateField<T>(object obj, string fieldName)
        {
            Type type = obj.GetType();
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T) fieldInfo?.GetValue(obj);
        }

        public static void SetPrivateField(object obj, string fieldName, object value)
        {
           Type type = obj.GetType();
           FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
           fieldInfo?.SetValue(obj, value);
        }

        public static void log(object obj = null)
        {
            string obj2 = obj == null? "log" : obj.ToString();
            MainClass.Logger.Log(obj2 + "\n" + new StackTrace(true));
        }
    }
}