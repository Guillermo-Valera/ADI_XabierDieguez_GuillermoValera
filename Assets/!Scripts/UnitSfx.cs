using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sonidos de una unidad o base. Unit los dispara al atacar, golpear, recibir daño y morir.
/// Los asigna el menú Tools > Configurar bases, fondo y oro.
/// </summary>
public class UnitSfx : MonoBehaviour
{
    public AudioClip attack;
    public AudioClip impact;
    public AudioClip hit;
    public AudioClip death;
    [Range(0f, 1f)] public float volume = 0.7f;

    public void PlayAttack() => SfxPlayer.Play(attack, volume);
    public void PlayImpact() => SfxPlayer.Play(impact, volume);
    public void PlayHit() => SfxPlayer.Play(hit, volume);
    public void PlayDeath() => SfxPlayer.Play(death, volume);
}

/// <summary>
/// Un único AudioSource 2D para todos los efectos. Evita que el mismo sonido
/// se repita muchas veces en el mismo instante cuando hay muchas tropas.
/// </summary>
public static class SfxPlayer
{
    const float MinRepeatTime = 0.06f;

    private static AudioSource source;
    private static readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        if (source == null)
        {
            var go = new GameObject("SfxPlayer");
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(clip, out float t) && now - t < MinRepeatTime) return;
        lastPlayed[clip] = now;

        source.PlayOneShot(clip, volume);
    }
}
