
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Animal : MonoBehaviour
{
    private static PlayerController jugadorCompartido;
    private static Collider[] collidersJugadorCompartidos;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarCacheCompartida()
    {
        jugadorCompartido = null;
        collidersJugadorCompartidos = null;
    }

    [Header("Animacion")]
    public Animator animator;
    public string speedParam = "speed";

    [Header("Navegacion (deambular)")]
    private NavMeshAgent agent;
    public float radioWander = 5f;
    public Transform centroCorral;
    public float tiempoEntreWander = 4f;
    private float temporizadorWander;
    private Vector3 puntoCentral; // NUEVO: el centro fijo del ÃƒÂ¡rea de paseo


    [Header("Produccion")]
    public string tipoProducto = "Huevo";
    public int cantidadProducto = 1;
    public float tiempoEspera = 15f;
    public GameObject productoVisual;  // nido CON huevo (ya lo tenÃƒÂ­as)
    public GameObject nidoVacioVisual; // NUEVO: nido SIN nada
    public GameObject cajaVisual;
    public bool crearProductoVisualAutomatico = false;

    private int estado = 0;
    private bool procesando = false;

    [Header("Sonidos")]
    public AudioClip sonidoComer;
    public AudioClip[] sonidosAmbiente;
    public float minTiempoAmbiente = 5f;
    public float maxTiempoAmbiente = 15f;
    private AudioSource audioSource;

    [Header("Movimiento hacia el comedero")]
    public string comerParam = "comer";
    public float distanciaLlegadaComedero = 0.3f;
    public float tiempoMaximoLlegada = 12f;

    private bool yendoAlComedero;
    private bool nivel2Preparado;
    private bool bloqueadoPorOrdeno;

    public bool FueAlimentado { get; private set; }
    public string TipoProducto => tipoProducto;
    public bool EsOrdenable =>
        tipoProducto == "LecheVaca" || tipoProducto == "LecheCabra";
    public Transform PuntoOrdeno => BuscarHijo(transform, "PuntoOrdeno");
    public Transform PuntoBalde => BuscarHijo(transform, "PuntoBalde");

    public bool ProductoListo
    {
        get
        {
            return estado == 2 &&
                   !procesando &&
                   !string.IsNullOrWhiteSpace(tipoProducto);
        }
    }

    public bool PuedeIrAlComedero
    {
        get
        {
            return !procesando &&
                   !yendoAlComedero &&
                   !FueAlimentado &&
                   estado == 0 &&
                   agent != null &&
                   agent.isOnNavMesh;
        }
    }
    public bool IrAlComedero(Transform puntoComida)
    {
        if (puntoComida == null || !PuedeIrAlComedero)
            return false;

        NavMeshHit hit;

        if (!NavMesh.SamplePosition(
                puntoComida.position,
                out hit,
                1.5f,
                NavMesh.AllAreas))
        {
            Debug.LogWarning(
                "El PuntoComida no está colocado sobre el NavMesh.",
                puntoComida
            );

            return false;
        }

        procesando = true;
        yendoAlComedero = true;

        StartCoroutine(IrYComer(puntoComida, hit.position));
        return true;
    }

    IEnumerator IrYComer(Transform puntoComida, Vector3 destino)
    {
        agent.isStopped = false;
        agent.SetDestination(destino);

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < tiempoMaximoLlegada)
        {
            if (!agent.pathPending &&
                agent.remainingDistance <=
                Mathf.Max(agent.stoppingDistance, distanciaLlegadaComedero))
            {
                break;
            }

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // La rotación del PuntoComida determina hacia dónde mira el animal.
        transform.rotation = puntoComida.rotation;

        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetTrigger(comerParam);
        }

        if (sonidoComer != null && audioSource != null)
            audioSource.PlayOneShot(sonidoComer);

        FueAlimentado = true;
        yendoAlComedero = false;

        yield return new WaitForSeconds(tiempoEspera);

        if (!nivel2Preparado)
        {
            estado = 2;
            ActualizarVisual();
        }

        temporizadorWander = tiempoEntreWander;
        procesando = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (agent != null)
        {
            // Evita que todos los animales resuelvan el NavMesh en el mismo frame.
            agent.avoidancePriority = 35 + Mathf.Abs(GetInstanceID() % 45);
        }

        if (audioSource != null)
        {
            // Los sonidos de un corral solo se oyen al estar cerca y no
            // saturan toda la mezcla del mapa.
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.dopplerLevel = 0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 12f;
        }

        IgnorarColisionesConJugador();
        ConfigurarProductoSegunAnimal();
        temporizadorWander = UnityEngine.Random.Range(
            0.35f,
            Mathf.Max(0.5f, tiempoEntreWander + 1f)
        );
        puntoCentral = centroCorral != null
       ? centroCorral.position
       : transform.position;
        if (audioSource != null && sonidosAmbiente != null &&
            sonidosAmbiente.Length > 0)
        {
            StartCoroutine(SonidoAmbienteLoop());
        }
        ActualizarVisual();
    }

    void Update()
    {
        if (CinematicaIntro.cinematicaActiva)
            return;

        // Solamente deambula cuando no está yendo al comedero ni comiendo.
        if (!procesando && !yendoAlComedero && !bloqueadoPorOrdeno)
        {
            temporizadorWander -= Time.deltaTime;

            if (temporizadorWander <= 0f)
            {
                MoverAPuntoAleatorio();

                temporizadorWander = Random.Range(
                    tiempoEntreWander - 1f,
                    tiempoEntreWander + 2f
                );
            }
        }

        // Envía la velocidad real al Blend Tree.
        if (animator != null && agent != null)
        {
            float velocidadNormalizada =
                agent.velocity.magnitude /
                Mathf.Max(agent.speed, 0.01f);

            animator.SetFloat(
                speedParam,
                velocidadNormalizada
            );
        }
    }

    void ActualizarVisual()
    {
        if (estado == 2 &&
            productoVisual == null &&
            crearProductoVisualAutomatico &&
            !string.IsNullOrWhiteSpace(tipoProducto))
        {
            CrearProductoVisualAutomatico();
        }

        if (productoVisual != null)
            productoVisual.SetActive(
                estado == 2 && !string.IsNullOrWhiteSpace(tipoProducto)
            );
        if (nidoVacioVisual != null) nidoVacioVisual.SetActive(estado != 2); // NUEVO: vacÃƒÂ­o en cualquier otro momento
        if (cajaVisual != null) cajaVisual.SetActive(estado == 3);
    }

    void CrearProductoVisualAutomatico()
    {
        bool esHuevo = tipoProducto.Equals(
            "Huevo",
            System.StringComparison.OrdinalIgnoreCase
        );

        GameObject producto = GameObject.CreatePrimitive(
            esHuevo ? PrimitiveType.Sphere : PrimitiveType.Cylinder
        );

        producto.name = $"Producto_{tipoProducto}_{name}";
        producto.transform.position =
            transform.position + transform.forward * 0.75f + Vector3.up * 0.3f;

        if (esHuevo)
        {
            producto.transform.localScale = new Vector3(0.3f, 0.42f, 0.3f);
        }
        else
        {
            producto.transform.localScale = new Vector3(0.34f, 0.28f, 0.34f);
        }

        Renderer rendererProducto = producto.GetComponent<Renderer>();
        if (rendererProducto != null)
        {
            rendererProducto.material.color = esHuevo
                ? new Color(1f, 0.97f, 0.82f, 1f)
                : new Color(0.82f, 0.9f, 1f, 1f);
        }

        Collider colliderProducto = producto.GetComponent<Collider>();
        if (colliderProducto != null)
            colliderProducto.isTrigger = true;

        ProductoRecolectable recolectable =
            producto.AddComponent<ProductoRecolectable>();
        recolectable.animalDueno = this;

        productoVisual = producto;
    }
    
    public bool Recolectar(InventarioProductos inventarioProd)
    {
        Debug.Log("EntrÃƒÂ³ a Recolectar");

        if (!ProductoListo)
            return false;

        if (inventarioProd == null)
            inventarioProd = InventarioProductos.BuscarPrincipal();

        if (inventarioProd == null)
        {
            Debug.LogWarning("No se encontro el inventario principal.");
            return false;
        }

        estado = 3;
        ActualizarVisual();

        inventarioProd.AgregarProducto(tipoProducto, cantidadProducto);
        GestorNivel2.RegistrarProductoGlobal(tipoProducto, cantidadProducto);

        Debug.Log($"Recogiste: {tipoProducto} x{cantidadProducto}");
        return true;
    }

    public void PrepararProduccionNivel2()
    {
        // Los modelos del mapa pueden llamarse "1", "2", etc. Antes de
        // preparar el nivel se identifica el producto por TODO el camino de
        // su jerarquia (corral de vacas, corral de cabras o gallinero).
        ConfigurarProductoSegunAnimal();
        nivel2Preparado = true;

        if (string.IsNullOrWhiteSpace(tipoProducto))
            return;

        procesando = false;
        yendoAlComedero = false;
        estado = 2;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }

        ActualizarVisual();
    }

    private void IgnorarColisionesConJugador()
    {
        if (jugadorCompartido == null)
        {
            jugadorCompartido = FindFirstObjectByType<PlayerController>(
                FindObjectsInactive.Include
            );
        }

        if (jugadorCompartido == null)
            return;

        Collider[] colisionesAnimal =
            GetComponentsInChildren<Collider>(true);

        if (collidersJugadorCompartidos == null)
        {
            collidersJugadorCompartidos =
                jugadorCompartido.GetComponentsInChildren<Collider>(true);
        }

        foreach (Collider colisionAnimal in colisionesAnimal)
        {
            if (colisionAnimal == null || colisionAnimal.isTrigger)
                continue;

            foreach (Collider colisionJugador in collidersJugadorCompartidos)
            {
                if (colisionJugador != null)
                    Physics.IgnoreCollision(
                        colisionAnimal,
                        colisionJugador,
                        true
                    );
            }
        }
    }

    public void BloquearParaOrdeno(bool bloquear)
    {
        bloqueadoPorOrdeno = bloquear;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = bloquear;
            if (bloquear)
                agent.ResetPath();
        }

        if (bloquear && animator != null)
            animator.SetFloat(speedParam, 0f);
    }

    public void ConsumirProduccionNivel2()
    {
        estado = 3;
        procesando = false;
        yendoAlComedero = false;
        ActualizarVisual();
    }

    void ConfigurarProductoSegunAnimal()
    {
        bool estaEnGallinero = JerarquiaContiene(
            transform,
            "gallina", "chicken", "gallinero"
        );
        bool estaEnCorralVacas = JerarquiaContiene(
            transform,
            "zonas_de_vacas", "zona vacas", "vaca", "cow"
        );
        bool estaEnCorralCabras = JerarquiaContiene(
            transform,
            "zonacabras", "zona cabras", "cabra", "goat"
        );

        if (estaEnGallinero)
        {
            tipoProducto = "Huevo";
        }
        else if (estaEnCorralVacas)
        {
            tipoProducto = "LecheVaca";
        }
        else if (estaEnCorralCabras)
        {
            tipoProducto = "LecheCabra";
        }
        else
        {
            // Caballos, ovejas y chanchos no producen objetos en el nivel 2.
            tipoProducto = string.Empty;
        }
    }

    private static bool JerarquiaContiene(
        Transform elemento,
        params string[] fragmentos)
    {
        Transform actual = elemento;

        while (actual != null)
        {
            string nombre = actual.name.ToLowerInvariant();

            foreach (string fragmento in fragmentos)
            {
                if (nombre.Contains(fragmento))
                    return true;
            }

            actual = actual.parent;
        }

        return false;
    }

    private static Transform BuscarHijo(Transform raiz, string nombre)
    {
        foreach (Transform hijo in raiz)
        {
            if (hijo.name == nombre)
                return hijo;

            Transform encontrado = BuscarHijo(hijo, nombre);
            if (encontrado != null)
                return encontrado;
        }

        return null;
    }

#if UNITY_EDITOR
    public void PrepararProductoParaPrueba()
    {
        FueAlimentado = true;

        if (string.IsNullOrWhiteSpace(tipoProducto))
            return;

        procesando = false;
        yendoAlComedero = false;
        estado = 2;
        ActualizarVisual();
    }
#endif

    void MoverAPuntoAleatorio()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        Vector3 direccionAleatoria = UnityEngine.Random.insideUnitSphere * radioWander;
        direccionAleatoria += puntoCentral; // ANTES decÃƒÂ­a transform.position
        NavMeshHit hit;
        if (NavMesh.SamplePosition(direccionAleatoria, out hit, radioWander, 1))
            agent.SetDestination(hit.position);
    }

    IEnumerator SonidoAmbienteLoop()
    {
        while (true)
        {
            float espera = UnityEngine.Random.Range(minTiempoAmbiente, maxTiempoAmbiente);
            yield return new WaitForSeconds(espera);
            if (CinematicaIntro.cinematicaActiva) continue;
            if (sonidosAmbiente.Length > 0 && audioSource != null)
            {
                AudioClip clip = sonidosAmbiente[UnityEngine.Random.Range(0, sonidosAmbiente.Length)];
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
