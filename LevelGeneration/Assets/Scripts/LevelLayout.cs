using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct LevelData
{
    public uint levelWidth;
    public uint levelHeight;
    public float turnChance;
    public int minCorridorSteps;
    public int maxCorridorSteps;
    public int minRooms;
    public int maxRooms;
    public int minRoomSize;
    public int maxRoomSize;
    public bool overlapRooms;
    public float branchChance;
    public int minRoomsPerBranch;
    public int maxRoomsPerBranch;
}

public class LevelLayout
{

    // 2D array
    private uint[] grid;
    LevelData myData;

    // data for drunkard's walk
    private enum Direction { None = -1, North, South, East, West, NUM_DIRECTIONS }
    private Vector2[] directionVectors = { new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0), new Vector2(-1, 0) };

    // customizable tile options
    // for more control, move this to LevelData as needed.
    private enum TileType
    {
        None,
        Corridor,
        Room
    }

    // branch data
    private struct BranchData
    {
        public Direction branchDirection;
        public Vector2 branchPosition;
        public int numRoomsInBranch;
    }
    private List<BranchData> branchPoints;

    // private member variables
    private Direction currentDirection;
    private Vector2 currentPosition;
    private int numRooms;

    // Getters / Setters ------------------------------------------------------------------------
    // cells are in range [0, size)
    // access row x, column y
    public uint getCell(int row, int column)
    {
        if (CheckInBounds(new Vector2(row, column)))
            return grid[row * myData.levelHeight + column];
        else
            Debug.LogWarning("Error: Trying to get a cell outside of the map!");

        return 0;
    }
    public uint getCell(Vector2 cell)
    {
        return getCell((int)cell.x, (int)cell.y);
    }
    public void setCell(uint value, int row, int column)
    {
        if (CheckInBounds(new Vector2(row, column)))
            grid[row * myData.levelHeight + column] = value;
        else
            Debug.LogWarning("Error: Trying to set a cell outside of the map!");
    }
    private void markCell(uint value, Vector2 pos)
    {
        setCell(value, (int)pos.x, (int)pos.y);
    }
    private void markCell(TileType value, Vector2 pos)
    {
        setCell((uint)value, (int)pos.x, (int)pos.y);
    }
    public Vector2 getSize() { return new Vector2(myData.levelWidth, myData.levelHeight); }
    private Vector2 GetDirectionVector(Direction dir)
    {
        if (dir < Direction.NUM_DIRECTIONS)
            return directionVectors[(int)dir];
        else
            Debug.LogError("Error: Trying to access direction " + (int)dir + " which does not exist.");

        return Vector2.zero;
    }

    // Constructor
    public LevelLayout(LevelData data)
    {
        myData = data;

        // init grid
        grid = new uint[data.levelWidth * data.levelHeight];
        branchPoints = new List<BranchData>();

        // populate the level with rooms and hallways
        GenerateLevel();
    }

    // Main level generation -----------------------------------------------------------
    private void GenerateLevel()
    {
        
        // drunkard's walk algorithm
        currentPosition = new Vector2(myData.levelWidth / 2, myData.levelHeight / 2);
        currentDirection = Direction.North;
        int desiredNumRooms = Random.Range(myData.minRooms, myData.maxRooms);

        // Place a starting room
        GenerateRoom(TileType.Room, currentPosition);
        numRooms++;

        while (numRooms < desiredNumRooms)
        {
            // make a corridor
            GenerateCorridor();

            // Place a room
            if (GenerateRoom(TileType.Room, currentPosition))
                numRooms++;

            // branch here if applicable
            if (RandomCheck(myData.branchChance))
            {
                BranchData data = new BranchData();
                data.branchPosition = currentPosition;
                data.branchDirection = RandomBranchDirection(currentPosition, currentDirection);
                data.numRoomsInBranch = Random.Range(myData.minRoomsPerBranch, myData.maxRoomsPerBranch + 1);
                int roomsBeforeBranch = numRooms;
                numRooms += data.numRoomsInBranch;

                // too many rooms after this branch?
                if (numRooms > desiredNumRooms)
                    data.numRoomsInBranch = desiredNumRooms - roomsBeforeBranch;

                branchPoints.Add(data);
            }    

        }

        // create branches
        for (int i = 0; i < branchPoints.Count; i++)
        {
            // jump to this branch point
            currentPosition = branchPoints[i].branchPosition;
            currentDirection = branchPoints[i].branchDirection;

            for (int j = 0; j < branchPoints[i].numRoomsInBranch; j++)
            {
                GenerateCorridor();
                // guarentee a room
                while (!GenerateRoom(TileType.Room, currentPosition))
                {
                    GenerateCorridor();
                }
            }
            // branch complete
            
        }

        // level generation complete

    }

    private void GenerateCorridor()
    {
        int desiredCorridorLength = Random.Range(myData.minCorridorSteps, myData.maxCorridorSteps);
        for (int i = 0; i < desiredCorridorLength; i++)
        {
            // Don't overrite other tiles
            if (getCell(currentPosition) == (uint)TileType.None)
                markCell(TileType.Corridor, currentPosition);

            if (RandomCheck(myData.turnChance))
                currentDirection = RandomValidDirection(currentPosition);

            // ensure the next space will be valid
            if (!CheckInBounds(currentPosition + GetDirectionVector(currentDirection)))
                currentDirection = RandomValidDirection(currentPosition);

            // move
            currentPosition += GetDirectionVector(currentDirection);
        }
    }

    // for branching - overrites current position and direction
    private void GenerateCorridor(Vector2 startPos, Direction startDir)
    {
        currentPosition = startPos;
        currentDirection = startDir;

        int desiredCorridorLength = Random.Range(myData.minCorridorSteps, myData.maxCorridorSteps);
        for (int i = 0; i < desiredCorridorLength; i++)
        {
            // Don't overrite other tiles
            if (getCell(currentPosition) == (uint)TileType.None)
                markCell(TileType.Corridor, currentPosition);

            if (RandomCheck(myData.turnChance))
                currentDirection = RandomValidDirection(currentPosition);

            // ensure the next space will be valid
            if (!CheckInBounds(currentPosition + GetDirectionVector(currentDirection)))
                currentDirection = RandomValidDirection(currentPosition);

            // move
            currentPosition += GetDirectionVector(currentDirection);
        }
    }

    private bool GenerateRoom(TileType type, Vector2 roomPos)
    {
        // place floors in a grid centered at roomPos
        int roomWidth = Random.Range(myData.minRoomSize, myData.maxRoomSize);
        int roomHeight = Random.Range(myData.minRoomSize, myData.maxRoomSize);
        Vector2 startPos = roomPos - new Vector2(roomWidth / 2, roomHeight / 2);

        // first check if the space is already occupied
        // NOTE: Also checks out one unit as to not connect rooms
        // NOTE: This will sometimes lead to longer corridors than specified to compensate.
        if (!myData.overlapRooms)
        {
            for (int i = -1; i < roomWidth + 1; i++)
            {
                for (int j = -1; j < roomHeight + 1; j++)
                {
                    uint cell = getCell(new Vector2(startPos.x + i, startPos.y + j));
                    if (cell == (uint)TileType.Room)
                    {
                        return false;
                    }
                }
            }
        }

        // place the room
        for (int i = 0; i < roomWidth; i++)
        {
            for (int j = 0; j < roomHeight; j++)
            {
                markCell(type, new Vector2(startPos.x + i, startPos.y + j));
            }
        }

        return true;
    }

    // Helper functions -------------------------------------------------------------------

    private bool RandomCheck(float percentChance)
    {
        float percent = percentChance * 100;
        if (Random.Range(0, 100) < percent)
            return true;
        return false;
    }

    private Direction RandomDirection()
    {
        return (Direction)Random.Range(0, (int)Direction.NUM_DIRECTIONS);
    }

    // Returns a direction which does not go outside of the map and does not backtrack on itself.
    private Direction RandomValidDirection(Vector2 currentPos)
    {
        // don't backtrack
        List<Direction> dirs = new List<Direction> { Direction.North, Direction.South, Direction.East, Direction.West };
        switch (currentDirection)
        {
            case Direction.North:
                dirs.Remove(Direction.South);
                break;
            case Direction.South:
                dirs.Remove(Direction.North);
                break;
            case Direction.East:
                dirs.Remove(Direction.West);
                break;
            case Direction.West:
                dirs.Remove(Direction.East);
                break;
            default:
                break;
        }

        // don't go out of bounds
        List<Direction> finalDirs = new List<Direction>();
        for (int i = 0; i < dirs.Count; i++)
        {
            if (CheckInBounds(currentPos + GetDirectionVector(dirs[i])))
                finalDirs.Add(dirs[i]);
        }

        // make sure there is a direction to pick - this should never happen
        if (finalDirs.Count == 0)
        {
            Debug.LogError("Error: No new directions to pick!");
            return Direction.None;
        }

        // pick a random valid direction
        return finalDirs[Random.Range(0, finalDirs.Count)];

    }

    private Direction RandomBranchDirection(Vector2 currentPos, Direction dirToAvoid)
    {
        // don't backtrack or move forward
        List<Direction> dirs = new List<Direction> { Direction.North, Direction.South, Direction.East, Direction.West };
        dirs.Remove(dirToAvoid);
        switch (currentDirection)
        {
            case Direction.North:
                dirs.Remove(Direction.South);
                break;
            case Direction.South:
                dirs.Remove(Direction.North);
                break;
            case Direction.East:
                dirs.Remove(Direction.West);
                break;
            case Direction.West:
                dirs.Remove(Direction.East);
                break;
            default:
                break;
        }

        // don't go out of bounds
        List<Direction> finalDirs = new List<Direction>();
        for (int i = 0; i < dirs.Count; i++)
        {
            if (CheckInBounds(currentPos + GetDirectionVector(dirs[i])))
                finalDirs.Add(dirs[i]);
        }

        // make sure there is a direction to pick - this might happen in a crowded corner of the level
        if (finalDirs.Count == 0)
        {
            Debug.LogError("Error: No new directions to pick while branching!");
            return Direction.None;
        }

        // pick a random valid direction
        return finalDirs[Random.Range(0, finalDirs.Count)];

    }

    private bool CheckInBounds(Vector2 pos)
    {
        int buffer = 0;
        return pos.x < myData.levelWidth - buffer && pos.y < myData.levelHeight - buffer && pos.x >= buffer && pos.y >= buffer;
    }

}
