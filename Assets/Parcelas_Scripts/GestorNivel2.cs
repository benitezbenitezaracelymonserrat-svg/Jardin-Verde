using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla el nivel 2 dentro del mismo mapa para conservar lo preparado
/// durante el nivel 1: cosechas, animales e inventario.
/// </summary>
public class GestorNivel2 : MonoBehaviour
{
    private class AsignacionOrdeno
    {
        public Animal animal;
        public BaldeLecheRecolectable balde;
        public bool enProceso;
        public bool completado;
    }

    public static GestorNivel2 Instancia { get; private set; }
    public static bool NivelActivo => Instancia != null && Instancia.nivelActivo;
    public static float Progreso01 =>
        Instancia != null ? Instancia.CalcularProgreso01() : 0f;

    private const int ObjetivoHuevos = 12;
    private const int ObjetivoLecheVaca = 6;
    private const int ObjetivoLecheCabra = 6;
    private const float DuracionOrdeno = 4f;

    private bool nivelActivo;
    private bool completadoMostrado;
    private int objetivoCajas;
    private int cajasRecogidas;
    private int huevosRecogidos;
    private int lecheVacaRecogida;
    private int lecheCabraRecogida;

    private readonly Dictionary<Animal, AsignacionOrdeno> ordenos =
        new Dictionary<Animal, AsignacionOrdeno>();

    private GameObject botonDesafios;
    private GameObject panelDesafios;
    private TextMeshProUGUI textoProgreso;
    private TextMeshProUGUI textoCompletado;
    private GameObject botonContinuarNivel3;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearAutomaticamente()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
            return;

        if (FindFirstObjectByType<GestorNivel2>() == null)
            new GameObject("GestorNivel2").AddComponent<GestorNivel2>();
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!nivelActivo && Input.GetKeyDown(KeyCode.F2))
            IniciarNivel2(0);
    }
