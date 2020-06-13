using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Deliver more ore to hq (left side of the map) than your opponent. Use radars to find ore but beware of traps!
 **/
class Player
{
    static void Main(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int width = int.Parse(inputs[0]);
        int height = int.Parse(inputs[1]); // size of the map

        Game game = new Game(width, height);
        int robotIdStart = -1;

        // game loop
        while (true)
        {
            game.createPersonalities();
            inputs = Console.ReadLine().Split(' ');
            game.MyScore = int.Parse(inputs[0]); // Amount of ore delivered
            int opponentScore = int.Parse(inputs[1]);
            for (int i = 0; i < height; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                for (int j = 0; j < width; j++)
                {
                    string ore = inputs[2*j];// amount of ore or "?" if unknown
                    int hole = int.Parse(inputs[2*j+1]);// 1 if cell has a hole
                    //Console.Error.WriteLine(ore);
                    game.Cells[j, i].Update(ore, hole);
                }
            }
            inputs = Console.ReadLine().Split(' ');
            int entityCount = int.Parse(inputs[0]); // number of entities visible to you
            game.RadarCooldown = int.Parse(inputs[1]); // turns left until a new radar can be requested
            game.TrapCooldown = int.Parse(inputs[2]); // turns left until a new trap can be requested
            game.MyRadars.Clear();
            game.MyTraps.Clear();
            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int entityId = int.Parse(inputs[0]); // unique id of the entity
                int entityType = int.Parse(inputs[1]); // 0 for your robot, 1 for other robot, 2 for radar, 3 for trap
                int x = int.Parse(inputs[2]);
                int y = int.Parse(inputs[3]); // position of the entity
                int item = int.Parse(inputs[4]); // if this entity is a robot, the item it is carrying (-1 for NONE, 2 for RADAR, 3 for TRAP, 4 for ORE)
                if (entityType == 0)
                {
                    if (robotIdStart == -1) 
                    {
                        robotIdStart = entityId;
                    }

                    if (x == -1) 
                    {
                        game.robots[entityId - robotIdStart].Dead = true;
                    } else {
                        game.robots[entityId - robotIdStart].updateStatus(game.Cells[x, y], item);
                        game.robots[entityId - robotIdStart].Index = entityId - robotIdStart;
                    }
                }
                else if (entityType == 1 && x != -1) 
                {
                    if (game.Enemies.ContainsKey(entityId))
                    {
                        game.Enemies[entityId].xPositions.Add(x);
                        game.Enemies[entityId].yPositions.Add(y);
                    }
                    else 
                    {
                        game.Enemies.Add(entityId, new Enemy());
                    }
                }
                else if (entityType == 2) 
                {
                    game.MyRadars.Add(game.Cells[x, y]);

                }
                else if (entityType == 3) 
                {
                    game.MyTraps.Add(game.Cells[x, y]);
                }
            }
            foreach (Cell h in game.SafeHoles ){
               // Console.Error.WriteLine("safe:" + h.x + " " + h.y);
            }
            game.inferDanger();
            for (int i = 0; i < 5; i++)
            {

                // Write an action using Console.WriteLine()
                // To debug: Console.Error.WriteLine("Debug messages...");
                if (!game.robots[i].Dead)
                    Console.WriteLine(game.robots[i].Action(game.Cells, game)); // WAIT|MOVE x y|DIG x y|REQUEST item
                else
                    Console.WriteLine("WAIT I'm dead :(");

            }
        }
    }
}

interface Robot
{
    void updateStatus(Cell cell, int _item);
    bool Dead { get; set;}
    int Index { get; set;}
    Cell position { get; set; }
    string Action(Cell[,] cells, Game game);

}
class BasicRobot : Robot
{
    public int item;
    public int x;
    public int y;
    public int Index { get; set;}
    public bool Dead {get; set;}
    public Cell position {get; set;} = new Cell(0, 0);
    
    public void updateStatus(Cell cell, int _item)
    {
        item = _item;
        position = cell;
        x = cell.x;
        y = cell.y;
    }
    public virtual string Action(Cell[,] cells, Game game)
    {
        return "WAIT";
    }

}

