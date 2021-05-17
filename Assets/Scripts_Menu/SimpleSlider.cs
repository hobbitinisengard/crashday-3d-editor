﻿using UnityEngine;
using UnityEngine.UI;

public class SimpleSlider : MonoBehaviour
{
  public Text text;
  public void JustValue(float val)
  {
    text.text = val.ToString();
  }
  public void PercentValue(float val)
  {
    text.text = val.ToString() + "%";
  }
}
