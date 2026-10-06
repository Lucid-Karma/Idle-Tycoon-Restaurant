using System;
using UnityEngine;

// Short sounds for what happens in the cafe, so the player hears it without looking: money coming in, a
// customer sitting down or leaving hungry, food flying, the chef getting splatted or slipping, a burger
// done, the cafe levelling up. One AudioSource playing one-shots with a little pitch jitter so repeats
// don't grate. Silent while the sound is off (the speaker button, Audio.IsMusicOn).
// Lives on a scene object (<<<Controllers>>>/Sfx): gameplay calls GameSfx.Play(cue); cues that already
// have an event (order rated, customer protest, level up) are picked up here.
[RequireComponent(typeof(AudioSource))]
public class GameSfx : MonoBehaviour
{
    // Append new cues at the end: the scene stores them by index.
    public enum Cue { Coin, Tip, Seated, LeftHungry, CustomerThrow, ChefThrow, Splat, Slip, Bonk, BurgerDone, LevelUp, ArrowShot, ArrowHit }

    [Serializable]
    private struct Sound
    {
        public Cue cue;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume;
        public float pitch;
        public float pitchJitter;
    }

    [SerializeField] private Sound[] sounds = Array.Empty<Sound>();

    private static GameSfx instance;
    // A few voices: pitch is per source, so overlapping sounds each get their own.
    private AudioSource[] voices;
    private int nextVoice;
    private readonly float[] lastPlayed = new float[Enum.GetValues(typeof(Cue)).Length];

    public static void Play(Cue cue)
    {
        if (instance != null) instance.PlayCue(cue);
    }

    private void Awake()
    {
        instance = this;
        var first = GetComponent<AudioSource>();
        first.playOnAwake = false;
        voices = new AudioSource[4];
        voices[0] = first;
        for (int i = 1; i < voices.Length; i++)
        {
            var voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.outputAudioMixerGroup = first.outputAudioMixerGroup;
            voice.spatialBlend = 0f;
            voice.volume = first.volume;
            voices[i] = voice;
        }
        for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = float.NegativeInfinity;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Named methods: these events are static and outlive a Replay.
    private void OnEnable()
    {
        EventManager.OnOrderRated.AddListener(OnOrderRated);
        EventManager.OnCustomerProtest.AddListener(OnProtest);
        CafeProgress.LeveledUp += OnLevelUp;
    }

    private void OnDisable()
    {
        EventManager.OnOrderRated.RemoveListener(OnOrderRated);
        EventManager.OnCustomerProtest.RemoveListener(OnProtest);
        CafeProgress.LeveledUp -= OnLevelUp;
    }

    private void OnOrderRated() => PlayCue(Cue.Coin);
    private void OnProtest() => PlayCue(Cue.LeftHungry);
    private void OnLevelUp(int level) => PlayCue(Cue.LevelUp);

    private void PlayCue(Cue cue)
    {
        if (!Audio.IsMusicOn) return;
        // The same cue twice in one moment (two tips at once) plays once.
        float now = Time.unscaledTime;
        if (now - lastPlayed[(int)cue] < 0.06f) return;
        lastPlayed[(int)cue] = now;

        foreach (var sound in sounds)
        {
            if (sound.cue != cue || sound.clips == null || sound.clips.Length == 0) continue;
            var clip = sound.clips[UnityEngine.Random.Range(0, sound.clips.Length)];
            if (clip == null) return;
            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = (sound.pitch > 0f ? sound.pitch : 1f) * (1f + UnityEngine.Random.Range(-sound.pitchJitter, sound.pitchJitter));
            voice.PlayOneShot(clip, sound.volume);
            return;
        }
    }
}
