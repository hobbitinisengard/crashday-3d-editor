using UnityEngine;
using UnityEngine.UI;
// Give every button in Build menu listener setting current tile name to name of image file (how convenient)
public class ShowTileName : MonoBehaviour
{
    Button buton;
    Image image;
    void Start()
    {
        buton = GetComponent<Button>();
        buton.onClick.AddListener(Poka_nazwe);
    }
    void Poka_nazwe()
    {
        if (Build.tile_name == "NULL" && !(Build.previous_tile_name == "NULL"))
            Build.ChangeCurrentTile(this.name, false);
        else
            Build.ChangeCurrentTile(this.name, true);
    }
}
