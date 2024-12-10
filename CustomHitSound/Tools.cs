using System;
using System.Diagnostics;
using System.Reflection;
using ADOFAI;
using LightJson;
using UnityEngine;
using UnityEngine.Audio;

namespace CustomHitSound
{
    public static class Tools
    {
        public static JsonObject GetEvent(JsonArray array, LevelEventType name)
        {
            foreach (JsonValue item in array)
            {
                if (item["name"]== name.ToString()) return item.AsJsonObject;
            }
            return null;
        }

        public static void PlayAudioClip(AudioClip clip, AudioMixerGroup group,
            float volume = 1, double time = 0)
        { 
            if (clip != null)
            {
                Main.Logger.Log("play:" + clip.name);
                AudioManager audioManager = AudioManager.Instance;
                Type type = audioManager.GetType();
                FieldInfo fieldInfo = type.GetField(nameof(AudioManager.audioSourcePrefab),
                    BindingFlags.NonPublic | BindingFlags.Instance);
                GameObject gameObject = fieldInfo?.GetValue(audioManager) as GameObject;
                AudioSource audioSource = audioManager.reusableSources.Dequeue();
                if (audioSource == null)
                    audioSource =
                        UnityEngine.Object.Instantiate(audioManager.audioSourcePrefab, gameObject?.transform);
                audioSource.gameObject.SetActive(true);
                audioSource.clip = clip;
                audioSource.pitch = 1f;
                audioSource.outputAudioMixerGroup = !(group != null) ? audioManager.fallbackMixerGroup : group;
                audioSource.volume = volume;
                audioSource.priority = 128;
                audioSource.PlayScheduled(time);

                float num = (bool)(UnityEngine.Object)audioSource.clip
                    ? audioSource.clip.length
                    : float.PositiveInfinity;
                audioManager.liveSources.Enqueue(audioSource, time + num);
            }
        }

        public static T GetPrivateField<T>(object instance, string fieldName)
        {
            Type type = instance.GetType();
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T) fieldInfo?.GetValue(instance);
        }

        public static void SetPrivateField(object instance, string fieldName, object value)
        {
            Type type = instance.GetType();
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo?.SetValue(instance, value);
        }

        public static T CallMethod<T>(object instance, string methodName, params object[] args)
        {
            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T) methodInfo?.Invoke(instance, args);
        }
        public static void CallMethod(object instance, string methodName, params object[] args)
        {
            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo?.Invoke(instance, args);
        }

        public static void log(object obj = null)
        {
            string obj2 = obj == null? "log" : obj.ToString();
            Main.Logger.Log(obj2 + "\n" + new StackTrace(true));
        }

        public static float calculateDelayTime()
        {
            int tileCount = scnGame.instance.levelData.angleData.Count;
            return tileCount / 2500.0f;
        }
    }
}