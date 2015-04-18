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

            int sarcophagusCount = mySarcophagi.Count;
            // Find the first open tiles and place the sarcophagi there
            for( int i = 0; i < tiles.Length; i++ )
            {
                Tile tile = tiles[i];
                // If the tile is on my side and is empty
                if( onMySide(tile.X) && tile.Type == Tile.EMPTY )
                {
                    //move my sarcophagus to that location
                    me.placeTrap(tile.X, tile.Y, TrapType.SARCOPHAGUS);
                    --sarcophagusCount;
                    if(sarcophagusCount == 0)
                    {
                        break;
                    }
                }
            }
            //make sure there aren't too many traps spawned
            std::vector<unsigned> trapCount(trapTypes.size());
            //continue spawning traps until there isn't enough money to spend
            for(unsigned i = 0; i < tiles.size(); ++i)
            {
                //if the tile is on my side
                Tile& tile = tiles[i];
                if(onMySide(tile.x()))
                {
                //make sure there isn't a trap on that tile
                if(getTrap(tile.x(), tile.y()) != NULL)
                {
                    continue;
                }
                //select a random trap type (make sure it isn't a sarcophagus)
                int trapType = (rand() % (trapTypes.size() - 1)) + 1;
                //make sure another can be spawned
                if(trapCount[trapType] < trapTypes[trapType].maxInstances())
                {
                    continue;
                }
                //if there are enough scarabs
                if(me->scarabs() >= trapTypes[trapType].cost())
                {
                    //check if the tile is the right type (wall or empty)
                    if(trapTypes[trapType].canPlaceOnWalls() && tile.type() == Tile::WALL)
                    {
                    me->placeTrap(tile.x(), tile.y(), trapType);
                    ++trapCount[trapType];
                    }
                    else if(!trapTypes[trapType].canPlaceOnWalls() && tile.type() == Tile::EMPTY)
                    {
                    me->placeTrap(tile.x(), tile.y(), trapType);
                    ++trapCount[trapType];
                    }
                }
                else
                {
                    break;
                }
                }
            }
            }
            //otherwise it's time to move and purchase thieves and activate traps
            else
            {
            //find my sarcophagi and the enemy sarcophagi
            for(unsigned i = 0; i < traps.size(); ++i)
            {
                Trap& trap = traps[i];
                if(trap.trapType() == TrapType::SARCOPHAGUS)
                {
                if(trap.owner() != playerID())
                {
                    enemySarcophagi.push_back(&trap);
                }
                else
                {
                    mySarcophagi.push_back(&trap);
                }
                }
            }
            //find my spawn tiles
            std::vector<Tile*> spawnTiles = getMySpawns();
            //select a random thief type
            int thiefNo = rand() % thiefTypes.size();
            //if you can afford the thief
            if(me->scarabs() >= thiefTypes[thiefNo].cost())
            {
                //make sure another can be spawned
                int max = thiefTypes[thiefNo].maxInstances();
                int count = 0;
                std::vector<Thief*> myThieves = getMyThieves();
                for(unsigned i = 0; i < myThieves.size(); ++i)
                {
                if(myThieves[i]->thiefType() == thiefNo)
                {
                    ++count;
                }
                }
                //only spawn if there aren't too many
                if(count < max)
                {
                //select a random spawn location
                int spawnLoc = rand() % spawnTiles.size();
                //spawn a thief there
                Tile* spawnTile = spawnTiles[spawnLoc];
                me->purchaseThief(spawnTile->x(), spawnTile->y(), thiefNo);
                }
            }
            //move my thieves
            std::vector<Thief*> myThieves = getMyThieves();
            for(unsigned i = 0; i < myThieves.size(); ++i)
            {
                Thief* thief = myThieves[i];
                //if the thief is alive and not frozen
                if(thief->alive() && thief->frozenTurnsLeft() == 0)
                {
                const int xChange[] = {-1, 1,  0, 0};
                const int yChange[] = { 0, 0, -1, 1};
                //try to dig or use a bomb before moving
                if(thief->thiefType() == ThiefType::DIGGER && thief->specialsLeft() > 0)
                {
                    for(unsigned i = 0; i < 4; ++i)
                    {
                    //if there is a wall adjacent and an empty space on the other side
                    int checkX = thief->x() + xChange[i];
                    int checkY = thief->y() + yChange[i];
                    Tile* wallTile = getTile(checkX, checkY);
                    Tile* emptyTile = getTile(checkX + xChange[i], checkY + yChange[i]);
                    //must be on the map, and not trying to dig to the other side
                    if(wallTile != NULL && emptyTile != NULL && !onMySide(checkX + xChange[i]))
                    {
                        //if there is a wall with an empty tile on the other side
                        if(wallTile->type() == Tile::WALL && emptyTile->type() == Tile::EMPTY)
                        {
                        //dig through the wall
                        thief->useSpecial(checkX, checkY);
                        //break out of the loop
                        break;
                        }
                    }
                    }
                }
                else if(thief->thiefType() == ThiefType::BOMBER && thief->specialsLeft() > 0)
                {
                    for(unsigned i = 0; i < 4; ++i)
                    {
                    //the place to check for things to blow up
                    int checkX = thief->x() + xChange[i];
                    int checkY = thief->y() + yChange[i];
                    //make sure that the spot isn't on the other side
                    if(!onMySide(checkX))
                    {
                        //if there is a wall tile there, blow it up
                        Tile* checkTile = getTile(checkX, checkY);
                        if(checkTile != NULL && checkTile->type() == Tile::WALL)
                        {
                        //blow up the wall
                        thief->useSpecial(checkX, checkY);
                        //break out of the loop
                        break;
                        }
                        //otherwise check if there is a trap there
                        Trap* checkTrap = getTrap(checkX, checkY);
                        //don't want to blow up the sarcophagus!
                        if(checkTrap != NULL && checkTrap->trapType() != TrapType::SARCOPHAGUS)
                        {
                        //blow up the trap
                        thief->useSpecial(checkX, checkY);
                        //break out of the loop
                        break;
                        }
                    }
                    }
                }
                //if the thief has any movement left
                if(thief->movementLeft() > 0)
                {
                    //find a path from the thief's location to the enemy sarcophagus
                    std::deque<Point> path;
                    int endX = enemySarcophagi[0]->x();
                    int endY = enemySarcophagi[0]->y();
                    path = findPath(Point(thief->x(), thief->y()), Point(endX, endY));
                    //if a path exists then move forward on the path
                    if(path.size() > 0)
                    {
                    thief->move(path[0].x, path[0].y);
                    }  
                }
                }
            }
            //do things with traps now
            std::vector<Trap*> myTraps = getMyTraps();
            for(unsigned i = 0; i < myTraps.size(); ++i)
            {
                const int xChange[] = {-1, 1,  0, 0};
                const int yChange[] = { 0, 0, -1, 1};
                Trap* trap = myTraps[i];
                //make sure trap can be used
                if(trap->active())
                {
                //if trap is a boulder
                if(trap->trapType() == TrapType::BOULDER)
                {
                    //if there is an enemy thief adjancent
                    for(unsigned i = 0; i < 4; ++i)
                    {
                    Thief* enemyThief = getThief(trap->x() + xChange[i], trap->y() + yChange[i]);
                    //roll over the thief
                    if(enemyThief != NULL)
                    {
                        trap->act(xChange[i], yChange[i]);
                        break;
                    }
                    }
                }
                else if(trap->trapType() == TrapType::MUMMY)
                {
                    //move around randomly if a mummy
                    int dir = rand() % 4;
                    int checkX = trap->x() + xChange[dir];
                    int checkY = trap->y() + yChange[dir];
                    Tile* checkTile = getTile(checkX, checkY);
                    Trap* checkTrap = getTrap(checkX, checkY);
                    //if the tile is empty, and there isn't a sarcophagus there
                    if(checkTrap == NULL || checkTrap->trapType() != TrapType::SARCOPHAGUS)
                    {
                    if(checkTile != NULL && checkTile->type() == Tile::EMPTY)
                    {
                        //move on that tile
                        trap->act(checkX, checkY);
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

    public Point() { this.x = -1; this.y = -1; }
    public Point(int x, int y) { this.x = x; this.y = y; }
};