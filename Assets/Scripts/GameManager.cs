using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 50;
    public int height = 30;
    public float updateTime = 0.1f;

    [Header("Arena")]
    [Range(0f, 1f)] public float initialDensity = 0.15f;
    [Range(0f, 1f)] public float seedHeightRatio = 0.3f;

    private bool[,] grid;
    private float timer;
    private int generation;
    private bool isPaused = false;
    private Texture2D texture;
    private Color32[] pixels;

    private static readonly Color32 SandColor = new Color32(194, 154, 82, 255);
    private static readonly Color32 EmptyColor = new Color32(20, 20, 25, 255);

    void Start()
    {
        grid = new bool[width, height];

        InputManager.Instance.OnPause += TogglePause;
        InputManager.Instance.OnRestart += RestartSimulation;
        InputManager.Instance.OnClear += ClearSimulation;
        InputManager.Instance.OnToggleCell += ToggleCellInput;

        BuildTexture();
        SeedSand();
    }

    void Update()
    {
        if (isPaused) return;

        timer += Time.deltaTime;
        if (timer >= updateTime)
        {
            Step();
            UpdateVisuals();
            timer = 0f;
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(10, 10, 400, 30), $"Dia {generation}   |   Granos de arena: {CountGrains()}", style);
    }

    void TogglePause()
    {
        isPaused = !isPaused;
        Debug.Log(isPaused ? "Simulación pausada" : "Simulación reanudada");
    }

    void ToggleCellInput()
    {
        if (Mouse.current != null)
        {
            HandleMouseClick();
            return;
        }

        Vector3 camPos = Camera.main.transform.position;
        ToggleCellAtWorld(camPos);
    }

    void ClearSimulation()
    {
        Debug.Log("Limpiando simulación...");
        ClearGrid();
        generation = 0;
        timer = 0f;
    }

    void RestartSimulation()
    {
        Debug.Log("Reiniciando simulación...");
        SeedSand();
        timer = 0f;
    }

    void BuildTexture()
    {
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        pixels = new Color32[width * height];

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.zero, 1f);

        SpriteRenderer rend = GetComponent<SpriteRenderer>();
        if (rend == null) rend = gameObject.AddComponent<SpriteRenderer>();
        rend.sprite = sprite;
    }

    public void ClearGrid()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = false;

        UpdateVisuals();
    }

    void SeedSand()
    {
        ClearGrid();

        int seedRows = Mathf.CeilToInt(height * seedHeightRatio);
        for (int x = 0; x < width; x++)
            for (int y = height - seedRows; y < height; y++)
                grid[x, y] = Random.value < initialDensity;

        generation = 0;
        UpdateVisuals();
    }

    // De abajo hacia arriba: asi cada grano se mueve maximo una celda por tick.
    void Step()
    {
        generation++;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!grid[x, y] || y == 0) continue;

                if (!grid[x, y - 1])
                {
                    MoveGrain(x, y, x, y - 1);
                    continue;
                }

                bool leftFree = x > 0 && !grid[x - 1, y - 1];
                bool rightFree = x < width - 1 && !grid[x + 1, y - 1];

                if (leftFree && rightFree)
                    MoveGrain(x, y, Random.value < 0.5f ? x - 1 : x + 1, y - 1);
                else if (leftFree)
                    MoveGrain(x, y, x - 1, y - 1);
                else if (rightFree)
                    MoveGrain(x, y, x + 1, y - 1);
            }
        }

        Debug.Log($"Día {generation}: {CountGrains()} granos de arena en la grilla");
    }

    void MoveGrain(int fromX, int fromY, int toX, int toY)
    {
        grid[fromX, fromY] = false;
        grid[toX, toY] = true;
    }

    int CountGrains()
    {
        int count = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y]) count++;
        return count;
    }

    void HandleMouseClick()
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        ToggleCellAtWorld(worldPos);
    }

    void ToggleCellAtWorld(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(worldPos.y);

        if (x < 0 || x >= width || y < 0 || y >= height) return;

        grid[x, y] = !grid[x, y];
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
                pixels[row + x] = grid[x, y] ? SandColor : EmptyColor;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
    }
}