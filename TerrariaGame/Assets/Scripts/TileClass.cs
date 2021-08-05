using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "newtileclass", menuName = "Tile Class")]
public class TileClass : ScriptableObject
{
    public string tileName;
    public Sprite[] tileSprites;
    public bool inBackground = true;

    [Range(0, 1)]
    public float frequency;

}
