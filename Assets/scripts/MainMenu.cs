using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
//First script. Main menu
public class MainMenu : MonoBehaviour {
    public GameObject loadScreen;
    public TextAsset flatters;
    public TextAsset pzeros;
    public TextAsset rmcs;
    public TextAsset kategorie;
    public TextAsset dims;
    public static int tile_limit = 4000;
    void Awake(){
        STATIC.PlaygamePass = true;
        STATIC.Isloading = false;
        STATIC.IsEditing = false;
        STATIC.Flatters = flatters;
        STATIC.Pzeros = pzeros;
        STATIC.Rmcs = rmcs;
        STATIC.Kategorie = kategorie;
        {//Ładowanie informacji o kategoriach
            string[] lines = Regex.Split(STATIC.Kategorie.text, "\r\n");
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
    private void Start()
    {
        SliderWidth.val = 26;
        SliderHeight.val = 25;
    }
    public void PlayGame ()
	{
        if (STATIC.PlaygamePass)
        {
            STATIC.IsEditing = true;
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
		if (STATIC.IsEditing == true) {
			SceneManager.LoadScene (SceneManager.GetActiveScene ().buildIndex + 1);
		}
	}
    public void Toggle_tileLimit()
    {
        tile_limit = (tile_limit == 4000) ? 8000 : 4000;
    }
    public void CheckValidity(string val)
    {
        float multiplier = int.Parse(val);
        if (multiplier <= 20)
            Helper.multiplier = multiplier;
    }
}
