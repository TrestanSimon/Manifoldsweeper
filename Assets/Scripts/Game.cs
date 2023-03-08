using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Game : MonoBehaviour {
    private Complex complex;
    private Quad[,] quads;
    private Quad mouseOver;
    private int ResU, ResV;
    private int mineCount;

    private Camera cam;
    private CameraHandler cameraHandler;

    private Material materialUknown;
    private Material materialEmpty;
    private Material materialMine;
    private Material materialExploded;
    private Material materialFlag;
    private Material materialNum1;
    private Material materialNum2;
    private Material materialNum3;
    private Material materialNum4;
    private Material materialNum5;
    private Material materialNum6;
    private Material materialNum7;
    private Material materialNum8;
    private GameObject flagPrefab;
    private Dictionary<Vector2Int, GameObject[]> flags = new Dictionary<Vector2Int, GameObject[]>();
    private GameObject breakPS;

    private bool gameon = false;
    private bool gameover = false;

    private List<Coroutine> coroutinePropagate = new List<Coroutine>();

    private void Awake() {
        // Load materials
        materialUknown = Resources.Load("Materials/DesertMats/TileUnknown", typeof(Material)) as Material;
        materialEmpty = Resources.Load("Materials/DesertMats/TileEmpty", typeof(Material)) as Material;
        materialMine = Resources.Load("Materials/DesertMats/TileMine", typeof(Material)) as Material;
        materialExploded = Resources.Load("Materials/DesertMats/TileExploded", typeof(Material)) as Material;
        materialFlag = Resources.Load("Materials/DesertMats/TileNo", typeof(Material)) as Material;
        materialNum1 = Resources.Load("Materials/DesertMats/Tile1", typeof(Material)) as Material;
        materialNum2 = Resources.Load("Materials/DesertMats/Tile2", typeof(Material)) as Material;
        materialNum3 = Resources.Load("Materials/DesertMats/Tile3", typeof(Material)) as Material;
        materialNum4 = Resources.Load("Materials/DesertMats/Tile4", typeof(Material)) as Material;
        materialNum5 = Resources.Load("Materials/DesertMats/Tile5", typeof(Material)) as Material;
        materialNum6 = Resources.Load("Materials/DesertMats/Tile6", typeof(Material)) as Material;
        materialNum7 = Resources.Load("Materials/DesertMats/Tile7", typeof(Material)) as Material;
        materialNum8 = Resources.Load("Materials/DesertMats/Tile8", typeof(Material)) as Material;
        // Load flag prefab
        flagPrefab = Resources.Load("Prefabs/Flag", typeof(GameObject)) as GameObject;
        breakPS = Resources.Load("Prefabs/BreakPS", typeof(GameObject)) as GameObject;
    }

    public void Setup(Complex complex, int mineCount) {
        cam = Camera.main;

        this.complex = complex;
        ResU = complex.ResU;
        ResV = complex.ResV;
        quads = complex.quads;

        this.mineCount = Mathf.Clamp(mineCount, 0, ResU*ResV);
    }

    // Checks for user inputs every frame
    private void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            NewGame();
        }
        else if (gameover != true) {
            if (Input.GetMouseButtonUp(1)) {
                Flag();
            } else if (Input.GetMouseButtonUp(0)) {
                AttemptReveal();
            }
        }
    }

    public void NewGame(bool clearFlags = true) {
        foreach (Coroutine coroutine in coroutinePropagate) {
            if (coroutine != null) {
                StopCoroutine(coroutine);
            }
        }
        coroutinePropagate.Clear();

        if (clearFlags)
            ClearFlags();
        
        gameon = false;
        gameover = false;

        foreach (Quad quad in quads) {
            quad.type = Quad.Type.Empty;
            quad.Revealed = false;
            quad.Exploded = false;
            quad.Visited = false;
            if (clearFlags)
                quad.Flagged = false;
        }

        GenerateMines();
        GenerateNumbers();
        Draw();
    }

    private void GenerateMines() {
        int u, v;
        for (int i = 0; i < mineCount; i++) {
            u = Random.Range(0, ResU);
            v = Random.Range(0, ResV);

            // Check if Quad is already a mine
            while (quads[u,v].type == Quad.Type.Mine) {
                u = Random.Range(0, ResU);
                v = Random.Range(0, ResV);
            }

            quads[u,v].type = Quad.Type.Mine;
        }
    }

    private void GenerateNumbers() {
        Quad quad;
        for (int i = 0; i < ResU; i++) {
            for (int j = 0; j < ResV; j++) {
                quad = quads[i,j];
                if (quad.type == Quad.Type.Mine) { continue; }

                quad.Number = CountMines(quad);
            }
        }
    }

    public int CountMines(Quad quad) {
        int u0 = quad.U, v0 = quad.V, u, v;
        int count = 0;

        for (int du = -1; du <= 1; du++) {
            for (int dv = -1; dv <= 1; dv++) {
                if (du == 0 && dv == 0) { continue; }

                v = v0 + du;
                u = u0 + dv;

                if (complex.GetNeighbor(u, v).type == Quad.Type.Mine) {
                    count++;
                }
            }
        }
        return count;
    }

    private void Flag() {
        mouseOver = complex.MouseIdentify();
        if (mouseOver == null) { return; }
        mouseOver.Flag(flags, flagPrefab);
        Draw();
    }

    private void ClearFlags() {
        foreach (KeyValuePair<Vector2Int, GameObject[]> flag in flags) {
            Complex.DestroyGOs(flag.Value);
        }
        flags.Clear();
    }

    private void AttemptReveal() {
        mouseOver = complex.MouseIdentify();
        if (mouseOver == null) { return; }
        if (mouseOver.type == Quad.Type.Invalid || mouseOver.Revealed || mouseOver.Flagged) {
            return;
        }

        // Require first click to be an empty tile
        if (!gameon) {
            while (mouseOver.type != Quad.Type.Empty) {
                NewGame(false);
            }
        }

        switch (mouseOver.type) {
            case Quad.Type.Mine:
                Explode(mouseOver); break;
            case Quad.Type.Empty:
                Flood(mouseOver);
                CheckWinCondition();
                gameon = true;
                break;
            default:
                mouseOver.Revealed = true;
                CheckWinCondition();
                gameon = true;
                Draw();
                break;
        }
    }

    private void Flood(Quad quad) {
        Queue<Quad> queue = new Queue<Quad>();

        // Reveal starting quad
        quad.Visited = true;
        quad.Depth = 0;
        RevealQuad(quad);

        queue.Enqueue(quad);

        while (queue.Any()) {
            quad = queue.Dequeue();

            List<Quad> neighbors = complex.GetNeighbors(quad);

            foreach (Quad neighbor in neighbors) {
                if (!neighbor.Revealed && !neighbor.Visited) {
                    // Add empty neighbors to queue
                    if (neighbor.type == Quad.Type.Empty) {
                        neighbor.Visited = true;
                        queue.Enqueue(neighbor);
                    }

                    neighbor.Depth = quad.Depth + 1;
                    RevealQuad(neighbor);
                }
            }
        }
    }

    private void RevealQuad(Quad quad) {
        quad.Revealed = true;

        if (quad.Flagged == true) {
            quad.Flagged = false;
            flags.Remove(new Vector2Int(quad.U, quad.V));
        }

        coroutinePropagate.Add(
            StartCoroutine(quad.Reveal(GetState(quad), breakPS))
        );
    }

    // Updates all materials instantaneously
    public void Draw() {
        foreach (Quad quad in quads)
            quad.SetMaterial(GetState(quad));
    }

    public Material GetState(Quad quad) {
        if (quad.Revealed) { return GetRevealed(quad); }
        else if (quad.Flagged) { return materialFlag; }
        else { return materialUknown; }
    }

    private Material GetRevealed(Quad quad) {
        switch (quad.type) {
            case Quad.Type.Empty: return materialEmpty;
            case Quad.Type.Mine: return quad.Exploded ? materialExploded : materialMine;
            case Quad.Type.Number: return GetNumber(quad);
            default: return null;
        }
    }

    private Material GetNumber(Quad quad) {
        switch (quad.Number) {
            case 1: return materialNum1;
            case 2: return materialNum2;
            case 3: return materialNum3;
            case 4: return materialNum4;
            case 5: return materialNum5;
            case 6: return materialNum6;
            case 7: return materialNum7;
            case 8: return materialNum8;
            default: return null;
        }
    }

    private void Explode(Quad quad) {
        Debug.Log("Game over");
        gameover = true;
        gameon = false;

        quad.Revealed = true;
        quad.Exploded = true;

        for (int i = 0; i < ResU; i++) {
            for (int j = 0; j < ResV; j++) {
                quad = quads[i,j];

                if (quad.type == Quad.Type.Mine) {
                    quad.Revealed = true;
                }
            }
        } 
        Draw();
    }

    private void CheckWinCondition() {
        Quad quad;
        for (int i = 0; i < ResU; i++) {
            for (int j = 0; j < ResV; j++) {
                quad = quads[i,j];

                if (quad.type != Quad.Type.Mine && !quad.Revealed) {
                    return;
                }
            }
        }

        Debug.Log("Game win");
        gameover = true;
        gameon = false;

        for (int i = 0; i < ResU; i++) {
            for (int j = 0; j < ResV; j++) {
                quad = quads[i, j];

                if (quad.type == Quad.Type.Mine) {
                    quad.Flagged = true;
                    quads[i,j] = quad;
                }
            }
        }
    }
}
