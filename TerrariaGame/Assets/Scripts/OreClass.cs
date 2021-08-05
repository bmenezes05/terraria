using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Ore", menuName = "Ore")]
public class OreClass : ScriptableObject
{
    public string tileName;

    [Range(0, 1)]
    public float frequency;

    [Range(0, 1)]
    public float size;

    public int maxSpawnHeight;

    public Texture2D spreadTexture;

    public Sprite[] tileSprites;
}