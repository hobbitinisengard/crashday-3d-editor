using UnityEngine;
using UnityEngine.UI;
public class SliderCase : MonoBehaviour {
	public Text title;
	public Slider slider;
	public GameObject roadTab;
	public GameObject stradaTab;
	public GameObject dirtTab;
	public GameObject tunnelTab;
	public GameObject bridgeTab;
	public GameObject rampTab;
	public GameObject buildTab;
	public GameObject plantTab;
	public GameObject worksTab;
	public GameObject raceTab;
    public static byte last_value = 1;
	void Update () {
		if (Input.GetAxis ("Mouse ScrollWheel") > 0) {
			hideCase (slider.value);
			if (slider.value == 10)
				slider.value = 1;
            else
                slider.value++;
            showCase (slider.value);
			changeText (slider.value);

		} else if (Input.GetAxis ("Mouse ScrollWheel") < 0) {
			hideCase (slider.value);
			if (slider.value == 1)
				slider.value = 10;
            else
                slider.value--;
            showCase (slider.value);
			changeText (slider.value);
		}
        if (Input.GetMouseButtonDown(0))
            last_value = (byte)slider.value;
	}

	public void changeText(float v)
	{
		if (v == 1)
			title.text = "Town roads";
		else if (v == 2)
			title.text = "Rural roads";
		else if (v == 3)
			title.text = "Dirt";
		else if (v == 4)
			title.text = "Race";
		else if (v == 5)
			title.text = "Bridge";
		else if (v == 6)
			title.text = "Tunnels";
		else if (v == 7)
			title.text = "Works";
		else if (v == 8)
			title.text = "Ramp";
		else if (v == 9)
			title.text = "Buildings & Checkpoints";
		else if (v == 10)
			title.text = "Plants";
	}

	public void showCase(float v)
	{
		if(v==1)
			roadTab.SetActive (true);
		else if(v==2)
			stradaTab.SetActive (true);
		else if(v==3)
			dirtTab.SetActive (true);
		else if(v==4)
			raceTab.SetActive (true);
		else if(v==5)
			bridgeTab.SetActive (true);
		else if(v==6)
			tunnelTab.SetActive (true);
		else if(v==7)
			worksTab.SetActive (true);
		else if(v==8)
			rampTab.SetActive (true);
		else if(v==9)
			buildTab.SetActive (true);
		else if(v==10)
			plantTab.SetActive (true);
	}
	public void hideCase(float v)
	{
		if(v==1)
			roadTab.SetActive (false);
		else if(v==2)
			stradaTab.SetActive (false);
		else if(v==3)
			dirtTab.SetActive (false);
		else if(v==4)
			raceTab.SetActive (false);
		else if(v==5)
			bridgeTab.SetActive (false);
		else if(v==6)
			tunnelTab.SetActive (false);
		else if(v==7)
			worksTab.SetActive (false);
		else if(v==8)
			rampTab.SetActive (false);
		else if(v==9)
			buildTab.SetActive (false);
		else if(v==10)
			plantTab.SetActive (false);
	}
}
