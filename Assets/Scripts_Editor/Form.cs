using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// hooked to e_formPANEL. Handles switching between forming modes and UndoBuffer calls
/// </summary>
public class Form : MonoBehaviour
{
    public GameObject ManualMenu;
    public GameObject ShapeMenu;
    public GameObject ProfileMenu;
    public Slider FormSlider;

    private void Start()
    {
        Physics.queriesHitBackfaces = true;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            ToggleFormingMode(Input.GetKey(KeyCode.LeftShift));
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Z))
            UndoBuffer.MoveThroughTerrainLayers(BufferDirection.BACKWARD);
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Y))
            UndoBuffer.MoveThroughTerrainLayers(BufferDirection.FORWARD);
    }
    /// <summary>
    /// Toggles between manual, shape forming, and profiles.
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
