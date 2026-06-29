using System;
using System.Collections.Generic;
using Boot.Script;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Script.Manager
{
    [Serializable]
    public enum AudioType
    {
        Master,
        Bgm,
        SFX,
    }

    [Serializable]
    public class AudioEntry
    {
        public string audioName;
        public AudioType type;
        public bool isLooping;
    }

    public class AudioManager : MonoSingleton<AudioManager>
    {
        [SerializeField] private AudioMixer audioMixer;

        private AudioSource selfAudioSource;

        private readonly Dictionary<string, AudioClip> clipCache = new();
        private readonly Dictionary<string, AssetWrapper<AudioClip>> wrapperCache = new();

        private bool isMuted;
        private float lastVolume = 1f;

        private void Awake()
        {
            selfAudioSource = gameObject.AddComponent<AudioSource>();
        }

        #region Load

        private async UniTask<AudioClip> LoadClipAsync(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return null;
            if (clipCache.TryGetValue(clipName, out var cached)) return cached;

            var fullPath = ResPrefix.Audio + clipName;
            var wrapper = await YooAssetManager.Instance.LoadAssetAsync<AudioClip>(fullPath);
            if (!wrapper?.Asset) return null;

            clipCache[clipName] = wrapper.Asset;
            wrapperCache[clipName] = wrapper;
            return wrapper.Asset;
        }

        #endregion

        #region Play

        /// <summary>
        /// 用自己的 AudioSource 播放
        /// </summary>
        public void PlaySound(AudioEntry audioEntry)
        {
            if (audioEntry == null || string.IsNullOrEmpty(audioEntry.audioName)) return;
            PlaySoundInternal(audioEntry, selfAudioSource).Forget();
        }

        /// <summary>
        /// 用外部 AudioSource 播放
        /// </summary>
        public void PlaySound(AudioEntry audioEntry, AudioSource audioSource)
        {
            if (audioEntry == null || string.IsNullOrEmpty(audioEntry.audioName) || !audioSource) return;
            PlaySoundInternal(audioEntry, audioSource).Forget();
        }

        /// <summary>
        /// 一次性音效
        /// </summary>
        public void PlayShot(AudioEntry audioEntry)
        {
            if (audioEntry == null || string.IsNullOrEmpty(audioEntry.audioName)) return;
            PlayShotInternal(audioEntry).Forget();
        }

        private AudioMixerGroup GetGroup(AudioType type)
        {
            if (!audioMixer) return null;
            var groups = audioMixer.FindMatchingGroups(type.ToString());
            return groups.Length > 0 ? groups[0] : null;
        }

        private async UniTask PlaySoundInternal(AudioEntry entry, AudioSource source)
        {
            var clip = await LoadClipAsync(entry.audioName);
            if (!clip) return;
            if (source.clip == clip && source.isPlaying) return;
            source.clip = clip;
            source.loop = entry.isLooping;
            source.outputAudioMixerGroup = GetGroup(entry.type);
            source.Play();
        }

        private async UniTask PlayShotInternal(AudioEntry entry)
        {
            var clip = await LoadClipAsync(entry.audioName);
            if (!clip) return;
            selfAudioSource.outputAudioMixerGroup = GetGroup(entry.type);
            selfAudioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// 停止自身播放
        /// </summary>
        public void StopBgm()
        {
            selfAudioSource?.Stop();
        }

        #endregion

        #region Volume

        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            lastVolume = volume;
            if (isMuted) return;
            ApplyVolume(volume);
        }

        public float GetMasterVolume() => lastVolume;

        public void ToggleMute()
        {
            isMuted = !isMuted;
            ApplyVolume(isMuted ? 0f : lastVolume);
        }

        public bool IsMuted => isMuted;

        private void ApplyVolume(float volume)
        {
            if (!audioMixer) return;
            var dB = volume <= 0.001f ? -80f : 20f * Mathf.Log10(volume);
            audioMixer.SetFloat(nameof(AudioType.Master), dB);
        }

        #endregion
    }
}