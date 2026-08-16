using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simulacion visual sencilla del ordeno. Mantiene al granjero quieto y
/// seguro sobre el suelo; no modifica sus huesos ni reproduce el clip que
/// lo empujaba debajo del terreno.
/// </summary>
public class SimulacionOrdenoUI : MonoBehaviour
{
    private static SimulacionOrdenoUI instancia;

    private GameObject panel;
    private TextMeshProUGUI titulo;
    private Image progreso;
    private RectTransform[] gotas;
    private Coroutine rutina;

    public static void Mostrar(float duracion, bool esCabra)
    {
        if (instancia == null)
        {
            GameObject control = new GameObject("SimulacionOrdenoUIControl");
            instancia = control.AddComponent<SimulacionOrdenoUI>();
        }

        instancia.Iniciar(duracion, esCabra);
    }

    private void Iniciar(float duracion, bool esCabra)
    {
        if (panel == null)
            Construir();

        if (panel == null)
            return;

        if (rutina != null)
            StopCoroutine(rutina);

        titulo.text = esCabra ? "ORDENANDO CABRA..." : "ORDENANDO VACA...";
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        rutina = StartCoroutine(Animar(Mathf.Max(0.5f, duracion)));
    }

    private IEnumerator Animar(float duracion)
    {
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float normalizado = Mathf.Clamp01(tiempo / duracion);
            progreso.fillAmount = normalizado;

            for (int i = 0; i < gotas.Length; i++)
            {
                float ciclo = Mathf.Repeat(tiempo * 1.55f + i * 0.34f, 1f);
                gotas[i].anchoredPosition = new Vector2(
                    -34f + i * 34f,
                    Mathf.Lerp(34f, -30f, ciclo)
                );
            }

            yield return null;
        }

        progreso.fillAmount = 1f;
        panel.SetActive(false);
        rutina = null;
    }

    private void Construir()
    {
        Canvas canvas = CanvasJuegoUI.BuscarInteractivo();
        if (canvas == null)
            return;

        panel = CrearUI("SimulacionOrdeno", canvas.transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 120f);
        panelRect.sizeDelta = new Vector2(430f, 180f);
        Image fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0.25f, 0.12f, 0.035f, 0.94f);

        Outline borde = panel.AddComponent<Outline>();
        borde.effectColor = new Color(0.82f, 0.56f, 0.25f, 1f);
        borde.effectDistance = new Vector2(3f, -3f);

        GameObject tituloObjeto = CrearUI("TextoOrdeno", panel.transform, typeof(TextMeshProUGUI));
        RectTransform tituloRect = tituloObjeto.GetComponent<RectTransform>();
        tituloRect.anchorMin = new Vector2(0f, 1f);
        tituloRect.anchorMax = Vector2.one;
        tituloRect.offsetMin = new Vector2(18f, -58f);
        tituloRect.offsetMax = new Vector2(-18f, -12f);
        titulo = tituloObjeto.GetComponent<TextMeshProUGUI>();
        titulo.alignment = TextAlignmentOptions.Center;
        titulo.fontSize = 28f;
        titulo.fontStyle = FontStyles.Bold;
        titulo.color = new Color(1f, 0.94f, 0.72f, 1f);
        titulo.raycastTarget = false;

        GameObject cubeta = CrearUI("Cubeta", panel.transform, typeof(Image));
        RectTransform cubetaRect = cubeta.GetComponent<RectTransform>();
        cubetaRect.anchorMin = cubetaRect.anchorMax = new Vector2(0.5f, 0.5f);
        cubetaRect.anchoredPosition = new Vector2(0f, -23f);
        cubetaRect.sizeDelta = new Vector2(126f, 48f);
        cubeta.GetComponent<Image>().color = new Color(0.58f, 0.65f, 0.70f, 1f);

        gotas = new RectTransform[3];
        for (int i = 0; i < gotas.Length; i++)
        {
            GameObject gota = CrearUI("GotaLeche" + i, panel.transform, typeof(Image));
            RectTransform gotaRect = gota.GetComponent<RectTransform>();
            gotaRect.anchorMin = gotaRect.anchorMax = new Vector2(0.5f, 0.5f);
            gotaRect.sizeDelta = new Vector2(10f, 24f);
            gota.GetComponent<Image>().color = Color.white;
            gotas[i] = gotaRect;
        }

        GameObject barra = CrearUI("FondoProgresoOrdeno", panel.transform, typeof(Image));
        RectTransform barraRect = barra.GetComponent<RectTransform>();
        barraRect.anchorMin = new Vector2(0.5f, 0f);
        barraRect.anchorMax = new Vector2(0.5f, 0f);
        barraRect.anchoredPosition = new Vector2(0f, 16f);
        barraRect.sizeDelta = new Vector2(360f, 18f);
        barra.GetComponent<Image>().color = new Color(0.10f, 0.06f, 0.02f, 0.9f);

        GameObject relleno = CrearUI("ProgresoOrdeno", barra.transform, typeof(Image));
        RectTransform rellenoRect = relleno.GetComponent<RectTransform>();
        rellenoRect.anchorMin = Vector2.zero;
        rellenoRect.anchorMax = Vector2.one;
        rellenoRect.offsetMin = new Vector2(3f, 3f);
        rellenoRect.offsetMax = new Vector2(-3f, -3f);
        progreso = relleno.GetComponent<Image>();
        progreso.color = new Color(0.92f, 0.92f, 0.80f, 1f);
        progreso.type = Image.Type.Filled;
        progreso.fillMethod = Image.FillMethod.Horizontal;

        panel.SetActive(false);
    }

    private static GameObject CrearUI(string nombre, Transform padre, params System.Type[] extras)
    {
        System.Type[] tipos = new System.Type[extras.Length + 2];
        tipos[0] = typeof(RectTransform);
        tipos[1] = typeof(CanvasRenderer);
        for (int i = 0; i < extras.Length; i++)
            tipos[i + 2] = extras[i];

        GameObject objeto = new GameObject(nombre, tipos);
        objeto.transform.SetParent(padre, false);
        objeto.layer = padre.gameObject.layer;
        return objeto;
    }
}
