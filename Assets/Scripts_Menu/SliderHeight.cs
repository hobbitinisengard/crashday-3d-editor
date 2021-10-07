using UnityEngine;
using UnityEngine.UI;
//Slider in main menu -> Create new track
public class SliderHeight : MonoBehaviour
{
    public static Text trackHeight;
    public static int val = 25;
    Color orange = new Color32(238, 170, 37, 255);

    void Start()
    {
        trackHeight = GetComponent<Text>();
    }

    public void UpdateHeight(float value)
    {
        if (value * SliderWidth.val > Consts.MAX_ELEMENTS)
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
