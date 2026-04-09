using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Pool / limits")]
    [SerializeField] private int maxSimultaneousSources = 16;
    [SerializeField] private bool persistAcrossScenes = true;

    [SerializeField] private float defaultMinIntervalPerClip = 0.08f;

    private readonly List<AudioSource> sourcePool = new List<AudioSource>();
    private readonly Dictionary<AudioClip, float> lastClipPlayTime = new Dictionary<AudioClip, float>();
    private readonly LinkedList<int> recentlyUsed = new LinkedList<int>();

    private SFXPlayOptions cachedDefaultOptions;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

        cachedDefaultOptions = new SFXPlayOptions();
        CreateInitialPool();
    }

    private void CreateInitialPool()
    {
        for (int i = 0; i < maxSimultaneousSources; i++)
        {
            var go = new GameObject("SFXSource_" + i);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.maxDistance = 1f;
            src.priority = 128;
            sourcePool.Add(src);
            recentlyUsed.AddLast(i);
        }
    }

    public class SFXPlayOptions
    {
        public float volume = 1f;
        public float volumeVariance = 0.05f;
        public float pitch = 1f;
        public float pitchVariance = 0.05f;
        public float maxDelaySeconds = 0f; // mac dinh doi ve 0 de am thanh phat ngay lap tuc
        public float minIntervalPerClip = -1f;
        public bool is2D = true;
        public bool allowStealWhenBusy = true;

        public SFXPlayOptions() { }
    }

    public void PlaySFX(AudioClip clip, SFXPlayOptions options = null)
    {
        if (clip == null) return;

        // su dung ban cached neu khong truyen option, chong rac ram
        if (options == null) options = cachedDefaultOptions;

        float minInterval = options.minIntervalPerClip >= 0f ? options.minIntervalPerClip : defaultMinIntervalPerClip;

        if (minInterval > 0f)
        {
            if (lastClipPlayTime.TryGetValue(clip, out float lastTime))
            {
                if (Time.unscaledTime - lastTime < minInterval) return;
            }
        }

        float delay = (options.maxDelaySeconds <= 0f) ? 0f : Random.Range(0f, options.maxDelaySeconds);

        if (delay > 0f)
        {
            StartCoroutine(PlayCoroutine(clip, options, delay));
        }
        else
        {
            PlayImmediate(clip, options);
        }
    }

    private IEnumerator PlayCoroutine(AudioClip clip, SFXPlayOptions options, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        // check lai lan nua sau khi doi
        float minInterval = options.minIntervalPerClip >= 0f ? options.minIntervalPerClip : defaultMinIntervalPerClip;
        if (minInterval > 0f)
        {
            if (lastClipPlayTime.TryGetValue(clip, out float lastTime))
            {
                if (Time.unscaledTime - lastTime < minInterval) yield break;
            }
        }

        PlayImmediate(clip, options);
    }

    // tach logic tim source va phat nhac ra mot ham rieng de dung chung
    private void PlayImmediate(AudioClip clip, SFXPlayOptions options)
    {
        int availableIndex = -1;

        // tim source dang ranh
        for (int i = 0; i < sourcePool.Count; i++)
        {
            var idx = (i + recentlyUsed.First.Value) % sourcePool.Count;
            if (!sourcePool[idx].isPlaying)
            {
                availableIndex = idx;
                break;
            }
        }

        // neu het source ranh
        if (availableIndex == -1)
        {
            if (!options.allowStealWhenBusy) return;
            availableIndex = recentlyUsed.First.Value; // cuop cai cu nhat
        }

        var source = sourcePool[availableIndex];

        // ap dung hieu ung
        float volume = Mathf.Clamp01(options.volume + Random.Range(-options.volumeVariance, options.volumeVariance));
        float pitch = Mathf.Clamp(options.pitch + Random.Range(-options.pitchVariance, options.pitchVariance), 0.5f, 3f);

        source.spatialBlend = options.is2D ? 0f : source.spatialBlend;
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;

        source.Play();

        // cap nhat lich su
        lastClipPlayTime[clip] = Time.unscaledTime;
        var node = recentlyUsed.Find(availableIndex);
        if (node != null) recentlyUsed.Remove(node);
        recentlyUsed.AddLast(availableIndex);
    }

    public void StopAllSFX()
    {
        foreach (var s in sourcePool) s.Stop();
    }
}