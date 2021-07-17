using UnityEngine;

[CreateAssetMenu(fileName = "TileAtlas", menuName = "Tile Atlas")]
public class TileAtlas : ScriptableObject
{
    [Header("Environment")]
    public TileClass grass;
    public TileClass dirt;
    public TileClass stone;
    public TileClass log;
    public TileClass leaf;
    public TileClass tallGrass;
    public TileClass sand;
    public TileClass snow;
    public TileClass forest;

    [Header("Environment")]
    public TileClass coal;
    public TileClass iron;
    public TileClass gold;
    public TileClass diamond;

}