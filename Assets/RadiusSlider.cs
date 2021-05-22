using UnityEngine;
using UnityEngine.UI;

public class RadiusSlider : MonoBehaviour
{
  public static int Radius = 5;

  private Slider slider;
  private void Start()
  {
    slider = GetComponent<Slider>();
  }
  void Update()
  {
    if (Input.GetKey(KeyCode.R))
    {
      if (Input.GetAxis("Mouse ScrollWheel") > 0 && slider.value < slider.maxValue)
      {
        slider.value++;
      }
      else if (Input.GetAxis("Mouse ScrollWheel") < 0 && slider.value > slider.minValue)
      {
        slider.value--;
      }
    }
  }
  public void OnValueChanged(float val)
  {
    Radius = (int)val;
  }
}
