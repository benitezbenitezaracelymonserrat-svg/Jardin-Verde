using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class VegetalesCelebracion : MonoBehaviour
{
    private static readonly string[] NombresRecursos =
    {
        "Tomate",
        "Lechuga",
        "Zanahoria",
        "Cebolla",
        "Papa",
        "Calabaza"
    };

    [Header("Lluvia lateral")]
    [SerializeField, Range(6, 20)] private int cantidad = 14;
    [SerializeField] private Vector2 intervalo = new Vector2(0.42f, 0.65f);
    [SerializeField] private Vector2 duracionCaida = new Vector2(4.4f, 5.8f);
    [SerializeField] private Vector2 tamano = new Vector2(74f, 104f);
    [SerializeField, Range(0f, 90f)] private float rotacionMaxima = 34f;

    private readonly List<GameObject> verdurasActivas = new List<GameObject>();
    private Texture2D[] texturas;
    private RectTransform areaLluvia;
    private Coroutine emisionActual;

    public void Reproducir()
    {
        Detener();

        if (!PrepararRecursos() || !PrepararArea())
        {
            return;
        }

        emisionActual = StartCoroutine(EmitirVerduras());
    }

    public void Detener()
    {
        StopAllCoroutines();
        emisionActual = null;

        for (int indice = verdurasActivas.Count - 1; indice >= 0; indice--)
        {
            if (verdurasActivas[indice] != null)
            {
                Destroy(verdurasActivas[indice]);
            }
        }

        verdurasActivas.Clear();
    }

    private bool PrepararRecursos()
    {
        if (texturas != null && texturas.Length == NombresRecursos.Length)
        {
            return true;
        }

        texturas = new Texture2D[NombresRecursos.Length];
        for (int indice = 0; indice < NombresRecursos.Length; indice++)
        {
            texturas[indice] = Resources.Load<Texture2D>(
                $"VegetalesMenu/{NombresRecursos[indice]}");

            if (texturas[indice] == null)
            {
                // La lluvia es decorativa. Si el recurso opcional no está
                // importado, generamos una ficha vegetal para no romper la
                // animación ni llenar la consola de errores.
                texturas[indice] = CrearTexturaVegetal(indice);
            }
        }

        return true;
    }

    private static Texture2D CrearTexturaVegetal(int indice)
    {
        Color[] colores =
        {
            new Color32(220, 55, 45, 255),
            new Color32(92, 176, 72, 255),
            new Color32(242, 132, 35, 255),
            new Color32(218, 188, 100, 255),
            new Color32(189, 142, 83, 255),
            new Color32(236, 145, 32, 255)
        };

        const int resolucion = 64;
        Texture2D textura = new Texture2D(
            resolucion,
            resolucion,
            TextureFormat.RGBA32,
            false);
        textura.name = NombresRecursos[indice];
        textura.hideFlags = HideFlags.HideAndDontSave;

        Color colorVerdura = colores[indice % colores.Length];
        Color colorHoja = new Color32(67, 137, 55, 255);
        Color transparente = new Color(0f, 0f, 0f, 0f);
        Color[] pixeles = new Color[resolucion * resolucion];
        Vector2 centro = new Vector2(31.5f, 29f);

        for (int y = 0; y < resolucion; y++)
        {
            for (int x = 0; x < resolucion; x++)
            {
                Vector2 normalizado = new Vector2(
                    (x - centro.x) / 25f,
                    (y - centro.y) / 22f);
                bool cuerpo = normalizado.sqrMagnitude <= 1f;
                bool hoja = y > 46 && Mathf.Abs(x - 32f) < (58f - y) * 1.1f;
                pixeles[y * resolucion + x] = cuerpo
                    ? colorVerdura
                    : hoja ? colorHoja : transparente;
            }
        }

        textura.SetPixels(pixeles);
        textura.Apply(false, false);
        return textura;
    }

    private bool PrepararArea()
    {
        if (areaLluvia != null)
        {
            return true;
        }

        RectTransform menu = GetComponent<RectTransform>();
        if (menu == null)
        {
            return false;
        }

        GameObject objetoArea = new GameObject(
            "LluviaVegetales",
            typeof(RectTransform),
            typeof(CanvasGroup));
        objetoArea.transform.SetParent(transform, false);

        areaLluvia = objetoArea.GetComponent<RectTransform>();
        areaLluvia.anchorMin = Vector2.zero;
        areaLluvia.anchorMax = Vector2.one;
        areaLluvia.offsetMin = Vector2.zero;
        areaLluvia.offsetMax = Vector2.zero;
        areaLluvia.localScale = Vector3.one;

        CanvasGroup grupo = objetoArea.GetComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        // El fondo del menu permanece primero. Las verduras quedan despues
        // del fondo pero detras del letrero y de todos los botones.
        areaLluvia.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
        return true;
    }

    private IEnumerator EmitirVerduras()
    {
        yield return new WaitForSecondsRealtime(0.12f);

        AudioSource musica = BuscarFuenteMusica();
        float tiempoMusicaAnterior = musica != null ? musica.time : 0f;
        bool cancionActiva = musica != null && musica.isPlaying;
        int primerRecurso = Random.Range(0, texturas.Length);
        int indice = 0;

        // Cuando hay musica se emite hasta que termina la vuelta actual del
        // tema. Como el AudioSource esta en loop, el salto de tiempo al inicio
        // indica el final sin cambiar la configuracion del audio.
        while (cancionActiva || (musica == null && indice < cantidad))
        {
            int recurso = (primerRecurso + indice) % texturas.Length;
            bool ladoDerecho = indice % 2 == 0;
            CrearVerdura(texturas[recurso], ladoDerecho);
            indice++;

            float espera = Random.Range(intervalo.x, intervalo.y);
            float tiempoEspera = 0f;
            while (tiempoEspera < espera)
            {
                tiempoEspera += Time.unscaledDeltaTime;

                if (musica != null)
                {
                    if (!musica.isPlaying)
                    {
                        cancionActiva = false;
                        break;
                    }

                    float tiempoMusicaActual = musica.time;
                    if (tiempoMusicaActual + 0.35f < tiempoMusicaAnterior)
                    {
                        cancionActiva = false;
                        break;
                    }

                    tiempoMusicaAnterior = tiempoMusicaActual;
                }

                yield return null;
            }
        }

        emisionActual = null;
    }

    private static AudioSource BuscarFuenteMusica()
    {
        AudioSource[] fuentes = FindObjectsByType<AudioSource>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (AudioSource fuente in fuentes)
        {
            if (fuente != null &&
                fuente.clip != null &&
                fuente.loop &&
                fuente.isPlaying)
            {
                return fuente;
            }
        }

        return null;
    }

    private void CrearVerdura(Texture2D textura, bool ladoDerecho)
    {
        GameObject objeto = new GameObject(
            $"Verdura_{textura.name}",
            typeof(RectTransform),
            typeof(RawImage));
        objeto.transform.SetParent(areaLluvia, false);

        RawImage imagen = objeto.GetComponent<RawImage>();
        imagen.texture = textura;
        imagen.raycastTarget = false;
        imagen.color = new Color(1f, 1f, 1f, 0.94f);

        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        float alto = Random.Range(tamano.x, tamano.y);
        float proporcion = textura.width / (float)textura.height;
        rect.sizeDelta = new Vector2(alto * proporcion, alto);

        float mitadAncho = areaLluvia.rect.width * 0.5f;
        float mitadAlto = areaLluvia.rect.height * 0.5f;
        float bordeInterior = mitadAncho * 0.70f;
        float bordeExterior = Mathf.Max(bordeInterior + 1f, mitadAncho - 70f);
        float x = Random.Range(bordeInterior, bordeExterior);
        if (!ladoDerecho)
        {
            x = -x;
        }

        Vector2 inicio = new Vector2(
            x,
            mitadAlto + Random.Range(50f, 145f));
        Vector2 final = new Vector2(
            x + Random.Range(-42f, 42f),
            -mitadAlto - alto - 45f);

        rect.anchoredPosition = inicio;
        float anguloInicial = Random.Range(-7f, 7f);
        rect.localRotation = Quaternion.Euler(0f, 0f, anguloInicial);

        verdurasActivas.Add(objeto);
        StartCoroutine(Caer(
            objeto,
            rect,
            imagen,
            inicio,
            final,
            anguloInicial,
            Random.Range(-rotacionMaxima, rotacionMaxima),
            Random.Range(duracionCaida.x, duracionCaida.y)));
    }

    private IEnumerator Caer(
        GameObject objeto,
        RectTransform rect,
        RawImage imagen,
        Vector2 inicio,
        Vector2 final,
        float anguloInicial,
        float giro,
        float duracion)
    {
        float tiempo = 0f;
        while (tiempo < duracion && objeto != null)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            float avance = Mathf.SmoothStep(0f, 1f, t);

            rect.anchoredPosition = Vector2.LerpUnclamped(inicio, final, avance);
            rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                anguloInicial + giro * avance);

            float alpha = t < 0.82f
                ? 0.94f
                : Mathf.Lerp(0.94f, 0f, (t - 0.82f) / 0.18f);
            imagen.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        verdurasActivas.Remove(objeto);
        if (objeto != null)
        {
            Destroy(objeto);
        }
    }

    private void OnDisable()
    {
        Detener();
    }
}
