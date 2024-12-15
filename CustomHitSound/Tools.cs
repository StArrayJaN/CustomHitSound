using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using ADOFAI;
using JetBrains.Annotations;
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
                AudioManager audioManager = AudioManager.Instance;
                GameObject gameObject = GetPrivateField<GameObject>(audioManager, "audioSourceContainer");
                AudioSource audioSource = null;
                try
                {
                    audioManager.reusableSources.Dequeue();
                }
                catch (Exception e)
                {
                    audioSource = UnityEngine.Object.Instantiate(audioManager.audioSourcePrefab, gameObject.transform);
                }
                audioSource?.gameObject.SetActive(true);
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
        
        public static T GetField<T>(object instance, string fieldName)
        {
            Type type = instance.GetType();
            FieldInfo fieldInfo = type.GetField(fieldName,  BindingFlags.Instance);
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

        public static void log(object info = null,bool printStack = false)
        {
            string message = info == null? "log" : info.ToString();
            string stack = printStack ? "\n" + new StackTrace(true) : "";
            Main.Logger.Log(message + stack);
        }

        public static float calculateDelayTime()
        {
            int tileCount = scnGame.instance.levelData.angleData.Count;
            return tileCount / 2500.0f;
        }

        public static string ToString(Dictionary<string, object> dictionary)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kvpair in dictionary)
            {
                sb.Append(kvpair.Key);
                sb.Append(" : ");
                sb.Append(kvpair.Value + "\n");
            }
            return sb.ToString();
        }
    }
}