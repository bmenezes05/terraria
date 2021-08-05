using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    public PlayerController player;
    public CamController camera;

    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;

    public float seed;
    public BiomeClass[] biomes;

    [Header("Biomes")]
    public float biomeFreq;

    public Gradient biomeGradient;
    public Texture2D biomeMap;

    [Header("Generation Settings")]
    public int chunkSize = 16;

    public int worldSize = 100;
    public int heightAddition = 25;
    public bool generateCaves = true;

    [Header("Noise Settings")]
    public Texture2D caveNoiseTexture;

    public float terrainFreq = 0.05f;
    public float caveFreq = 0.05f;

    [Header("Ore Settings")]
    public OreClass[] ores;

    private GameObject[] worldChunks;

    private List<Vector2> worldTiles = new List<Vector2>();
    private List<GameObject> worldTileObjects = new List<GameObject>();
    private List<TileClass> worldTileClasses = new List<TileClass>();

    private BiomeClass currentBiome;
    private Color[] biomeColors;

    private void Start()
    {
        seed = Random.Range(-10000, 10000);

        biomeColors = new Color[biomes.Length];
        for (int i = 0; i < biomes.Length; i++)
        {
            biomeColors[i] = biomes[i].biomeColor;
            foreach (var ore in biomes[i].ores)
            {
                ore.spreadTexture = new Texture2D(worldSize, worldSize);
            }
        }

        DrawBiomeMap();
        DrawCavesAndOres();

        CreateChunks();
        GenerateTerrain();

        camera.Spawn(new Vector3(player.spawnPos.x, player.spawnPos.y, camera.transform.position.z));
        camera.worldSize = worldSize;
        player.Spawn();
    }

    private void Update()
    {
        RefreshChunks();
    }

    public void RefreshChunks()
    {
        for (int i = 0; i < worldChunks.Length; i++)
        {
            if (Vector2.Distance(new Vector2((i * chunkSize) + (chunkSize / 2), 0), new Vector2(player.transform.position.x, 0)) > Camera.main.orthographicSize * 4f)
                worldChunks[i].SetActive(false);
            else
                worldChunks[i].SetActive(true);
        }
    }

    public void DrawBiomeMap()
    {
        float b;
        biomeMap = new Texture2D(worldSize, worldSize);
        for (int x = 0; x < biomeMap.width; x++)
        {
            for (int y = 0; y < biomeMap.height; y++)
            {
                b = Mathf.PerlinNoise((x + seed) * biomeFreq, (seed) * biomeFreq);
                biomeMap.SetPixel(x, y, biomeGradient.Evaluate(b));
            }
        }

        biomeMap.Apply();
    }

    public void DrawCavesAndOres()
    {
        caveNoiseTexture = new Texture2D(worldSize, worldSize);
        float v;
        float o;
        for (int x = 0; x < caveNoiseTexture.width; x++)
        {
            for (int y = 0; y < caveNoiseTexture.height; y++)
            {
                currentBiome = GetCurrentBiome(x, y);
                v = Mathf.PerlinNoise((x + seed) * caveFreq, (y + seed) * caveFreq);
                if (v > currentBiome.surfaceValue)
                    caveNoiseTexture.SetPixel(x, y, Color.white);
                else
                    caveNoiseTexture.SetPixel(x, y, Color.black);

                //GenerateOres
                for (int i = 0; i < currentBiome.ores.Length; i++)
                {
                    currentBiome.ores[i].spreadTexture.SetPixel(x, y, Color.black);
                    if (currentBiome.ores.Length >= i + 1)
                    {
                        o = Mathf.PerlinNoise((x + seed) * currentBiome.ores[i].frequency, (y + seed) * currentBiome.ores[i].frequency);
                        if (o > currentBiome.ores[i].size)
                            currentBiome.ores[i].spreadTexture.SetPixel(x, y, Color.white);

                        currentBiome.ores[i].spreadTexture.Apply();
                    }
                }
            }
        }

        caveNoiseTexture.Apply();
    }

    public void CreateChunks()
    {
        int numChunks = worldSize / chunkSize;
        worldChunks = new GameObject[numChunks];

        for (int i = 0; i < numChunks; i++)
        {
            GameObject newChunk = new GameObject();
            newChunk.name = i.ToString();
            newChunk.transform.parent = this.transform;
            worldChunks[i] = newChunk;
        }
    }

    public BiomeClass GetCurrentBiome(int x, int y)
    {
        if (System.Array.IndexOf(biomeColors, biomeMap.GetPixel(x, y)) >= 0)
            return biomes[System.Array.IndexOf(biomeColors, biomeMap.GetPixel(x, y))];

        return currentBiome;
    }

    public void GenerateTerrain()
    {
        TileClass tileClass;
        for (int x = 0; x < worldSize; x++)
        {
            float height;

            for (int y = 0; y <= worldSize; y++)
            {
                currentBiome = GetCurrentBiome(x, y);

                height = Mathf.PerlinNoise((x + seed) * terrainFreq, seed * terrainFreq) * currentBiome.heightMultiplier + heightAddition;
                if (x == worldSize / 2)
                    player.spawnPos = new Vector2(x, height + 1);

                if (y >= height)
                    break;

                if (y < height - currentBiome.dirtLayerHeight)
                {
                    tileClass = currentBiome.tileAtlas.stone;

                    foreach (var ore in currentBiome.ores)
                    {
                        if (ore.spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ore.maxSpawnHeight)
                            tileClass = MapOreToTile(ore);
                    }
                }
                else if (y < height - 1)
                {
                    tileClass = currentBiome.tileAtlas.dirt;
                }
                else
                {
                    // top layer of the terrain
                    tileClass = currentBiome.tileAtlas.grass;
                }

                if (generateCaves)
                {
                    if (caveNoiseTexture.GetPixel(x, y).r > 0.5f)
                    {
                        PlaceTile(tileClass, x, y, false);
                    }
                }
                else
                {
                    PlaceTile(tileClass, x, y, false);
                }

                if (y >= height - 1)
                {
                    int chanceTree = Random.Range(0, currentBiome.treeChance);
                    if (chanceTree == 1 && worldTiles.Contains(new Vector2(x, y)))
                    {
                        GenerateTree(Random.Range(currentBiome.minTreeHeight, currentBiome.maxTreeHeight), x, y + 1);
                    }
                    else
                    {
                        int chanceTallGrass = Random.Range(0, currentBiome.tallGrassChance);
                        if (chanceTallGrass == 1 && worldTiles.Contains(new Vector2(x, y)))
                        {
                            if (currentBiome.tileAtlas.tallGrass != null)
                                PlaceTile(currentBiome.tileAtlas.tallGrass, x, y + 1, true);
                        }
                    }
                }
            }
        }
    }

    public void GenerateTree(int treeHeight, int x, int y)
    {
        for (int i = 0; i < treeHeight; i++)
        {
            PlaceTile(tileAtlas.log, x, y + i, true);
        }

        //generate leaves
        PlaceTile(tileAtlas.leaf, x, y + treeHeight, true);
        PlaceTile(tileAtlas.leaf, x, y + treeHeight + 1, true);
        PlaceTile(tileAtlas.leaf, x, y + treeHeight + 2, true);

        PlaceTile(tileAtlas.leaf, x - 1, y + treeHeight, true);
        PlaceTile(tileAtlas.leaf, x - 1, y + treeHeight + 1, true);

        PlaceTile(tileAtlas.leaf, x + 1, y + treeHeight, true);
        PlaceTile(tileAtlas.leaf, x + 1, y + treeHeight + 1, true);
    }

    public void RemoveTile(int x, int y)
    {
        if (worldTiles.Contains(new Vector2Int(x, y)) && x >= 0 && x <= worldSize && y >= 0 && y <= worldSize)
        {
            Destroy(worldTileObjects[worldTiles.IndexOf(new Vector2(x, y))]);

            worldTileObjects.RemoveAt(worldTiles.IndexOf(new Vector2(x, y)));
            worldTileClasses.RemoveAt(worldTiles.IndexOf(new Vector2(x, y)));
            worldTiles.RemoveAt(worldTiles.IndexOf(new Vector2(x, y)));
        }
    }

    public void CheckTile(TileClass tile, int x, int y, bool backgroundElement)
    {
        if (x >= 0 && x <= worldSize && y >= 0 && y <= worldSize)
        {
            if (!worldTiles.Contains(new Vector2Int(x, y)))
            {
                //place the tile regardless
                PlaceTile(tile, x, y, backgroundElement);
            }
            else
            {
                if (worldTileClasses[worldTiles.IndexOf(new Vector2Int(x, y))].inBackground)
                {
                    //overwrite existing tile
                    RemoveTile(x, y);
                    PlaceTile(tile, x, y, backgroundElement);
                }
            }
        }
    }

    public void PlaceTile(TileClass tile, int x, int y, bool backgroundElement)
    {
        if (x >= 0 && x <= worldSize && y >= 0 && y <= worldSize)
        {
            GameObject newTile = new GameObject();

            int chunkCoord = Mathf.RoundToInt(x / chunkSize) * chunkSize;
            chunkCoord /= chunkSize;
            newTile.transform.parent = worldChunks[chunkCoord].transform;

            newTile.AddComponent<SpriteRenderer>();
            if (!backgroundElement)
            {
                newTile.AddComponent<BoxCollider2D>();
                newTile.GetComponent<BoxCollider2D>().size = Vector2.one;
                newTile.tag = "Ground";
            }

            int spriteIndex = Random.Range(0, tile.tileSprites.Length);
            newTile.GetComponent<SpriteRenderer>().sprite = tile.tileSprites[spriteIndex];
            if (tile.inBackground)
                newTile.GetComponent<SpriteRenderer>().sortingOrder = -10;
            else
                newTile.GetComponent<SpriteRenderer>().sortingOrder = -5;
            newTile.name = tile.tileSprites[0].name;
            newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);

            worldTiles.Add(newTile.transform.position - (Vector3.one * 0.5f));
            worldTileObjects.Add(newTile);
            worldTileClasses.Add(tile);
        }
    }

    public TileClass MapOreToTile(OreClass ore)
    {
        return new TileClass()
        {
            name = ore.name,
            tileName = ore.name,
            frequency = ore.frequency,
            tileSprites = ore.tileSprites
        };
    }
}