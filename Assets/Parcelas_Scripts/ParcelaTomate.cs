
using TMPro;
using UnityEngine;
public class ParcelaTomate : MonoBehaviour, IParcela, IParcelaConsultable
{
    [Header("Estados de la planta")]
    public GameObject[] semillas;
    public GameObject[] crecimientos;
    public GameObject[] maduros;
    public GameObject[] cajas;
    [Header("Texto flotante de estado (TextMeshPro 3D, uno por slot)")]
    public TextMeshPro[] textosEstado;
    [Header("Tiempo de crecimiento")]
    public float tiempoCrecimiento = 10f;
    [Header("Efecto de agua")]
    public GameObject efectoAguaPrefab;
    private int[] estados; // 0 = vacio, 1 = semilla (esperando riego), 2 = creciendo, 3 = maduro
    private bool[] mostrandoCaja;
    void Start()
    {
        estados = new int[semillas.Length];
        mostrandoCaja = new bool[semillas.Length];
        ZonaCosechaNivel2.Preparar(this, cajas, "Tomate");
        ActualizarTodo();
    }
    void ActualizarTodo()
    {
        for (int i = 0; i < estados.Length; i++)
            ActualizarVisual(i);
    }
    void ActualizarVisual(int i)
    {
        if (semillas[i]) semillas[i].SetActive(estados[i] == 1);
        if (crecimientos[i]) crecimientos[i].SetActive(estados[i] == 2);
        if (maduros[i]) maduros[i].SetActive(estados[i] == 3);
        if (textosEstado != null && textosEstado.Length > i && textosEstado[i] != null)
        {
            bool mostrar = estados[i] == 2 || estados[i] == 3;
            textosEstado[i].gameObject.SetActive(mostrar);
            if (estados[i] == 3)
                textosEstado[i].text = "¡Listo para cosechar!";
        }
    }
    /// <summary>
    /// Interactúa con UN slot puntual (el que corresponde a donde está parado
    /// el jugador). Esto es lo que llama SlotParcela.
    /// </summary>
    public bool InteractuarSlot(int i, string herramienta)
    {
        if (i < 0 || i >= estados.Length) return false;
        if (herramienta == "semilla" && estados[i] == 0)
        {
            estados[i] = 1;
            mostrandoCaja[i] = false;
            ActualizarVisual(i);
            return true;
        }
        else if (herramienta == "regadera" && estados[i] == 1)
        {
            estados[i] = 2;
            ActualizarVisual(i);
            StartCoroutine(Madurar(i));
            if (efectoAguaPrefab != null && semillas[i] != null)
            {
                Vector3 posicion = semillas[i].transform.position + Vector3.up * 0.5f;
                GameObject efecto = Instantiate(efectoAguaPrefab, posicion, Quaternion.identity);
                ParticleSystem ps = efecto.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();
                Destroy(efecto, 1f);
            }
            return true;
        }
        else if (herramienta == "canasta" && estados[i] == 3)
        {
            estados[i] = 0;
            mostrandoCaja[i] = true;
            ActualizarVisual(i);
            GetComponent<ZonaCosechaNivel2>()?.RegistrarCosecha();
            Debug.Log("Cosechaste 1 tomate!");
            return true;
        }
        return false;
    }

    public bool PuedeInteractuarSlot(int i, string herramienta)
    {
        if (estados == null || i < 0 || i >= estados.Length)
            return false;

        if (herramienta == "semilla") return estados[i] == 0;
        if (herramienta == "regadera") return estados[i] == 1;
        if (herramienta == "canasta") return estados[i] == 3;
        return false;
    }

    public int CultivosPendientes
    {
        get
        {
            int cantidad = 0;
            if (estados == null) return cantidad;
            foreach (int estado in estados)
                if (estado > 0) cantidad++;
            return cantidad;
        }
    }

    System.Collections.IEnumerator Madurar(int i)
    {
        float restante = tiempoCrecimiento;
        while (restante > 0f)
        {
            if (textosEstado != null && textosEstado.Length > i && textosEstado[i] != null)
                textosEstado[i].text = "Listo en " + Mathf.CeilToInt(restante) + "s";
            yield return new WaitForSeconds(1f);
            restante -= 1f;
        }
        estados[i] = 3;
        ActualizarVisual(i);
    }
}
