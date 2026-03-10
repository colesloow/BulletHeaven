using System;
using UnityEngine;
using UnityEngine.Audio;

//list of sound types for different events
public enum SoundType
{
    FIRE,
    LASER,
    IMPACT,
    COLLECT,
    DEATH,
    LEVELUP,
    WIN,
    CLICK
}

//automatically add an AudioSource component to associated game objects
[RequireComponent(typeof(AudioSource))]

public class SoundManager : MonoBehaviour
{
    //audioclip array containing the sound clips used in game
    [SerializeField] private AudioClip[] soundList;
    private static SoundManager instance;
    private AudioSource audioSource;

    //initialize instance
    private void Awake()
    {
        instance = this;
    }

    //returns the first AudioSource component found on the GameObject
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    //Play sound - by default the volume is 100%
    public static void PlaySound(SoundType sound, float volume = 1)
    {
        //Play a given sound type once, at specified volume
        //We look for the corresponding audioclip in soundList
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], volume);
    }
}