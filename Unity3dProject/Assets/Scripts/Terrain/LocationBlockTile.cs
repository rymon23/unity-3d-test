using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LocationBlockTileSize { Small = 0, Medium = 1, Large = 2 }

public class LocationBlockTile : MonoBehaviour
{
    public LocationBlockTileSize _tileSize;
    public Vector3 _center;
    public float _radius;
    public LocationBlockTile(Vector3 center, LocationBlockTileSize tileSize, float radius)
    {
        _center = center;
        _tileSize = tileSize;
        _tileSize = tileSize;
        _radius = radius;
    }
}
