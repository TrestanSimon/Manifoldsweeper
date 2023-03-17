using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public abstract class Complex : MonoBehaviour {
    protected int resU, resV;
    protected Vector3[,] vertices;
    protected Quad[,] quads;
    protected int sideCount;
    protected bool planar;

    public virtual int ResU {
        get => resU;
        protected set {
            if (value < 1 || value > 99)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "U res. range is between 1 and 99.");
            resU = value;
        }
    }
    public virtual int ResV {
        get => resV;
        protected set {
            if (value < 1 || value > 99)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "V res. range is between 1 and 99.");
            resV = value;
        }
    }
    public Quad[,] Quads {
        get => quads;
        private set => quads = value;
    }
    public bool Planar {
        get => planar;
        set => planar = value;
    }

    public abstract void Setup(int resU, int resV);

    // Generates vertices (p, q)
    // Returns an [resU+1, resV+1] array with Vector3 elements
    // Unique to each surface
    protected abstract void GenerateVertices();

    // Generates quads (u, v) given vertices
    // Returns an [resU, resV] array with Quad elements
    protected void GenerateQuads() {
        quads = new Quad[resU, resV];

        for (int v = 0; v < resV; v++) {
            for (int u = 0; u < resU; u++) {
                quads[u,v] = new Quad(
                    u, v, sideCount,
                    vertices[u,v], vertices[u+1,v],
                    vertices[u+1,v+1], vertices[u,v+1],
                    this
                );
            }
        }
    }

    public void UpdateVertices(Vector3[,] newVerts) {
        for (int u = 0; u < resU; u++) {
            for (int v = 0; v < resV; v++) {
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
        Vector3[,] tempVerts = new Vector3[resU+1,resV+1];

        for (int i = 0; i < vertSteps.Length-1; i++) {
            // Lerp loop
            while (time < duration) {
                // Update verts
                for (int u = 0; u < resU+1; u++) {
                    for (int v = 0; v < resV+1; v++) {
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
        if (u >= 0 && u < resU && v >= 0 && v < resV) return quads[u,v];
        else return new Quad(); // returns Quad of type Invalid
    }

    public List<Quad> GetNeighbors(Quad quad) {
        List<Quad> neighbors = new List<Quad>();
        Quad neighbor;
        for (int du = -1; du <= 1; du++) {
            for (int dv = -1; dv <= 1; dv++) {
                if (!(du == 0 && dv == 0)) {
                    neighbor = GetNeighbor(quad.U + du, quad.V + dv);
                    if (neighbor.type != Quad.Type.Invalid)
                        neighbors.Add(neighbor);
                }
            }
        }
        return neighbors;
    }

    public virtual void RepeatComplex() {
        if (!planar) return;
    }

    // Identifies the Quad instance the cursor is over
    // returns null if there is no Quad
    public Quad MouseIdentify() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
            return Identify(hit.collider.gameObject);
        else return null;
    }

    // Identifies the Quad instance associated with a GameObject
    private Quad Identify(GameObject go) {
        go.TryGetComponent<Tag>(out Tag tag);
        return quads[tag.u, tag.v];
    }

    public static void DestroyGOs(GameObject[] gos) {
        foreach (GameObject go in gos)
            if (go != null) Destroy(go);
    }

    public static GameObject CreateGO(GameObject prefab, Vector3 pos, Quaternion rot, float scale){
        GameObject go = Instantiate(prefab, pos, rot);
        go.transform.localScale *= scale;
        return go;
    }

    public virtual IEnumerator ToPlane() {
        Vector3[,] newVerts = new Vector3[resU+1,resV+1];

        if (planar) yield break;

        // Set new verts
        for (int u = 0; u <= resU; u++) {
            for (int v = 0; v <= resV; v++) {
                newVerts[u,v] = new Vector3(
                    -u + resU/2f,
                    0,
                    -v + resV/2f
                ) / 2f;
            }
        }

        yield return StartCoroutine(ComplexLerp(
            new Vector3[][,]{vertices, newVerts}, 2f));

        planar = true;
    }

    // Maps from cylinder to plane
    // Used in Cylinder.cs and Torus.cs
    public Vector3[,] CylinderToPlaneMap(float progress, float radius) {
        Vector3[,] tempVerts = new Vector3[resU+1,resV+1];
        float a, t, sinp, cosp;

        for (int p = 0; p < resU+1; p++) {
            for (int q = 0; q < resV+1; q++) {
                // Transformation follows involutes
                a = 2*PI*p/resU; // Starting point
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
