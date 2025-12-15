using System;
using System.Collections.Generic;
using Junya;

[Serializable]
public class ConstellationData
{
    public string constellationName;
    public string createdAt;
    public List<StarData> stars = new List<StarData>();
    public List<ConnectionData> connections = new List<ConnectionData>();
    public Node<String> root = new Node<String>();

}

[Serializable]
public class StarData
{
    public int id;
    public float x;
    public float y;
    public float scale;
}

[Serializable]
public class ConnectionData
{
    public int fromStarId;
    public int toStarId;
}