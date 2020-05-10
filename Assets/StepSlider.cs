using UnityEngine;
using UnityEngine.UI;

public class StepSlider : MonoBehaviour
{
  public Text text;
  private static float Step = 5;
  public void OnValueChanged(float val)
  {
    text.text = val.ToString();
    Step = (int)val;
  }
  public static float GetPercent()
  {
    return Step / 100f;
  }
}
