using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//Pokazuje nazwe obrazka będącego dzieckiem buttona - źródła
//Wywoływany przez onclick, ale też można wywołać funkcję poka_nazwe zdalnie
public class Poka_nazwe_tilesa : MonoBehaviour {
	Button buton;
	Image image;
	void Start () {
		buton = GetComponent<Button>();
		buton.onClick.AddListener (poka_nazwe);
	}
	void poka_nazwe(){
		image = buton.transform.GetChild (0).GetComponent<Image>();
		EditorMenu.nazwa_tilesa = image.sprite.name;
	}
}
