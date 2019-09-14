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
        buton.onClick.AddListener(poka_nazwe);
    }
    void poka_nazwe()
    {
        image = buton.transform.GetChild(0).GetComponent<Image>();
        EditorMenu.tile_name = image.sprite.name;
    }
}
