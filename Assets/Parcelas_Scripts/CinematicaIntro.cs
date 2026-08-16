using UnityEngine;
using System.Collections;
public class CinematicaIntro : MonoBehaviour
{
    public static bool cinematicaActiva = false;
    public static event System.Action CinematicaIniciada;
    public static event System.Action CinematicaTerminada;

    // Las cinemáticas de los niveles 2 y 3 reutilizan estas notificaciones
    // para ocultar la interfaz y abrir el manual al terminar.
    public static void NotificarInicioExterno()
    {
        cinematicaActiva = true;
        CinematicaIniciada?.Invoke();
    }

    public static void NotificarFinExterno()
    {
        cinematicaActiva = false;
        CinematicaTerminada?.Invoke();
    }

    [Header("Camaras")]
    public Camera camaraCinematica;
    public GameObject jugador;
    [Header("Puntos de recorrido")]
    public Transform[] puntos;
    public float duracionPorPunto = 2.5f;
    [Header("Texto bienvenida")]
    public CanvasGroup textoBienvenida;
    public float duracionTexto = 2f;
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip clipIntro;
    void Start()
    {
        cinematicaActiva = true;
        CinematicaIniciada?.Invoke();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (jugador != null)
            jugador.SetActive(false);
        if (camaraCinematica != null)
            camaraCinematica.gameObject.SetActive(true);
        if (audioSource != null && clipIntro != null)
        {
            audioSource.Stop();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.dopplerLevel = 0f;
            audioSource.clip = clipIntro;
            audioSource.Play();
        }
        StartCoroutine(Reproducir());
    }
    IEnumerator Reproducir()
    {
        if (puntos.Length > 0)
        {
            camaraCinematica.transform.position = puntos[0].position;
            camaraCinematica.transform.rotation = puntos[0].rotation;
        }
        if (textoBienvenida != null)
            StartCoroutine(AnimarTexto());
        for (int i = 0; i < puntos.Length - 1; i++)
        {
            yield return MoverCamara(puntos[i], puntos[i + 1], duracionPorPunto);
        }
        yield return new WaitForSeconds(0.5f);
        if (camaraCinematica != null)
            camaraCinematica.gameObject.SetActive(false);
        if (jugador != null)
            jugador.SetActive(true);
        cinematicaActiva = false;
        CinematicaTerminada?.Invoke();
    }
    IEnumerator MoverCamara(Transform desde, Transform hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float progreso = t / duracion;
            camaraCinematica.transform.position = Vector3.Lerp(desde.position, hasta.position, progreso);
            camaraCinematica.transform.rotation = Quaternion.Slerp(desde.rotation, hasta.rotation, progreso);
            yield return null;
        }
    }
    IEnumerator AnimarTexto()
    {
        textoBienvenida.transform.localScale = Vector3.one * 1.3f;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            textoBienvenida.alpha = t / 0.5f;
            textoBienvenida.transform.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, t / 0.5f);
            yield return null;
        }
        textoBienvenida.alpha = 1f;
        textoBienvenida.transform.localScale = Vector3.one;
        yield return new WaitForSeconds(duracionTexto);
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            textoBienvenida.alpha = 1f - (t / 0.5f);
            yield return null;
        }
        textoBienvenida.alpha = 0f;
    }
}
