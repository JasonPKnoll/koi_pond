
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AudioManager : UdonSharpBehaviour
{

    public AudioSource[] audioEat;
    public AudioSource[] audioSplash;
    public AudioSource[] audioMakingOffspring;
    public AudioSource[] audioBonk;

    public AudioSource GetAudio(AudioSource[] audioArray, GameObject audioSource) {
        foreach (AudioSource audio in audioArray) {
            if (!audio.isPlaying) {
                audio.transform.position = audioSource.transform.position;
                return audio;
            }
        }
        return null;
    }

    public void PlayOnce(AudioSource[] audioArray, GameObject audioSource, float pitchValue) {
        foreach (AudioSource audio in audioArray) {
            if (!audio.isPlaying) {
                audio.transform.position = audioSource.transform.position;
                audio.pitch = pitchValue;
                audio.Play();
                return;
            }
        }
        return;
    }
}
