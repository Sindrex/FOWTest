using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public enum TileSetting
{
    white = 0,
    green = 1,
    blue = 2,
    black = 3,
    red = 4
}

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

    public bool raycastMe;
    public int raydivisor = 12;

    public bool DDA;
    public bool DDAMine;
    public int maxSeeThrough;


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

    private void FixedUpdate()
    {
        updateLOSMap();
    }

    public void updateLOSMap()
    {
        List<FOWTile> players = new List<FOWTile>();
        foreach (FOWTile tile in tiles)
        {
            if (tile.setting == (int)TileSetting.white)
            {
                tile.gameObject.GetComponent<MeshRenderer>().material = black;
                tile.setting = (int)TileSetting.black;
            }
            if (tile.setting == (int)TileSetting.green)
            {
                players.Add(tile);
            }
            if(tile.setting == (int)TileSetting.blue)
            {
                tile.gameObject.GetComponent<MeshRenderer>().material = blue;
            }
        }
        //print("Players: " + players.Count);
        foreach (FOWTile tile in players)
        {
            if (raycastMe)
            {
                raycast(tile.gameObject);
            }
            else if (DDA)
            {
                DDAAlgorithm(tile);
            }
            else if (DDAMine)
            {
                DDAAlgorithmMine(tile);
            }

            getEdgeIndexesCircle(tile.myIndex);

            //int index = tile.myIndex;
            //int curIndex = index;
            //spiralIterate2(index);
            //lines(index)
        }
    }

    //help methods that set a tile white
    void setWhite(FOWTile tile)
    {
        if (tile.setting == 2)
        {
            tile.GetComponent<MeshRenderer>().material = red;
        }
        else
        {
            tile.GetComponent<MeshRenderer>().material = white;
            tile.setting = 0;
        }
    }

    void setWhite(int index)
    {
        if(index >= 0 && index < tiles.Count - 1)
        {
            FOWTile myTile = tiles[index];
            if(myTile.setting == 2)
            {
                myTile.GetComponent<MeshRenderer>().material = red;
            }
            else
            {
                myTile.GetComponent<MeshRenderer>().material = white;
                myTile.setting = 0;
            }
        }
    }

    #region Raycasting
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
    #endregion

    void DDAAlgorithm(FOWTile startTile)
    {
        //note: square viewarea
        //1. get edge tiles/indexes and start index (gotta know start and stop)
        //2. Find line between start and edge pieces with DDA, stop if wall

        //print(">>>Starting Line Algorithm!");
        int startIndex = startTile.myIndex;
        List<int> edgeIndexes = getEdgeIndexesCircle(startIndex);

        //make lines!
        //given x = start index column
        int x0 = makeX(startIndex);
        int y0 = makeY(startIndex);

        //for all the edge tiles, threaded/multicore
        Parallel.ForEach(edgeIndexes, edgeIndex =>
        {
           int x1 = makeX(edgeIndex); //convert index to x-y coords
            int y1 = makeY(edgeIndex);

           float dx = x1 - x0;
           float dy = y1 - y0;

           float x = x0;
           float y = y0;

           float a = 0; //slope
            if (dx != 0)
           {
               a = dy / dx;
           }
           a = Mathf.Abs(a);

            //iterate
            if (dx == 0) //straight up or down
            {
               while (y != y1)
               {
                   if (dy > 0)
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
                   if (!coordCheckTile(newIndex))
                   {
                       setWhite(newIndex);
                       break;
                   }
                   setWhite(newIndex);
               }
           }
           else
           {
               if (Mathf.Abs(a) > 1) //steep slope
                {
                   float rev = 1 / a;
                   while (y != y1)
                   {
                       if (dy > 0)
                       {
                           y++;
                       }
                       else
                       {
                           y--;
                       }
                       if (dx > 0)
                       {
                           x += rev;
                       }
                       else
                       {
                           x -= rev;
                       }
                       int ix = Mathf.RoundToInt(x);
                       int iy = Mathf.RoundToInt(y);
                       int newIndex = makeIndex(ix, iy);
                       if (!coordCheckTile(newIndex))
                       {
                           setWhite(newIndex);
                           break;
                       }
                       setWhite(newIndex);
                   }
               }
               else //a<1, slow slope
                {
                   while (x != x1)
                   {
                       if (dx > 0)
                       {
                           x++;
                       }
                       else
                       {
                           x--;
                       }
                       if (dy > 0)
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
                       if (!coordCheckTile(newIndex))
                       {
                           setWhite(newIndex);
                           break;
                       }
                       setWhite(newIndex);
                   }
               }
           }
        });
    }
    
    //Help method for DDAAlgorithm
    bool coordCheckTile(int index)
    {
        if (tiles[index].setting == 2) //its wall
        {
            return false;
        }
        return true;
    }

    //Mine version, customizable length on see into walls
    void DDAAlgorithmMine(FOWTile startTile)
    {
        //note: square viewarea
        //1. get edge tiles/indexes and start index (gotta know start and stop)
        //2. Find line between start and edge pieces, stop if wall with DDA

        int startIndex = startTile.myIndex;
        List<int> edgeIndexes = getEdgeIndexesSquare(startIndex);

        //make lines!
        //given x = start index column
        int x0 = makeX(startIndex);
        int y0 = makeY(startIndex);

        //for all the edge tiles
        Parallel.ForEach(edgeIndexes, edgeIndex =>
        {
            int x1 = makeX(edgeIndex); //convert index to x-y coords
            int y1 = makeY(edgeIndex);

            float dx = x1 - x0;
            float dy = y1 - y0;

            float x = x0;
            float y = y0;

            float a = 0; //slope
            if (dx != 0)
            {
                a = dy / dx;
            }
            a = Mathf.Abs(a);

            //mine
            int seenThroughCount = 0;

            //iterate
            if (dx == 0) //straight up or down
            {
                while (y != y1)
                {
                    if (dy > 0)
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

                    int check = coordCheckTileMine(newIndex, seenThroughCount);
                    if (check < 0)
                    {
                        //setWhite(newIndex); //Practically you'd want this for bitmap?
                        break;
                    }
                    else if (check == 0)
                    {
                        seenThroughCount++;
                    }
                    setWhite(newIndex);
                }
            }
            else
            {
                if (Mathf.Abs(a) > 1) //steep slope
                {
                    float rev = 1 / a;
                    while (y != y1)
                    {
                        if (dy > 0)
                        {
                            y++;
                        }
                        else
                        {
                            y--;
                        }
                        if (dx > 0)
                        {
                            x += rev;
                        }
                        else
                        {
                            x -= rev;
                        }
                        int ix = Mathf.RoundToInt(x);
                        int iy = Mathf.RoundToInt(y);
                        int newIndex = makeIndex(ix, iy);

                        int check = coordCheckTileMine(newIndex, seenThroughCount);
                        if (check < 0)
                        {
                            //setWhite(newIndex); //Practically you'd want this for bitmap?
                            break;
                        }
                        else if (check == 0)
                        {
                            seenThroughCount++;
                        }
                        setWhite(newIndex);
                    }
                }
                else //a<1, slow slope
                {
                    while (x != x1)
                    {
                        if (dx > 0)
                        {
                            x++;
                        }
                        else
                        {
                            x--;
                        }
                        if (dy > 0)
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

                        int check = coordCheckTileMine(newIndex, seenThroughCount);
                        if (check < 0)
                        {
                            //setWhite(newIndex); //Practically you'd want this for bitmap?
                            break;
                        }
                        else if (check == 0)
                        {
                            seenThroughCount++;
                        }
                        setWhite(newIndex);
                    }
                }
            }
        });
    }

    //Help method for DDAAlgorithmMine
    int coordCheckTileMine(int index, int seenThrough)
    {
        if (tiles[index].setting == (int)TileSetting.blue && seenThrough < maxSeeThrough) //its wall
        {
            return 0;
        }
        else if(tiles[index].setting == (int)TileSetting.blue)
        {
            return -1;
        }
        else if(tiles[index].setting == (int)TileSetting.black && seenThrough > 0)
        {
            return -1;
        }

        return 1;
    }

    List<int> getEdgeIndexesSquare(int startIndex)
    {
        List<int> edgeIndexes = new List<int>();

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

        return edgeIndexes;
    }

    List<int> getEdgeIndexesCircle(int startIndex)
    {
        //get the edge indexes of square
        List<int> edgeIndexes = getEdgeIndexesSquare(startIndex);

        int x0 = makeX(startIndex);
        int y0 = makeY(startIndex);

        //check if inside our view radius
        foreach (int edgeIndex in edgeIndexes)
        {
            int x1 = makeX(edgeIndex);
            int y1 = makeY(edgeIndex);

            int dx = x1 - x0;
            int dy = y1 - y0;

            int nx = x1;
            int ny = y1;
            while (!checkInsideCircleView(dx, dy))
            {
                if(Mathf.Abs(dx) >= Mathf.Abs(dy)) //increment the one that's largest
                {
                    if(dx > 0)
                    {
                        nx--;
                    }
                    else
                    {
                        nx++;
                    }
                }
                else
                {
                    if(dy > 0)
                    {
                        ny--;
                    }
                    else
                    {
                        ny++;
                    }
                }
                dx = nx - x0;
                dy = ny - y0;
            }
            int index = makeIndex(nx, ny);
            setWhite(index);
        }

        return edgeIndexes;
    }

    bool checkInsideCircleView(int dx, int dy)
    {
        float sqdist = Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2);
        //print("Checking " + edgeIndex + ": " + sqdist);
        if (sqdist <= Mathf.Pow(playerViewDist, 2))
        {
            //print("Edge: " + edgeIndex + " is inside circle!");
            return true;
        }
        return false;
    }

    //covert to x-y coordinates
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
