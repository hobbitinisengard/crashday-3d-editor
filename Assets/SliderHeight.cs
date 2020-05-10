using UnityEngine;
using UnityEngine.UI;
//Slider in main menu -> Create new track
public class SliderHeight : MonoBehaviour
{

  public static Text trackHeight;
  static public int val = 27;
  Color orange = new Color32(238, 170, 37, 255);

  void Start()
  {
    trackHeight = GetComponent<Text>();
  }

  public void UpdateHeight(float value)
  {
    if (value * SliderWidth.val > Service.TrackTileLimit)
    {
      SliderWidth.trackWidth.color = Color.red;
      trackHeight.color = Color.red;
      MainMenu.CanCreateTrack = false;
    }
    else
    {
      SliderWidth.trackWidth.color = orange;
      trackHeight.color = orange;
      MainMenu.CanCreateTrack = true;
    }
    val = (int)value;
    trackHeight.text = value.ToString();
  }
 
}
