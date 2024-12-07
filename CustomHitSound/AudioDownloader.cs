// AudioDownloader
using System;
using System.IO;
using System.Net;
using UnityEngine;

namespace CustomHitSound
{
    public class AudioDownloader
    {
        private string lastDownloadedPath = string.Empty;

        private AudioClip lastReturnedClip;

        public AudioClip DownloadAudioClip(string audioUrl)
        {
            if (audioUrl == lastDownloadedPath)
            {
                return lastReturnedClip;
            }
            try
            {
                WebResponse response = WebRequest.Create(audioUrl).GetResponse();
                Stream responseStream = response.GetResponseStream();
                string text = Path.Combine(Application.temporaryCachePath, "tempAudioFile");
                using (FileStream destination = new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    responseStream.CopyTo(destination);
                }
                responseStream.Close();
                response.Close();
                AudioClip result = null;
                using (WWW wWW = new WWW("file://" + text))
                {
                    while (!wWW.isDone)
                    {
                    }
                    AudioType audioType = AudioType.UNKNOWN;
                    switch (Path.GetExtension(audioUrl))
                    {
                        case ".ogg":
                            audioType = AudioType.OGGVORBIS;
                            break;
                        case ".wav":
                            audioType = AudioType.WAV;
                            break;
                        case ".mp3":
                            audioType = AudioType.MPEG;
                            break;
                        case ".aiff":
                            audioType = AudioType.AIFF;
                            break;
                    }
                    if (string.IsNullOrEmpty(wWW.error))
                    {
                        result = wWW.GetAudioClip(threeD: false, stream: false, audioType);
                    }
                    else
                    {
                        Debug.LogError("Error loading audio clip: " + wWW.error);
                    }
                }
                lastDownloadedPath = audioUrl;
                lastReturnedClip = result;
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error downloading audio file: " + ex.Message);
                return null;
            }
        }
    }

}