using UnityEngine;

public class UIAutoAdapt : MonoBehaviour
{
    public RectTransform bandera;
    public RectTransform panelAyuda;
    public RectTransform panelCreditos;


    void Start()
    {
        AjustarUI();
    }


    void AjustarUI()
    {
        AjustarPantallaCompleta(bandera);

        AjustarPanel(panelAyuda);
        AjustarPanel(panelCreditos);
    }


    void AjustarPantallaCompleta(RectTransform objeto)
    {
        if(objeto == null) return;

        objeto.anchorMin = new Vector2(0,0);
        objeto.anchorMax = new Vector2(1,1);

        objeto.offsetMin = Vector2.zero;
        objeto.offsetMax = Vector2.zero;

        objeto.localScale = Vector3.one;
    }


    void AjustarPanel(RectTransform panel)
    {
        if(panel == null) return;


        panel.anchorMin = new Vector2(0.5f,0.5f);
        panel.anchorMax = new Vector2(0.5f,0.5f);

        panel.anchoredPosition = Vector2.zero;

        panel.localScale = Vector3.one;
    }
}
