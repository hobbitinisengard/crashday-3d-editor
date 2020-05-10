using UnityEngine;
using UnityEngine.UI;

public class RadiusSlider : MonoBehaviour
{
  public static int Radius = 5;

  private Slider slider;
  public Text text;
  public Text ApproxText;
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
    text.text = val.ToString();
    Radius = (int)val;
    ApproxText.text = "= " + (val / 4f).ToString(); 
  }
}
