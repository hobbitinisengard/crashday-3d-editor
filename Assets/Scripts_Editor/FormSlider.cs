using UnityEngine;
using UnityEngine.UI;
public enum Mode { Manual, Shape, Profiles}
public class FormSlider : MonoBehaviour
{
  public Slider slider;
  public Text text;

  public void SwitchSliderTo(Mode mode)
  {
    if (mode == Mode.Manual)
    {
      text.text = "Manual mode";
      slider.value = 1;
    }
    if (mode == Mode.Shape)
    {
      text.text = "Shape mode";
      slider.value = 2;
    }
    if(mode == Mode.Profiles)
    {
      text.text = "Profiles mode";
      slider.value = 3;
    }
  }
  public void SwitchTextStatus(string Text)
  {
    text.text = Text;
  }
}
