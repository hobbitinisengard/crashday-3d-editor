using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
// Give every button in Build menu listener setting current tile name to name of image file (how convenient)
public class ShowTileName : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    SliderCase slider_case;
    Button buton;
    Image image;

    void Start()
    {
        slider_case = GameObject.Find("e_editorPANEL").GetComponent<SliderCase>();
        buton = GetComponent<Button>();
        buton.onClick.AddListener(Poka_nazwe);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        slider_case.ShowTileDescription(this.name);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        slider_case.HideTileDescription();
    }

    void Poka_nazwe()
    {
        if (Build.tile_name == "NULL" && !(Build.previous_tile_name == "NULL"))
            Build.ChangeCurrentTile(this.name, false);
        else
            Build.ChangeCurrentTile(this.name, true);
    }
}