class Scout : BasicRobot
{
    Random rnd = new Random();
    private int xTarget;
    private int yTarget;
    public Scout()  { }
    public virtual int Cost (Game game, int i, int j)
    {
        int localCost = Int32.MaxValue;
        for(int _i = Math.Max(0, i - 4); _i < Math.Min(game.Width, i + 4); _i++)
        {
            for(int _j = Math.Max(0, j - 4); _j < Math.Min(game.Height, j + 4); _j++)
            {
                if (game.Cells[_i, _j].Known == false ) {
                    localCost -= 100;
                }
            }
        }

        if ((!game.Cells[i, j].Hole || game.SafeHoles.Contains(game.Cells[i,j])))
            localCost += 10000;

        if (i < 4)
            localCost += 10000;

        if (game.Cells[i, j].Suspicious)
            localCost = Math.Max(Int32.MaxValue, localCost + 1000);

        if (game.MyTraps.Contains(game.Cells[i, j]))
            localCost = Int32.MaxValue;

        return localCost;

    }
    private Cell findTarget(Game game) 
    {
        int minCost = Int32.MaxValue;
        for(int i = 1; i < game.Cells.GetLength(0); i++)
        {
            for(int j = 0; j < game.Cells.GetLength(1); j++)
            {
                int localCost = Cost(game, i, j);
                if (localCost < minCost) 
                {
                    minCost = localCost;
                    xTarget = i;
                    yTarget = j;
                }
            }
        }
        return new Cell(xTarget, yTarget);
        xTarget = rnd.Next(4, game.Cells.GetLength(0) - 4);
        yTarget = rnd.Next(1, game.Cells.GetLength(1));
        return new Cell(xTarget, yTarget);
        throw new System.InvalidOperationException("Couldn't find Scout target");
    }
    public override string Action(Cell[,] cells, Game game)
    {
        Cell Target = findTarget(game);
        xTarget = Target.x;
        yTarget = Target.y;
        if (item != 2 && x == 0)
        {
            return "REQUEST RADAR";

        }
        else if (item != 2)
        {
            return "MOVE 0 " + y;
        }
        else if (x == xTarget && y == yTarget)
        {
            game.SafeHoles.Add(new Cell(xTarget, yTarget));
            return "DIG " + xTarget +" " + yTarget;
        }

        return "MOVE " + xTarget +" " + yTarget;
    }

}

class Miner : BasicRobot
{
    private int xTarget;
    private int yTarget;
    
    public virtual int Cost (Game game, int i, int j)
    {
        int localCost = Int32.MaxValue;
        if ((!game.Cells[i, j].Hole || game.SafeHoles.Contains(game.Cells[i,j]))
        && i > 8)
            localCost = 10000 + (2 * position.Distance(game.Cells[i, j])) - (10 * x);

        if (game.Cells[i, j].Ore > 0 
        && (!game.Cells[i, j].Hole || game.SafeHoles.Contains(game.Cells[i, j]))
        && !game.MyTraps.Contains(game.Cells[i, j])) {
            localCost = 2 * position.Distance(game.Cells[i,j]);
            localCost += game.Cells[i,j].GetHashCode() % (Index + 1);
        }
        
        if (game.Cells[i, j].Suspicious)
            localCost = Math.Max(Int32.MaxValue, localCost + 1000);

        if (game.MyTraps.Contains(game.Cells[i, j]))
            localCost = Int32.MaxValue;

        return localCost;


    }
    public override string Action(Cell[,] cells, Game game)
    {
        if (Dead)
            return "WAIT";
        Random rnd = new Random();
        bool FoundTarget = false; 
        int minCost = Int32.MaxValue;
        for(int i = 1; i < cells.GetLength(0); i++)
        {
            for(int j = 0; j < cells.GetLength(1); j++)
            {
                if (game.Cells[i, j].Suspicious)
                    Console.Error.WriteLine("Suspect " + i + " " + j);
                int localCost = Cost(game, i, j);
                if (localCost < minCost) 
                {
                    minCost = localCost;
                    xTarget = i;
                    yTarget = j;
                    FoundTarget = true;
                }
            }
        }


        if (item == 4)
        {
            return "MOVE 0 " + y;
        }
        else if (game.TrapCooldown == 0 && x == 0 && !(game.robots.Count(r => r.position.x == 0) > 1))
        {
            return "REQUEST TRAP";
        }
        else if (game.RadarCooldown == 0 && x == 0 && !(game.robots.Count(r => r.position.x == 0) > 1))
        {
            return "REQUEST RADAR";
        }
        else if (position.Distance(cells[x, y]) > 3) 
        {
            return "MOVE " + xTarget +" " + yTarget;
        }
        else 
        {
            game.SafeHoles.Add(new Cell(xTarget, yTarget));
            return "DIG " + xTarget +" " + yTarget;
        }
    }

}
class Enemy 
{
    public List<int> xPositions = new List<int>();
    public List<int> yPositions = new List<int>();

}
class AssholeMiner : Miner
{
    private int xTarget;
    private int yTarget;
    private bool plantedMine;
    
    public override int Cost (Game game, int i, int j)
    {
        int localCost = Int32.MaxValue;
        if ((!game.Cells[i, j].Hole || game.SafeHoles.Contains(game.Cells[i,j]))
        && i > 8)
            localCost = 10000 + (2 * position.Distance(game.Cells[i, j])) - (10 * x);

        if (game.Cells[i, j].Ore > 0 
        && (!game.Cells[i, j].Hole || game.SafeHoles.Contains(game.Cells[i, j]))
        && !game.MyTraps.Contains(game.Cells[i, j])) {
            localCost = 2 * position.Distance(game.Cells[i,j]);
            localCost += game.Cells[i,j].GetHashCode() % (Index + 1);
        }

        if (!game.Cells[i, j].Hole)
            localCost -= 10;

        if (game.Cells[i, j].Suspicious)
            localCost = Math.Max(Int32.MaxValue, localCost + 1000);

        if (game.MyTraps.Contains(game.Cells[i, j]))
            localCost = Int32.MaxValue;

        return localCost;

    }

}
class Cell : IEquatable<Cell>
{
    public int Ore { get; set; }
    public bool Hole { get; set; }
    public bool Known { get; set; }
    public bool Suspicious { get; set; } = false;
    public int x;
    public int y;

