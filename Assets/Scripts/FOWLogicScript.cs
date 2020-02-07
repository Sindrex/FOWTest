using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FOWLogicScript : MonoBehaviour
{
    public Material green;
    public Material red;
    public Material blue;
    public Material white;
    public Material black;

    public GameObject tilePrefab;
    public int length;
    public int playerViewDist;
    public List<FOWTile> tiles;

    //raycasting
    public int raydivisor = 12;


    // Start is called before the first frame update
    void Start()
    {
        tiles = new List<FOWTile>();
        int index = 0;
        Vector3 vec = new Vector3(0, 0, 0);
        for(int i = 0; i < length; i++)
        {
            vec = new Vector3(0, -i * 1f, 0);
            //print(i + ": " + vec);
            for (int j = 0; j < length; j++)
            {
                GameObject g = Instantiate(tilePrefab, this.gameObject.transform);
                g.transform.position = vec;
                g.GetComponent<FOWTile>().logic = this;
                g.GetComponent<FOWTile>().myIndex = index;
                g.GetComponent<FOWTile>().indexText.text = index + "";
                index++;
                vec += new Vector3(1*1f, 0, 0);
                tiles.Add(g.GetComponent<FOWTile>());
            }
        }
    }

    public void updateLOSMap()
    {
        List<FOWTile> players = new List<FOWTile>();
        foreach (FOWTile tile in tiles)
        {
            if (tile.setting == 0)
            {
                tile.gameObject.GetComponent<MeshRenderer>().material = black;
                tile.setting = 3;
            }
            if (tile.setting == 1)
            {
                players.Add(tile);
            }
        }
        print("Players: " + players.Count);
        foreach (FOWTile tile in players)
        {
            //int index = tiles.IndexOf(tile);
            //int curIndex = index;

            //bresenhamLines(tile);
            
            raycast(tile.gameObject);
            //spiralIterate2(index);
            //lines(index)
        }
    }

    void setWhite(FOWTile tile)
    {
        tile.GetComponent<MeshRenderer>().material = white;
        tile.setting = 0;
    }

    void setWhite(int index)
    {
        if(index < tiles.Count - 1)
        {
            tiles[index].GetComponent<MeshRenderer>().material = white;
            tiles[index].setting = 0;
        }
    }

    void raycast(GameObject startTile){
        int rays = 360/raydivisor; //ex. raydiv 12 => 30 lines in a circle
        float dTheta = raydivisor; //degrees between lines
        float curTheta = 0;
        GameObject player = startTile;
        Vector3 pos = player.transform.position;
        for(int i = 0; i < rays; i++){
            //print("casting rays: " + i);
            Vector3 rotation = Quaternion.AngleAxis(curTheta, transform.forward) * transform.right;
            RaycastHit[] hits = Physics.RaycastAll(pos, rotation, playerViewDist);
            System.Array.Sort(hits, (x,y) => x.distance.CompareTo(y.distance)); //sort by distance
            //sortDist(hits);

            checkHits(hits);

            curTheta += dTheta;
        }
    }

    void sortDist(RaycastHit[] hits)
    {
        //RaycastHit[] sorted = hits;
        int minIndex = 0;
        for(int i = 0; i < hits.Length; i++)
        {
            for (int j = i; j < hits.Length; j++)
            {
                if(hits[j].distance < hits[i].distance)
                {
                    minIndex = j;
                }
            }
            RaycastHit temp = hits[i];
            hits[i] = hits[minIndex];
            hits[minIndex] = temp;
            //print("i: " + i + ", dist: " + hits[i].distance);
        }
    }

    void checkHits(RaycastHit[] hits)
    {
        foreach (RaycastHit hit in hits)
        {
            FOWTile hitTile = hit.collider.gameObject.GetComponent<FOWTile>();
            if (hitTile != null)
            {
                //print("ray: " + i + ", hit a tile: " + hit.transform.GetComponent<FOWTile>().setting);
                //Debug.DrawLine(pos, hit.point, Color.red);

                if (hitTile.setting == 2)
                {
                    break;
                }
                else if (hitTile.setting == 1)
                {
                    //do nothing pls
                }
                else
                {
                    setWhite(hitTile);
                }
            }
        }
    }

    void bresenhamLines(FOWTile startTile)
    {
        //note: square viewarea
        //1. get edge tiles/indexes and start index (gotta know start and stop for bresenham)
        //2. brasenham line between start and edge pieces, stop if wall

        print(">>>Starting Bresenham!");
        int startIndex = tiles.IndexOf(startTile);
        List<int> edgeIndexes = new List<int>();

        #region find edge pieces
        //Find edge pieces
        //top row
        int offset = -playerViewDist;
        for (int i = 0; i < playerViewDist * 2 + 1; i++)
        {
            int index = startIndex - playerViewDist * length + offset;
            edgeIndexes.Add(index);
            offset++;
        }
        //bottom row
        offset = -playerViewDist;
        for (int i = 0; i < playerViewDist * 2 + 1; i++)
        {
            int index = startIndex + playerViewDist * length + offset;
            edgeIndexes.Add(index);
            offset++;
        }
        //left row
        offset = -(playerViewDist - 1) * length;
        for (int i = 0; i < playerViewDist * 2 - 1; i++)
        {
            int index = startIndex - playerViewDist + offset;
            edgeIndexes.Add(index);
            offset += length;
        }
        //right row
        offset = -(playerViewDist - 1) * length;
        for (int i = 0; i < playerViewDist * 2 - 1; i++)
        {
            int index = startIndex + playerViewDist + offset;
            edgeIndexes.Add(index);
            offset += length;
        }
        #endregion

        //make lines!
        //given x = start index column
        int x0 = makeX(startIndex);
        int y0 = makeY(startIndex);
        print("StartIndex: " + startIndex + " -> x/y:" + x0 + "/" + y0);

        foreach (int edgeIndex in edgeIndexes)
        {
            setWhite(tiles[edgeIndex]);
            int x1 = makeX(edgeIndex);
            int y1 = makeY(edgeIndex);

            print(">>Next EdgeIndex: " + edgeIndex + " -> x/y:" + x1 + "/" + y1);

            //Note: Looked up something called DDA, but didnt end up using.

            //pretty much DDA
            float dx = x1 - x0;
            float dy = y1 - y0;

            float x = x0;
            float y = y0;

            float a = 0;
            if(dx != 0)
            {
                a = dy / dx;
            }

            print("a: " + a + ", dx/dy: " + dx + "/" + dy);

            //iterate
            if(dx == 0)
            {
                while(y != y1)
                {
                    if(dy > 0)
                    {
                        y++;
                    }
                    else
                    {
                        y--;
                    }
                    int ix = Mathf.RoundToInt(x);
                    int iy = Mathf.RoundToInt(y);
                    int newIndex = makeIndex(ix, iy);
                    print("Set index: " + newIndex + ", x/y: " + x + "/" + y + ", ix/iy: " + ix + "/" + iy);
                    setWhite(newIndex);
                }
            }
            else
            {
                while(x != x1)
                {
                    if(dx > 0)
                    {
                        x++;
                    }
                    else
                    {
                        x--;
                    }
                    if(dy > 0)
                    {
                        y += a;
                    }
                    else
                    {
                        y -= a;
                    }
                    int ix = Mathf.RoundToInt(x);
                    int iy = Mathf.RoundToInt(y);
                    int newIndex = makeIndex(ix, iy);
                    print("Set index: " + newIndex + ", x/y: " + x + "/" + y + ", ix/iy: " + ix + "/" + iy);
                    setWhite(newIndex);
                }
            }
        }
    }

    int makeY(int index)
    {
        return index / length;
    }

    int makeX(int index)
    {
        return index % length;
    }

    int makeIndex(int x, int y)
    {
        return y * length + x;
    }

    void lines(int index)
    {
        int curIndex = index;
        //left
        for(int i = 0; i < playerViewDist; i++)
        {
            curIndex++;
            if(tiles[curIndex].setting == 2)
            {
                break;
            }
            setWhite(tiles[curIndex]);
        }
        //right
        curIndex = index;
        for (int i = 0; i < playerViewDist; i++)
        {
            curIndex--;
            if (tiles[curIndex].setting == 2)
            {
                break;
            }
            setWhite(tiles[curIndex]);
        }
        //down
        curIndex = index;
        for (int i = 0; i < playerViewDist; i++)
        {
            curIndex += length;
            if (tiles[curIndex].setting == 2)
            {
                break;
            }
            setWhite(tiles[curIndex]);
        }
        //up
        curIndex = index;
        for (int i = 0; i < playerViewDist; i++)
        {
            curIndex -= length;
            if (tiles[curIndex].setting == 2)
            {
                break;
            }
            setWhite(tiles[curIndex]);
        }

        //diagonal down right
        curIndex = index;
        for (int i = 0; i < playerViewDist; i++)
        {
            curIndex += length + 1;

            if (tiles[curIndex].setting == 2)
            {
                break;
            }

            if (tiles[curIndex - 1].setting == 0 && tiles[curIndex - length].setting == 0)
            {
                setWhite(tiles[curIndex]);
            }
        }
        //Diagonal down left
        curIndex = index;
        for (int i = 0; i < playerViewDist; i++)
        {
            curIndex += length - 1;

            if (tiles[curIndex].setting == 2)
            {
                break;
            }

            if (tiles[curIndex + 1].setting == 0 && tiles[curIndex - length].setting == 0)
            {
                setWhite(tiles[curIndex]);
            }
        }
        //Diagonal up left
        curIndex = index;
        for (int i = 0; i < playerViewDist; i++)
        {
            curIndex += -length - 1;

            if (tiles[curIndex].setting == 2)
            {
                break;
            }

            if (tiles[curIndex + 1].setting == 0 && tiles[curIndex + length].setting == 0)
            {
                setWhite(tiles[curIndex]);
            }
        }
        //diagonal up right
        curIndex = index;
        for (int i = 0; i < playerViewDist; i++)
        {
            curIndex += -length + 1;

            if (tiles[curIndex].setting == 2)
            {
                break;
            }

            if (tiles[curIndex - 1].setting == 0 && tiles[curIndex + length].setting == 0)
            {
                setWhite(tiles[curIndex]);
            }
        }
    }

    void spiralIterate2(int playerIndex)
    {
        //M3
        int curIndex = playerIndex;
        int layers = 2;
        for (int i = 0; i < layers; i++) //i = layer, 2 layers: (0, 1)
        {
            for (int j = 0; j < 4; j++) //j = right, down, left, up
            {
                int checkOffset = 0;
                switch (j)
                {
                    case 0:
                        curIndex = playerIndex + (i + 1);
                        checkOffset = -1;
                        break;
                    case 1:
                        curIndex = playerIndex + (i + 1) * length;
                        checkOffset = -length;
                        break;
                    case 2:
                        curIndex = playerIndex - (i + 1);
                        checkOffset = 1;
                        break;
                    case 3:
                        curIndex = playerIndex - (i + 1) * length;
                        checkOffset = length;
                        break;
                }
                if (tiles[curIndex].setting != 2 && tiles[curIndex + checkOffset].setting != 2)
                {
                    setWhite(tiles[curIndex]);
                }

            }
            for (int j = 0; j < 4; j++) //j = down-right, down-left, up-left, up-right
            {
                int checkOffset1 = 0;
                int checkOffset2 = 0;
                int curIndex1 = 1;
                int curIndex2 = length;
                if (i == 0)
                {
                    if(j < 2) //0 & 1
                    {
                        curIndex2 = length;
                        checkOffset2 = -length;
                    }
                    else
                    {
                        curIndex2 = -length;
                        checkOffset2 = length;
                    }
                    if(j == 0 || j == 3)
                    {
                        curIndex1 = 1;
                        checkOffset1 = -1;
                    }
                    else
                    {
                        curIndex1 = -1;
                        checkOffset1 = 1;
                    }
                    curIndex = playerIndex + curIndex1 + curIndex2;

                    if (tiles[curIndex + checkOffset1].setting == 0 && tiles[curIndex + checkOffset2].setting == 0 && tiles[curIndex].setting != 2)
                    {
                        setWhite(tiles[curIndex]);
                    }
                }
                if(i == 1)
                {
                    //for(int k = 0; k < (1 + layers * 2); k++)

                    if (j < 2) //0 & 1
                    {
                        curIndex2 = length;
                        checkOffset2 = -length;
                    }
                    else
                    {
                        curIndex2 = -length;
                        checkOffset2 = length;
                    }
                    if (j == 0 || j == 3)
                    {
                        curIndex1 = 1;
                        checkOffset1 = -1;
                    }
                    else
                    {
                        curIndex1 = -1;
                        checkOffset1 = 1;
                    }
                    curIndex = playerIndex + curIndex1 + curIndex2;

                    if (tiles[curIndex + checkOffset1].setting == 0 && tiles[curIndex + checkOffset2].setting == 0 && tiles[curIndex].setting != 2)
                    {
                        setWhite(tiles[curIndex]);
                    }

                    //print("yo: " + i + "/" + j);
                }
            }
        }
    }

    void spiralIterate(int playerIndex) //manual
    {
        //M3
        #region DONE
        int curIndex = playerIndex;

        //Layer 1
        //tile 1 (right)
        curIndex = playerIndex + 1;
        if (tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 2 (down)
        curIndex = playerIndex + length;
        if (tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 3
        curIndex = playerIndex - 1;
        if (tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 4
        curIndex = playerIndex - length;
        if (tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 5
        curIndex = playerIndex + length + 1;
        if (tiles[curIndex - 1].setting == 0 && tiles[curIndex - length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 6
        curIndex = playerIndex + length - 1;
        if (tiles[curIndex + 1].setting == 0 && tiles[curIndex - length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 7
        curIndex = playerIndex - length - 1;
        if (tiles[curIndex + 1].setting == 0 && tiles[curIndex + length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 8
        curIndex = playerIndex - length + 1;
        if (tiles[curIndex - 1].setting == 0 && tiles[curIndex + length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //Layer 2
        //tile 1 (right)
        curIndex = playerIndex + 2;
        if (tiles[curIndex].setting != 2 && tiles[curIndex - 1].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 2 (down)
        curIndex = playerIndex + 2*length;
        if (tiles[curIndex].setting != 2 && tiles[curIndex - length].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 3
        curIndex = playerIndex - 2;
        if (tiles[curIndex].setting != 2 && tiles[curIndex + 1].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 4
        curIndex = playerIndex - 2*length;
        if (tiles[curIndex].setting != 2 && tiles[curIndex + length].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }
        #endregion

        //down right
        //tile 5
        curIndex = playerIndex + length + 2;
        if (tiles[curIndex - 1].setting == 0 && tiles[curIndex - length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 6
        curIndex = playerIndex + 2*length + 1;
        if (tiles[curIndex - 1].setting == 0 && tiles[curIndex - length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 7
        curIndex = playerIndex + 2 * length + 2;
        if (tiles[curIndex - 1].setting == 0 && tiles[curIndex - length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //down left
        //tile 9
        curIndex = playerIndex + length - 2;
        if (tiles[curIndex + 1].setting == 0 && tiles[curIndex - length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 8
        curIndex = playerIndex + 2 * length - 1;
        if (tiles[curIndex + 1].setting == 0 && tiles[curIndex - length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 10
        curIndex = playerIndex + 2 * length - 2;
        if (tiles[curIndex + 1].setting == 0 && tiles[curIndex - length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //up left
        //tile 11
        curIndex = playerIndex - length - 2;
        if (tiles[curIndex + 1].setting == 0 && tiles[curIndex + length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 12
        curIndex = playerIndex - 2 * length - 1;
        if (tiles[curIndex + 1].setting == 0 && tiles[curIndex + length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 13
        curIndex = playerIndex - 2 * length - 2;
        if (tiles[curIndex + 1].setting == 0 && tiles[curIndex + length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //up right
        //tile 15
        curIndex = playerIndex - length + 2;
        if (tiles[curIndex - 1].setting == 0 && tiles[curIndex + length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 14
        curIndex = playerIndex - 2 * length + 1;
        if (tiles[curIndex - 1].setting == 0 && tiles[curIndex + length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }

        //tile 16
        curIndex = playerIndex - 2 * length + 2;
        if (tiles[curIndex - 1].setting == 0 && tiles[curIndex + length].setting == 0 && tiles[curIndex].setting != 2)
        {
            setWhite(tiles[curIndex]);
        }
    }
}
