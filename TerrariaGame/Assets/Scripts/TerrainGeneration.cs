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

    [Header("Trees")]
    public int treeChance = 10;

    public int minTreeHeight = 4;
    public int maxTreeHeight = 6;

    [Header("Addons")]
    public int tallGrassChance = 10;

    [Header("Generation Settings")]
    public int chunkSize = 16;

    public int worldSize = 100;
    public int heightAddition = 25;
    public bool generateCaves = true;
    public int dirtLayerHeight = 5;
    public float surfaceValue = 0.25f;
    public float heightMultiplier = 4f;

    [Header("Noise Settings")]
    public float terrainFreq = 0.05f;

    public float caveFreq = 0.05f;
    public Texture2D caveNoiseTexture;

    [Header("Ore Settings")]
    public OreClass[] ores;

    private BiomeClass currentBiome;
    private GameObject[] worldChunks;
    private List<Vector2> worldTiles = new List<Vector2>();
    public Color[] biomeColors;

    private void OnValidate()
    {
        biomeColors = new Color[biomes.Length];
        for (int i = 0; i < biomeColors.Length; i++)
        {
            biomeColors[i] = biomes[i].biomeColor;
        }

        DrawTextures();
    }

    private void Start()
    {
        seed = Random.Range(-10000, 10000);
        DrawTextures();
        CreateChunks();
        GenerateTerrain();
    }

    public void DrawTextures()
    {
        biomeMap = new Texture2D(worldSize, worldSize);
        DrawBiomeTexture();

        for (int i = 0; i < biomes.Length; i++)
        {
            //Generate Caves
            biomes[i].caveNoiseTexture = new Texture2D(worldSize, worldSize);
            GenerateNoiseTexture(biomes[i].caveFreq, biomes[i].surfaceValue, biomes[i].caveNoiseTexture);

            //Generate Ores
            for (int o = 0; o < biomes[i].ores.Length; o++)
            {
                biomes[i].ores[o].spreadTexture = new Texture2D(worldSize, worldSize);
                GenerateNoiseTexture(biomes[i].ores[o].rarity, biomes[i].ores[o].size, biomes[i].ores[o].spreadTexture);
            }
        }
    }

    public void DrawBiomeTexture()
    {
        for (int x = 0; x < biomeMap.width; x++)
        {
            for (int y = 0; y < biomeMap.width; y++)
            {
                float v = Mathf.PerlinNoise((x + seed) * biomeFreq, (seed) * biomeFreq);
                biomeMap.SetPixel(x, y, biomeGradient.Evaluate(v));
            }
        }

        biomeMap.Apply();
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
        float v = Mathf.PerlinNoise((x + seed) * biomeFreq, (seed) * biomeFreq);
        for (int i = 0; i < biomes.Length; i++)
        {
            if (biomes[i].biomeColor == biomeMap.GetPixel(x, y))
            {
                return biomes[i];
            }
        }

        return currentBiome;
    }

    public void GenerateTerrain()
    {
        Sprite[] tileSprites;
        for (int x = 0; x < worldSize; x++)
        {
            currentBiome = GetCurrentBiome(x, 0);
            float height = Mathf.PerlinNoise((x + seed) * currentBiome.terrainFreq, seed * currentBiome.terrainFreq) * currentBiome.heightMultiplier + heightAddition;

            for (int y = 0; y <= height; y++)
            {
                currentBiome = GetCurrentBiome(x, y);

                tileSprites = currentBiome.tileAtlas.stone.tileSprites;

                if (y < height - dirtLayerHeight)
                {
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

    public void GenerateNoiseTexture(float frequency, float limit, Texture2D noiseTexture)
    {
        for (int x = 0; x < noiseTexture.width; x++)
        {
            for (int y = 0; y < noiseTexture.width; y++)
            {
                float v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);
                if (v > limit)
                    noiseTexture.SetPixel(x, y, Color.white);
                else
                    noiseTexture.SetPixel(x, y, Color.black);
            }
        }

        noiseTexture.Apply();
    }
}