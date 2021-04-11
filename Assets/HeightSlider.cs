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
  public Material partiallytransparent;
  bool IsEnteringKeypadValue = false; // used in numericenter();
  private void Start()
  {
    HSlider.minValue = Service.minHeight;
    HSlider.maxValue = Service.maxHeight;
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
      int index = Service.PosToIndex(Highlight.pos);
      HSlider.value = Service.RealHeight2SliderValue(Service.current_heights[index]);
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
    float y = Service.SliderValue2RealHeight(v);
    GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
    Destroy(preview.GetComponent<BoxCollider>());
    preview.GetComponent<MeshRenderer>().material = partiallytransparent;
    preview.transform.localScale = new Vector3(3f, 0.05f, 3);
    preview.transform.position = new Vector3(2 + Highlight.TL.x, y, 2 + Highlight.TL.z);
    Destroy(preview, 2);
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
    KeyCode[] keyCodes = { KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.KeypadPeriod, KeyCode.KeypadMinus };
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
    if (!FlyCamera.isStandardCam)
      return;
    if (Input.GetKey(KeyCode.R))
      return;
    if (Input.GetAxis("Mouse ScrollWheel") != 0 && IsEnteringKeypadValue)
    {
      Hide_text_helper();
    }
    if (Input.GetKey(KeyCode.LeftShift))
    {
      if (Input.GetAxis("Mouse ScrollWheel") > 0 && HSlider.value < Service.maxHeight)
      {
        HSlider.value += 10;
      }
      else if (Input.GetAxis("Mouse ScrollWheel") < 0 && HSlider.value > Service.minHeight)
      {
        HSlider.value -= 10;
      }
    }
    else if (Input.GetKey(KeyCode.LeftAlt))
    {
      if (Input.GetAxis("Mouse ScrollWheel") > 0 && HSlider.value < Service.maxHeight)
      {
        HSlider.value += 0.25f;
      }
      else if (Input.GetAxis("Mouse ScrollWheel") < 0 && HSlider.value > Service.minHeight)
      {
        HSlider.value -= 0.25f;
      }
    }
    else if (Input.GetAxis("Mouse ScrollWheel") > 0 && HSlider.value < Service.maxHeight)
    {
      HSlider.value += 1;
    }
    else if (Input.GetAxis("Mouse ScrollWheel") < 0 && HSlider.value > Service.minHeight)
    {
      HSlider.value -= 1;
    }
  }
}