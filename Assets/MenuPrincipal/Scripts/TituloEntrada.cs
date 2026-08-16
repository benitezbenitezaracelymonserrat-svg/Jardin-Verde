using System.Collections;
using UnityEngine;

public class TituloAnimacion : MonoBehaviour
{
    [Header("Referencia")]
    public RectTransform titulo;

    [Header("Entrada desde arriba")]
    public float distanciaArriba = 1000f;
    public float duracionBajada = 2.0f;

    [Header("Salto al llegar")]
    public float alturaSalto = 45f;
    public float escalaGrande = 1.12f;

    public float duracionSubida = 0.22f;
    public float tiempoGrande = 0.18f;
    public float duracionBajadaSalto = 0.30f;

    private Vector2 posicionFinal;
    private Vector3 escalaFinal;

    private Coroutine animacionActual;
    private bool inicioCompletado;


    private void Awake()
    {
        if (titulo == null)
        {
            titulo = GetComponent<RectTransform>();
        }

        // Guardamos exactamente la posición
        // donde acomodaste ALDEA VERDE en Unity.
        posicionFinal = titulo.anchoredPosition;
        escalaFinal = titulo.localScale;
    }


    private void OnEnable()
    {
        if (inicioCompletado)
        {
            ReiniciarEntrada();
        }
    }


    private void Start()
    {
        // El Canvas ya termino su primera preparacion: la bajada siempre
        // sera visible al cargar la escena del menu.
        inicioCompletado = true;
        ReiniciarEntrada();
    }


    public void ReiniciarEntrada()
    {
        if (titulo == null)
            return;

        VegetalesCelebracion celebracion =
            titulo.parent.GetComponent<VegetalesCelebracion>();
        if (celebracion != null)
        {
            celebracion.Detener();
        }

        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
        }

        titulo.anchoredPosition = new Vector2(
            posicionFinal.x,
            posicionFinal.y + distanciaArriba
        );
        titulo.localScale = escalaFinal;
        animacionActual = StartCoroutine(AnimarEntrada());
    }


    private IEnumerator AnimarEntrada()
    {
        // Esperamos un frame para que Unity
        // termine de activar correctamente el menú.
        yield return null;


        // ==============================
        // POSICIÓN INICIAL
        // ==============================

        Vector2 posicionInicial = new Vector2(
            posicionFinal.x,
            posicionFinal.y + distanciaArriba
        );


        titulo.anchoredPosition = posicionInicial;
        titulo.localScale = escalaFinal;


        // ==============================
        // 1. BAJADA LENTA DESDE ARRIBA
        // ==============================

        float tiempo = 0f;


        while (tiempo < duracionBajada)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                tiempo / duracionBajada
            );


            // Movimiento suave.
            // Empieza tranquilo y frena bonito al llegar.
            float suave = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            titulo.anchoredPosition =
                Vector2.Lerp(
                    posicionInicial,
                    posicionFinal,
                    suave
                );


            yield return null;
        }


        titulo.anchoredPosition = posicionFinal;


        // ==============================
        // 2. SALTO + AGRANDAMIENTO
        // ==============================

        Vector2 posicionSalto = new Vector2(
            posicionFinal.x,
            posicionFinal.y + alturaSalto
        );


        Vector3 escalaSalto =
            escalaFinal * escalaGrande;


        tiempo = 0f;


        while (tiempo < duracionSubida)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                tiempo / duracionSubida
            );

            float suave = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            titulo.anchoredPosition =
                Vector2.Lerp(
                    posicionFinal,
                    posicionSalto,
                    suave
                );


            titulo.localScale =
                Vector3.Lerp(
                    escalaFinal,
                    escalaSalto,
                    suave
                );


            yield return null;
        }


        titulo.anchoredPosition = posicionSalto;
        titulo.localScale = escalaSalto;


        // ==============================
        // 3. QUEDA GRANDE UN RATITO
        // ==============================

        yield return new WaitForSecondsRealtime(
            tiempoGrande
        );


        // ==============================
        // 4. VUELVE SUAVEMENTE
        // ==============================

        tiempo = 0f;


        while (tiempo < duracionBajadaSalto)
        {
            tiempo += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                tiempo / duracionBajadaSalto
            );

            float suave = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            titulo.anchoredPosition =
                Vector2.Lerp(
                    posicionSalto,
                    posicionFinal,
                    suave
                );


            titulo.localScale =
                Vector3.Lerp(
                    escalaSalto,
                    escalaFinal,
                    suave
                );


            yield return null;
        }


        // Posición definitiva
        titulo.anchoredPosition = posicionFinal;
        titulo.localScale = escalaFinal;

        animacionActual = null;
        ReproducirVerduras();
    }


    private void ReproducirVerduras()
    {
        if (titulo == null || titulo.parent == null)
        {
            return;
        }

        VegetalesCelebracion celebracion =
            titulo.parent.GetComponent<VegetalesCelebracion>();
        if (celebracion == null)
        {
            celebracion =
                titulo.parent.gameObject.AddComponent<VegetalesCelebracion>();
        }

        celebracion.Reproducir();
    }
}

