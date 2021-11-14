using UnityEngine;
using UnityEngine.UI;
//Slider in main menu -> Create new track
public enum Size { width, height };
public class TrackSize : MonoBehaviour
{
    public Text[] text;
    public InputField[] inputField;
    public Slider[] slider;

    public static int[] value = new int[] { 25, 25 };
    private static bool text_already_updated = false;
    Color orange = new Color32(238, 170, 37, 255);

    void Start()
    {
        inputField[0].onEndEdit.AddListener(delegate { UpdateSlider(Size.width); });
        inputField[1].onEndEdit.AddListener(delegate { UpdateSlider(Size.height); });
        slider[0].onValueChanged.AddListener(delegate { UpdateText(Size.width); });
        slider[1].onValueChanged.AddListener(delegate { UpdateText(Size.height); });

        inputField[0].text = value[0].ToString();
        inputField[1].text = value[1].ToString();
        slider[0].value = value[0];
        slider[1].value = value[1];
    }

    public void UpdateText(Size dim)
    {
        if (text_already_updated)
        {
            text_already_updated = false;
            return;
        }
        if (slider[0].value * slider[1].value > Consts.MAX_ELEMENTS)
        {
            text[0].color = Color.red;
            text[1].color = Color.red;
            MainMenu.CanCreateTrack = false;
        }
        else
        {
            text[0].color = orange;
            text[1].color = orange;
            MainMenu.CanCreateTrack = true;
        }
        int i = (int)dim;
        value[i] = (int)slider[i].value;
        inputField[i].text = slider[i].value.ToString();
    }

    public void UpdateSlider(Size dim)
    {
        text_already_updated = true;
        int i = (int)dim;
        if (inputField[i].text.Length == 0)
            inputField[i].text = "3";
        inputField[i].text = Mathf.Clamp(float.Parse(inputField[i].text), 3f, (float)(Consts.MAX_ELEMENTS / 3)).ToString();

        if (int.Parse(inputField[0].text) * int.Parse(inputField[1].text) > Consts.MAX_ELEMENTS)
        {
            text[0].color = Color.red;
            text[1].color = Color.red;
            MainMenu.CanCreateTrack = false;
        }
        else
        {
            text[0].color = orange;
            text[1].color = orange;
            MainMenu.CanCreateTrack = true;
        }
        slider[i].value = float.Parse(inputField[i].text);
        value[i] = (int)slider[i].value;
    }
}
