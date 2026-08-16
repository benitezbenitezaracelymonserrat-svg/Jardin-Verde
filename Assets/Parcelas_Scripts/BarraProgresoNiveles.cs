using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Barra superior común para visualizar cuánto falta en el nivel actual.
/// </summary>
public class BarraProgresoNiveles : MonoBehaviour
{
    private GameObject raizUI;
    private GameObject canvasUI;
    private Image relleno;
    private TextMeshProUGUI texto;
    private float progresoVisual;
    private int nivelVisualizado;
    private GameObject panelManual;
    private float proximaBusquedaManual;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearAutomaticamente()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene" ||
            FindFirstObjectByType<BarraProgresoNiveles>() != null)
        {
            return;
        }

        new GameObject("BarraProgresoNivelesControl")
            .AddComponent<BarraProgresoNiveles>();
    }

    private IEnumerator Start()
    {
        yield return null;
        Construir();
    }

    private void Update()
    {
        if (raizUI == null)
            return;

        if (panelManual == null && Time.unscaledTime >= proximaBusquedaManual)
        {
            panelManual = BuscarObjeto("PanelManualJuego");
            proximaBusquedaManual = Time.unscaledTime + 1f;
        }

        bool manualAbierto = panelManual != null && panelManual.activeInHierarchy;
        // La cinemática puede mantener su bandera activa unos instantes aun
        // después de devolver el control al jugador. La barra no debe quedar
        // bloqueada por ese estado; solo se oculta mientras el manual ocupa
        // la pantalla.
        bool mostrar = !manualAbierto;
        raizUI.SetActive(mostrar);

        if (!mostrar)
            return;

        int nivel = GestorNivel3.NivelActivo ? 3 :
            GestorNivel2.NivelActivo ? 2 : 1;
        float progreso = nivel == 3 ? GestorNivel3.Progreso01 :
            nivel == 2 ? GestorNivel2.Progreso01 :
            GestorDesafiosNivel1.Progreso01;

        progreso = Mathf.Clamp01(progreso);
        if (nivelVisualizado != nivel)
        {
            nivelVisualizado = nivel;
            progresoVisual = 0f;
        }

        progresoVisual = Mathf.MoveTowards(
            progresoVisual,
            progreso,
            Time.unscaledDeltaTime * 0.32f
        );

        relleno.fillAmount = progresoVisual;
        texto.text = $"NIVEL {nivel}  {Mathf.RoundToInt(progresoVisual * 100f)}%";
    }

    private void Construir()
    {
        // La escena activa y desactiva sus Canvas durante la cinemática y el
        // manual. Usar uno de ellos hacía que la barra quedara invisible si
        // se construía mientras ese Canvas estaba desactivado. Un Canvas
        // propio mantiene la barra independiente del resto de la interfaz.
        canvasUI = new GameObject(
            "CanvasBarraProgreso",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        int capaUI = LayerMask.NameToLayer("UI");
        if (capaUI >= 0)
            canvasUI.layer = capaUI;

        Canvas canvas = canvasUI.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler escalador = canvasUI.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        raizUI = new GameObject(
            "BarraProgresoNivel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        raizUI.layer = canvasUI.layer;
        raizUI.transform.SetParent(canvasUI.transform, false);

        RectTransform rect = raizUI.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(360f, 42f);

        Image fondo = raizUI.GetComponent<Image>();
        fondo.color = new Color(0.20f, 0.10f, 0.035f, 0.94f);
        fondo.raycastTarget = false;
        Outline borde = raizUI.AddComponent<Outline>();
        borde.effectColor = new Color(0.73f, 0.43f, 0.16f, 1f);
        borde.effectDistance = new Vector2(2f, -2f);

        GameObject rellenoObjeto = new GameObject(
            "RellenoProgreso",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        rellenoObjeto.layer = raizUI.layer;
        rellenoObjeto.transform.SetParent(raizUI.transform, false);
        RectTransform rellenoRect = rellenoObjeto.GetComponent<RectTransform>();
        rellenoRect.anchorMin = Vector2.zero;
        rellenoRect.anchorMax = Vector2.one;
        rellenoRect.offsetMin = new Vector2(5f, 5f);
        rellenoRect.offsetMax = new Vector2(-5f, -5f);

        relleno = rellenoObjeto.GetComponent<Image>();
        relleno.color = new Color(0.30f, 0.70f, 0.28f, 1f);
        relleno.raycastTarget = false;
        relleno.type = Image.Type.Filled;
        relleno.fillMethod = Image.FillMethod.Horizontal;
        relleno.fillOrigin = (int)Image.OriginHorizontal.Left;

        GameObject textoObjeto = new GameObject(
            "TextoProgresoNivel",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textoObjeto.layer = raizUI.layer;
        textoObjeto.transform.SetParent(raizUI.transform, false);
        RectTransform textoRect = textoObjeto.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = Vector2.zero;
        textoRect.offsetMax = Vector2.zero;

        texto = textoObjeto.GetComponent<TextMeshProUGUI>();
        texto.alignment = TextAlignmentOptions.Center;
        texto.fontSize = 20f;
        texto.fontStyle = FontStyles.Bold;
        texto.color = new Color(1f, 0.95f, 0.76f, 1f);
        texto.raycastTarget = false;
    }

    private static GameObject BuscarObjeto(string nombre)
    {
        RectTransform[] objetos = FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (RectTransform objeto in objetos)
        {
            if (objeto != null && objeto.name == nombre)
                return objeto.gameObject;
        }

        return null;
    }
}
