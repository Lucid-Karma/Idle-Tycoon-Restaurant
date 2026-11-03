using System;
using UnityEngine;

// Short sounds for what happens in the cafe, so the player hears it without looking: money coming in, a
// customer sitting down or leaving hungry, food flying, the chef getting splatted or slipping, a burger
// done, the cafe levelling up - and the kitchen itself: picking food up and putting it down, the knife on the
// board, the patty sizzling, a bun or patty ready (a bright ding) or burnt (a poof), a bin, the dishes. One AudioSource playing one-shots with a little pitch jitter so repeats
// don't grate. Silent while the sound is off (the speaker button, Audio.IsMusicOn).
// Lives on a scene object (<<<Controllers>>>/Sfx): gameplay calls GameSfx.Play(cue); cues that already
// have an event (order rated, customer protest, level up) are picked up here.
[RequireComponent(typeof(AudioSource))]
public class GameSfx : MonoBehaviour
{
    // Append new cues at the end: the scene stores them by index.
    public enum Cue { Coin, Tip, Seated, LeftHungry, CustomerThrow, ChefThrow, Splat, Slip, Bonk, BurgerDone, LevelUp, ArrowShot, ArrowHit,
        Pickup, PutDown, Chop, Ready, Burnt, Trash, Dish }

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

    // Every cue as it happens, sound on or off: the moments themselves, for whoever wants to react to them
    // (the camera experiment shakes on a few - Experiments/CameraAB).
    public static event Action<Cue> Played;

    public static void Play(Cue cue)
    {
        Played?.Invoke(cue);
        if (instance != null) instance.PlayCue(cue);
    }

    // A pan says whether it is frying right now.
    public static void Sizzle(UnityEngine.Object pan, bool on)
    {
        if (on) sizzlers.Add(pan); else sizzlers.Remove(pan);
    }

    // The fryer says whether its oil is bubbling (its own crackle, not the pan's sizzle).
    public static void Fry(UnityEngine.Object fryer, bool on)
    {
        if (on) friers.Add(fryer); else friers.Remove(fryer);
    }

    // The fryer's crackle: a recording, CC0 (BigSoundBank, Graphics/Audio/CREDITS.txt). (A restaurant murmur that
    // grew with the seated customers was tried and taken out after the user heard it in a build, 2026-10-28.)
    private const float FryVolume = 0.2f;
    private static readonly System.Collections.Generic.HashSet<UnityEngine.Object> friers = new();
    private AudioSource fry;

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
        dishes = Resources.LoadAll<AudioClip>("Sounds/Plate");

        friers.Clear();
        fry = Loop(Resources.Load<AudioClip>("Sounds/Fryer/fryer_loop"), first);

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
            // Its import has "Preload Audio Data" off, so nothing loads it until asked: ask now (Update waits
            // for it to be decoded before starting it - the build that only waited never heard it, 2026-10-17).
            if (sizzleLoop.loadState == AudioDataLoadState.Unloaded) sizzleLoop.LoadAudioData();
        }
    }

    // A looping source for a recorded loop, silent until asked for (Steer).
    private AudioSource Loop(AudioClip clip, AudioSource like)
    {
        if (clip == null) return null;
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = like.outputAudioMixerGroup;
        if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
        return source;
    }

    // Fades a loop towards a volume, starting it (once its data is in, at a random point) or stopping it at 0.
    private static void Steer(AudioSource source, float target, float speed)
    {
        if (source == null) return;
        source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * speed);
        if (source.volume > 0f && !source.isPlaying)
        {
            var state = source.clip.loadState;
            if (state == AudioDataLoadState.Loaded) { source.time = UnityEngine.Random.Range(0f, source.clip.length); source.Play(); }
            else if (state == AudioDataLoadState.Unloaded) source.clip.LoadAudioData();
        }
        else if (source.volume <= 0f && source.isPlaying) source.Stop();
    }

    private void Update()
    {
        bool live = Audio.IsMusicOn && Time.timeScale > 0f && !LessonIntro.Playing;
        friers.RemoveWhere(x => x == null);
        Steer(fry, live && friers.Count > 0 ? FryVolume : 0f, FryVolume * 4f);

        if (sizzle == null) return;
        sizzlers.RemoveWhere(x => x == null);
        // Paused (menus, the result card): the kitchen goes quiet too.
        float target = sizzlers.Count > 0 && Audio.IsMusicOn && Time.timeScale > 0f ? sizzleVolume : 0f;
        sizzle.volume = Mathf.MoveTowards(sizzle.volume, target, Time.unscaledDeltaTime * sizzleVolume * 4f);
        // (Not before the clip is decoded: on the web its length isn't known until then - "Trying to get length of
        // sound which is not loaded yet" in the console.)
        if (sizzle.volume > 0f && !sizzle.isPlaying)
        {
            var state = sizzle.clip.loadState;
            if (state == AudioDataLoadState.Loaded) { sizzle.time = UnityEngine.Random.Range(0f, sizzle.clip.length); sizzle.Play(); }
            else if (state == AudioDataLoadState.Unloaded) sizzle.clip.LoadAudioData();
        }
        else if (sizzle.volume <= 0f && sizzle.isPlaying) sizzle.Stop();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Dishes (user, 2026-10-28: "plate sounds" - a clink made in code was turned down, real ones were asked for):
    // a light china clink when food goes onto a plate or a burger onto a customer's table. Recorded sounds, CC0
    // (Kenney's "Impact Sounds", Resources/Sounds/Plate, see Graphics/Audio/CREDITS.txt), one of five at random.
    private AudioClip[] dishes;
    private const float DishVolume = 0.26f;   // its peak about the put-down sound's (it rings longer)

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
        if (!Audio.IsMusicOn || LessonIntro.Playing) return;   // (the titles are quiet: music only)
        // The same cue twice in one moment (two tips at once) plays once.
        float now = Time.unscaledTime;
        if (now - lastPlayed[(int)cue] < 0.06f) return;
        lastPlayed[(int)cue] = now;

        if (cue == Cue.Dish)
        {
            if (dishes == null || dishes.Length == 0) return;
            var dishVoice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            dishVoice.pitch = 1f + UnityEngine.Random.Range(-0.05f, 0.05f);
            dishVoice.PlayOneShot(dishes[UnityEngine.Random.Range(0, dishes.Length)], DishVolume);
            return;
        }


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
