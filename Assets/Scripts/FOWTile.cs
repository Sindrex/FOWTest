using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOWTile : MonoBehaviour
{
    public FOWLogicScript logic;
    public int myIndex;
    public int setting;
    public bool recentChange = false;

    private void OnMouseOver()
    {
        if(Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1) || Input.GetKey(KeyCode.R))
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                setting = 1;
            }
            else if (Input.GetKey(KeyCode.Mouse1))
            {
                setting = 2;
            }
            else if (Input.GetKey(KeyCode.R))
            {
                setting = 3;
            }

            switch (setting)
            {
                case 0:
                    GetComponent<MeshRenderer>().material = logic.white;
                    break;
                case 1:
                    GetComponent<MeshRenderer>().material = logic.green;
                    break;
                case 2:
                    GetComponent<MeshRenderer>().material = logic.blue;
                    break;
                case 3:
                    GetComponent<MeshRenderer>().material = logic.black;
                    break;
            }

            logic.updateLOSMap();
        }
    }
}
