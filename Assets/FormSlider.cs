using UnityEngine;
using UnityEngine.UI;

public class FormSlider : MonoBehaviour
{
    public Slider slider;
    public Text text;

    void Update()
    {
        // When somebody pressed F
        if (text.text == "Manual forming..")
            slider.value = 1;
        if (text.text == "Shape forming..")
            slider.value = 2;
    }
    void OnValueChanged()
    {
        Terraining.firstFormingMode = !Terraining.firstFormingMode;
    }
}