#endif

    public static void IniciarNivel2Global(int cosechasEsperadas)
    {
        GestorNivel2 gestor = Instancia;

        if (gestor == null)
            gestor = FindFirstObjectByType<GestorNivel2>();

        if (gestor == null)
            gestor = new GameObject("GestorNivel2").AddComponent<GestorNivel2>();

        gestor.IniciarNivel2(cosechasEsperadas);
    }

    public static void RegistrarProductoGlobal(string tipo, int cantidad)
    {
        if (!NivelActivo || string.IsNullOrWhiteSpace(tipo) || cantidad <= 0)
            return;

        Instancia.RegistrarProducto(tipo, cantidad);
    }

    public static bool PuedeOrdenarGlobal(Animal animal)
    {
        return NivelActivo && Instancia.PuedeOrdenar(animal);
    }

    public static bool SolicitarOrdenoGlobal(
        Interaccion interaccion,
        Animal animal)
    {
        if (!NivelActivo || interaccion == null || animal == null)
            return false;

        return Instancia.SolicitarOrdeno(interaccion, animal);
    }

    public static void PrepararSalidaNivel3Global()
    {
        if (Instancia == null)
            return;

        Instancia.nivelActivo = false;

        if (Instancia.botonDesafios != null)
            Instancia.botonDesafios.SetActive(false);

        if (Instancia.panelDesafios != null)
            Instancia.panelDesafios.SetActive(false);
    }

    private void IniciarNivel2(int cosechasEsperadas)
    {
        if (nivelActivo)
            return;

        Time.timeScale = 1f;
        nivelActivo = true;
        int cajasConfiguradas = Mathf.Max(1, ContarCajasConfiguradas());
        objetivoCajas = cosechasEsperadas > 0
            ? Mathf.Clamp(cosechasEsperadas, 1, cajasConfiguradas)
            : cajasConfiguradas;

        PrepararProductosGanaderos();
        ConfigurarInteracciones();
        ConfigurarPuertasCorrales();
        CorralTransitableNivel2.Preparar();
        TeletransportarJugador();
        ConstruirInterfaz();
        ActualizarInterfaz();

        CinematicaNiveles.ReproducirNivel2();

        Debug.Log(
            $"Nivel 2 iniciado: {objetivoCajas} cajas, " +
            $"{ObjetivoHuevos} huevos y 12 baldes de leche."
        );
    }

    private void PrepararProductosGanaderos()
    {
        Animal[] animales = FindObjectsByType<Animal>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Animal animal in animales)
        {
            if (animal != null)
                animal.PrepararProduccionNivel2();
        }

        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        List<GameObject> baldesVaca = new List<GameObject>();
        List<GameObject> baldesCabra = new List<GameObject>();

        foreach (Transform elemento in transforms)
        {
            if (elemento == null ||
                !elemento.name.StartsWith(
                    "Baldeleche",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TieneAncestro(elemento, "Zonas_de_Vacas"))
                baldesVaca.Add(elemento.gameObject);
            else if (TieneAncestro(elemento, "ZonaCabras"))
                baldesCabra.Add(elemento.gameObject);
        }

        EmparejarAnimalesYBaldes(
            animales.Where(a => a != null && a.TipoProducto == "LecheVaca"),
            baldesVaca,
            "LecheVaca"
        );

        EmparejarAnimalesYBaldes(
            animales.Where(a => a != null && a.TipoProducto == "LecheCabra"),
            baldesCabra,
            "LecheCabra"
        );

        foreach (Transform elemento in transforms)
        {
            if (elemento == null ||
                !elemento.name.StartsWith(
                    "GallinaHuevo",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            NidoGallina nido = elemento.GetComponent<NidoGallina>();
            if (nido == null)
                nido = elemento.gameObject.AddComponent<NidoGallina>();

            nido.ConfigurarDesdeJerarquia();
            nido.PrepararNivel2();
        }
    }

    private void EmparejarAnimalesYBaldes(
        IEnumerable<Animal> animales,
        List<GameObject> baldes,
        string tipoProducto)
    {
        List<GameObject> disponibles = new List<GameObject>(baldes);
        List<Animal> ordenados = animales
            .OrderBy(a => a.transform.position.x)
            .ThenBy(a => a.transform.position.z)
            .ToList();

        foreach (Animal animal in ordenados)
        {
            if (disponibles.Count == 0)
                break;

            Vector3 referencia = animal.PuntoBalde != null
                ? animal.PuntoBalde.position
                : animal.transform.position;

            GameObject elegido = disponibles
                .OrderBy(b => (b.transform.position - referencia).sqrMagnitude)
                .First();

            disponibles.Remove(elegido);

            BaldeLecheRecolectable balde =
                elegido.GetComponent<BaldeLecheRecolectable>();
            if (balde == null)
                balde = elegido.AddComponent<BaldeLecheRecolectable>();

            balde.Configurar(tipoProducto);

            ordenos[animal] = new AsignacionOrdeno
            {
                animal = animal,
                balde = balde
            };
        }

        if (ordenados.Count != baldes.Count)
        {
            Debug.LogWarning(
                $"Nivel 2: {tipoProducto} tiene {ordenados.Count} animales " +
                $"y {baldes.Count} baldes."
            );
        }
    }

    private bool PuedeOrdenar(Animal animal)
    {
        if (animal == null || !animal.ProductoListo)
            return false;

        return ordenos.TryGetValue(animal, out AsignacionOrdeno asignacion) &&
               !asignacion.enProceso &&
               !asignacion.completado;
    }

    private bool SolicitarOrdeno(Interaccion interaccion, Animal animal)
    {
        if (!PuedeOrdenar(animal))
            return false;

        StartCoroutine(Ordenar(interaccion, ordenos[animal]));
        return true;
    }

    private IEnumerator Ordenar(
        Interaccion interaccion,
        AsignacionOrdeno asignacion)
    {
        asignacion.enProceso = true;
        Animal animal = asignacion.animal;
        Transform jugador = interaccion.transform;
        PlayerController movimiento = jugador.GetComponent<PlayerController>();
        CharacterController character = jugador.GetComponent<CharacterController>();

        if (movimiento != null)
            movimiento.enabled = false;

        animal.BloquearParaOrdeno(true);

        Transform puntoOrdeno = animal.PuntoOrdeno;
        if (character != null)
            character.enabled = false;

        Vector3 posicionOrdeno = CalcularPuntoOrdenoSeguro(
            animal,
            puntoOrdeno
        );
        jugador.position = posicionOrdeno;

        Vector3 direccion = animal.transform.position - jugador.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude > 0.001f)
            jugador.rotation = Quaternion.LookRotation(direccion.normalized);

        if (character != null)
            character.enabled = true;

        Animator animatorJugador = interaccion.animator;
        if (animatorJugador != null && TieneParametro(animatorJugador, "Ordenar"))
            animatorJugador.ResetTrigger("Ordenar");

        // Se muestra una simulacion estable en pantalla en vez del clip que
        // deformaba la pose y hundia al personaje bajo el terreno.
        SimulacionOrdenoUI.Mostrar(
            DuracionOrdeno,
            animal.TipoProducto == "LecheCabra"
        );

        yield return new WaitForSeconds(DuracionOrdeno);

        // El control vuelve antes de crear el producto. Así, incluso si un
        // balde quedó sin referencia, el jugador nunca queda inmovilizado.
        if (movimiento != null)
            movimiento.enabled = true;

        if (asignacion.balde != null)
            asignacion.balde.MostrarEn(animal.PuntoBalde);
        else
            Debug.LogWarning($"Nivel 2: {animal.name} no tiene balde asignado.");

        animal.ConsumirProduccionNivel2();
        animal.BloquearParaOrdeno(false);

        asignacion.enProceso = false;
        asignacion.completado = true;

    }

    private static Vector3 CalcularPuntoOrdenoSeguro(
        Animal animal,
        Transform puntoMarcado)
    {
        Vector3 centro = animal.transform.position;
        Vector3 lado = puntoMarcado != null
            ? puntoMarcado.position - centro
            : -animal.transform.right;
        lado.y = 0f;

        // Varios PuntoOrdeno están en el propio pivote del animal. Se conserva
        // el lado indicado, pero se aplica una distancia exterior real.
        if (lado.sqrMagnitude < 0.0025f)
            lado = -animal.transform.right;
        lado.Normalize();

        Bounds limites = new Bounds(centro, Vector3.one * 0.4f);
        bool encontroRenderer = false;
        Renderer[] renderers =
            animal.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rendererAnimal in renderers)
        {
            if (rendererAnimal == null || !rendererAnimal.enabled)
                continue;

            if (!encontroRenderer)
            {
                limites = rendererAnimal.bounds;
                encontroRenderer = true;
            }
            else
            {
                limites.Encapsulate(rendererAnimal.bounds);
            }
        }

        float radioEnDireccion =
            Mathf.Abs(lado.x) * limites.extents.x +
            Mathf.Abs(lado.z) * limites.extents.z;
        float distancia = Mathf.Clamp(radioEnDireccion + 0.72f, 1.05f, 2.35f);

        Vector3 puntoExterior = limites.center + lado * distancia;
        puntoExterior.y = puntoMarcado != null
            ? puntoMarcado.position.y
            : centro.y;

        return AjustarPuntoOrdenoAlSuelo(puntoExterior);
    }

    private static Vector3 AjustarPuntoOrdenoAlSuelo(Vector3 puntoDeseado)
    {
        NavMeshHit puntoValido;
        Vector3 origenBusqueda = puntoDeseado + Vector3.up * 1.5f;

        if (NavMesh.SamplePosition(
                origenBusqueda,
                out puntoValido,
                4f,
                NavMesh.AllAreas))
        {
            return puntoValido.position + Vector3.up * 0.06f;
        }

        RaycastHit[] impactos = Physics.RaycastAll(
            puntoDeseado + Vector3.up * 5f,
            Vector3.down,
            12f,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit impacto in impactos.OrderBy(i => i.distance))
        {
            if (impacto.collider == null ||
                impacto.collider.GetComponentInParent<Animal>() != null ||
                impacto.normal.y < 0.55f)
            {
                continue;
            }

            puntoDeseado.y = impacto.point.y + 0.06f;
            return puntoDeseado;
        }

        return puntoDeseado;
    }

    private static bool TieneParametro(Animator animator, string nombre)
    {
        foreach (AnimatorControllerParameter parametro in animator.parameters)
        {
            if (parametro.name == nombre)
                return true;
        }

        return false;
    }

    private void TeletransportarJugador()
    {
        PlayerController jugador = FindFirstObjectByType<PlayerController>();
        if (jugador == null)
            return;

        Transform destinoMarcado = BuscarTransformPorNombre("PuntoInicioNivel2");
        Vector3 destino;

        if (destinoMarcado != null)
        {
            destino = destinoMarcado.position;
        }
        else
        {
            SlotParcela[] slots = FindObjectsByType<SlotParcela>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            if (slots.Length == 0)
                return;

            Vector3 centro = Vector3.zero;
            foreach (SlotParcela slot in slots)
                centro += slot.transform.position;
            centro /= slots.Length;

            Vector3 candidato = centro + new Vector3(-6f, 0f, -6f);
            if (NavMesh.SamplePosition(
                    candidato,
                    out NavMeshHit hit,
                    12f,
                    NavMesh.AllAreas))
            {
                destino = hit.position;
            }
            else
            {
                destino = new Vector3(
                    candidato.x,
                    jugador.transform.position.y,
                    candidato.z
                );
            }
        }

        CharacterController character = jugador.GetComponent<CharacterController>();
        if (character != null)
            character.enabled = false;

        jugador.transform.position = destino + Vector3.up * 0.1f;

        if (character != null)
            character.enabled = true;
    }

    private void ConfigurarInteracciones()
    {
        Interaccion[] interacciones = FindObjectsByType<Interaccion>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Interaccion interaccion in interacciones)
        {
            if (interaccion != null)
                interaccion.SeleccionarCanasta();
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        RectTransform panelCultivo = null;
        RectTransform panelAlimentar = null;
        RectTransform panelRecolectar = null;
        GameObject botonSemilla = null;
        GameObject botonRegadera = null;
        GameObject botonCanasta = null;
        GameObject botonAlimentar = null;
        GameObject botonRecoger = null;

        foreach (RectTransform elemento in
                 canvas.GetComponentsInChildren<RectTransform>(true))
        {
            if (elemento.name == "Panel Cultivo") panelCultivo = elemento;
            else if (elemento.name == "PanelAlimentar") panelAlimentar = elemento;
            else if (elemento.name == "PanelRecolectar") panelRecolectar = elemento;
            else if (elemento.name == "BotonSemilla") botonSemilla = elemento.gameObject;
            else if (elemento.name == "ButtonRegaderaNuevo") botonRegadera = elemento.gameObject;
            else if (elemento.name == "BotonCanasta") botonCanasta = elemento.gameObject;
            else if (elemento.name == "BotonAlimentar") botonAlimentar = elemento.gameObject;
            else if (elemento.name == "BotonRecoger") botonRecoger = elemento.gameObject;
        }

        if (botonSemilla != null) botonSemilla.SetActive(false);
        if (botonRegadera != null) botonRegadera.SetActive(false);
        if (botonAlimentar != null) botonAlimentar.SetActive(false);
        if (panelAlimentar != null) panelAlimentar.gameObject.SetActive(false);
        if (botonCanasta != null) botonCanasta.SetActive(true);
        if (botonRecoger != null) botonRecoger.SetActive(true);

        ConfigurarPanelInferior(panelCultivo, canvas, 320f);
        ConfigurarPanelInferior(panelRecolectar, canvas, 320f);

        if (panelRecolectar != null)
            panelRecolectar.gameObject.SetActive(false);

        GameObject panelOrdenar = CrearPanelOrdenar(canvas);

        foreach (Interaccion interaccion in interacciones)
        {
            if (interaccion != null)
                interaccion.ConfigurarPanelOrdenar(panelOrdenar);
        }

        Button botonOrdenar = panelOrdenar.GetComponentInChildren<Button>(true);
        Interaccion principal = interacciones.FirstOrDefault(i => i != null);
        if (botonOrdenar != null && principal != null)
            botonOrdenar.onClick.AddListener(principal.OrdenarAnimalCercano);

        panelOrdenar.SetActive(false);
    }

    private void ConfigurarPuertasCorrales()
    {
        string[] nombresZonas = { "ZonaPuertaVacas", "ZonaPuertaCabras" };
        HashSet<Animator> puertasAsignadas = new HashSet<Animator>();
        Animator[] animadores = FindObjectsByType<Animator>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (string nombreZona in nombresZonas)
        {
            Transform zona = BuscarTransformPorNombre(nombreZona);
            if (zona == null)
            {
                Debug.LogWarning($"Nivel 2: no se encontro {nombreZona}.");
                continue;
            }

            Collider colision = zona.GetComponent<Collider>();
            if (colision == null)
                colision = zona.gameObject.AddComponent<BoxCollider>();
            colision.isTrigger = true;

            Animator puerta = animadores
                .Where(a => a != null &&
                       !puertasAsignadas.Contains(a) &&
                       TieneParametro(a, "abrirpuerta") &&
                       TieneParametro(a, "Cerrarpuerta"))
                .OrderBy(a =>
                    (a.transform.position - zona.position).sqrMagnitude)
                .FirstOrDefault();

            if (puerta != null)
                puertasAsignadas.Add(puerta);

            PuertaCorralNivel2 controlador =
                zona.GetComponent<PuertaCorralNivel2>();
            if (controlador == null)
                controlador = zona.gameObject.AddComponent<PuertaCorralNivel2>();

            controlador.Configurar(puerta);

            if (puerta == null)
                Debug.LogWarning($"Nivel 2: {nombreZona} no encontro su Animator.");
        }
    }

    private static void ConfigurarPanelInferior(
        RectTransform panel,
        Canvas canvas,
        float ancho)
    {
        if (panel == null)
            return;

        panel.SetParent(canvas.transform, false);
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 18f);
        panel.sizeDelta = new Vector2(ancho, 80f);
        panel.localScale = Vector3.one;

        HorizontalLayoutGroup layout = panel.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
    }

    private GameObject CrearPanelOrdenar(Canvas canvas)
    {
        GameObject panel = CrearObjetoUI("PanelOrdenarNivel2", canvas.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 18f);
        panelRect.sizeDelta = new Vector2(270f, 70f);

        GameObject botonObjeto = CrearObjetoUI("BotonOrdenar", panel.transform);
        RectTransform botonRect = botonObjeto.GetComponent<RectTransform>();
        Estirar(botonRect, 5f);

        Image fondo = botonObjeto.AddComponent<Image>();
        fondo.sprite = CrearFondoRedondeado(260, 60, 14);
        fondo.color = new Color(0.48f, 0.28f, 0.10f, 0.98f);
        AgregarBorde(botonObjeto);

        Button boton = botonObjeto.AddComponent<Button>();
        boton.targetGraphic = fondo;

        TextMeshProUGUI texto = CrearTexto(
            "TextoOrdenar",
            botonObjeto.transform,
            "ORDEÑAR",
            23f,
            TextAlignmentOptions.Center
        );
        Estirar(texto.rectTransform, 4f);
        texto.fontStyle = FontStyles.Bold;
        texto.color = new Color(1f, 0.93f, 0.68f, 1f);
        return panel;
    }

    private void ConstruirInterfaz()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        botonDesafios = CrearObjetoUI("BotonDesafiosNivel2", canvas.transform);
        RectTransform botonRect = botonDesafios.GetComponent<RectTransform>();
        botonRect.anchorMin = new Vector2(1f, 1f);
        botonRect.anchorMax = new Vector2(1f, 1f);
        botonRect.pivot = new Vector2(1f, 1f);
        botonRect.anchoredPosition = new Vector2(-96f, -22f);
        botonRect.sizeDelta = new Vector2(140f, 60f);

        Image fondoBoton = botonDesafios.AddComponent<Image>();
        fondoBoton.sprite = CrearFondoRedondeado(140, 60, 16);
        fondoBoton.color = new Color(0.22f, 0.42f, 0.24f, 0.96f);
        AgregarBorde(botonDesafios);

        Button abrir = botonDesafios.AddComponent<Button>();
        abrir.targetGraphic = fondoBoton;

        TextMeshProUGUI textoBoton = CrearTexto(
            "TextoBotonNivel2",
            botonDesafios.transform,
            "TAREAS",
            22f,
            TextAlignmentOptions.Center
        );
        Estirar(textoBoton.rectTransform, 3f);
        textoBoton.fontStyle = FontStyles.Bold;
        textoBoton.color = new Color(1f, 0.93f, 0.68f, 1f);

        panelDesafios = CrearObjetoUI("PanelDesafiosNivel2", canvas.transform);
        RectTransform panelRect = panelDesafios.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(22f, -86f);
        panelRect.sizeDelta = new Vector2(540f, 455f);

        Image fondoPanel = panelDesafios.AddComponent<Image>();
        fondoPanel.color = new Color(0.055f, 0.19f, 0.13f, 0.97f);
        AgregarBorde(panelDesafios);

        TextMeshProUGUI titulo = CrearTexto(
            "TituloNivel2",
            panelDesafios.transform,
            "DESAFIOS - NIVEL 2",
            27f,
            TextAlignmentOptions.Center
        );
        RectTransform tituloRect = titulo.rectTransform;
        tituloRect.anchorMin = new Vector2(0f, 1f);
        tituloRect.anchorMax = new Vector2(1f, 1f);
        tituloRect.pivot = new Vector2(0.5f, 1f);
        tituloRect.offsetMin = new Vector2(24f, -62f);
        tituloRect.offsetMax = new Vector2(-60f, -14f);
        titulo.fontStyle = FontStyles.Bold;
        titulo.color = new Color(1f, 0.91f, 0.62f, 1f);

        GameObject cerrarObjeto = CrearObjetoUI(
            "BotonCerrarNivel2",
            panelDesafios.transform
        );
        RectTransform cerrarRect = cerrarObjeto.GetComponent<RectTransform>();
        cerrarRect.anchorMin = new Vector2(1f, 1f);
        cerrarRect.anchorMax = new Vector2(1f, 1f);
        cerrarRect.pivot = new Vector2(1f, 1f);
        cerrarRect.anchoredPosition = new Vector2(-12f, -12f);
        cerrarRect.sizeDelta = new Vector2(42f, 42f);

        Image fondoCerrar = cerrarObjeto.AddComponent<Image>();
        fondoCerrar.color = new Color(0.48f, 0.12f, 0.10f, 0.95f);
        Button cerrar = cerrarObjeto.AddComponent<Button>();
        cerrar.targetGraphic = fondoCerrar;
        cerrar.onClick.AddListener(() => panelDesafios.SetActive(false));

        TextMeshProUGUI textoCerrar = CrearTexto(
            "TextoCerrarNivel2",
            cerrarObjeto.transform,
            "X",
            23f,
            TextAlignmentOptions.Center
        );
        Estirar(textoCerrar.rectTransform, 2f);

        textoProgreso = CrearTexto(
            "ProgresoNivel2",
            panelDesafios.transform,
            string.Empty,
            22f,
            TextAlignmentOptions.TopLeft
        );
        RectTransform progresoRect = textoProgreso.rectTransform;
        progresoRect.anchorMin = Vector2.zero;
        progresoRect.anchorMax = Vector2.one;
        progresoRect.offsetMin = new Vector2(34f, 128f);
        progresoRect.offsetMax = new Vector2(-28f, -82f);
        textoProgreso.lineSpacing = 10f;

        textoCompletado = CrearTexto(
            "Nivel2Completado",
            panelDesafios.transform,
            "¡FELICIDADES!\nCompletaste los desafios del nivel 2.",
            21f,
            TextAlignmentOptions.Center
        );
        RectTransform completadoRect = textoCompletado.rectTransform;
        completadoRect.anchorMin = new Vector2(0f, 0f);
        completadoRect.anchorMax = new Vector2(1f, 0f);
        completadoRect.pivot = new Vector2(0.5f, 0f);
        completadoRect.offsetMin = new Vector2(24f, 66f);
        completadoRect.offsetMax = new Vector2(-24f, 122f);
        textoCompletado.fontStyle = FontStyles.Bold;
        textoCompletado.color = new Color(0.46f, 1f, 0.48f, 1f);
        textoCompletado.gameObject.SetActive(false);

        botonContinuarNivel3 = CrearBotonNivel3(panelDesafios.transform);
        botonContinuarNivel3.SetActive(false);

        abrir.onClick.AddListener(() =>
        {
            panelDesafios.SetActive(true);
            panelDesafios.transform.SetAsLastSibling();
        });

        panelDesafios.SetActive(true);
        panelDesafios.transform.SetAsLastSibling();
    }

    private void RegistrarProducto(string tipo, int cantidad)
    {
        if (tipo.Equals("Huevo", StringComparison.OrdinalIgnoreCase))
            huevosRecogidos += cantidad;
        else if (tipo.Equals("LecheVaca", StringComparison.OrdinalIgnoreCase))
            lecheVacaRecogida += cantidad;
        else if (tipo.Equals("LecheCabra", StringComparison.OrdinalIgnoreCase))
            lecheCabraRecogida += cantidad;
        else
            cajasRecogidas += cantidad;

        ActualizarInterfaz();
    }

    private void ActualizarInterfaz()
    {
        if (textoProgreso == null)
            return;

        textoProgreso.text =
            $"{Marca(cajasRecogidas, objetivoCajas)} Recoger cajas: " +
            $"{Mathf.Min(cajasRecogidas, objetivoCajas)}/{objetivoCajas}\n\n" +
            $"{Marca(huevosRecogidos, ObjetivoHuevos)} Recoger huevos: " +
            $"{Mathf.Min(huevosRecogidos, ObjetivoHuevos)}/{ObjetivoHuevos}\n\n" +
            $"{Marca(lecheVacaRecogida, ObjetivoLecheVaca)} Leche de vaca: " +
            $"{Mathf.Min(lecheVacaRecogida, ObjetivoLecheVaca)}/{ObjetivoLecheVaca}\n\n" +
            $"{Marca(lecheCabraRecogida, ObjetivoLecheCabra)} Leche de cabra: " +
            $"{Mathf.Min(lecheCabraRecogida, ObjetivoLecheCabra)}/{ObjetivoLecheCabra}";

        bool completado =
            cajasRecogidas >= objetivoCajas &&
            huevosRecogidos >= ObjetivoHuevos &&
            lecheVacaRecogida >= ObjetivoLecheVaca &&
            lecheCabraRecogida >= ObjetivoLecheCabra;

        if (textoCompletado != null)
            textoCompletado.gameObject.SetActive(completado);

        if (botonContinuarNivel3 != null)
            botonContinuarNivel3.SetActive(completado);

        if (completado && !completadoMostrado && panelDesafios != null)
        {
            completadoMostrado = true;
            panelDesafios.SetActive(true);
            panelDesafios.transform.SetAsLastSibling();
        }
    }

    private float CalcularProgreso01()
    {
        int total = Mathf.Max(1,
            objetivoCajas + ObjetivoHuevos +
            ObjetivoLecheVaca + ObjetivoLecheCabra);
        int actual =
            Mathf.Min(cajasRecogidas, objetivoCajas) +
            Mathf.Min(huevosRecogidos, ObjetivoHuevos) +
            Mathf.Min(lecheVacaRecogida, ObjetivoLecheVaca) +
            Mathf.Min(lecheCabraRecogida, ObjetivoLecheCabra);

        return Mathf.Clamp01(actual / (float)total);
    }

    private GameObject CrearBotonNivel3(Transform padre)
    {
        GameObject objeto = CrearObjetoUI("BotonContinuarNivel3", padre);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 12f);
        rect.sizeDelta = new Vector2(190f, 48f);

        Image fondo = objeto.AddComponent<Image>();
        fondo.sprite = CrearFondoRedondeado(190, 48, 13);
        fondo.color = new Color(0.31f, 0.55f, 0.25f, 1f);
        AgregarBorde(objeto);

        Button boton = objeto.AddComponent<Button>();
        boton.targetGraphic = fondo;
        boton.onClick.AddListener(PasarAlNivel3);

        TextMeshProUGUI texto = CrearTexto(
            "TextoContinuarNivel3",
            objeto.transform,
            "NIVEL 3",
            21f,
            TextAlignmentOptions.Center
        );
        Estirar(texto.rectTransform, 3f);
        texto.fontStyle = FontStyles.Bold;
        texto.color = new Color(1f, 0.93f, 0.68f, 1f);
        return objeto;
    }

    private void PasarAlNivel3()
    {
        nivelActivo = false;

        if (botonDesafios != null)
            botonDesafios.SetActive(false);

        if (panelDesafios != null)
            panelDesafios.SetActive(false);

        GestorNivel3.IniciarNivel3Global();
    }

    private int ContarCajasConfiguradas()
    {
        HashSet<GameObject> cajas = new HashSet<GameObject>();

        foreach (ParcelaTomate p in FindObjectsByType<ParcelaTomate>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AgregarCajas(cajas, p.cajas);
        foreach (ParcelaZanahoria p in FindObjectsByType<ParcelaZanahoria>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AgregarCajas(cajas, p.cajas);
        foreach (ParcelaCalabaza p in FindObjectsByType<ParcelaCalabaza>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AgregarCajas(cajas, p.cajas);
        foreach (ParcelaRepollo p in FindObjectsByType<ParcelaRepollo>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AgregarCajas(cajas, p.cajas);
        foreach (ParcelaPapa p in FindObjectsByType<ParcelaPapa>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AgregarCajas(cajas, p.cajas);
        foreach (ParcelaCebolla p in FindObjectsByType<ParcelaCebolla>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            AgregarCajas(cajas, p.cajas);

        return cajas.Count;
    }

    private static void AgregarCajas(
        HashSet<GameObject> destino,
        GameObject[] origen)
    {
        if (origen == null)
            return;

        foreach (GameObject caja in origen)
        {
            if (caja != null)
                destino.Add(caja);
        }
    }

    private static bool TieneAncestro(Transform elemento, string nombre)
    {
        Transform actual = elemento;
        while (actual != null)
        {
            if (actual.name == nombre)
                return true;
            actual = actual.parent;
        }

        return false;
    }

    private static Transform BuscarTransformPorNombre(string nombre)
    {
        Transform[] todos = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        return todos.FirstOrDefault(t => t != null && t.name == nombre);
    }

    private static string Marca(int actual, int objetivo)
    {
        return actual >= objetivo ? "[LISTO]" : "[ ]";
    }

    private static GameObject CrearObjetoUI(string nombre, Transform padre)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.layer = padre.gameObject.layer;
        objeto.transform.SetParent(padre, false);
        return objeto;
    }

    private static TextMeshProUGUI CrearTexto(
        string nombre,
        Transform padre,
        string contenido,
        float tamano,
        TextAlignmentOptions alineacion)
    {
        GameObject objeto = CrearObjetoUI(nombre, padre);
        TextMeshProUGUI texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.alignment = alineacion;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.textWrappingMode = TextWrappingModes.Normal;
        return texto;
    }

    private static void Estirar(RectTransform rect, float margen)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margen, margen);
        rect.offsetMax = new Vector2(-margen, -margen);
    }

    private static void AgregarBorde(GameObject objeto)
    {
        Outline borde = objeto.AddComponent<Outline>();
        borde.effectColor = new Color(0.35f, 0.19f, 0.07f, 1f);
        borde.effectDistance = new Vector2(3f, -3f);
        borde.useGraphicAlpha = true;
    }

    private static Sprite CrearFondoRedondeado(int ancho, int alto, int radio)
    {
        Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;
        textura.hideFlags = HideFlags.HideAndDontSave;

        Color32 transparente = new Color32(255, 255, 255, 0);
        Color32 blanco = new Color32(255, 255, 255, 255);
        Color32[] pixeles = new Color32[ancho * alto];

        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                float esquinaX = x < radio
                    ? radio - x
                    : x >= ancho - radio ? x - (ancho - radio - 1) : 0f;
                float esquinaY = y < radio
                    ? radio - y
                    : y >= alto - radio ? y - (alto - radio - 1) : 0f;

                bool dentro = esquinaX == 0f || esquinaY == 0f ||
                    esquinaX * esquinaX + esquinaY * esquinaY <= radio * radio;

                pixeles[y * ancho + x] = dentro ? blanco : transparente;
            }
        }

        textura.SetPixels32(pixeles);
        textura.Apply();

        Sprite sprite = Sprite.Create(
            textura,
            new Rect(0f, 0f, ancho, alto),
            new Vector2(0.5f, 0.5f),
            100f
        );
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
