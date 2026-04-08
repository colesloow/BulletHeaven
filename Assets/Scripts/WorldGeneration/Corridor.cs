using UnityEngine;

public enum CorridorType
{
    Straight,
    Corner,
    Junction,      // T-shape: 3 sockets
    Intersection,  // cross: 4 sockets
    End,           // single socket, dead-end cap
    Transition     // changes corridor width (4 to 8 units)
}

public class Corridor : DungeonPiece
{
    public CorridorType Type;
}
