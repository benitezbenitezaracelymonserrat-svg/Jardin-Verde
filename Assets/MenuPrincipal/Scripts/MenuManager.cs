using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Menu principal")]
    public GameObject mainMenu; // arrastrá "Mainmenu" acá en el Inspector

    [Header("Paneles")]
    public GameObject panCreditos;
    public GameObject panAyuda;
    public GameObject panOpciones;
    public GameObject fondoOscuroOpciones;

    private void ReiniciarTitulo()
    {
        if (mainMenu == null)
        {
            return;
        }

        TituloAnimacion animacion = mainMenu.GetComponentInChildren<TituloAnimacion>(true);
        if (animacion != null)
        {
            animacion.ReiniciarEntrada();
        }
    }

    // Botón JUGAR
    public void Jugar()
    {
        const string escenaJuego = "SampleScene";
        if (Application.CanStreamedLevelBeLoaded(escenaJuego))
        {
            SceneManager.LoadScene(escenaJuego);
        }
        else
        {
            Debug.LogError($"La escena '{escenaJuego}' no está incluida en Build Settings.");
        }
    }

    // Botón CREDITOS
    public void AbrirCreditos()
    {
        if (mainMenu != null) mainMenu.SetActive(false);
        if (panCreditos != null) panCreditos.SetActive(true);
    }

    public void CerrarCreditos()
    {
        if (panCreditos != null) panCreditos.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);
        ReiniciarTitulo();
    }

    // Botón AYUDA
    public void AbrirAyuda()
    {
        if (mainMenu != null) mainMenu.SetActive(false);
        if (panAyuda != null) panAyuda.SetActive(true);
    }

    public void CerrarAyuda()
    {
        if (panAyuda != null) panAyuda.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);
        ReiniciarTitulo();
    }

    // Botón OPCIONES
    public void AbrirOpciones()
    {
        if (fondoOscuroOpciones != null)
        {
            fondoOscuroOpciones.SetActive(true);
        }

        if (panOpciones != null)
        {
            panOpciones.SetActive(true);
        }
    }

    public void CerrarOpciones()
    {
        if (panOpciones != null)
        {
            panOpciones.SetActive(false);
        }

        if (fondoOscuroOpciones != null)
        {
            fondoOscuroOpciones.SetActive(false);
        }

        ReiniciarTitulo();
    }
}
