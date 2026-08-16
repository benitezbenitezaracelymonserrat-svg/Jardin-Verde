using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicMenu : MonoBehaviour
{
    private const string MusicVolumeKey = "AldeaVerde.Menu.MusicVolume";
    private const string SfxVolumeKey = "AldeaVerde.Menu.SfxVolume";
    private const string AudioSetupVersionKey = "AldeaVerde.Menu.AudioSetupVersion";

    [Header("Volumenes iniciales")]
    [Range(0f, 1f)] public float volumenNormal = 0.4f;
    [Range(0f, 1f)] public float volumenEfectos = 1f;

    [Header("Fuentes de audio")]
    [SerializeField] private AudioSource musicaSource;
    [SerializeField] private AudioSource efectosSource;
    [SerializeField] private AudioClip musicaClip = null;
    [SerializeField] private AudioClip efectoClickClip = null;

    private readonly HashSet<string> nombresBotonesConSonido = new HashSet<string>
    {
        "Jugar", "Creditos", "Ayuda", "opciones", "Cerrar", "Anterior", "Siguiente"
    };

    public float VolumenMusica => volumenNormal;
    public float VolumenEfectos => volumenEfectos;

    private void Awake()
    {
        ResolverFuentesDeAudio();

        AudioListener.pause = false;
        AudioListener.volume = 1f;

        // La version anterior podia guardar silencio aunque el administrador
        // estuviera desactivado dentro de Ayuda. Se restablece una sola vez.
        if (PlayerPrefs.GetInt(AudioSetupVersionKey, 0) < 2)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, 0.4f);
            PlayerPrefs.SetFloat(SfxVolumeKey, 1f);
            PlayerPrefs.SetInt(AudioSetupVersionKey, 2);
            PlayerPrefs.Save();
        }

        if (musicaSource != null && musicaSource.clip == null && musicaClip != null)
        {
            musicaSource.clip = musicaClip;
        }

        if (efectosSource != null && efectosSource.clip == null && efectoClickClip != null)
        {
            efectosSource.clip = efectoClickClip;
        }

        volumenNormal = PlayerPrefs.GetFloat(MusicVolumeKey, volumenNormal);
        volumenEfectos = PlayerPrefs.GetFloat(SfxVolumeKey, volumenEfectos);
        AplicarVolumenes();

        if (musicaSource != null && musicaSource.clip != null)
        {
            musicaSource.loop = true;
            musicaSource.spatialBlend = 0f;
            musicaSource.clip.LoadAudioData();
        }

        if (efectosSource != null && efectosSource.clip != null)
        {
            efectosSource.loop = false;
            efectosSource.spatialBlend = 0f;
            efectosSource.clip.LoadAudioData();
        }
    }

    private void Start()
    {
        ReproducirMusica();
        ConectarSonidoDeBotones();

        OptionsMenuController opciones = GetComponent<OptionsMenuController>();
        if (opciones == null)
        {
            opciones = gameObject.AddComponent<OptionsMenuController>();
        }

        opciones.Inicializar(this);
    }

    private void ResolverFuentesDeAudio()
    {
        AudioSource[] audios = GetComponents<AudioSource>();

        if (musicaSource == null && audios.Length > 0)
        {
            musicaSource = audios[0];
        }

        if (efectosSource == null && audios.Length > 1)
        {
            efectosSource = audios[1];
        }

        if (musicaSource == null)
        {
            musicaSource = gameObject.AddComponent<AudioSource>();
        }

        if (efectosSource == null || efectosSource == musicaSource)
        {
            efectosSource = gameObject.AddComponent<AudioSource>();
        }

        musicaSource.playOnAwake = false;
        musicaSource.loop = true;
        musicaSource.spatialBlend = 0f;

        efectosSource.playOnAwake = false;
        efectosSource.loop = false;
        efectosSource.spatialBlend = 0f;
    }

    private void ConectarSonidoDeBotones()
    {
        Button[] botones = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Button boton in botones)
        {
            if (boton == null || !nombresBotonesConSonido.Contains(boton.gameObject.name))
            {
                continue;
            }

            bool yaTieneSonido = false;
            for (int i = 0; i < boton.onClick.GetPersistentEventCount(); i++)
            {
                if (boton.onClick.GetPersistentMethodName(i) == "ReproducirSonidoBoton")
                {
                    yaTieneSonido = true;
                    break;
                }
            }

            if (!yaTieneSonido)
            {
                boton.onClick.AddListener(ReproducirClick);
            }
        }
    }

    private void AplicarVolumenes()
    {
        if (musicaSource != null)
        {
            musicaSource.volume = volumenNormal;
        }

        if (efectosSource != null)
        {
            efectosSource.volume = volumenEfectos;
        }
    }

    public void CambiarVolumenMusica(float valor)
    {
        volumenNormal = Mathf.Clamp01(valor);
        if (musicaSource != null)
        {
            musicaSource.volume = volumenNormal;
        }

        PlayerPrefs.SetFloat(MusicVolumeKey, volumenNormal);
        PlayerPrefs.Save();
    }

    public void CambiarVolumenEfectos(float valor)
    {
        volumenEfectos = Mathf.Clamp01(valor);
        if (efectosSource != null)
        {
            efectosSource.volume = volumenEfectos;
        }

        PlayerPrefs.SetFloat(SfxVolumeKey, volumenEfectos);
        PlayerPrefs.Save();
    }

    public void ReproducirMusica()
    {
        if (musicaSource == null)
        {
            return;
        }

        musicaSource.volume = volumenNormal;
        if (!musicaSource.isPlaying)
        {
            musicaSource.Play();
        }
    }

    public void DetenerMusica()
    {
        if (musicaSource != null)
        {
            musicaSource.Stop();
        }
    }

    public void ReproducirClick()
    {
        if (efectosSource == null || efectosSource.clip == null || volumenEfectos <= 0f)
        {
            return;
        }

        efectosSource.PlayOneShot(efectosSource.clip, 1f);
    }

    // Se conserva para no romper referencias anteriores del menu.
    public IEnumerator ClickConDucking()
    {
        ReproducirClick();
        yield return null;
    }
}

