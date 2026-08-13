using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Comedero : MonoBehaviour
{
    [Header("Animales asignados")]
    public Animal[] animales;
    public Transform[] puntosComida;

    [Header("Costo")]
    [Min(1)]
    public int costoComida = 1;

    [Header("Efectos")]
    public Transform puntoEfectoBolsa;
    public GameObject efectoBolsaPrefab;
    public float duracionEfecto = 2f;
    [Header("Comida visible opcional")]
    public GameObject comidaVisual;
    public float retrasoAparicionComida = 0.5f;
    public float tiempoComidaVisible = 20f;

    public bool Usado { get; private set; }

    public bool Alimentar(InventarioComida inventario)
    {
        if (Usado)
        {
            Debug.Log("Este comedero ya fue utilizado.");
            return false;
        }

        if (animales == null || puntosComida == null)
            return false;

        int cantidad = Mathf.Min(animales.Length, puntosComida.Length);
        List<int> asignacionesValidas = new List<int>();

        for (int i = 0; i < cantidad; i++)
        {
            if (animales[i] != null &&
                puntosComida[i] != null &&
                animales[i].PuedeIrAlComedero)
            {
                asignacionesValidas.Add(i);
            }
        }

        if (asignacionesValidas.Count == 0)
        {
            Debug.LogWarning("Este comedero no tiene animales disponibles.");
            return false;
        }

        if (inventario == null ||
            !inventario.UsarComida(Mathf.Max(1, costoComida)))
        {
            Debug.Log("No hay suficiente comida.");
            return false;
        }

        Usado = true;
        ReproducirEfectos();

        foreach (int i in asignacionesValidas)
            animales[i].IrAlComedero(puntosComida[i]);

        return true;
    }

    void ReproducirEfectos()
    {
        if (efectoBolsaPrefab != null)
        {
            Transform punto = puntoEfectoBolsa != null
                ? puntoEfectoBolsa
                : transform;

            GameObject efecto = Instantiate(
                efectoBolsaPrefab,
                punto.position,
                punto.rotation
            );

            Destroy(efecto, duracionEfecto);
        }

        if (comidaVisual != null)
            StartCoroutine(MostrarComida());
    }

    IEnumerator MostrarComida()
    {
        yield return new WaitForSeconds(retrasoAparicionComida);

        comidaVisual.SetActive(true);

        yield return new WaitForSeconds(tiempoComidaVisible);

        comidaVisual.SetActive(false);
    }
}