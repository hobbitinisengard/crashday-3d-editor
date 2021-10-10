using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DuVec3
{
    public Vector3 P1;
    public Vector3 P2;
    public DuVec3(Vector3 p1, Vector3 p2)
    {
        P1 = p1;
        P2 = p2;
    }
}

public enum ManualMode { Single, Areal }
public enum ManualSubMode { Set, Avg, Amp }
/// <summary>
/// hooked to e_formPANEL. Handles Form workflow
/// </summary>
public class Form : MonoBehaviour
{
    // Manual mode menu
    public GameObject ManualMenu;
    public Text CurrentModeText;
    public Button SingleModeButton;
    public Button SmoothModeButton;
    public Button AmplifyModeButton;
    public GameObject IntensitySlider;
    public GameObject DistortionSlider;
    public GameObject RadiusSlider;

    // Shape mode menu
    public GameObject ShapeMenu;
    public GameObject FormMenu;
  
    // Profile mode menu
    public GameObject ProfileMenu;

    public Slider HeightSlider;
    public Slider FormSlider;
    public static ManualMode mode;
    public static ManualSubMode submode;

    private void Start()
    {
        mode = ManualMode.Single;
        Physics.queriesHitBackfaces = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SwitchMode(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchMode(1);
            ManualMenu.GetComponent<SingleMode>().RemoveIndicator();
            ManualMenu.GetComponent<ArealMode>().RemoveIndicator();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SwitchMode(2);

        if (Input.GetKeyDown(KeyCode.F))
            ToggleFormingMode(Input.GetKey(KeyCode.LeftShift));
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleManipMode();
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Z))
            UndoBuffer.MoveThroughLayers(BufferDirection.BACKWARD);
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Y))
            UndoBuffer.MoveThroughLayers(BufferDirection.FORWARD);
    }
  
    /// <summary>
    /// Toggles between 'single mode' and 'areal mode' in Manual forming mode
    /// </summary>
    void ToggleManipMode()
    {
        if (ManualMenu.activeSelf)
        {
            if (mode == ManualMode.Single)
            {
                mode = ManualMode.Areal;
                CurrentModeText.text = "Areal Mode";
            }
            else
            {
                mode = ManualMode.Single;
                CurrentModeText.text = "Single Mode";
            }

            if (submode == ManualSubMode.Set)
                IntensitySlider.SetActive(!IntensitySlider.activeSelf);

            RadiusSlider.SetActive(!RadiusSlider.activeSelf);
        }
    }

    // buttons use this function
    public void SwitchMode(float mode)
    {
        submode = (ManualSubMode)mode;
        SingleModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
        SmoothModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
        AmplifyModeButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;
        if (mode == 0)
        {
            SingleModeButton.transform.GetChild(0).GetComponent<Text>().color = new Color32(219, 203, 178, 255);
            IntensitySlider.SetActive(Form.mode == ManualMode.Areal);
            DistortionSlider.SetActive(false);
        }
        else if (mode == 1)
        {
            SmoothModeButton.transform.GetChild(0).GetComponent<Text>().color = new Color32(219, 203, 178, 255);
            IntensitySlider.SetActive(true);
            DistortionSlider.SetActive(true);
        }
        else if (mode == 2)
        {
            AmplifyModeButton.transform.GetChild(0).GetComponent<Text>().color = new Color32(219, 203, 178, 255);
            IntensitySlider.SetActive(true);
            DistortionSlider.SetActive(false);
        }
    }
    /// <summary>
    /// Toggles between manual and shape forming
    /// </summary>
    void ToggleFormingMode(bool backwards)
    {
        if (ManualMenu.activeSelf)
        {
            if (backwards)
            {
                ManualMenu.SetActive(false);
                FormSlider.GetComponent<FormSlider>().SwitchSliderTo(Mode.Profiles);
                ProfileMenu.SetActive(true);
            }
            else
            {
                ManualMenu.SetActive(false);
                ShapeMenu.SetActive(true);
                FormSlider.GetComponent<FormSlider>().SwitchSliderTo(Mode.Shape);
            }
        }
        else if(ShapeMenu.activeSelf)
        {
            if (backwards)
            {
                ShapeMenu.SetActive(false);
                ManualMenu.SetActive(true);
                FormSlider.GetComponent<FormSlider>().SwitchSliderTo(Mode.Manual);
            }
            else
            {
                ShapeMenu.SetActive(false);
                ProfileMenu.SetActive(true);
                FormSlider.GetComponent<FormSlider>().SwitchSliderTo(Mode.Profiles);
            }
        }
        else // ProfileMenu.activeSelf
        {
            if(backwards)
            {
                ProfileMenu.SetActive(false);
                ShapeMenu.SetActive(true);
                FormSlider.GetComponent<FormSlider>().SwitchSliderTo(Mode.Shape);
                }
            else
            {
                ProfileMenu.SetActive(false);
                ManualMenu.SetActive(true);
                FormSlider.GetComponent<FormSlider>().SwitchSliderTo(Mode.Manual);
            }
        }
    }
}
