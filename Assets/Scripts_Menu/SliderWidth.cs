﻿using UnityEngine;
using UnityEngine.UI;
//Slider in main menu -> Create new track
public class SliderWidth : MonoBehaviour
{
    public static Text trackWidth;
    public static int val = 25; //Edytor wykorzystuje
    Color orange = new Color32(238, 170, 37, 255);

    void Start()
    {
        trackWidth = GetComponent<Text>();
    }

    public void updateWidth(float value)
    {
        if (value * SliderHeight.val > Consts.MAX_ELEMENTS)
        {
            SliderHeight.trackHeight.color = Color.red;
            trackWidth.color = Color.red;
            MainMenu.CanCreateTrack = false;
        }
        else
        {
            trackWidth.color = orange;
            SliderHeight.trackHeight.color = orange;
            MainMenu.CanCreateTrack = true;
        }
        val = (int)value;
        trackWidth.text = value.ToString();
    }
}