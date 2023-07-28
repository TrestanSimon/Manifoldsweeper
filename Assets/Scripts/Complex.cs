using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public abstract class Complex : MonoBehaviour {
    public enum Map {
        Invalid,
        Flat,
        Cylinder, Annulus,
        Torus,
        MobiusStrip, MobiusSudanese,
        KleinBottle
    }

    // Dictionary for IDing acceptable mappings
    // Manually hide in children with "new" keyword
    public static Dictionary<String, Map> MapDict {
        get => new Dictionary<string, Map>(){
            {"Flat", Map.Flat}
        };
    }

    protected int resU, resV;
    private Map _currentMap;
    protected Vector3[,] vertices;
    protected Tile[,] tiles;
    protected int sideCount;
    private Vector3[] _offset;
    private Vector3[] _interiorCorners;
    protected Vector3[] _corners;

    private int _copyDepthU = 0;
    private int _copyDepthV = 0;

    public virtual int ResU {
        get => resU;
        protected set {
            if (value < 2 || value > 99)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "U res. range is between 1 and 99.");
            resU = value;
        }
    }
    public virtual int ResV {
        get => resV;
        protected set {
            if (value < 2 || value > 99)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "V res. range is between 1 and 99.");
            resV = value;
        }
    }
    public Map CurrentMap {
        get => _currentMap;
        set {
            if (value != Map.Flat) {
                CopyDepthU = 0;
                CopyDepthV = 0;
            }
            _currentMap = value;
        }
    }
    public Vector3[] Offset {
        get {
            if (CurrentMap != Complex.Map.Flat)
                return null;
            _offset ??= new Vector3[]{
                vertices[resU/2,0] + vertices[resU/2+resU%2,0],
                vertices[0,resV/2] + vertices[0,resV/2+resV%2]
            };
            return _offset;
        }
    }
    public Vector3[] InteriorCorners {
        get {
            _interiorCorners ??= new Vector3[]{
                vertices[0,ResV],
                vertices[ResU,ResV],
                vertices[ResU,0],
                vertices[0,0],
            };
            return _interiorCorners;
        }
    }
    public Vector3[] Corners { get => _corners; }
    public Tile[,] Tiles {
        get => tiles;
        private set => tiles = value;
    }
    public int CopyDepthU {
        get => _copyDepthU;
        protected set => _copyDepthU = value;
    }
    public int CopyDepthV {
        get => _copyDepthV;
        protected set => _copyDepthV = value;
    }

    public abstract void Setup(int resU, int resV, Map initMap);

    // Generates vertices (p, q) according to mapping
    // Returns an [resU+1, resV+1] array with Vector3 elements
    // Unique to each surface
    protected abstract void InitVertices(Map map);

    // Generates tiles (u, v) given vertices
    // Returns an [resU, resV] array with tile elements
    protected void InitTiles() {
        tiles = new Tile[resU, resV];

        for (int v = 0; v < resV; v++) {
            for (int u = 0; u < resU; u++) {
                tiles[u,v] = new Tile(
                    u, v,
                    new Vector3[]{
                        vertices[u,v], vertices[u+1,v],
                        vertices[u+1,v+1], vertices[u,v+1]
                    },
                    this
                );
            }
        }
    }

    public void UpdateVertices(Vector3[,] newVerts) {
        for (int u = 0; u < resU; u++) {
            for (int v = 0; v < resV; v++) {
                tiles[u,v].UpdateVertices(
                    newVerts[u,v], newVerts[u+1,v],
                    newVerts[u+1,v+1], newVerts[u,v+1]
                );
            }
        }
    }

    public abstract void CalculateCorners(int depthU, int depthV);

    public IEnumerator ComplexLerp(
        Vector3[][,] vertSteps, float duration
    ) {
        float time = 0f;
        float t;
        Vector3[,] tempVerts = new Vector3[resU+1,resV+1];

        for (int i = 0; i < vertSteps.Length-1; i++) {
            // Lerp loop
            while (time < duration) {
                t = time / duration;
                t = t * t * (3f - 2f * t);

                // Update verts
                for (int u = 0; u < resU+1; u++) {
                    for (int v = 0; v < resV+1; v++) {
                        tempVerts[u,v] = Vector3.Lerp(
                            vertSteps[i][u,v], vertSteps[i+1][u,v], t
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

    // Returns a tile given a coordinate neighboring another tile
    // Depends on edge gluing
    public abstract Tile GetNeighbor(int u, int v);

    // Returns list of all neighbors
    // (0) (1) (2)
    // (3)  X  (4)
    // (5) (6) (7)
    public List<Tile> GetNeighbors(Tile tile, bool filter = true) {
        List<Tile> neighbors = new List<Tile>();
        Tile neighbor;
        for (int du = 1; du >= -1; du--) {
            for (int dv = 1; dv >= -1; dv--) {
                if (!(du == 0 && dv == 0)) {
                    neighbor = GetNeighbor(tile.U + du, tile.V + dv);
                    if (neighbor.type != Tile.Type.Invalid || !filter)
                        neighbors.Add(neighbor);
                }
            }
        }
        return neighbors;
    }

    public abstract IEnumerator RepeatU();

    public abstract IEnumerator RepeatV();

    public IEnumerator DumpRepeatComplex() {
        foreach (Tile tile in tiles)
            tile.DestroyClones();
        yield break;
    }

    private void Placeholdering() {
        float time = 0f;
        float duration = 2f;
        float t = 0f;

        while (time < duration) {
            t = time / duration;
            t = t*t*(3f - 2f * t);

            time += Time.deltaTime;
            // yield return null;
        }
    }

    // Identifies the tile instance the cursor is over
    // returns null if there is no tile
    public Tile MouseIdentify() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
            return Identify(hit.collider.gameObject);
        else return null;
    }

    // Identifies the tile instance associated with a GameObject
    private Tile Identify(GameObject go) {
        go.TryGetComponent<Tag>(out Tag tag);
        return tiles[tag.u, tag.v];
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

    public abstract IEnumerator ReMap(Map newMap);

    public virtual Vector3[,] PlaneMap() {
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];
        for (int p = 0; p <= resU; p++) {
            for (int q = 0; q <= resV; q++) {
                tempVerts[p,q] = new Vector3(
                    -p + resU/2f,
                    0,
                    -q + resV/2f
                ) / 2f;
            }
        }
        return tempVerts;
    }

    // Maps from cylinder to plane
    // Used in Cylinder.cs and Torus.cs
    public Vector3[,] CylinderInvoluteMap(float progress, float radius) {
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
                    radius * (cosp + (t - a)*sinp) + radius * progress,
                    PI * (resV/2f - q) / 8f
                );
            }
        }
        return tempVerts;
    }
}
