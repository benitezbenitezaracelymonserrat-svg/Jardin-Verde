using UnityEngine;

public class CreditsManager : MonoBehaviour
{
    [Header("Arrastra aquí el Panel de Créditos")]
    public GameObject panelCreditos;

    public void AbrirCreditos()
    {
        panelCreditos.SetActive(true);
    }

    public void CerrarCreditos()
    {
        panelCreditos.SetActive(false);
    }
}
