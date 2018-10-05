using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
//1. Skrypt. Menu
public class MainMenu : MonoBehaviour {
    public GameObject loadScreen;
    public TextAsset flatters;
    public TextAsset pzeros;
    public TextAsset rmcs;
    public TextAsset kategorie;
    public TextAsset dims;
    void Awake(){
        STATIC.playgamePass = true;
        STATIC.isloading = false;
        STATIC.isEditing = false;
        STATIC.flatters = flatters;
        STATIC.pzeros = pzeros;
        STATIC.rmcs = rmcs;
        STATIC.kategorie = kategorie;
        {//Ładowanie informacji o kategoriach
            string[] lines = Regex.Split(STATIC.kategorie.text, "\r\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] tab = lines[i].Split(' ');
                EditorMenu.kategorie.Add(new Kategoria(tab[0], byte.Parse(tab[1])));
            }
        }
        {//Ładowanie informacji o wymiarach
            string[] lines = Regex.Split(dims.text, "\r\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] tab = lines[i].Split(' ');
                Loader.dims.Add(new Dimension(tab[0], new Vector2Int(int.Parse(tab[1].Substring(0, 1)), int.Parse(tab[1].Substring(2, 1)))));
            }
        }
    }
	public void PlayGame ()
	{
        if (STATIC.playgamePass)
        {
            STATIC.isEditing = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }    
	}
    //Przycisk Load scheme.. użyje funkcji Loader.OpenExplorer();
	public void QuitGame()
	{
		Application.Quit();
	}
	
	void Continue()
	{
		if (STATIC.isEditing == true) {
			SceneManager.LoadScene (SceneManager.GetActiveScene ().buildIndex + 1);
		}
	}
    public void CheckValidity(string val)
    {
        float multiplier = int.Parse(val);
        if (multiplier <= 20)
            Helper.multiplier = multiplier;
    }
}
