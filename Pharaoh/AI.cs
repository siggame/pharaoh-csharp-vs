using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


/// <summary>
/// The class implementing gameplay logic.
/// </summary>
class AI : BaseAI
{
    Player me;
    Random rand;
    
    public override string username()
    {
        return "Shell AI";
    }

    public override string password()
    {
        return "password";
    }

    /// <summary>
    /// This function is called each time it is your turn.
    /// </summary>
    /// <returns>True to end your turn. False to ask the server for updated information.</returns>
    public override bool run()
    {
        // Lists to track sarcophagi
        List<Trap> mySarcophagi = new List<Trap>();
        List<Trap> enemySarcophagi = new List<Trap>();

#region FirstTurn
        // If it's the first turn, place traps
        if (roundTurnNumber() <= 1)
        {
            // Find my sarcophagi
            foreach (var trap in traps)
            {
                if (trap.Owner == playerID() && trap.TrapType == TrapType.SARCOPHAGUS)
                {
                    mySarcophagi.Add(trap);
                }
            }

            int sarcophagusCount = mySarcophagi.Count;
            // Find the first open tiles and place the sarcophagi there
            for (int i = 0; i < tiles.Length; i++)
            {
                Tile tile = tiles[i];
                // If the tile is on my side and is empty
                if (onMySide(tile.X) && tile.Type == Tile.EMPTY)
                {
                    // Move my sarcophagus to that location
                    me.placeTrap(tile.X, tile.Y, TrapType.SARCOPHAGUS);
                    sarcophagusCount--;
                    if (sarcophagusCount == 0)
                    {
                        break;
                    }
                }
            }
            // Make sure there aren't too many traps spawned
            List<int> trapCount = new List<int>(trapTypes.Length);
            //continue spawning traps until there isn't enough money to spend
            for (int i = 0; i < tiles.Length; i++)
            {
                // If the tile is on my side
                Tile tile = tiles[i];
                if (onMySide(tile.X))
                {
                    // Make sure there isn't a trap on that tile
                    if (getTrap(tile.X, tile.Y) != null)
                    {
                        continue;
                    }
                    // Select a random trap type (make sure it isn't a sarcophagus)
                    int trapType = rand.Next(trapTypes.Length - 1) + 1;
                    // Make sure another can be spawned
                    if (trapCount[trapType] < trapTypes[trapType].MaxInstances)
                    {
                        continue;
                    }
                    // If there are enough scarabs
                    if (me.Scarabs >= trapTypes[trapType].Cost)
                    {
                        // Check if the tile is the right type (wall or empty)
                        if (trapTypes[trapType].CanPlaceOnWalls == 1 && tile.Type == Tile.WALL)
                        {
                            me.placeTrap(tile.X, tile.Y, trapType);
                            trapCount[trapType]++;
                        }
                        else if (trapTypes[trapType].CanPlaceOnWalls == 0 && tile.Type == Tile.EMPTY)
                        {
                            me.placeTrap(tile.X, tile.Y, trapType);
                            trapCount[trapType]++;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
#endregion
        // Otherwise it's time to move and purchase thieves and activate traps
        else
        {
            // Find my sarcophagi and the enemy sarcophagi
            foreach (var trap in traps)
            {
                if (trap.TrapType == TrapType.SARCOPHAGUS)
                {
                    if (trap.Owner != playerID())
                    {
                        enemySarcophagi.Add(trap);
                    }
                    else
                    {
                        mySarcophagi.Add(trap);
                    }
                }
            }
            // Find my spawn tiles
            List<Tile> spawnTiles = getMySpawns();
            // Find my thieves
            List<Thief> myThieves = getMyThieves();
            // Select a random thief type
            int thiefNo = rand.Next(thiefTypes.Length);
            // If you can afford the thief
            if (me.Scarabs >= thiefTypes[thiefNo].Cost)
            {
                // Make sure another can be spawned
                int max = thiefTypes[thiefNo].MaxInstances;
                int count = 0;
                foreach (var thief in myThieves)
                {
                    if (thief.ThiefType == thiefNo)
                    {
                        count++;
                    }
                }
                // Only spawn if there aren't too many
                if (count < max)
                {
                    // Select a random spawn location
                    int spawnLoc = rand.Next(spawnTiles.Count);
                    // Spawn a thief there
                    Tile spawnTile = spawnTiles[spawnLoc];
                    me.purchaseThief(spawnTile.X, spawnTile.Y, thiefNo);
                }
            }
            // Move my thieves
            foreach (var thief in myThieves)
            {
                // If the thief is alive and not frozen
                if (thief.Alive == 1 && thief.FrozenTurnsLeft == 0)
                {
                    int[] xChange = new int[] { -1, 1, 0, 0 };
                    int[] yChange = new int[] { 0, 0, -1, 1 };
                    // Try to dig or use a bomb before moving
                    if (thief.ThiefType == ThiefType.DIGGER && thief.SpecialsLeft > 0)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            // If there is a wall adjacent and an empty space on the other side
                            int checkX = thief.X + xChange[i];
                            int checkY = thief.Y + yChange[i];
                            Tile wallTile = getTile(checkX, checkY);
                            Tile emptyTile = getTile(checkX + xChange[i], checkY + yChange[i]);
                            // Must be on the map, and not trying to dig to the other side
                            if (wallTile != null && emptyTile != null && !onMySide(checkX + xChange[i]))
                            {
                                // If there is a wall with an empty tile on the other side
                                if (wallTile.Type == Tile.WALL && emptyTile.Type == Tile.EMPTY)
                                {
                                    // Dig through the wall
                                    thief.useSpecial(checkX, checkY);
                                    //break out of the loop
                                    break;
                                }
                            }
                        }
                    }
                    else if (thief.ThiefType == ThiefType.BOMBER && thief.SpecialsLeft > 0)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            // The place to check for things to blow up
                            int checkX = thief.X + xChange[i];
                            int checkY = thief.Y + yChange[i];
                            // Make sure that the spot isn't on the other side
                            if (!onMySide(checkX))
                            {
                                // If there is a wall tile there, blow it up
                                Tile checkTile = getTile(checkX, checkY);
                                if (checkTile != null && checkTile.Type == Tile.WALL)
                                {
                                    // Blow up the wall
                                    thief.useSpecial(checkX, checkY);
                                    // Break out of the loop
                                    break;
                                }
                                // Otherwise check if there is a trap there
                                Trap checkTrap = getTrap(checkX, checkY);
                                // Don't want to blow up the sarcophagus!
                                if (checkTrap != null && checkTrap.TrapType != TrapType.SARCOPHAGUS)
                                {
                                    // Blow up the trap
                                    thief.useSpecial(checkX, checkY);
                                    // Break out of the loop
                                    break;
                                }
                            }
                        }
                    }
                    // If the thief has any movement left
                    if (thief.MovementLeft > 0)
                    {
                        // Find a path from the thief's location to the enemy sarcophagus
                        Queue<Point> path = new Queue<Point>();
                        int endX = enemySarcophagi[0].X;
                        int endY = enemySarcophagi[0].Y;
                        path = findPath(new Point(thief.X, thief.Y), new Point(endX, endY));
                        // If a path exists then move forward on the path
                        if (path.Count > 0)
                        {
                            Point nextMove = path.Dequeue();
                            thief.move(nextMove.x, nextMove.y);
                        }
                    }
                }
            }
            // Do things with traps now
            List<Trap> myTraps = getMyTraps();
            foreach (var trap in myTraps)
            {
                int[] xChange = new int[] { -1, 1, 0, 0 };
                int[] yChange = new int[] { 0, 0, -1, 1 };
                // Make sure trap can be used
                if (trap.Active == 1)
                {
                    // If trap is a boulder
                    if (trap.TrapType == TrapType.BOULDER)
                    {
                        // If there is an enemy thief adjancent
                        for (int i = 0; i < 4; i++)
                        {
                            Thief enemyThief = getThief(trap.X + xChange[i], trap.Y + yChange[i]);
                            // Roll over the thief
                            if (enemyThief != null)
                            {
                                trap.act(xChange[i], yChange[i]);
                                break;
                            }
                        }
                    }
                    else if (trap.TrapType == TrapType.MUMMY)
                    {
                        // Move around randomly if a mummy
                        int dir = rand.Next(4);
                        int checkX = trap.X + xChange[dir];
                        int checkY = trap.Y + yChange[dir];
                        Tile checkTile = getTile(checkX, checkY);
                        Trap checkTrap = getTrap(checkX, checkY);
                        // If the tile is empty, and there isn't a sarcophagus there
                        if (checkTrap == null || checkTrap.TrapType != TrapType.SARCOPHAGUS)
                        {
                            if (checkTile != null && checkTile.Type == Tile.EMPTY)
                            {
                                //move on that tile
                                trap.act(checkX, checkY);
                            }
                        }
                    }
                }
            }
        }
        return true;
    }

    /// <summary>
    /// This function is called once, before your first turn.
    /// </summary>
    public override void init()
    {
        // Initialize random number generator
        rand = new Random();
        // Find out which player I am
        me = players[playerID()];
    }

    /// <summary>
    /// This function is called once, after your last turn.
    /// </summary>
    public override void end() { }

    public AI(IntPtr c)
        : base(c) { }

    // Returns true if the position is on your side of the field
    bool onMySide(int x)
    {
        if(playerID() == 0)
        {
            return (x < mapWidth() / 2);
        }
        else
        {
            return (x >= mapWidth() / 2);
        }
    }

    // Returns the first thief encountered on x, y or null if no thief
    Thief getThief(int x, int y)
    {
        if (x < 0 || x >= mapWidth() || y < 0 || y >= mapHeight())
        {
            return null;
        }
        foreach( var thief in thiefs )
        {
            if( thief.X == x && thief.Y == y)
            {
                return thief;
            }
        }
        return null;
    }

    // Returns the tile at the given x,y position or null otherwise
    Tile getTile(int x, int y)
    {
        if (x < 0 || x >= mapWidth() || y < 0 || y >= mapHeight())
        {
            return null;
        }
        return tiles[y + x * mapHeight()];
    }

    // Returns the trap at the given x,y position or null otherwise
    Trap getTrap(int x, int y)
    {
        if (x < 0 || x >= mapWidth() || y < 0 || y >= mapHeight())
        {
            return null;
        }
        foreach( var trap in traps )
        {
            if( trap.X == x && trap.Y == y )
            {
                return trap;
            }
        }
        return null;
    }

    // Returns a list of all of your traps
    List<Trap> getMyTraps()
    {
        List<Trap> toReturn = new List<Trap>();
        foreach( var trap in traps )
        {
            if( trap.Owner == playerID())
            {
                toReturn.Add(trap);
            }
        }
        return toReturn;
    }

    // Returns a list of all of your enemy's traps
    List<Trap> getEnemyTraps()
    {
        List<Trap> toReturn = new List<Trap>();
        foreach( var trap in traps )
        {
            if( trap.Owner != playerID() )
            {
                toReturn.Add(trap);
            }
        }
        return toReturn;
    }

    // Returns a list of all of your spawn tiles
    List<Tile> getMySpawns()
    {
        List<Tile> toReturn = new List<Tile>();
        foreach( var tile in tiles )
        {
            if( !onMySide(tile.X) && tile.Type == Tile.SPAWN )
            {
                toReturn.Add(tile);
            }
        }
        return toReturn;
    }

    // Returns a list of all of your thieves
    List<Thief> getMyThieves()
    {
        List<Thief> toReturn = new List<Thief>();
        foreach( var thief in thiefs )
        {
            if( thief.Owner == playerID() )
            {
                toReturn.Add(thief);
            }
        }
        return toReturn;
    }

    // Returns a list of all of the enemy thieves
    List<Thief> getEnemyThieves()
    {
        List<Thief> toReturn = new List<Thief>();
        foreach( var thief in thiefs )
        {
            if( thief.Owner != playerID() )
            {
                toReturn.Add(thief);
            }
        }
        return toReturn;
    }

    //returns a path from start to end, or nothing if no path is found.
    Queue<Point> findPath(Point start, Point end)
    {
        Stack<Point> reversedReturn = new Stack<Point>();
        Queue<Point> toReturn = new Queue<Point>();
        // The set of open tiles to look at
        Queue<Tile> openSet = new Queue<Tile>();
        // Points back to parent tile
        Dictionary<Tile, Tile> parent = new Dictionary<Tile,Tile>();
        // Push back the starting tile
        openSet.Enqueue(getTile(start.x, start.y));
        // The start tile has no parent
        parent[getTile(start.x, start.y)] = null;
        // The end tile
        Tile endTile = getTile(end.x, end.y);
        // As long as the end tile has no parent
        while( ! parent.ContainsKey(endTile) )
        {
    	    // If there are no tiles in the openSet then there is no path
            if( openSet.Count == 0 )
            {
                return toReturn;
            }
            // Check tiles from the front and remove
            Tile curTile = openSet.Dequeue();

            int[] xChange = new int[]{ 0, 0, -1, 1};
            int[] yChange = new int[]{-1, 1,  0, 0};
            // Look in all directions
            for( int i = 0; i < 4; i++ )
            {
                Point loc = new Point(curTile.X + xChange[i], curTile.Y+ yChange[i]);
                Tile toAdd = getTile(loc.x, loc.y);
                // If a tile exists there
                if( toAdd != null )
                {
        	        // If it's an open tile and it doesn't have a parent
                    if( toAdd.Type == Tile.EMPTY && ! parent.ContainsKey(toAdd) )
                    {
          	            // Add the tile to the open set; and mark its parent as the current tile
                        openSet.Enqueue(toAdd);
                        parent[toAdd] = curTile;
                    }
                }
            }
        }
        // Find the path back
        for(Tile tile = endTile; parent[tile] != null; tile = parent[tile])
        {
            reversedReturn.Push(new Point(tile.X, tile.Y));
        }
        // Convert stack to a queue
        while( reversedReturn.Count > 0 )
        {
            toReturn.Enqueue(reversedReturn.Pop());
        }
        return toReturn;
    }
}

struct Point
{
    public int x;
    public int y;

    public Point(int x, int y) { this.x = x; this.y = y; }
};