using UnityEngine;
using UnityEngine.EventSystems;

public class BotonAnimado : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    RectTransform rect;

    Vector3 escalaNormal;
    Vector3 escalaGrande;

    Vector2 posicionNormal;
    Vector2 posicionHover;

    bool mouseEncima = false;

    void Start()
    {
        rect = GetComponent<RectTransform>();

        escalaNormal = Vector3.one;
        escalaGrande = Vector3.one * 1.1f;

        posicionNormal = rect.anchoredPosition;
        posicionHover = posicionNormal + new Vector2(0,5);
    }

    void Update()
    {
        if(mouseEncima)
        {
            rect.localScale = Vector3.Lerp(
                rect.localScale,
                escalaGrande,
                Time.deltaTime * 10f);

            rect.anchoredPosition = Vector2.Lerp(
                rect.anchoredPosition,
                posicionHover,
                Time.deltaTime * 10f);
        }
        else
        {
            rect.localScale = Vector3.Lerp(
                rect.localScale,
                escalaNormal,
                Time.deltaTime * 10f);

            rect.anchoredPosition = Vector2.Lerp(
                rect.anchoredPosition,
                posicionNormal,
                Time.deltaTime * 10f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseEncima = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseEncima = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rect.localScale = Vector3.one * 0.95f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(mouseEncima)
            rect.localScale = escalaGrande;
        else
            rect.localScale = escalaNormal;
    }
}
