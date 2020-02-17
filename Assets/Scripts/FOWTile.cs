using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOWTile : MonoBehaviour
{
    public FOWLogicScript logic;
    public int myIndex;
    public int setting;
    public bool recentChange = false;
    public TextMesh indexText;

    private void OnMouseOver()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1) || Input.GetKey(KeyCode.R))
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                logic.setResource(myIndex, (int)ResourceSetting.player);
                logic.mapView[myIndex] = logic.view;
            }
            else if (Input.GetKey(KeyCode.Mouse1))
            {
                logic.setResource(myIndex, (int)ResourceSetting.wall);
            }
            else if (Input.GetKey(KeyCode.R))
            {
                logic.setResource(myIndex, (int)ResourceSetting.nothing);
            }

            logic.updateLOSMap();
        }
    }
}
