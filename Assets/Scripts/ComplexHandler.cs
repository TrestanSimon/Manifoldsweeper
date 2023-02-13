using UnityEngine;
using System.Collections.Generic;

using static Unity.Mathematics.math;

public class ComplexHandler : MonoBehaviour {
    public int ResU = 16, ResV = 16*3;
    public int mineCount = 16;
    public float r = 1f, R = 3f;
    public GameObject flagPrefab;

    private QuadHandler quadHandler;
    private Vector3[,] vertices;
    private Vector3[,] normals;
    private Quad[,] quads;
    private Vector3[,] quadNormals;
    private Quad mouseOver;

    private bool gameon;
    private bool gameover;

    private void Awake() {
        quadHandler = GetComponentInChildren<QuadHandler>();
    }

    private void Start() {
        GenerateComplex();
        NewGame();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.R)) { NewGame(); }
        else if (gameover != true) {
            if (Input.GetMouseButtonDown(1)) {
                Flag();
            } else if (Input.GetMouseButtonDown(0)) {
                Reveal();
            }
        }
        /*
        Quad quad;
        foreach (KeyValuePair<Vector2Int, GameObject> flag in flags) {
            quad = quads[flag.Key.x, flag.Key.y];
            flag.Value.transform.LookAt(Camera.main.transform);
            flag.Value.transform.Rotate(new Vector3(0, 10f*Time.deltaTime, 0), Space.Self);
        }*/
    }

    private void NewGame() {
        gameon = false;
        gameover = false;
        GenerateField();
    }

    public void GenerateComplex() {
        GenerateVertices();
        GenerateQuads();
    }

    private void GenerateVertices() {
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
        for (int u = 0; u <= ResU; u++) {
            sincos(2*PI*u / ResU, out float sinu, out float cosu);
            float minor = R + r*cosu;

            for (int v = 0; v <= ResV; v++) {
                sincos(2*PI*v / ResV, out float sinv, out float cosv);

                vertices[u, v] = new Vector3(
                    minor * cosv,
                    r * sinu,
                    minor * sinv
                );
                normals[u, v] = new Vector3(
                    cosu * cosv,
                    sinu,
                    cosu * sinv
                );
            }
        }
    }

    private void GenerateQuads() {
        quads = new Quad[ResU, ResV];
        quadNormals = new Vector3[ResU, ResV];

        for (int v = 0; v < ResV; v++) {
            for (int u = 0; u < ResU; u++) {
                quadNormals[u,v] = (normals[u,v] + normals[u+1,v] + normals[u+1,v+1] + normals[u,v+1])/4;
                quads[u,v] = new Quad(
                    u, v,
                    vertices[u,v], vertices[u+1,v],
                    vertices[u+1,v+1], vertices[u,v+1],
                    quadNormals[u,v]
                );
            }
        }
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
        foreach (KeyValuePair<Vector2Int, GameObject> flag in QuadHandler.flags) {
            Destroy(flag.Value);
        }
        QuadHandler.flags.Clear();

        GenerateMines();
        GenerateNumbers();
        quadHandler.Draw(quads);
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

    private int CountMines(Quad quad) {
        int u0 = quad.u, v0 = quad.v, u, v;
        int count = 0;

        for (int du = -1; du <= 1; du++) {
            for (int dv = -1; dv <= 1; dv++) {
                if (du == 0 && dv == 0) { continue; }

                v = v0 + du;
                u = u0 + dv;

                if (GetNeighbor(u, v).type == Quad.Type.Mine) {
                    count++;
                }
            }
        }
        return count;
    }

    private void Flag() {
        mouseOver = MouseIdentify();

        if (mouseOver == null) { return; }
        mouseOver.Flag(flagPrefab);
        quadHandler.Draw(quads);
    }

    private void Reveal() {
        mouseOver = MouseIdentify();
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
                break;
            default:
                mouseOver.revealed = true;
                CheckWinCondition();
                break;
        }

        gameon = true;
        quadHandler.Draw(quads);
    }

    private void Flood(Quad quad) {
        if (quad.revealed) return;
        if (quad.type == Quad.Type.Mine || quad.type == Quad.Type.Invalid) return;

        if (quad.flagged == true) {
            quad.flagged = false;
            QuadHandler.flags.Remove(new Vector2Int(quad.u, quad.v));
            Destroy(quad.flag);
        }

        quad.revealed = true;
        if (quad.type == Quad.Type.Empty) {
            for (int du = -1; du <= 1; du++) {
                for (int dv = -1; dv <= 1; dv++) {
                    if (!(du == 0 && dv == 0)) {
                        Flood(GetNeighbor(quad.u + du, quad.v + dv));
                    }
                }
            }
        }
    }

    // Returns a Quad given the coordinates of a neighbor
    private Quad GetNeighbor(int u, int v) {
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v >= 0 ? v % ResV : v + ResV;

        if (IsValid(u1, v1)) { return quads[u1,v1]; }
        else { return null; }
    }

    private bool IsValid(int u, int v) {
        return u >= 0 && u < ResU && v >= 0 && v < ResV;
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

    // Identifies the Quad instance the cursor is over
    // returns null if there is no Quad
    private Quad MouseIdentify() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            return Identify(hit.collider.gameObject);
        } else {
            return null;
        }
    }

    // Identifies the Quad instance associated with a GameObject
    private Quad Identify(GameObject go) {
        Tag tag = go.GetComponent<Tag>();
        return quads[tag.u, tag.v];
    }
}
