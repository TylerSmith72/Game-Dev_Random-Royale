using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI buttonText;
    public Color hoverColor = Color.red;
    public float hoverSize = 1.2f;

    private Color originalColor;
    private float originalSize;

    void Start()
    {
        originalColor = buttonText.color;
        originalSize = buttonText.fontSize;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonText.color = hoverColor;
        buttonText.fontSize = originalSize * hoverSize;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonText.color = originalColor;
        buttonText.fontSize = originalSize;
    }
}
