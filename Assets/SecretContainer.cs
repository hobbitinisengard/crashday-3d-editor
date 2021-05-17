using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class SecretContainer : MonoBehaviour, IPointerEnterHandler
{
  public Button InfinityButton;

  public void OnPointerEnter(PointerEventData eventData)
  {
    InfinityButton.gameObject.SetActive(true);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    InfinityButton.gameObject.SetActive(false);
  }
}
