using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour {
    public Complex complex;
    private Quad[,] quads;
    private Quad mouseOver;
    private int ResU, ResV;
    public int mineCount;

    public Camera cam;

    public Material materialUknown;
    public Material materialEmpty;
    public Material materialMine;
    public Material materialExploded;
    public Material materialFlag;
    public Material materialNum1;
    public Material materialNum2;
    public Material materialNum3;
    public Material materialNum4;
    public Material materialNum5;
    public Material materialNum6;
    public Material materialNum7;
    public Material materialNum8;
    public GameObject flagPrefab;
    public Dictionary<Vector2Int, GameObject[]> flags = new Dictionary<Vector2Int, GameObject[]>();
    public GameObject breakPS;

    private bool gameon = false;
    private bool gameover = false;

    private List<Coroutine> coroutinePropagate = new List<Coroutine>();

    private void Awake() {
        // Load materials
        materialUknown = Resources.Load("Materials/DesertMats/TileUnknown", typeof(Material)) as Material;
        materialEmpty = Resources.Load("Materials/DesertMats/TileFlat", typeof(Material)) as Material;
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
        complex.UpdateCamera(cam);
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

    public void NewGame() {
        foreach (Coroutine coroutine in coroutinePropagate) {
            if (coroutine != null) {
                StopCoroutine(coroutine);
            }
        }
        coroutinePropagate.Clear();
        DestroyBreak();
        
        gameon = false;
        gameover = false;
        GenerateField();
    }

    private void GenerateField() {
        Quad quad;

        // Reset all Quad instances
        for (int i = 0; i < ResU; i++) {
            for (int j = 0; j < ResV; j++) {
                quad = quads[i,j];
                quad.type = Quad.Type.Empty;
                quad.revealed = false;
                quad.flagged = false;
                quad.exploded = false;
            }
        }
        
        // Destroy all flags
        foreach (KeyValuePair<Vector2Int, GameObject[]> flag in flags) {
            Complex.DestroyGOs(flag.Value);
        }
        flags.Clear();

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

                quad.number = CountMines(quad);

                if (quad.number > 0) {
                    quad.type = Quad.Type.Number;
                }
            }
        }
    }

    public int CountMines(Quad quad) {
        int u0 = quad.u, v0 = quad.v, u, v;
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

    private void AttemptReveal() {
        mouseOver = complex.MouseIdentify();
        if (mouseOver == null) { return; }
        if (mouseOver.type == Quad.Type.Invalid || mouseOver.revealed || mouseOver.flagged) {
            return;
        }

        // Require first click to be an empty tile
        if (!gameon) {
            while (mouseOver.type != Quad.Type.Empty) {
                NewGame();
            }
        }

        switch (mouseOver.type) {
            case Quad.Type.Mine:
                Explode(mouseOver); break;
            case Quad.Type.Empty:
                StartCoroutine(Flood(mouseOver));
                // coroutinePropagate.Add(StartCoroutine(Flood(mouseOver)));
                CheckWinCondition();
                gameon = true;
                break;
            default:
                mouseOver.revealed = true;
                CheckWinCondition();
                gameon = true;
                Draw();
                break;
        }
    }

    private IEnumerator Flood(Quad quad) {

        if (quad.revealed) yield break;
        if (quad.type == Quad.Type.Mine || quad.type == Quad.Type.Invalid) yield break;

        quad.revealed = true;

        if (quad.flagged == true) {
            quad.flagged = false;
            flags.Remove(new Vector2Int(quad.u, quad.v));
            Complex.DestroyGOs(quad.flag);
        }

        Break(quad);

        // Breadth-first search
        if (quad.type == Quad.Type.Empty) {
            for (int du = -1; du <= 1; du++) {
                for (int dv = -1; dv <= 1; dv++) {
                    if (!(du == 0 && dv == 0)) {
                        coroutinePropagate.Add(StartCoroutine(Flood(
                            complex.GetNeighbor(quad.u + du, quad.v + dv)
                        )));
                        yield return new WaitForSeconds(0.02f);
                    }
                }
            }
        }
    }

    // Updates all materials instantaneously
    public void Draw() {
        Quad quad;

        for (int i = 0; i < ResU; i++) {
            for (int j = 0; j < ResV; j++) {
                quad = quads[i,j];
                quad.SetMaterial(GetState(quad));
            }
        }
    }

    private void Break(Quad quad) {
        if (quad.type == Quad.Type.Invalid) { return; }
        quad.SetMaterial(GetState(quad));
        quad.Break(breakPS);
    }

    private void DestroyBreak() {
        for (int u = 0; u < ResU; u++) {
            for (int v = 0; v < ResV; v++) {
                Destroy(quads[u,v].ps);
            }
        }
    }

    public Material GetState(Quad quad) {
        if (quad.revealed) { return GetRevealed(quad); }
        else if (quad.flagged) { return materialFlag; }
        else { return materialUknown; }
    }

    private Material GetRevealed(Quad quad) {
        switch (quad.type) {
            case Quad.Type.Empty: return materialEmpty;
            case Quad.Type.Mine: return quad.exploded ? materialExploded : materialMine;
            case Quad.Type.Number: return GetNumber(quad);
            default: return null;
        }
    }

    private Material GetNumber(Quad quad) {
        switch (quad.number) {
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

        quad.revealed = true;
        quad.exploded = true;

        for (int i = 0; i < ResU; i++) {
            for (int j = 0; j < ResV; j++) {
                quad = quads[i,j];

                if (quad.type == Quad.Type.Mine) {
                    quad.revealed = true;
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

                if (quad.type != Quad.Type.Mine && !quad.revealed) {
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
                    quad.flagged = true;
                    quads[i,j] = quad;
                }
            }
        }
    }
}
