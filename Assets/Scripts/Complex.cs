using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public abstract class Complex : MonoBehaviour {
    public int ResU, ResV;

    public Vector3[,] vertices;
    public Quad[,] quads;
    public int sideCount;
    public bool planar;

    public abstract void Setup(Camera cam, int ResU, int ResV);

    public Game Gamify(int mineCount) {
        Game game;
        gameObject.TryGetComponent<Game>(out game);

        if (game == null)
            game = gameObject.AddComponent<Game>();

        game.Setup(this, mineCount);
        return game;
    }

    public void GenerateComplex() {
        GenerateVertices();
        GenerateQuads();
    }

    // Generates vertices (p, q)
    // Returns an [ResU+1, ResV+1] array with Vector3 elements
    // Unique to each surface
    public abstract void GenerateVertices();

    // Generates quads (u, v) given vertices
    // Returns an [ResU, ResV] array with Quad elements
    private void GenerateQuads() {
        quads = new Quad[ResU, ResV];

        for (int v = 0; v < ResV; v++) {
            for (int u = 0; u < ResU; u++) {
                quads[u,v] = new Quad(
                    u, v, sideCount,
                    vertices[u,v], vertices[u+1,v],
                    vertices[u+1,v+1], vertices[u,v+1]
                );
                // Make quads child of Complex GameObject
                foreach (GameObject quad in quads[u,v].gameObjects)
                    quad.transform.parent = gameObject.transform;
            }
        }
    }

    public void UpdateVertices(Vector3[,] newVerts) {
        for (int u = 0; u < ResU; u++) {
            for (int v = 0; v < ResV; v++) {
                quads[u,v].UpdateVertices(
                    newVerts[u,v], newVerts[u+1,v],
                    newVerts[u+1,v+1], newVerts[u,v+1]
                );
            }
        }
    }

    public IEnumerator ComplexLerp(
        Vector3[][,] vertSteps, float duration
    ) {
        float time = 0f;
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];

        for (int i = 0; i < vertSteps.Length-1; i++) {
            // Lerp loop
            while (time < duration) {
                // Update verts
                for (int u = 0; u < ResU+1; u++) {
                    for (int v = 0; v < ResV+1; v++) {
                        tempVerts[u,v] = Vector3.Lerp(
                            vertSteps[i][u,v], vertSteps[i+1][u,v], time/duration
                        );
                    }
                }

                UpdateVertices(tempVerts);
                
                time += Time.deltaTime;
                yield return null;
            }
            // Finalize mapping
            UpdateVertices(vertSteps[i+1]);
        }
        vertices = vertSteps[vertSteps.Length-1];
    }

    // Returns a Quad given a coordinate neighboring another Quad
    // Can be overridden to glue edges together
    public virtual Quad GetNeighbor(int u, int v) {
        if (u >= 0 && u < ResU && v >= 0 && v < ResV) return quads[u,v];
        else return new Quad(); // returns Quad of type Invalid
    }

    public List<Quad> GetNeighbors(Quad quad) {
        List<Quad> neighbors = new List<Quad>();
        Quad neighbor;
        for (int du = -1; du <= 1; du++) {
            for (int dv = -1; dv <= 1; dv++) {
                if (!(du == 0 && dv == 0)) {
                    neighbor = GetNeighbor(quad.u + du, quad.v + dv);
                    if (neighbor.type != Quad.Type.Invalid)
                        neighbors.Add(neighbor);
                }
            }
        }
        return neighbors;
    }

    // Identifies the Quad instance the cursor is over
    // returns null if there is no Quad
    public Quad MouseIdentify() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            return Identify(hit.collider.gameObject);
        } else return null;
    }

    // Identifies the Quad instance associated with a GameObject
    private Quad Identify(GameObject go) {
        Tag tag = go.GetComponent<Tag>();
        return quads[tag.u, tag.v];
    }

    public static void DestroyGOs(GameObject[] gos) {
        foreach (GameObject go in gos) Destroy(go);
    }

    public static GameObject CreateGO(GameObject prefab, Vector3 pos, Quaternion rot, float scale){
        GameObject go = Instantiate(prefab, pos, rot);
        go.transform.localScale *= scale;
        return go;
    }

    public virtual IEnumerator ToPlane() {
        Vector3[,] newVerts = new Vector3[ResU+1,ResV+1];

        if (planar) yield break;

        // Set new verts
        for (int u = 0; u <= ResU; u++) {
            for (int v = 0; v <= ResV; v++) {
                newVerts[u,v] = new Vector3(
                    -u + ResU/2f,
                    0,
                    -v + ResV/2f) / 2f;
            }
        }

        yield return StartCoroutine(ComplexLerp(
            new Vector3[][,]{vertices, newVerts}, 2f));

        planar = true;
    }

    // Maps from cylinder to plane
    // Used in Cylinder.cs and Torus.cs
    public Vector3[,] CylinderToPlaneMap(float progress, float radius) {
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];
        float a, t, sinp, cosp;

        for (int p = 0; p < ResU+1; p++) {
            for (int q = 0; q < ResV+1; q++) {
                // Transformation follows involutes
                a = 2*PI*p/ResU; // Starting point
                t = (PI - a)*progress + a; // Involute curve parameter
                sincos(t, out sinp, out cosp);

                tempVerts[p,q] = new Vector3(
                    radius * (sinp - (t - a)*cosp),
                    radius * (cosp + (t - a)*sinp),
                    vertices[p,q].z
                );
            }
        }
        return tempVerts;
    }
}
