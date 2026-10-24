using System;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    /// <summary>
    /// License-clean, high-fidelity procedural audio manager for SiteShield.
    /// Synthesizes UI clicks, score chimes, caution tones, equipment snaps, and portal effects
    /// at runtime using pure mathematical waveforms without external audio assets.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SafetyAudioManager : MonoBehaviour
    {
        public static SafetyAudioManager Instance { get; private set; }

        [Range(0f, 1f)] public float masterVolume = 0.85f;
        [Range(0f, 1f)] public float sfxVolume = 0.90f;
        public bool isMuted = false;

        AudioSource audioSource;

        AudioClip clipClick;
        AudioClip clipHover;
        AudioClip clipScoreGain;
        AudioClip clipPenalty;
        AudioClip clipPlacementSnap;
        AudioClip clipComplete;
        AudioClip clipPortal;
        AudioClip clipCoachChirp;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D interface audio
            }

            GenerateProceduralClips();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void GenerateProceduralClips()
        {
            clipClick = CreateClickClip();
            clipHover = CreateHoverClip();
            clipScoreGain = CreateScoreChimeClip();
            clipPenalty = CreatePenaltyClip();
            clipPlacementSnap = CreateSnapClip();
            clipComplete = CreateFanfareClip();
            clipPortal = CreatePortalWhooshClip();
            clipCoachChirp = CreateChirpClip();
        }

        public void PlayClick() => PlayOneShot(clipClick, 0.7f);
        public void PlayHover() => PlayOneShot(clipHover, 0.35f);
        public void PlayScoreGain() => PlayOneShot(clipScoreGain, 0.85f);
        public void PlayPenalty() => PlayOneShot(clipPenalty, 0.80f);
        public void PlayPlacementSnap() => PlayOneShot(clipPlacementSnap, 0.90f);
        public void PlayObjectiveComplete() => PlayOneShot(clipComplete, 0.90f);
        public void PlayPortal() => PlayOneShot(clipPortal, 0.75f);
        public void PlayCoachChirp() => PlayOneShot(clipCoachChirp, 0.65f);

        void PlayOneShot(AudioClip clip, float volumeScale)
        {
            if (isMuted || clip == null || audioSource == null)
                return;

            var volume = masterVolume * sfxVolume * volumeScale;
            if (volume > 0.001f)
                audioSource.PlayOneShot(clip, volume);
        }

        // ==========================================
        // Procedural Audio Synthesizers (44.1kHz)
        // ==========================================

        static AudioClip CreateClickClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.045f;
            var sampleCount = (int)(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / sampleRate;
                var envelope = Mathf.Exp(-t * 90f);
                var sine = Mathf.Sin(2f * Mathf.PI * 920f * t);
                samples[i] = sine * envelope * 0.8f;
            }

            var clip = AudioClip.Create("Procedural_Click", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip CreateHoverClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.025f;
            var sampleCount = (int)(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / sampleRate;
                var envelope = Mathf.Exp(-t * 140f);
                var sine = Mathf.Sin(2f * Mathf.PI * 1200f * t);
                samples[i] = sine * envelope * 0.5f;
            }

            var clip = AudioClip.Create("Procedural_Hover", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip CreateScoreChimeClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.32f;
            var sampleCount = (int)(sampleRate * duration);
            var samples = new float[sampleCount];

            // 2-tone pleasant industrial confirmation: C5 (523Hz) -> E5 (659Hz)
            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / sampleRate;
                var freq = t < 0.12f ? 523.25f : 659.25f;
                var envelope = Mathf.Exp(-((t % 0.12f) * 16f));
                var totalEnv = Mathf.Clamp01((duration - t) / duration);
                var sine = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f +
                           Mathf.Sin(4f * Mathf.PI * freq * t) * 0.2f;
                samples[i] = sine * envelope * totalEnv;
            }

            var clip = AudioClip.Create("Procedural_ScoreChime", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip CreatePenaltyClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.28f;
            var sampleCount = (int)(sampleRate * duration);
            var samples = new float[sampleCount];

            // Restrained double low tone: 220Hz (A3) -> 196Hz (G3)
            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / sampleRate;
                var freq = t < 0.12f ? 220f : 196f;
                var envelope = Mathf.Exp(-((t % 0.12f) * 22f));
                var sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = sine * envelope * 0.65f;
            }

            var clip = AudioClip.Create("Procedural_Penalty", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip CreateSnapClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.18f;
            var sampleCount = (int)(sampleRate * duration);
            var samples = new float[sampleCount];

            // Crisp mechanical latch: transient pulse + metallic decay
            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / sampleRate;
                var env = Mathf.Exp(-t * 35f);
                var tone = Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.5f +
                           Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.35f +
                           (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 120f) * 0.3f;
                samples[i] = tone * env;
            }

            var clip = AudioClip.Create("Procedural_Snap", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip CreateFanfareClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.55f;
            var sampleCount = (int)(sampleRate * duration);
            var samples = new float[sampleCount];

            // 3-tone ascending major chord: C5 -> E5 -> G5
            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / sampleRate;
                float freq;
                if (t < 0.14f) freq = 523.25f;
                else if (t < 0.28f) freq = 659.25f;
                else freq = 783.99f;

                var segTime = t < 0.28f ? (t % 0.14f) : (t - 0.28f);
                var segEnv = Mathf.Exp(-segTime * (t < 0.28f ? 14f : 6f));
                var sine = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f +
                           Mathf.Sin(4f * Mathf.PI * freq * t) * 0.25f;
                samples[i] = sine * segEnv;
            }

            var clip = AudioClip.Create("Procedural_Complete", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip CreatePortalWhooshClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.42f;
            var sampleCount = (int)(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / sampleRate;
                var normT = t / duration;
                var envelope = Mathf.Sin(normT * Mathf.PI);
                var sweepFreq = Mathf.Lerp(250f, 680f, normT);
                var tone = Mathf.Sin(2f * Mathf.PI * sweepFreq * t) * 0.45f +
                           (UnityEngine.Random.value * 2f - 1f) * 0.2f;
                samples[i] = tone * envelope;
            }

            var clip = AudioClip.Create("Procedural_Portal", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioClip CreateChirpClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.16f;
            var sampleCount = (int)(sampleRate * duration);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / sampleRate;
                var env = Mathf.Exp(-t * 26f);
                var freq = 620f + Mathf.Sin(2f * Mathf.PI * 18f * t) * 120f;
                var sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = sine * env * 0.6f;
            }

            var clip = AudioClip.Create("Procedural_Chirp", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
