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
            int myScore = int.Parse(inputs[0]); // Amount of ore delivered
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
                else if (entityType == 1) 
                {
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
    string Action(Cell[,] cells, Game game);

}
class BasicRobot : Robot
{
    public int item;
    public int x;
    public int y;
    public int Index { get; set;}
    public bool Dead {get; set;}
    public Cell position;
    
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
    private Cell findTarget(Game game) 
    {
        for(int i = 6; i < game.Cells.GetLength(0) - 3; i++)
        {
            for(int j = 4; j < game.Cells.GetLength(1) - 3; j++)
            {
             //   Console.Error.WriteLine("may" + new Cell(i, j).DistanceToClosestRadar(game));
                if (game.Cells[i, j].Hole == false && new Cell(i, j).DistanceToClosestRadar(game) > 8) {
                    xTarget = i;
                    yTarget = j;
                    Console.Error.WriteLine("once");
                    return new Cell(i,j);
                }
            }
        }
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
            return "DIG " + xTarget +" " + yTarget;
        }

        return "MOVE " + xTarget +" " + yTarget;
    }

}

class AssholeMiner : BasicRobot
{
    private int xTarget;
    private int yTarget;
    private bool plantedMine;
    
    public AssholeMiner () { }
    public override string Action(Cell[,] cells, Game game)
    {
        if (Dead)
            return "WAIT";
        Random rnd = new Random();
        xTarget = rnd.Next(20, cells.GetLength(0));
        yTarget = rnd.Next(1, cells.GetLength(1));
        bool FoundTarget = false; 
        int minDist = 99999999;
        for(int i = 0; i < cells.GetLength(0); i++)
        {
            for(int j = 0; j < cells.GetLength(1); j++)
            {
                if (cells[i, j].Ore > 0 
                && (!game.Cells[i, j].Hole || game.SafeHoles.Contains(cells[i, j]))
                && position.Distance(cells[i, j]) < minDist 
                && !game.MyTraps.Contains(cells[i, j])) {
                    xTarget = i;
                    yTarget = j;
                    minDist = position.Distance(cells[i,j]);
                    Console.Error.WriteLine("Found target" + Index +" " + i + " " + j);
                    FoundTarget = true;
                }
            }
        }
        Cell res =
            from c in game.Cells.Cast<Cell>()


        if (!FoundTarget && x > 20) 
        {
            xTarget = x + rnd.Next(1, 3) - 2;
            yTarget = y + rnd.Next(1, 3) - 2;
            FoundTarget = true;
        }

        if (item == 4)
        {
            return "MOVE 0 " + y;
        }
        else if (game.TrapCooldown == 0 && x == 0)
        {
            return "REQUEST TRAP";
        }
        else if (game.RadarCooldown == 0 && x == 0)
        {
            return "REQUEST RADAR";
        }
        else if (!FoundTarget && x > 5 && !game.Cells[x, y].Hole) 
        {
            game.SafeHoles.Add(new Cell(x, y));
            return "DIG " + x +" " + y + " digging out of boredom";
        }
        else if (position.Distance(cells[x, y]) > 3) 
        {
            return "MOVE " + xTarget +" " + yTarget;
        }
        else if (!game.Cells[x, y].Hole || game.SafeHoles.Contains(position))
        {
            game.SafeHoles.Add(new Cell(x, y));
            return "DIG " + xTarget +" " + yTarget;
        }
        else 
        {
            return "MOVE " + xTarget +" " + yTarget;
        }
    }

}
class Miner : BasicRobot
{
    private int xTarget;
    private int yTarget;
    private bool plantedMine;
    
    public override string Action(Cell[,] cells, Game game)
    {
        Random rnd = new Random();
        xTarget = rnd.Next(20, cells.GetLength(0));
        yTarget = rnd.Next(1, cells.GetLength(1));
        bool FoundTarget = false; 
        int minDist = 99999999;
        for(int i = 0; i < cells.GetLength(0); i++)
        {
            for(int j = cells.GetLength(1) - 1; j > 0; j--)
            {
                if (cells[i, j].Ore > 0 
                && position.Distance(cells[i, j]) < minDist 
                && (!game.Cells[x, y].Hole || game.SafeHoles.Contains(cells[i, j]))
                && !game.MyTraps.Contains(cells[i, j])) {
                    xTarget = i;
                    yTarget = j;
                    minDist = position.Distance(cells[i,j]);
                    FoundTarget = true;
                }
            }
        }

        if (item == 4)
        {
            return "MOVE 0 " + y;
        }
        else if (game.RadarCooldown == 0 && x == 0)
        {
            return "REQUEST RADAR";
        }
        else if (!FoundTarget && x > 5 && !game.Cells[x, y].Hole) 
        {
            game.SafeHoles.Add(new Cell(x, y));
            return "DIG " + x +" " + y;
        }
        else if (position.Distance(cells[x, y]) > 3) 
        {
            return "MOVE " + xTarget +" " + yTarget;
        }
        else if (!game.Cells[x, y].Hole || game.SafeHoles.Contains(position))
        {
            game.SafeHoles.Add(new Cell(x, y));
            return "DIG " + xTarget +" " + yTarget;
        }
        else 
        {
            return "MOVE " + xTarget +" " + yTarget;
            return "WAIT nothing to do";
        }
    }

}
class Cell
{
    public int Ore { get; set; }
    public bool Hole { get; set; }
    public bool Known { get; set; }
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

}

class Game
{
    public readonly int Width;
    public readonly int Height;
    public Cell[,] Cells { get; set;}
    public List<Cell> MyRadars = new List<Cell>();
    public List<Cell> SafeHoles = new List<Cell>();
    public List<Cell> MyTraps = new List<Cell>();
    public int TrapCooldown;
    public int RadarCooldown;

    public List<Robot> robots = new List<Robot>();
    public List<Cell> getFlattenCells () 
    {
        return Cells.Cast<List<Cell>>();
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
        if (VisibleOre < 10) {
            robots.Add(new Scout());
            robots.Add(new AssholeMiner());
            robots.Add(new AssholeMiner());
            robots.Add(new AssholeMiner());
            robots.Add(new AssholeMiner());
        }
        else 
        {
            robots.Add(new AssholeMiner());
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
