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

    public void updateHeight(float value)
    {
        if (value * SliderWidth.val > MainMenu.tile_limit)
        {
            SliderWidth.trackWidth.color = Color.red;
            trackHeight.color = Color.red;
            Data.PlaygamePass = false;
        }
        else
        {
            SliderWidth.trackWidth.color = orange;
            trackHeight.color = orange;
            Data.PlaygamePass = true;
        }
        //Debug.Log ("th" + value + "tw" + val);
        val = (int)value;
        trackHeight.text = value.ToString();
    }
}
