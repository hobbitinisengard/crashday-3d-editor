using UnityEngine;
using UnityEngine.UI;

public class FormSlider : MonoBehaviour {
    public Slider slider;
    public Text text;

	void Update () {
        // Ktoś wcisnął F
        if (text.text == "Manual forming..")
            slider.value = 1;
        if (text.text == "Shape forming..")
            slider.value = 2;
	}
    void OnValueChanged()
    {
        Terenowanie.firstFormingMode = !Terenowanie.firstFormingMode;
    }
}
