using UnityEngine;
using System.Collections;
public class ChangeTrackDimensions : MonoBehaviour {
    public GameObject editorPANEL;
    bool selectingBL = false;
    bool selectingTR = false;
    public Vector2Int BottomLeftPos = new Vector2Int(0, 0);
    public Vector2Int TopRightPos = new Vector2Int(SliderWidth.val, SliderHeight.val);

     public void ChangeStateToBL()
    {
        if (!selectingTR)
            selectingBL = true;
        
    }
    public void ChangeStateToTR()
    {
        if (!selectingBL)
            selectingTR = true;
    }
    public void ApplyDimensionsChange()
    {
        //1. Delete everything redundant
        //Left sector
        for (int z = 0; z < SliderHeight.val; z++)
            for (int x = 0; x < BottomLeftPos.x; x++)
                DeleteHits(Physics.RaycastAll(new Vector3(4 * x + 2, Terenowanie.maxHeight+1, 4 * z + 2), Vector3.down, Terenowanie.rayHeight)); // get grass and overlaying tile (if it exists)
        //right sector
        for (int z = 0; z < SliderHeight.val; z++)
            for (int x = TopRightPos.x; x < SliderWidth.val; x++)
                DeleteHits(Physics.RaycastAll(new Vector3(4 * x + 2, Terenowanie.maxHeight+1, 4 * z + 2), Vector3.down, Terenowanie.rayHeight));
        //bottom sector
        for (int z = 0; z < BottomLeftPos.y; z++)
            for (int x = BottomLeftPos.x; x < TopRightPos.x; x++)
                DeleteHits(Physics.RaycastAll(new Vector3(4 * x + 2, Terenowanie.maxHeight+1, 4 * z + 2), Vector3.down, Terenowanie.rayHeight));
        //top sector
        for (int z = TopRightPos.y; z < SliderHeight.val; z++)
            for (int x = BottomLeftPos.x; x < TopRightPos.x; x++)
                DeleteHits(Physics.RaycastAll(new Vector3(4 * x + 2, Terenowanie.maxHeight+1, 4 * z + 2), Vector3.down, Terenowanie.rayHeight));


        // 2. move all elements (tiles+terrain) so that first grass tile has its bottom-left vertex in (0,0,0) position
        RaycastHit[] allelements = Physics.BoxCastAll(new Vector3(SliderWidth.val / 2f, Terenowanie.maxHeight+1, SliderHeight.val / 2f), new Vector3(SliderWidth.val / 2f, 1, SliderHeight.val / 2f), Vector3.down);
        foreach(RaycastHit el in allelements)
            el.transform.position.Set(el.transform.position.x - BottomLeftPos.x, el.transform.position.y, el.transform.position.z - BottomLeftPos.y);
        

        //3. Update certain variables
        
        this.gameObject.SetActive(false);
    }
    void DeleteHits(RaycastHit[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.gameObject.layer == 8) // delete grass
                DestroyImmediate(hits[i].transform.gameObject);

            else if (hits[i].transform.gameObject.layer == 9) // delete tile
            {
                Vector3Int pos = Budowanie.vpos2epos(hits[i].transform.gameObject);
                DestroyImmediate(hits[i].transform.gameObject);
                STATIC.tiles[pos.x, pos.z]._nazwa = null;
            }
        }
        
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            this.gameObject.SetActive(false);
            editorPANEL.gameObject.SetActive(true);
        }
            
        if(selectingBL || selectingTR)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (selectingBL)
                {
                    BottomLeftPos.Set(Highlight.t.x / 4, Highlight.t.z / 4);
                    selectingBL = false;
                }
                   
                if (selectingTR)
                {
                    TopRightPos.Set(Highlight.t.x / 4 + 4, Highlight.t.z / 4 + 4);
                    selectingTR = false;
                }
                   
            }
        }   
    }
}
