using UnityEngine;

public class InventarioUI : MonoBehaviour
{
    public GameObject panelInventario;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            panelInventario.SetActive(!panelInventario.activeSelf);
        }
    }
}