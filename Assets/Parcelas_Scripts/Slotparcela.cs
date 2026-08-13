
using UnityEngine;

/// <summary>
/// Poner en un GameObject con un Collider chico (Is Trigger) ubicado
/// justo sobre cada slot de semilla individual (uno por cada tomate,
/// zanahoria, etc.). El jugador tiene que pararse ahÃƒÂ­ para poder
/// plantar/regar/cosechar ESE slot puntual.
/// </summary>
public class SlotParcela : MonoBehaviour
{
    private static readonly Vector3 TamanoColliderInteraccion =
        new Vector3(1.15f, 0.45f, 1.15f);

    [Tooltip("El ÃƒÂ­ndice de este slot dentro de los arrays de la parcela padre (0, 1, 2...)")]
    public int indice;

    [Tooltip("ArrastrÃƒÂ¡ acÃƒÂ¡ el script de la parcela padre (ParcelaTomate, ParcelaZanahoria, etc.)")]
    public MonoBehaviour parcelaComponente;

    // Permite que la pizarra identifique la zona padre sin cambiar
    // la mecanica individual de cada slot.
    public MonoBehaviour ParcelaComponente => parcelaComponente;

    private IParcela parcela;
    private IParcelaConsultable parcelaConsultable;

    /// <summary>
    /// Posicion individual que debe usar el jugador para elegir este slot.
    /// No utiliza el centro de un collider grande de la huerta padre.
    /// </summary>
    public Vector3 PosicionInteraccion => transform.position;

    public bool EstaConfigurado =>
        indice >= 0 && parcelaComponente is IParcela;

    public ZonaCosechaNivel2 ZonaCosecha =>
        parcelaComponente != null
            ? parcelaComponente.GetComponent<ZonaCosechaNivel2>()
            : null;

    void Awake()
    {
        parcela = parcelaComponente as IParcela;
        parcelaConsultable = parcelaComponente as IParcelaConsultable;

        // Cada slot necesita su propio trigger. Tomate y zanahoria tenian
        // varios SlotParcela sin collider directo, por eso algunos lugares
        // no se detectaban y Unity terminaba usando un slot vecino.
        if (GetComponent<Collider>() == null)
        {
            BoxCollider zonaInteraccion = gameObject.AddComponent<BoxCollider>();
            zonaInteraccion.isTrigger = true;
            zonaInteraccion.center = new Vector3(0f, 0.2f, 0f);
            zonaInteraccion.size = TamanoColliderInteraccion;
        }

        if (parcela == null)
            Debug.LogWarning($"SlotParcela en {name}: el componente asignado no implementa IParcela.");
    }

    public bool Interactuar(string herramienta)
    {
        if (parcela == null) return false;
        return parcela.InteractuarSlot(indice, herramienta);
    }

    public bool PuedeInteractuar(string herramienta)
    {
        return parcelaConsultable == null ||
               parcelaConsultable.PuedeInteractuarSlot(indice, herramienta);
    }
}
