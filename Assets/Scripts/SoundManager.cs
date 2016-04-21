using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour 
{
    public static SoundManager instance;
    public static PooledAudioSource[] allAudioSources;
    public int pooledAudioSourcesAmount = 64;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        allAudioSources = new PooledAudioSource[pooledAudioSourcesAmount];
        for(int i = 0; i < pooledAudioSourcesAmount; i++)
        {
            GameObject go = new GameObject("PooledAudioSource(" + i + ")");
            go.transform.parent = transform;
            AudioSource audiosource = go.AddComponent<AudioSource>();
            PooledAudioSource pas = new PooledAudioSource();
            pas.gameObject = go;
            pas.transform = go.transform;
            pas.audioSource = audiosource;
            pas.transform.localPosition = Vector3.zero;

            audiosource.spatialBlend = 1;

            allAudioSources[i] = pas;
        }
    }

    public static PooledAudioSource PlayAudioAtPoint(AudioClip clip, Vector3 position, Transform parent)
    {
        return PlayAudioAtPoint(clip, position, parent, 0, 0);
    }

    public static PooledAudioSource PlayAudioAtPoint(AudioClip clip, Vector3 position, Transform parent, float delay, float startPosition, float spatialBlend = 1, float volume = 1, float minimumRange = 1, float maximumRange = 100, AudioMixerGroup targetGroup = null)
    {
        if (clip == null)
            return null;

        for (int i = 0; i < instance.pooledAudioSourcesAmount; i++)
        {
            if (allAudioSources[i].endTime < Time.time)
            {
                allAudioSources[i].transform.parent = parent;
                allAudioSources[i].transform.position = position;
                allAudioSources[i].audioSource.clip = clip;
                allAudioSources[i].endTime = Time.time + clip.length;
                allAudioSources[i].duration = clip.length;
                allAudioSources[i].audioSource.spatialBlend = spatialBlend;
                allAudioSources[i].audioSource.volume = 1;
                allAudioSources[i].audioSource.outputAudioMixerGroup = targetGroup;
                allAudioSources[i].audioSource.minDistance = minimumRange;
                allAudioSources[i].audioSource.maxDistance = maximumRange;

                if (startPosition != 0) allAudioSources[i].audioSource.time = startPosition;
                if (delay == 0) allAudioSources[i].audioSource.Play(); else allAudioSources[i].audioSource.PlayDelayed(delay);

                instance.StartCoroutine("HandleAudioAtPoint", allAudioSources[i]);
                return allAudioSources[i];
            }
        }
        return null;
    }

    IEnumerator HandleAudioAtPoint(PooledAudioSource pas)
    {
        //yield return new WaitForSeconds(pas.duration);
        float time = Time.time;
        while (Time.time - time < pas.duration - 0.05f)
            yield return new WaitForEndOfFrame();

        time = Time.time;
        float volume = pas.audioSource.volume;
        while (Time.time - time <= 0.05f)
        {
            yield return new WaitForEndOfFrame();
            pas.audioSource.volume = Mathf.Lerp(volume, 0, (Time.time - time) * 20);
        }
        pas.audioSource.volume = 0;

        yield return new WaitForEndOfFrame();
        pas.audioSource.Stop();

        pas.transform.parent = transform;
        pas.transform.localPosition = Vector3.zero;
    }
}

public class PooledAudioSource
{
    public GameObject gameObject;
    public Transform transform;
    public AudioSource audioSource;

    public float endTime;
    public float duration;
}

[System.Serializable]
public class SoundItem
{
    public AudioClip clip;
    public AudioMixerGroup targetGroup = null;
    public float delay, startPosition, spatialBlend = 1, volume = 1, minimumRange = 1, maximumRange = 100; 

    public bool hasPlayed { get; set; }
    public float lastTimePlayed { get; set; }

    public void Play(Vector3 position, Transform parent = null)
    {
        SoundManager.PlayAudioAtPoint(clip, position, parent, delay, startPosition, spatialBlend, volume, minimumRange, maximumRange, targetGroup);
    }
}