using System.Collections;
using UnityEngine;
public class ParcelaPapa : MonoBehaviour, IParcela, IParcelaConsultable
{
    [Header("Estados de la papa")]
    public GameObject[] semillas;
    public GameObject[] crecimientos;
    public GameObject[] maduros;
    public GameObject[] cajas;
    [Header("Tiempo de crecimiento")]
    public float tiempoCrecimiento = 10f;
    [Header("Efecto de agua")]
    public GameObject efectoAguaPrefab;
    private int[] estados;
    private bool[] mostrandoCaja;
    void Start()
    {
        estados = new int[semillas.Length];
        mostrandoCaja = new bool[semillas.Length];
        ZonaCosechaNivel2.Preparar(this, cajas, "Papa");
        ActualizarTodo();
    }
    void ActualizarTodo()
    {
        for (int i = 0; i < estados.Length; i++)
            ActualizarVisual(i);
    }
    void ActualizarVisual(int i)
    {
        if (i < semillas.Length && semillas[i] != null)
            semillas[i].SetActive(estados[i] == 1);
        if (i < crecimientos.Length && crecimientos[i] != null)
            crecimientos[i].SetActive(estados[i] == 2);
        if (i < maduros.Length && maduros[i] != null)
            maduros[i].SetActive(estados[i] == 3);
    }
    public bool InteractuarSlot(int i, string herramienta)
    {
        if (i < 0 || i >= estados.Length)
            return false;
        if (herramienta == "semilla" && estados[i] == 0)
        {
            estados[i] = 1;
            mostrandoCaja[i] = false;
            ActualizarVisual(i);
            return true;
        }
        if (herramienta == "regadera" && estados[i] == 1)
        {
            estados[i] = 2;
            ActualizarVisual(i);
            StartCoroutine(Madurar(i));
            if (efectoAguaPrefab != null && semillas[i] != null)
            {
                Vector3 posicion = semillas[i].transform.position + Vector3.up * 0.5f;
                GameObject efecto = Instantiate(efectoAguaPrefab, posicion, Quaternion.identity);
                ParticleSystem particulas = efecto.GetComponent<ParticleSystem>();
                if (particulas != null)
                    particulas.Play();
                Destroy(efecto, 1f);
            }
            return true;
        }
        if (herramienta == "canasta" && estados[i] == 3)
        {
            estados[i] = 0;
            mostrandoCaja[i] = true;
            ActualizarVisual(i);
            GetComponent<ZonaCosechaNivel2>()?.RegistrarCosecha();
            Debug.Log("Cosechaste 1 papa!");
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
        yield return new WaitForSeconds(tiempoCrecimiento);
        if (i >= 0 && i < estados.Length && estados[i] == 2)
        {
            estados[i] = 3;
            ActualizarVisual(i);
        }
    }
}
