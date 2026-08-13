using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Interaccion : MonoBehaviour
{
    [Header("Interfaz")]
    public TextMeshProUGUI textoInteraccion;
    public GameObject panelBotones;
    public GameObject panelCultivo;
    public GameObject panelAlimentar;
    public GameObject panelRecolectar;
    private GameObject panelOrdenar;

    [Header("Herramientas")]
    public string herramientaActual = "semilla";

    [Header("Inventarios")]
    public InventarioComida inventario;
    public InventarioProductos inventarioProductos;

    [Header("Animacion del jugador")]
    public Animator animator;

    [Header("Deteccion por cercania")]
    public float radioDeteccion = 1.5f;

    private SlotParcela slotCercano;
    private Comedero comederoCercano;
    private ProductoRecolectable productoCercano;
    private CajaCosechaRecolectable cajaCercana;
    private ZonaCosechaNivel2 zonaCosechaCercana;
    private NidoGallina nidoCercano;
    private BaldeLecheRecolectable baldeCercano;
    private Animal animalOrdenableCercano;

    void Start()
    {
        radioDeteccion = Mathf.Max(radioDeteccion, 1.5f);

        if (inventarioProductos == null)
            inventarioProductos = InventarioProductos.BuscarPrincipal();
    }

    void Update()
    {
        DetectarCercanos();

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SeleccionarSemilla();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SeleccionarRegadera();

        if (Input.GetKeyDown(KeyCode.Alpha3) && GestorNivel2.NivelActivo)
            SeleccionarCanasta();

        if (Input.GetKeyDown(KeyCode.E))
            Interactuar();
    }

    void Interactuar()
    {
        // Durante el nivel 2, Recoger tiene prioridad sobre el slot que esta
        // debajo de una caja para que ambos colliders no se estorben.
        if (GestorNivel2.NivelActivo)
        {
            if (RecolectarProductoDisponible())
                return;

            if (animalOrdenableCercano != null &&
                GestorNivel2.SolicitarOrdenoGlobal(
                    this,
                    animalOrdenableCercano))
            {
                return;
            }
        }

        if (slotCercano != null)
        {
            bool huboAccion = slotCercano.Interactuar(herramientaActual);

            if (huboAccion)
            {
                GestorDesafiosNivel1.RegistrarCultivoGlobal(
                    herramientaActual,
                    slotCercano.ParcelaComponente,
                    slotCercano
                );
            }

            bool estabaCosechando = string.Equals(
                herramientaActual,
                "canasta",
                System.StringComparison.OrdinalIgnoreCase
            );

            if (huboAccion && estabaCosechando && animator != null)
                animator.SetTrigger("Cosechar");

            return;
        }

        if (comederoCercano != null && !GestorNivel2.NivelActivo)
            comederoCercano.Alimentar(inventario);
    }

    bool RecolectarProductoDisponible()
    {
        if (inventarioProductos == null)
            inventarioProductos = InventarioProductos.BuscarPrincipal();

        if (cajaCercana != null && cajaCercana.EstaDisponible)
            return cajaCercana.Recoger(inventarioProductos);

        if (nidoCercano != null && nidoCercano.EstaDisponible)
            return nidoCercano.Recoger(inventarioProductos);

        if (baldeCercano != null && baldeCercano.EstaDisponible)
            return baldeCercano.Recoger(inventarioProductos);

        if (productoCercano != null && productoCercano.EstaDisponible)
            return productoCercano.Recoger(inventarioProductos);

        return false;
    }

    void DetectarCercanos()
    {
        SlotParcela slotEncontrado = null;
        float distanciaSlotMasCercano = float.PositiveInfinity;
        SlotParcela slotAccionable = null;
        float distanciaSlotAccionable = float.PositiveInfinity;
        HashSet<SlotParcela> slotsRevisados = new HashSet<SlotParcela>();
        Comedero comederoEncontrado = null;
        ProductoRecolectable productoEncontrado = null;
        CajaCosechaRecolectable cajaEncontrada = null;
        float distanciaCajaMasCercana = float.PositiveInfinity;
        NidoGallina nidoEncontrado = null;
        BaldeLecheRecolectable baldeEncontrado = null;
        Animal animalOrdenableEncontrado = null;

        Collider[] cercanos = Physics.OverlapSphere(
            transform.position,
            radioDeteccion,
            ~0,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider col in cercanos)
        {
            {
                CajaCosechaRecolectable posibleCaja =
                    col.GetComponentInParent<CajaCosechaRecolectable>();

                if (posibleCaja != null && posibleCaja.EstaDisponible)
                {
                    Vector3 diferenciaCaja =
                        posibleCaja.transform.position - transform.position;
                    diferenciaCaja.y = 0f;
                    float distanciaCaja = diferenciaCaja.sqrMagnitude;

                    if (distanciaCaja < distanciaCajaMasCercana)
                    {
                        distanciaCajaMasCercana = distanciaCaja;
                        cajaEncontrada = posibleCaja;
                    }
                }
            }

            if (nidoEncontrado == null)
            {
                NidoGallina posibleNido = col.GetComponentInParent<NidoGallina>();
                if (posibleNido != null && posibleNido.EstaDisponible)
                    nidoEncontrado = posibleNido;
            }

            if (baldeEncontrado == null)
            {
                BaldeLecheRecolectable posibleBalde =
                    col.GetComponentInParent<BaldeLecheRecolectable>();
                if (posibleBalde != null && posibleBalde.EstaDisponible)
                    baldeEncontrado = posibleBalde;
            }

            if (productoEncontrado == null)
            {
                ProductoRecolectable posible =
                    col.GetComponentInParent<ProductoRecolectable>();

                if (posible != null && posible.EstaDisponible)
                    productoEncontrado = posible;
            }

            if (animalOrdenableEncontrado == null)
            {
                Animal posibleAnimal = col.GetComponentInParent<Animal>();

                if (GestorNivel2.PuedeOrdenarGlobal(posibleAnimal))
                    animalOrdenableEncontrado = posibleAnimal;
            }

            SlotParcela posibleSlot = col.GetComponentInParent<SlotParcela>();

            if (posibleSlot != null &&
                posibleSlot.EstaConfigurado &&
                slotsRevisados.Add(posibleSlot))
            {
                // Se compara solo X/Z porque la altura del personaje no debe
                // hacer que gane una parcela vecina. Esto evita que tomate
                // active accidentalmente un indice de zanahoria.
                Vector3 diferencia =
                    posibleSlot.PosicionInteraccion - transform.position;
                diferencia.y = 0f;
                float distancia = diferencia.sqrMagnitude;

                if (distancia < distanciaSlotMasCercano)
                {
                    distanciaSlotMasCercano = distancia;
                    slotEncontrado = posibleSlot;
                }

                // Para cosechar no obliga a encontrar el centro exacto de
                // cada planta: dentro del radio elige la planta madura mas
                // cercana. Lo mismo mejora plantar y regar.
                if (posibleSlot.PuedeInteractuar(herramientaActual) &&
                    distancia < distanciaSlotAccionable)
                {
                    distanciaSlotAccionable = distancia;
                    slotAccionable = posibleSlot;
                }
            }

            if (comederoEncontrado == null)
                comederoEncontrado = col.GetComponentInParent<Comedero>();
        }

        slotCercano = slotAccionable != null
            ? slotAccionable
            : slotEncontrado;
        zonaCosechaCercana = slotCercano != null
            ? slotCercano.ZonaCosecha
            : null;
        comederoCercano = comederoEncontrado;
        productoCercano = productoEncontrado;
        cajaCercana = cajaEncontrada;
        nidoCercano = nidoEncontrado;
        baldeCercano = baldeEncontrado;
        animalOrdenableCercano = animalOrdenableEncontrado;

        bool hayProductoNivel2 = GestorNivel2.NivelActivo &&
            (cajaCercana != null ||
             nidoCercano != null ||
             baldeCercano != null ||
             productoCercano != null);

        if (hayProductoNivel2)
            MostrarRecolectar();
        else if (GestorNivel2.NivelActivo && animalOrdenableCercano != null)
            MostrarOrdenar();
        else if (slotCercano != null)
            MostrarCultivo();
        else if (comederoCercano != null && !GestorNivel2.NivelActivo)
            MostrarAlimentar();
        else
            OcultarUI();
    }

    void OcultarUI()
    {
        if (textoInteraccion != null) textoInteraccion.text = "";
        if (panelBotones != null) panelBotones.SetActive(false);
        if (panelCultivo != null) panelCultivo.SetActive(false);
        if (panelAlimentar != null) panelAlimentar.SetActive(false);
        if (panelRecolectar != null) panelRecolectar.SetActive(false);
        if (panelOrdenar != null) panelOrdenar.SetActive(false);
    }

    void MostrarCultivo()
    {
        if (textoInteraccion != null)
            textoInteraccion.text = "Presiona E para interactuar";

        if (panelBotones != null) panelBotones.SetActive(true);
        if (panelCultivo != null) panelCultivo.SetActive(true);
        if (panelAlimentar != null) panelAlimentar.SetActive(false);
        if (panelRecolectar != null) panelRecolectar.SetActive(false);
        if (panelOrdenar != null) panelOrdenar.SetActive(false);
    }

    void MostrarAlimentar()
    {
        if (textoInteraccion != null)
            textoInteraccion.text = "Presiona E para llenar el comedero";

        if (panelBotones != null) panelBotones.SetActive(true);
        if (panelAlimentar != null) panelAlimentar.SetActive(true);
        if (panelCultivo != null) panelCultivo.SetActive(false);
        if (panelRecolectar != null) panelRecolectar.SetActive(false);
        if (panelOrdenar != null) panelOrdenar.SetActive(false);
    }

    void MostrarRecolectar()
    {
        if (textoInteraccion != null)
            textoInteraccion.text = "Presiona RECOGER o E";

        if (panelBotones != null) panelBotones.SetActive(true);
        if (panelRecolectar != null) panelRecolectar.SetActive(true);
        if (panelCultivo != null) panelCultivo.SetActive(false);
        if (panelAlimentar != null) panelAlimentar.SetActive(false);
        if (panelOrdenar != null) panelOrdenar.SetActive(false);
    }

    void MostrarOrdenar()
    {
        if (textoInteraccion != null)
            textoInteraccion.text = "Presiona ORDEÑAR o E";

        if (panelBotones != null) panelBotones.SetActive(true);
        if (panelOrdenar != null) panelOrdenar.SetActive(true);
        if (panelRecolectar != null) panelRecolectar.SetActive(false);
        if (panelCultivo != null) panelCultivo.SetActive(false);
        if (panelAlimentar != null) panelAlimentar.SetActive(false);
    }

    public void SetHerramienta(string herramienta)
    {
        herramientaActual = herramienta;
    }

    public void SeleccionarSemilla()
    {
        if (!GestorNivel2.NivelActivo)
            herramientaActual = "semilla";
    }

    public void SeleccionarRegadera()
    {
        if (!GestorNivel2.NivelActivo)
            herramientaActual = "regadera";
    }

    public void SeleccionarCanasta()
    {
        herramientaActual = "canasta";
    }

    // Se conserva el nombre para no romper la conexion existente del boton UI.
    public void AlimentarAnimalCercano()
    {
        if (comederoCercano != null && !GestorNivel2.NivelActivo)
            comederoCercano.Alimentar(inventario);
    }

    // Este metodo ya esta conectado al BotonRecoger de la escena.
    public void RecolectarProductoCercano()
    {
        if (GestorNivel2.NivelActivo)
            RecolectarProductoDisponible();
    }

    public void ConfigurarPanelOrdenar(GameObject nuevoPanel)
    {
        panelOrdenar = nuevoPanel;
    }

    public void OrdenarAnimalCercano()
    {
        if (GestorNivel2.NivelActivo && animalOrdenableCercano != null)
            GestorNivel2.SolicitarOrdenoGlobal(this, animalOrdenableCercano);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
    }
}
