using UnityEngine;
using UnityEngine.UI;
public class SliderHeight : MonoBehaviour {
	
	public static Text trackHeight;
	static public int val = 27; //Edytor wykorzystuje
    Color orange = new Color32(238, 170, 37, 255);

    void Start () {
		trackHeight = GetComponent<Text> ();
	}

	public void updateHeight(float value)
	{
		if (value * SliderWidth.val > 2000) {
			SliderWidth.trackWidth.color = Color.red;
			trackHeight.color = Color.red;
			STATIC.playgamePass = false;
		} else {
			SliderWidth.trackWidth.color = orange;
			trackHeight.color = orange;
			STATIC.playgamePass = true;
		}
		//Debug.Log ("th" + value + "tw" + val);
		val = (int)value;
		trackHeight.text = value.ToString ();
	}
}
