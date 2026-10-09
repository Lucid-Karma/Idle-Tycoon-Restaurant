using System;
using UnityEngine;

// Short sounds for what happens in the cafe, so the player hears it without looking: money coming in, a
// customer sitting down or leaving hungry, food flying, the chef getting splatted or slipping, a burger
// done, the cafe levelling up - and the kitchen itself: picking food up and putting it down, the knife on the
// board, the patty sizzling, a bun or patty ready (a bright ding) or burnt (a poof), a bin. One AudioSource playing one-shots with a little pitch jitter so repeats
// don't grate. Silent while the sound is off (the speaker button, Audio.IsMusicOn).
// Lives on a scene object (<<<Controllers>>>/Sfx): gameplay calls GameSfx.Play(cue); cues that already
// have an event (order rated, customer protest, level up) are picked up here.
[RequireComponent(typeof(AudioSource))]
public class GameSfx : MonoBehaviour
{
    // Append new cues at the end: the scene stores them by index.
    public enum Cue { Coin, Tip, Seated, LeftHungry, CustomerThrow, ChefThrow, Splat, Slip, Bonk, BurgerDone, LevelUp, ArrowShot, ArrowHit,
        Pickup, PutDown, Chop, Ready, Burnt, Trash }

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
    // A patty on a pan sizzles for as long as it is on the heat (any pan; the loop fades in and out).
    [SerializeField] private AudioClip sizzleLoop;
    [SerializeField, Range(0f, 1f)] private float sizzleVolume = 0.22f;

    private static readonly System.Collections.Generic.HashSet<UnityEngine.Object> sizzlers = new();
    private AudioSource sizzle;

    private static GameSfx instance;
    // A few voices: pitch is per source, so overlapping sounds each get their own.
    private AudioSource[] voices;
    private int nextVoice;
    private readonly float[] lastPlayed = new float[Enum.GetValues(typeof(Cue)).Length];

    public static void Play(Cue cue)
    {
        if (instance != null) instance.PlayCue(cue);
    }

    // A pan says whether it is frying right now.
    public static void Sizzle(UnityEngine.Object pan, bool on)
    {
        if (on) sizzlers.Add(pan); else sizzlers.Remove(pan);
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

        sizzlers.Clear();
        if (sizzleLoop != null)
        {
            sizzle = gameObject.AddComponent<AudioSource>();
            sizzle.clip = sizzleLoop;
            sizzle.loop = true;
            sizzle.playOnAwake = false;
            sizzle.volume = 0f;
            sizzle.spatialBlend = 0f;
            sizzle.outputAudioMixerGroup = first.outputAudioMixerGroup;
        }
    }

    private void Update()
    {
        if (sizzle == null) return;
        sizzlers.RemoveWhere(x => x == null);
        // Paused (menus, the result card): the kitchen goes quiet too.
        float target = sizzlers.Count > 0 && Audio.IsMusicOn && Time.timeScale > 0f ? sizzleVolume : 0f;
        sizzle.volume = Mathf.MoveTowards(sizzle.volume, target, Time.unscaledDeltaTime * sizzleVolume * 4f);
        if (sizzle.volume > 0f && !sizzle.isPlaying) { sizzle.time = UnityEngine.Random.Range(0f, sizzle.clip.length); sizzle.Play(); }
        else if (sizzle.volume <= 0f && sizzle.isPlaying) sizzle.Stop();
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
        EventManager.OnFoodHolded.AddListener(OnPickup);
        CafeProgress.LeveledUp += OnLevelUp;
    }

    private void OnDisable()
    {
        EventManager.OnOrderRated.RemoveListener(OnOrderRated);
        EventManager.OnCustomerProtest.RemoveListener(OnProtest);
        EventManager.OnFoodHolded.RemoveListener(OnPickup);
        CafeProgress.LeveledUp -= OnLevelUp;
    }

    private void OnOrderRated() => PlayCue(Cue.Coin);
    private void OnProtest() => PlayCue(Cue.LeftHungry);
    private void OnPickup() => PlayCue(Cue.Pickup);
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
