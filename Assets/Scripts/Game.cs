using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour {
    public Complex complex;
    private Quad[,] quads;
    private Quad mouseOver;
    private int ResU, ResV;
    public int mineCount = 64;

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

    private IEnumerator coroutinePropagate;

    private void Awake() {
        // Load materials
        materialUknown = Resources.Load("Materials/OceanMats/TileCloud", typeof(Material)) as Material;
        materialEmpty = Resources.Load("Materials/OceanMats/TileOcean", typeof(Material)) as Material;
        materialMine = Resources.Load("Materials/TileMine", typeof(Material)) as Material;
        materialExploded = Resources.Load("Materials/TileExploded", typeof(Material)) as Material;
        materialFlag = Resources.Load("Materials/TileNo", typeof(Material)) as Material;
        materialNum1 = Resources.Load("Materials/Tile1", typeof(Material)) as Material;
        materialNum2 = Resources.Load("Materials/Tile2", typeof(Material)) as Material;
        materialNum3 = Resources.Load("Materials/Tile3", typeof(Material)) as Material;
        materialNum4 = Resources.Load("Materials/Tile4", typeof(Material)) as Material;
        materialNum5 = Resources.Load("Materials/Tile5", typeof(Material)) as Material;
        materialNum6 = Resources.Load("Materials/Tile6", typeof(Material)) as Material;
        materialNum7 = Resources.Load("Materials/Tile7", typeof(Material)) as Material;
        materialNum8 = Resources.Load("Materials/Tile8", typeof(Material)) as Material;
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
                Reveal();
            }
        }
    }

    public void NewGame() {
        if (coroutinePropagate != null)
            StopCoroutine(coroutinePropagate);
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
            Complex.DestroyFlags(flag.Value);
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

    private void Reveal() {
        mouseOver = complex.MouseIdentify();
        if (mouseOver == null) { return; }
        if (mouseOver.type == Quad.Type.Invalid || mouseOver.revealed || mouseOver.flagged) {
            return;
        }

        switch (mouseOver.type) {
            case Quad.Type.Mine:
                if (gameon == false) {NewGame(); Reveal(); break;}
                else {Explode(mouseOver); break;}
            case Quad.Type.Empty:
                Flood(mouseOver);
                CheckWinCondition();
                gameon = true;
                coroutinePropagate = PropagateDraw(mouseOver);
                StartCoroutine(coroutinePropagate);
                break;
            default:
                mouseOver.revealed = true;
                CheckWinCondition();
                gameon = true;
                Draw();
                break;
        }
    }

    private void Flood(Quad quad) {
        if (quad.revealed) return;
        if (quad.type == Quad.Type.Mine || quad.type == Quad.Type.Invalid) return;

        if (quad.flagged == true) {
            quad.flagged = false;
            flags.Remove(new Vector2Int(quad.u, quad.v));
            Complex.DestroyFlags(quad.flag);
        }

        quad.revealed = true;
        if (quad.type == Quad.Type.Empty) {
            for (int du = -1; du <= 1; du++) {
                for (int dv = -1; dv <= 1; dv++) {
                    if (!(du == 0 && dv == 0)) {
                        Flood(complex.GetNeighbor(quad.u + du, quad.v + dv));
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

    // Updates materials in concentric square rings around the provided quad
    private IEnumerator PropagateDraw(Quad quad) {
        Quad quad2;
        for (int n = 0; n < (int)Mathf.Max(ResU, ResV); n++) {
            for (int du = -n-1; du <= n+1; du++) {
                for (int dv = -n-1; dv <= n+1; dv++) {
                    quad2 = complex.GetNeighbor(quad.u + du, quad.v + dv);
                    quad2.SetMaterial(GetState(quad2));
                    Complex.CreateBreakPS(breakPS, quad2.gameObjects[0].transform.position);
                }
            }
            yield return new WaitForSeconds(0.01f);
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
