using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


/// <summary>
/// The class implementing gameplay logic.
/// </summary>
class AI : BaseAI
{
    Player me;
    
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

        // If it's the first turn, place traps
        if( roundTurnNumber() <= 1 )
        {
            // Find my sarcophagi
            foreach( var trap in traps )
            {
                if( trap.Owner == playerID() && trap.TrapType == TrapType.SARCOPHAGUS)
                {
                    mySarcophagi.Add(trap);
                }
            }

            // Find the first open tiles and place the sarcophagi there
            for( int i = 0; i < tiles.Length; i++ )
            {
                Tile tile = tiles[i];
                if( )
            }
        }

        return true;
    }

    /// <summary>
    /// This function is called once, before your first turn.
    /// </summary>
    public override void init()
    {
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
        while( !parent.ContainsKey(endTile) )
        {
    	    // If there are no tiles in the openSet then there is no path
            if(openSet.Count == 0)
            {
                return toReturn;
            }
            // Check tiles from the front and remove
            Tile curTile = openSet.Dequeue();

            const int xChange[] = { 0, 0, -1, 1};
            const int yChange[] = {-1, 1,  0, 0};
            //look in all directions
            for(unsigned i = 0; i < 4; ++i)
            {
            Point loc(curTile->x() + xChange[i], curTile->y() + yChange[i]);
            Tile* toAdd = getTile(loc.x, loc.y);
            //if a tile exists there
            if(toAdd != NULL)
            {
        	    //if it's an open tile and it doesn't have a parent
                if(toAdd->type() == Tile::EMPTY && parent.count(toAdd) == 0)
                {
          	        //add the tile to the open set; and mark its parent as the current tile
                    openSet.push_back(toAdd);
                    parent[toAdd] = curTile;
                }
            }
            }
        }
        //find the path back
        for(Tile* tile = endTile; parent[tile] != NULL; tile = parent[tile])
        {
            toReturn.push_front(Point(tile->x(), tile->y()));
        }
        return toReturn;
    }
}

struct Point
{
    public int x;
    public int y;

    public Point() { this.x = -1; this.y = -1; }
    public Point(int x, int y) { this.x = x; this.y = y; }
};