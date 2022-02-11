using UnityEngine;
using UnityEngine.UI;
public class HeightSlider : MonoBehaviour
{
    /// <summary>
    /// Text for setting height with keypad
    /// </summary>
    public Slider HSlider;
    public Text SliderText;
    public Text HelperInputField;
    public Toggle PreviewToggle;
    public Material partiallytransparent;
    bool IsEnteringKeypadValue = false; // used in numericenter();
    private static GameObject HeightIndicator;
    private void Start()
    {
        HSlider.minValue = Consts.MIN_H;
        HSlider.maxValue = Consts.MAX_H - 5;
    }
    private void Update()
    {
        Numericenter();
        Mousewheelcheck();
        Ctrl_key_works();
    }
    void Ctrl_key_works()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Highlight.over && !Input.GetKey(KeyCode.LeftAlt))
        {
            if (IsEnteringKeypadValue)
            Hide_text_helper();

            Vector3 v = Highlight.pos;
            int index = Consts.PosToIndex(Highlight.pos);
            HSlider.value = Consts.RealHeight2SliderValue(Consts.current_heights[index]);
        }
    }
    /// <summary> shows current height value next to HeightSlider </summary>
    public void ShowValue(float val)
    {
        SliderText.text = val.ToString();
        HSlider.value = val;
    }
    /// <summary>Displays transparent cuboid for 2 secs.</summary>
    public void SliderPreview(float v)
    {
        if (HeightIndicator != null)
            Destroy(HeightIndicator);
        if (PreviewToggle.isOn && !CopyPaste.IsEnabled())
        {
            float y = Consts.SliderValue2RealHeight(v);
            HeightIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(HeightIndicator.GetComponent<BoxCollider>());
            HeightIndicator.GetComponent<MeshRenderer>().material = partiallytransparent;
            HeightIndicator.transform.localScale = new Vector3(3f, 0.05f, 3f);
            HeightIndicator.transform.position = new Vector3(4 * Highlight.tile_pos.x + 2, y, 4 * Highlight.tile_pos.z + 2);
            Destroy(HeightIndicator, 2);
        }
    }
    private void Hide_text_helper()
    {
        transform.GetChild(0).gameObject.SetActive(true);
        HelperInputField.text = "";
        IsEnteringKeypadValue = false;
    }
    /// <summary>
    /// Handles setting sliderheight with keypad
    /// </summary>
    private void Numericenter()
    {
        if (Input.GetKeyDown(KeyCode.KeypadMultiply))
        {
            try
            {
                HSlider.value = float.Parse(HelperInputField.text, System.Globalization.CultureInfo.InvariantCulture);
                Hide_text_helper();
            }
            catch
            {
                HSlider.value = 0;
                Hide_text_helper();
            }
        }
        KeyCode[] keyCodes = { KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5,
            KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.KeypadPeriod, KeyCode.KeypadMinus };
        for (int i = 0; i < keyCodes.Length; i++)
        {
            if (Input.GetKeyDown(keyCodes[i]))
            {
                transform.GetChild(0).gameObject.SetActive(false);
                if (i < 10)
                    HelperInputField.text += i.ToString();
                else if (i == 10)
                    HelperInputField.text += ".";
                else if (i == 11)
                    HelperInputField.text += "-";
                IsEnteringKeypadValue = true;
                return;
            }
        }
    }
    /// <summary>
    /// Handles setting sliderheight with mousewheel and other keys
    /// </summary>
    void Mousewheelcheck()
    {
        if (Input.GetKey(KeyCode.R))
            return;
        if (Input.GetAxis("Mouse ScrollWheel") != 0 && IsEnteringKeypadValue)
        {
            Hide_text_helper();
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0 && HSlider.value < Consts.MAX_H)
            {
                HSlider.value += 10;
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0 && HSlider.value > Consts.MIN_H)
            {
                HSlider.value -= 10;
            }
        }
        else if (Input.GetKey(KeyCode.LeftAlt))
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0 && HSlider.value < Consts.MAX_H)
            {
                HSlider.value += 0.1f;
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0 && HSlider.value > Consts.MIN_H)
            {
                HSlider.value -= 0.1f;
            }
        }
        else if (Input.GetAxis("Mouse ScrollWheel") > 0 && HSlider.value < Consts.MAX_H)
        {
            HSlider.value += 1;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && HSlider.value > Consts.MIN_H)
        {
            HSlider.value -= 1;
        }
    }
}
