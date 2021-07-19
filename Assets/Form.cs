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

public enum ManualMode { single, areal }
/// <summary>
/// hooked to e_formPANEL. Handles Form workflow
/// </summary>
public class Form : MonoBehaviour
{
  // Manual mode menus
  public GameObject ManualMenu;
    public GameObject ArealMenu;
    public GameObject SingleMenu;

  // Shape mode menu
  public GameObject ShapeMenu;
  public GameObject FormMenu;
  
  // Profile mode menu
  public GameObject ProfileMenu;

  public Slider HeightSlider;
  public Slider FormSlider;
  public static ManualMode mode;

  private void Start()
  {
    mode = ManualMode.single;
    Physics.queriesHitBackfaces = true;
  }

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.F))
      ToggleFormingMode(Input.GetKey(KeyCode.LeftShift));
    if (Input.GetKeyDown(KeyCode.Tab))
      ToggleManipMode();
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
      UndoBuffer.Paste();
  }
  
  /// <summary>
  /// Toggles between 'single mode' and 'areal mode' in Manual forming mode
  /// </summary>
  void ToggleManipMode()
  {
    ArealMenu.SetActive(!ArealMenu.activeSelf);
    SingleMenu.SetActive(!SingleMenu.activeSelf);
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