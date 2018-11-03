using UnityEngine;
using UnityEngine.UI;
//Slider in main menu -> Create new track
public class SliderWidth : MonoBehaviour
{
    public static Text trackWidth;
    static public int val = 26; //Edytor wykorzystuje
    Color orange = new Color32(238, 170, 37, 255);
    void Start()
    {
        trackWidth = GetComponent<Text>();
    }

    public void updateWidth(float value)
    {
        if (value * SliderHeight.val > MainMenu.tile_limit)
        {
            SliderHeight.trackHeight.color = Color.red;
            trackWidth.color = Color.red;
            STATIC.PlaygamePass = false;
        }
        else
        {
            trackWidth.color = orange;
            SliderHeight.trackHeight.color = orange;
            STATIC.PlaygamePass = true;
        }
        val = (int)value;
        trackWidth.text = value.ToString();
    }
}
