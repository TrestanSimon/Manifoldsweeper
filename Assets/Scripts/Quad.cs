using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quad {
    public enum Type {
        Invalid,
        Empty,
        Mine,
        Number
    }

    public int u, v;
    public GameObject[] gameObjects;
    public Vector3[] vertices;
    public Vector3[] normals;
    public Mesh[] meshes;
    public int sideCount = 1;
    private float scale;

    public Type type;

    
    private int _number;
    private bool _revealed;
    private bool _flagged;
    private bool _exploded;

    public int Number {
        get => _number;
        set {
            if (value < 0 || value > 8)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Tile number range is between 0 and 8.");
            if (type != Quad.Type.Mine) {
                if (value == 0) type = Quad.Type.Empty;
                else type = Quad.Type.Number;
            }
            _number = value;
        }
    }
    public bool Revealed { get; set; }
    public bool Flagged { 
        get => _flagged;
        set { if (!_revealed) _flagged = value; }
    }
    public bool Exploded {
        get => _exploded;
        set => _exploded = value;
    }
    public bool visited;
    public int depth;

    public GameObject[] flag;
    private GameObject ps;

    public Quad() {}

    public Quad(
        int u, int v, int sideCount,
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3
    ) {
        this.sideCount = sideCount;
        gameObjects = new GameObject[sideCount];
        meshes = new Mesh[sideCount];
        normals =  new Vector3[sideCount];
        flag = new GameObject[sideCount];

        vertices = new Vector3[]{
            vert0, vert1,
            vert2, vert3
        };

        // Normal winding
        // 1 --> 2
        // |  /  |
        // 0 <-- 3
        int[] winding = new int[]{
            0, 1, 2,
            2, 3, 0
        };
        Vector2[] uvCoords = new Vector2[]{
            Vector2.zero, Vector2.up,
            Vector2.one, Vector2.right
        };
    
        for (int i = 0; i < sideCount; i++) {
            gameObjects[i] = new GameObject();
            gameObjects[i].name = "Quad" + i.ToString() + " " + u.ToString() + ", " + v.ToString();

            // For identifying Quad instance from GameObject
            Tag tag = gameObjects[i].AddComponent<Tag>();
            this.u = tag.u = u;
            this.v = tag.v = v;

            MeshFilter filter = gameObjects[i].AddComponent<MeshFilter>();
            Mesh mesh = filter.mesh;
            meshes[i] = mesh;

            gameObjects[i].AddComponent<MeshRenderer>();

            mesh.vertices = vertices;
            mesh.triangles = winding;
            mesh.uv = uvCoords;
            normals[i] = Vector3.Cross(vert0 - vert1, vert0 - vert2).normalized;

            if (i % 2 == 1) {
                // Reverse winding
                mesh.triangles = mesh.triangles.Reverse().ToArray();
                mesh.uv = mesh.uv.Reverse().ToArray();
                normals[i] *= -1f;
            }

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            
            MeshCollider collider = gameObjects[i].AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            scale = Vector3.Magnitude(vertices[0] - vertices[2]);
        }
    }

    // Updates mesh(es) with provided vertices
    public void UpdateVertices(
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3
    ) {
        vertices = new Vector3[]{
            vert0, vert1,
            vert2, vert3
        };
        for (int i = 0; i < sideCount; i++) {
            meshes[i].vertices = vertices;
            meshes[i].RecalculateBounds();
            meshes[i].RecalculateNormals();
            meshes[i].RecalculateTangents();
            MeshCollider collider = gameObjects[i].GetComponent<MeshCollider>();
            collider.sharedMesh = meshes[i];
        }
    }

    public IEnumerator Reveal(Material material, GameObject breakPS) {
        if (type == Type.Invalid) { yield break; }
        yield return new WaitForSeconds(0.02f * depth);
        SetMaterial(material);
        ps = Complex.CreateGO(breakPS, vertices[0], Quaternion.identity, scale);
        ps.transform.parent = gameObjects[0].transform;
    }

    // Sets material
    public void SetMaterial(Material material) {
        if (type == Type.Invalid) return;
        for (int i = 0; i < sideCount; i++) {
            MeshRenderer meshRenderer = gameObjects[i].GetComponent<MeshRenderer>();
            meshRenderer.material = material;
        }
    }

    // Places flag(s)
    public void Flag(Dictionary<Vector2Int, GameObject[]> flags, GameObject flag = null) {
        if (type == Type.Invalid || _revealed) return;
        if (_flagged) {
            flags.Remove(new Vector2Int(u,v));
            Complex.DestroyGOs(this.flag);
        } else if (flag != null) {
            // Points to where flag will be planted
            Vector3 stake = (vertices[0] + vertices[2]) / 2f;

            // Create flag for each side
            for (int i = 0; i < sideCount; i++) {
                Vector3 flagPos = stake + normals[i] * scale/2f;
                Quaternion flagRot = Quaternion.LookRotation(normals[i]) * Quaternion.AngleAxis(90, Vector3.up);
                this.flag[i] = Complex.CreateGO(flag, flagPos, flagRot, scale);
                this.flag[i].transform.parent = gameObjects[i].transform;
                this.flag[i].name = "Flag";
            }
            flags.Add(new Vector2Int(u,v), this.flag);
        }
        _flagged = !_flagged;
    }

    public void UpdateFlags() {
        if (type == Type.Invalid || _revealed || !_flagged) return;

    }
}
