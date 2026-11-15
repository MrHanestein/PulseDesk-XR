using UnityEngine;

namespace PulseDesk
{
    /// <summary>Small sounds generated in code, so there are no audio files to license or import.</summary>
    public class AudioFeedback : MonoBehaviour
    {
        private AudioSource _source;
        private AudioClip _tick;
        private AudioClip _chime;
        private AudioClip _start;
        private AudioClip _alert;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.5f;

            _tick = Tones("tick", new[] { 1320f }, 0.06f);
            _chime = Tones("chime", new[] { 1046.5f, 1318.5f, 1568f }, 0.14f);
            _start = Tones("start", new[] { 523.25f, 784f }, 0.10f);
            _alert = Tones("alert", new[] { 660f, 660f }, 0.16f);
        }

        public void Tick() { Play(_tick); }
        public void Chime() { Play(_chime); }
        public void SprintStart() { Play(_start); }
        public void AlertTone() { Play(_alert); }

        private void Play(AudioClip clip)
        {
            if (_source != null && clip != null) _source.PlayOneShot(clip);
        }

        private static AudioClip Tones(string clipName, float[] frequencies, float secondsEach)
        {
            const int rate = 44100;
            int per = Mathf.Max(1, (int)(rate * secondsEach));
            float[] data = new float[per * frequencies.Length];
            for (int n = 0; n < frequencies.Length; n++)
            {
                for (int i = 0; i < per; i++)
                {
                    float t = i / (float)rate;
                    float attack = Mathf.Min(1f, i / 220f);
                    float decay = Mathf.Exp(-5f * i / per);
                    data[n * per + i] = Mathf.Sin(2f * Mathf.PI * frequencies[n] * t) * attack * decay * 0.6f;
                }
            }
            AudioClip clip = AudioClip.Create(clipName, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
