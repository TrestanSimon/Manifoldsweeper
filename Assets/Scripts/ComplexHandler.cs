using UnityEngine;
using System.Collections.Generic;

using static Unity.Mathematics.math;

public class ComplexHandler : MonoBehaviour {
    public Material defaultMaterial;
    public int ResU = 16, ResV = 16;
    public int mineCount = 16;
    public float r = 1f, R = 3f;
    public GameObject flagPrefab;
    private Dictionary<Vector2Int, GameObject> flags = new Dictionary<Vector2Int, GameObject>();

    private QuadHandler quadHandler;
    private Vector3[,] vertices;
    private Quad[,] quads;
    private Vector3[,] normals;
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
        Quad quad;
        foreach (KeyValuePair<Vector2Int, GameObject> flag in flags) {
            quad = quads[flag.Key.x, flag.Key.y];
            // flag.Value.transform.LookAt(Camera.main.transform);
            //flag.Value.transform.Rotate(new Vector3(0, 10f*Time.deltaTime, 0), Space.Self);
        }
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

    private void CreateQuad(
        int u, int v,
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3
    ) {
        Quad quad = new Quad();
        quad.u = u;
        quad.v = v;
        quad.go.name = "Quad " + u.ToString() + ", " + v.ToString();
        
        // For identifying Quad instance from GameObject
        Tag tag = quad.go.AddComponent<Tag>();
        tag.u = u;
        tag.v = v;

        quad.go.transform.parent = gameObject.transform;

        quad.SetMaterial(defaultMaterial);

        quad.GenerateMesh(
            vert0, vert1,
            vert2, vert3
        );
        quads[u,v] = quad;
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

        for (int v = 0; v < ResV; v++) {
            for (int u = 0; u < ResU; u++) {
                CreateQuad(
                    u, v,
                    vertices[u,v], vertices[u+1,v],
                    vertices[u+1,v+1], vertices[u,v+1]
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
        foreach (KeyValuePair<Vector2Int, GameObject> flag in flags) {
            Destroy(flag.Value);
        }
        flags.Clear();

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
        if (mouseOver.type == Quad.Type.Invalid || mouseOver.revealed) { return; }

        if (mouseOver.flagged) {
            flags.Remove(new Vector2Int(mouseOver.u, mouseOver.v));
            Destroy(mouseOver.flag);
        } else {
            Vector3 normal = normals[mouseOver.u, mouseOver.v];
            Vector3 quadPos = (mouseOver.vertices[0] + mouseOver.vertices[2]) / 2f;
            Vector3 flagPos = quadPos + normal*0.2f;
            Quaternion flagRot = Quaternion.LookRotation(normal); // Add Quaternion
            Debug.Log(flagRot);

            mouseOver.flag = Instantiate(flagPrefab, flagPos, flagRot);
            flags.Add(new Vector2Int(mouseOver.u, mouseOver.v), mouseOver.flag);
        }

        mouseOver.flagged = !mouseOver.flagged;
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
            flags.Remove(new Vector2Int(quad.u, quad.v));
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
        else { return new Quad(); }
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
