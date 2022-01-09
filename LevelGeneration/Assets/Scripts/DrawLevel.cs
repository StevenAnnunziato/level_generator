using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawLevel : MonoBehaviour
{

    // assumes all prefabs are the same size and square
    [Header("General Properties")]
    public GameObject[] tilePrefabs;
    public float cellSize = 1f;

    [Header("Level Properties")]
    public uint levelSize = 20;
    public float turnChance = 0.2f;
    public int minCorridorSteps = 6;
    public int maxCorridorSteps = 15;
    public int minRooms = 5;
    public int maxRooms = 8;
    public int minRoomSize = 3;
    public int maxRoomSize = 10;
    public bool overlapRooms = false;
    public float branchChance = 0.2f;
    public int minRoomsPerBranch = 1;
    public int maxRoomsPerBranch = 3;

    // level to draw
    private LevelLayout myLevel;

    // Start is called before the first frame update
    void Start()
    {
        CreateLevel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearLevel();
            CreateLevel();
        }
    }

    public void CreateLevel()
    {
        // create data for the level
        LevelData data = new LevelData();
        data.levelWidth = levelSize;
        data.levelHeight = levelSize;
        data.turnChance = turnChance;
        data.minCorridorSteps = minCorridorSteps;
        data.maxCorridorSteps = maxCorridorSteps;
        data.minRooms = minRooms;
        data.maxRooms = maxRooms;
        data.minRoomSize = minRoomSize;
        data.maxRoomSize = maxRoomSize;
        data.overlapRooms = overlapRooms;
        data.branchChance = branchChance;
        data.minRoomsPerBranch = minRoomsPerBranch;
        data.maxRoomsPerBranch = maxRoomsPerBranch;

        // create the level
        myLevel = new LevelLayout(data);

        // Draw the level
        PlaceObjects();
    }
    
    void PlaceObjects()
    {
        Vector2 size = myLevel.getSize();
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                uint cell = myLevel.getCell(i, j);
                if (cell == 0)
                    continue;

                // place an object
                Vector3 pos = new Vector3(i * cellSize, 0, j * cellSize);
                pos -= new Vector3(levelSize / 2, 0, levelSize / 2);
                Instantiate(tilePrefabs[cell], pos, Quaternion.identity);
            }
        }
    }

    void ClearLevel()
    {
        GameObject[] gos = GameObject.FindGameObjectsWithTag("Level");
        foreach (GameObject g in gos)
        {
            Destroy(g);
        }
    }

}
