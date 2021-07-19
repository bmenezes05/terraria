using System;
using UnityEngine;

[Serializable]
public class OreClass
{
    public string name;            

    [Range(0, 1)]
    public float frequency;

    [Range(0, 1)]
    public float size;

    public int maxSpawnHeight;

    public Texture2D spreadTexture;
    
    public Sprite[] tileSprites;
}