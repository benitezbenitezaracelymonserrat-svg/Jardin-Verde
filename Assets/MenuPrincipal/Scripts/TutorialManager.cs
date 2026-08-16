using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("Páginas del Manual")]
    public RectTransform[] paginas;

    [Header("Botones")]
    public Button btnAnterior;
    public Button btnSiguiente;
    public Button btnCerrar;

    [Header("Paneles")]
    public GameObject panelAyuda;
    public GameObject menuPrincipal;

    [Header("Animación")]
    [Range(0.2f, 2f)]
    public float duracion = 0.85f;

    [Range(50f, 600f)]
    public float distanciaDesplazamiento = 250f;

    private int paginaActual = 0;
    private bool animando = false;

    private Vector2 centro = Vector2.zero;

    private void OnEnable()
    {
        paginaActual = 0;
        animando = false;

        PrepararPaginas();
    }

    private void PrepararPaginas()
    {
        if (paginas == null || paginas.Length == 0)
            return;

        for (int i = 0; i < paginas.Length; i++)
        {
            if (paginas[i] == null)
                continue;

            paginas[i].anchoredPosition = centro;

            CanvasGroup grupo = ObtenerCanvasGroup(paginas[i]);

            if (i == 0)
            {
                paginas[i].gameObject.SetActive(true);
                grupo.alpha = 1f;
            }
            else
            {
                grupo.alpha = 0f;
                paginas[i].gameObject.SetActive(false);
            }
        }

        ActualizarBotones();
    }

    public void Siguiente()
    {
        if (animando)
            return;

        if (paginaActual < paginas.Length - 1)
        {
            StartCoroutine(
                AnimarCambio(paginaActual + 1, 1)
            );
        }
    }

    public void Anterior()
    {
        if (animando)
            return;

        if (paginaActual > 0)
        {
            StartCoroutine(
                AnimarCambio(paginaActual - 1, -1)
            );
        }
    }

    private IEnumerator AnimarCambio(int nuevaPagina, int direccion)
    {
        animando = true;

        RectTransform actual = paginas[paginaActual];
        RectTransform nueva = paginas[nuevaPagina];

        if (actual == null || nueva == null)
        {
            animando = false;
            yield break;
        }

        CanvasGroup grupoActual = ObtenerCanvasGroup(actual);
        CanvasGroup grupoNueva = ObtenerCanvasGroup(nueva);

        Vector2 salidaActual =
            centro - Vector2.right * distanciaDesplazamiento * direccion;

        Vector2 entradaNueva =
            centro + Vector2.right * distanciaDesplazamiento * direccion;

        actual.anchoredPosition = centro;
        grupoActual.alpha = 1f;

        nueva.gameObject.SetActive(true);
        nueva.anchoredPosition = entradaNueva;

        // La nueva página empieza invisible
        grupoNueva.alpha = 0f;

        // -----------------------------
        // FASE 1:
        // Sale la página actual
        // -----------------------------

        float tiempo = 0f;
        float duracionSalida = duracion * 0.45f;

        while (tiempo < duracionSalida)
        {
            tiempo += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    tiempo / duracionSalida
                );

            float suave =
                Mathf.SmoothStep(0f, 1f, t);

            actual.anchoredPosition =
                Vector2.Lerp(
                    centro,
                    salidaActual,
                    suave
                );

            grupoActual.alpha =
                Mathf.Lerp(
                    1f,
                    0f,
                    suave
                );

            yield return null;
        }

        // Ocultamos completamente
        // la página anterior
        actual.gameObject.SetActive(false);

        actual.anchoredPosition = centro;
        grupoActual.alpha = 1f;

        // Pequeñísima pausa.
        // Evita que ambas páginas se mezclen.
        yield return new WaitForSecondsRealtime(0.05f);

        // -----------------------------
        // FASE 2:
        // Entra la página nueva
        // -----------------------------

        tiempo = 0f;
        float duracionEntrada = duracion * 0.55f;

        while (tiempo < duracionEntrada)
        {
            tiempo += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    tiempo / duracionEntrada
                );

            float suave =
                Mathf.SmoothStep(0f, 1f, t);

            nueva.anchoredPosition =
                Vector2.Lerp(
                    entradaNueva,
                    centro,
                    suave
                );

            grupoNueva.alpha =
                Mathf.Lerp(
                    0f,
                    1f,
                    suave
                );

            yield return null;
        }

        // Posición final exacta
        nueva.anchoredPosition = centro;
        grupoNueva.alpha = 1f;

        paginaActual = nuevaPagina;

        ActualizarBotones();

        animando = false;
    }

    private CanvasGroup ObtenerCanvasGroup(RectTransform pagina)
    {
        CanvasGroup grupo =
            pagina.GetComponent<CanvasGroup>();

        if (grupo == null)
        {
            grupo =
                pagina.gameObject.AddComponent<CanvasGroup>();
        }

        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        return grupo;
    }

    private void ActualizarBotones()
    {
        if (btnAnterior != null)
        {
            btnAnterior.interactable =
                paginaActual > 0;
        }

        if (btnSiguiente != null)
        {
            btnSiguiente.interactable =
                paginaActual < paginas.Length - 1;
        }
    }

    public void CerrarAyuda()
    {
        if (animando)
            return;

        // Primero mostramos el menú principal
        // para evitar que aparezca el fondo gris/celeste.
        if (menuPrincipal != null)
        {
            menuPrincipal.SetActive(true);
        }

        // Después ocultamos ayuda.
        if (panelAyuda != null)
        {
            panelAyuda.SetActive(false);
        }

        paginaActual = 0;
        animando = false;
    }
}