    public Cell(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    public int Distance(Cell c)
    {
        return Math.Abs(x - c.x) + Math.Abs(y - c.y);
    }
    public bool Equals (Cell c)
    {
        return (c.x == x && c.y == y);
    }

    public int DistanceToClosestRadar(Game g)
    {
        int minDist = 99999999;
        g.MyRadars.ForEach(r => {
            if ( Math.Abs(x - r.x) + Math.Abs(y - r.y) < minDist) 
            {
                minDist = Math.Abs(x - r.x) + Math.Abs(y - r.y);
            }
        });
        return minDist;
    }

    public void Update(string ore, int hole)
    {
        Hole = hole == 1;
        Known = !"?".Equals(ore);
        if (Known) 
        {
            Ore = int.Parse(ore);
        }

    }
    public override int GetHashCode()
    {
        return 1 + 31 * (31 + x) + y;
    }

}

class Game
{
    public readonly int Width;
    public readonly int Height;
    public Cell[,] Cells { get; set;}
    public List<Cell> MyRadars = new List<Cell>();
    public List<Cell> SafeHoles = new List<Cell>();
    public List<Cell> MyTraps = new List<Cell>();
    public Dictionary<int, Enemy> Enemies = new Dictionary<int, Enemy>();
    public int TrapCooldown;
    public int MyScore;
    public int RadarCooldown;

    public List<Robot> robots = new List<Robot>();
    public void inferDanger()
    {
        foreach ( Enemy e in Enemies.Values) 
        {
            if (e.xPositions.Count < 3)
                break;
            
            bool armed = false;
            for (int i = 1; i < e.xPositions.Count - 1; i++)
            {


                if (armed && e.xPositions[i] == e.xPositions[i - 1] && e.yPositions[i] == e.yPositions[i - 1])
                {
 //                   Console.Error.WriteLine("danger at " + e.xPositions[i] + " " + e.yPositions[i]);
                    armed = false;

                    Cells[e.xPositions[i], e.yPositions[i]].Suspicious = Cells[e.xPositions[i], e.yPositions[i]].Hole;
                    Cells[Math.Max(0, e.xPositions[i] - 1), e.yPositions[i]].Suspicious = Cells[Math.Max(0, e.xPositions[i] - 1), e.yPositions[i]].Hole;;
                    Cells[Math.Min(Width - 1, e.xPositions[i] + 1), e.yPositions[i]].Suspicious = Cells[Math.Min(Width - 1, e.xPositions[i] + 1), e.yPositions[i]].Hole;
                    Cells[e.xPositions[i], Math.Min(Height - 1, e.yPositions[i] + 1)].Suspicious = Cells[e.xPositions[i], Math.Min(Height - 1, e.yPositions[i] + 1)].Hole;
                    Cells[e.xPositions[i], Math.Max(0, e.yPositions[i] - 1)].Suspicious = Cells[e.xPositions[i], Math.Max(0, e.yPositions[i] - 1)].Hole;
                }

                if (e.xPositions[i] == 0 && e.xPositions[i - 1] == 0)
                {
                    armed = true; // danger if got something at base 
//                    Console.Error.WriteLine("Enemy got armed");
                }

            }
        }

    }
    public void createPersonalities()
    {
        robots.Clear();
        int VisibleOre = 0;
        foreach (Cell c in Cells) {
            if (!MyTraps.Contains(c) && (SafeHoles.Contains(c) || c.Hole == false))
            {
                VisibleOre += c.Ore;
            }
        }
        Console.Error.WriteLine("visible Ore:" + VisibleOre);
        if (MyScore < 10) {
            robots.Add(new Scout());
            robots.Add(new Miner());
            robots.Add(new AssholeMiner());
            robots.Add(new AssholeMiner());
            robots.Add(new AssholeMiner());
        }
        else if (VisibleOre < 10) {
            robots.Add(new Scout());
            robots.Add(new Miner());
            robots.Add(new AssholeMiner());
            robots.Add(new AssholeMiner());
            robots.Add(new AssholeMiner());
        }
        else if (MyScore > 55) {
            robots.Add(new Miner());
            robots.Add(new Miner());
            robots.Add(new Miner());
            robots.Add(new Miner());
            robots.Add(new Miner());
        }
        else 
        {
            robots.Add(new Miner());
            robots.Add(new AssholeMiner());
            robots.Add(new AssholeMiner());
            robots.Add(new AssholeMiner());
            robots.Add(new AssholeMiner());
        }

    }
    public Game(int width, int height)
    {
        Width = width;
        Height = height;
        Cells = new Cell[width, height];

        for (int x = 0; x < width; ++x)
        {
            for(int y = 0; y < height; ++y)
            {
                Cells[x, y] = new Cell(x, y);
            }
        }

        createPersonalities();
    }
}
