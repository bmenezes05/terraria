using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
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

    private BiomeClass currentBiome;
    private GameObject[] worldChunks;
    private List<Vector2> worldTiles = new List<Vector2>();
    private Color[] biomeColors;

    private void Start()
    {
        seed = Random.Range(-10000, 10000);

        for (int i = 0; i < ores.Length; i++)
        {
            ores[i].spreadTexture = new Texture2D(worldSize, worldSize);
        }

        biomeColors = new Color[biomes.Length];
        for (int i = 0; i < biomes.Length; i++)
        {
            biomeColors[i] = biomes[i].biomeColor;
        }
        
        DrawBiomeMap();
        DrawCavesAndOres();

        CreateChunks();
        GenerateTerrain();
    }

    //private void OnValidate()
    //{
    //    DrawTextures();
    //    DrawCavesAndOres();
    //}

    public void DrawBiomeMap()
    {
        float b;        
        biomeMap = new Texture2D(worldSize, worldSize);
        for (int x = 0; x < biomeMap.width; x++)
        {
            for (int y = 0; y < biomeMap.width; y++)
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

                for (int i = 0; i < ores.Length; i++)
                {
                    ores[i].spreadTexture.SetPixel(x, y, Color.black);
                    if (currentBiome.ores.Length >= i + 1)
                    {
                        o = Mathf.PerlinNoise((x + seed) * currentBiome.ores[i].frequency, (y + seed) * currentBiome.ores[i].frequency);
                        if (o > currentBiome.ores[i].size)
                            ores[i].spreadTexture.SetPixel(x, y, Color.white);

                        ores[i].spreadTexture.Apply();
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
        if(System.Array.IndexOf(biomeColors, biomeMap.GetPixel(x, y)) >= 0)
            return biomes[System.Array.IndexOf(biomeColors, biomeMap.GetPixel(x, y))];        

        return currentBiome;
    }

    public void GenerateTerrain()
    {
        Sprite[] tileSprites;
        for (int x = 0; x < worldSize; x++)
        {
            float height;

            for (int y = 0; y <= worldSize; y++)
            {
                currentBiome = GetCurrentBiome(x, y);
                height = Mathf.PerlinNoise((x + seed) * terrainFreq, seed * terrainFreq) * currentBiome.heightMultiplier + heightAddition;

                if (y >= height)
                    break;

                if (y < height - currentBiome.dirtLayerHeight)
                {
                    tileSprites = currentBiome.tileAtlas.stone.tileSprites;

                    if (ores[0].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[0].maxSpawnHeight)
                        tileSprites = currentBiome.tileAtlas.coal.tileSprites;
                    if (ores[1].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[1].maxSpawnHeight)
                        tileSprites = currentBiome.tileAtlas.iron.tileSprites;
                    if (ores[2].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[2].maxSpawnHeight)
                        tileSprites = currentBiome.tileAtlas.gold.tileSprites;
                    if (ores[3].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[3].maxSpawnHeight)
                        tileSprites = currentBiome.tileAtlas.diamond.tileSprites;
                }
                else if (y < height - 1)
                {
                    tileSprites = currentBiome.tileAtlas.dirt.tileSprites;
                }
                else
                {
                    // top layer of the terrain
                    tileSprites = currentBiome.tileAtlas.grass.tileSprites;
                }

                if (generateCaves)
                {
                    if (caveNoiseTexture.GetPixel(x, y).r > 0.5f)
                    {
                        PlaceTile(tileSprites, x, y);
                    }
                }
                else
                {
                    PlaceTile(tileSprites, x, y);
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
                                PlaceTile(currentBiome.tileAtlas.tallGrass.tileSprites, x, y + 1);
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
            PlaceTile(tileAtlas.log.tileSprites, x, y + i);
        }

        //generate leaves
        PlaceTile(tileAtlas.leaf.tileSprites, x, y + treeHeight);
        PlaceTile(tileAtlas.leaf.tileSprites, x, y + treeHeight + 1);
        PlaceTile(tileAtlas.leaf.tileSprites, x, y + treeHeight + 2);

        PlaceTile(tileAtlas.leaf.tileSprites, x - 1, y + treeHeight);
        PlaceTile(tileAtlas.leaf.tileSprites, x - 1, y + treeHeight + 1);

        PlaceTile(tileAtlas.leaf.tileSprites, x + 1, y + treeHeight);
        PlaceTile(tileAtlas.leaf.tileSprites, x + 1, y + treeHeight + 1);
    }

    public void PlaceTile(Sprite[] tileSprites, int x, int y)
    {
        if (!worldTiles.Contains(new Vector2Int(x, y)))
        {
            GameObject newTile = new GameObject();

            int chunkCoord = Mathf.RoundToInt(x / chunkSize) * chunkSize;
            chunkCoord /= chunkSize;
            newTile.transform.parent = worldChunks[chunkCoord].transform;

            newTile.AddComponent<SpriteRenderer>();

            int spriteIndex = Random.Range(0, tileSprites.Length);

            newTile.GetComponent<SpriteRenderer>().sprite = tileSprites[spriteIndex];
            newTile.name = tileSprites[0].name;
            newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);

            worldTiles.Add(newTile.transform.position - (Vector3.one * 0.5f));
        }
    }

}