using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public enum ResourceSetting
{
    nothing = 0,
    green = 1,
    player = 1,
    blue = 2,
    wall = 2,
    red = 3,
    seenWall = 3
}

public enum ViewSetting
{
    noView = 0,
    view = 1
}

/* Note about Dwarfheim:
 * 3x 1D arrays for the Grid:
 * * Terrain
 * * Resources (stone/ore in underworld, trees in overworld)
 * * WorldObject (buildings)
 */

public class FOWLogicScript : MonoBehaviour
{
    public Material[] colorsResource;
    public Material[] colorsView;

    public InputField viewDistInput;

    public GameObject tilePrefab;
    public int length;
    private int lengthSq;
    public int playerViewDist;
    public List<FOWTile> tiles;

    public int raydivisor = 12; //Deprecated

    public bool DDA;
    public int maxSeeThrough;

    public BitArray mapView;
    public readonly bool noView = false;
    public readonly bool view = true;
    public int[] resourceMap;

    // Start is called before the first frame update
    void Start()
    {
        lengthSq = length * length;
        resourceMap = new int[lengthSq];
        mapView = new BitArray(lengthSq);

        viewDistInput.text = playerViewDist + "";

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

    private void Update()
    {
        //updateLOSMap();
    }

    public void updateLOSMap()
    {
        int.TryParse(viewDistInput.text, out playerViewDist);

        List<int> players = new List<int>();
        for (int index = 0; index < lengthSq; index++)
        {
            if (resourceMap[index] == (int)ResourceSetting.player)
            {
                players.Add(index);
            }
            else if(resourceMap[index] == (int)ResourceSetting.wall)
            {
                setResource(index, (int)ResourceSetting.blue);
            }
            else
            {
                setResource(index, (int)ResourceSetting.nothing);
                setSeen(index, false);
            }
        }
        //print("Players: " + players.Count);
        foreach (int index in players)
        {
            if (DDA)
            {
                DDAAlgorithm(index);
            }
            else
            {
                DDAAlgorithmMine(index);
            }
        }
    }

    //help methods that set a tile white
    public void setSeen(int index, bool seen)
    {
        if(index >= 0 && index < lengthSq)
        {
            mapView[index] = seen;

            if (seen)
            {
                if (resourceMap[index] == (int)ResourceSetting.wall)
                {
                    tiles[index].GetComponent<MeshRenderer>().material = colorsResource[(int)ResourceSetting.seenWall];
                }
                else if (resourceMap[index] != (int)ResourceSetting.green) //dont take away greenies
                {
                    tiles[index].GetComponent<MeshRenderer>().material = colorsView[(int)ViewSetting.view];
                }
            }
            else
            {
                if (resourceMap[index] == (int)ResourceSetting.wall)
                {
                    tiles[index].GetComponent<MeshRenderer>().material = colorsResource[(int)ResourceSetting.wall];
                }
            }
        }
    }

    public void setResource(int index, int setting)
    {
        if (index >= 0 && index < lengthSq)
        {
            resourceMap[index] = setting;
            tiles[index].GetComponent<MeshRenderer>().material = colorsResource[setting];
            tiles[index].setting = setting;
        }
    }

    #region Raycasting
    /*
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
    */
    #endregion

    #region DDA
    void DDAAlgorithm(int startIndex)
    {
        //note: square viewarea
        //1. get edge tiles/indexes and start index (gotta know start and stop)
        //2. Find line between start and edge pieces with DDA, stop if wall

        //print(">>>Starting Line Algorithm!");
        List<int> edgeIndexes = getEdgeIndexesCircle(startIndex, playerViewDist);

        //make lines!
        //given x = start index column
        int x0 = makeX(startIndex);
        int y0 = makeY(startIndex);

        //for all the edge tiles, threaded/multicore
        foreach(int edgeIndex in edgeIndexes)
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
                   if (!checkWall(newIndex))
                   {
                       setSeen(newIndex, true);
                       break;
                   }
                   setSeen(newIndex, true);
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
                       if (!checkWall(newIndex))
                       {
                           setSeen(newIndex, true);
                           break;
                       }
                       setSeen(newIndex, true);
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
                       if (!checkWall(newIndex))
                       {
                           setSeen(newIndex, true);
                           break;
                       }
                       setSeen(newIndex, true);
                   }
               }
           }
        }
    }
    
    //Help method for DDAAlgorithm
    bool checkWall(int index)
    {
        if(index >= 0 && index < tiles.Count)
        {
            if (resourceMap[index] == (int)ResourceSetting.wall) //its wall
            {
                return false;
            }
            return true;
        }
        return false;
    }

    //Mine version, customizable length on see into walls
    void DDAAlgorithmMine(int startIndex)
    {
        //note: square viewarea
        //1. get edge tiles/indexes and start index (gotta know start and stop)
        //2. Find line between start and edge pieces, stop if wall with DDA

        List<int> edgeIndexes = getEdgeIndexesCircle(startIndex, playerViewDist);

        //make lines!
        //given x = start index column
        int x0 = makeX(startIndex);
        int y0 = makeY(startIndex);

        List<int> viewIndexes = new List<int>();

        //for all the edge tiles
        foreach(int edgeIndex in edgeIndexes)
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
                    viewIndexes.Add(newIndex);
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
                        viewIndexes.Add(newIndex);
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
                        viewIndexes.Add(newIndex);
                    }
                }
            }
        }

        //Set them white (btw, less efficient than setting as we find them
        foreach(int i in viewIndexes)
        {
            setSeen(i, true);
        }
    }

    //Help method for DDAAlgorithmMine
    int coordCheckTileMine(int index, int seenThrough)
    {
        if (resourceMap[index] == (int)ResourceSetting.blue && seenThrough < maxSeeThrough) //its wall
        {
            return 0;
        }
        else if(resourceMap[index] == (int)ResourceSetting.wall)
        {
            return -1;
        }
        else if(resourceMap[index] == (int)ResourceSetting.nothing && seenThrough > 0)
        {
            return -1;
        }
        return 1;
    }

    List<int> getEdgeIndexesSquare(int startIndex, int viewDist)
    {
        List<int> edgeIndexes = new List<int>();

        //Find edge pieces in a square fashion
        int x0 = makeX(startIndex);
        int y0 = makeY(startIndex);

        //top
        int x = newCoord(x0 - viewDist);
        int y = newCoord(y0 - viewDist);
        int dx = x - x0;
        int dy = y - y0;
        for (int i = 0; i < viewDist * 2 + 1; i++)
        {
            int index = makeIndex(x, y);
            int nx = x;
            int ny = y;
            addToEdgeList(edgeIndexes, nx, ny);
            x++; //doesnt skip corner piece
        }

        //bottom
        x = newCoord(x0 - viewDist);
        y = newCoord(y0 + viewDist);
        for (int i = 0; i < viewDist * 2 + 1; i++)
        {
            int index = makeIndex(x, y);
            int nx = x;
            int ny = y;
            addToEdgeList(edgeIndexes, nx, ny);
            x++; //no skip
        }

        //Left
        x = newCoord(x0 - viewDist);
        y = newCoord(y0 - viewDist);
        for (int i = 0; i < viewDist * 2 - 1; i++) //skips last corner piece
        {
            y++; //note: always skips the corner piece
            int index = makeIndex(x, y);
            int nx = x;
            int ny = y;
            addToEdgeList(edgeIndexes, nx, ny);
        }

        //Right
        x = newCoord(x0 + viewDist);
        y = newCoord(y0 - viewDist);
        for (int i = 0; i < viewDist * 2 - 1; i++)
        {
            y++;
            int index = makeIndex(x, y);
            int nx = x;
            int ny = y;
            addToEdgeList(edgeIndexes, nx, ny);
        }
        return edgeIndexes;
    }

    int newCoord(int x)
    {
        if (x > length)
        {
            x = length - 1;
        }
        if (x < 0)
        {
            x = 0;
        }
        return x;
    }

    bool addToEdgeList(List<int> list, int index)
    {
        if (!list.Contains(index) && index >= 0 && index < lengthSq)
        {
            list.Add(index);
            return true;
        }
        return false;
    }

    bool addToEdgeList(List<int> list, int x, int y)
    {
        if ((x >= 0 && x < length) && (y >= 0 && y < length)) //inside grid
        {
            int index = makeIndex(x, y);
            if (!list.Contains(index))
            {
                list.Add(index);
                return true;
            }
        }
        return false;
    }

    List<int> getEdgeIndexesCircle(int startIndex, int viewDist)
    {
        //get the edge indexes of square
        List<int> edgeIndexes = getEdgeIndexesSquare(startIndex, viewDist);
        List<int> newEdgeIndexes = new List<int>();

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
            while (!checkInsideCircleView(dx, dy, viewDist))
            {
                (nx, ny) = DecrementXY(dx, dy, nx, ny);
                dx = nx - x0;
                dy = ny - y0;
            } //end while

            if (addToEdgeList(newEdgeIndexes, nx, ny))
            {
                //print("Inside radius index: " + makeIndex(nx, ny) + ", " + nx + "/" + ny);
            }

            //Add inner edge of this edge
            (nx, ny) = DecrementXY(dx, dy, nx, ny);

            if (addToEdgeList(newEdgeIndexes, nx, ny))
            {
                //print("InnerEdge index: " + makeIndex(nx, ny) + ", x0/y0: " + x0 + "/" + y0 + ", dx/dy: " + nx + "/" + ny);
            }
        }

        return newEdgeIndexes;
    }

    bool checkInsideCircleView(int dx, int dy, int viewDist)
    {
        float tol = 0.25f; //(playerViewDist - 3)*0.25f;

        float sqdist = Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2);
        //print(">>>Checking " + dx + "/" + dy + ": " + sqdist);
        float maxDist = Mathf.Pow(viewDist + tol, 2);
        if (sqdist <= maxDist)
        {
            //print("Edge: " + dx + "/" + dy + " (" + sqdist + ")" + " is inside rad " + maxDist + " circle!");
            return true;
        }
        return false;
    }
    
    (int nx, int ny) DecrementXY(int dx, int dy, int x, int y)
    {
        if (Mathf.Abs(dx) >= Mathf.Abs(dy)) // dec/inc-rement the one that's largest
        {
            if (dx > 0)
            {
                x--;
            }
            else
            {
                x++;
            }
        }
        else
        {
            if (dy > 0)
            {
                y--;
            }
            else
            {
                y++;
            }
        }
        return (x, y);
    }
    #endregion

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

    #region incomplete solutions
    /*
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
    */
    #endregion
}