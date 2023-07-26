using System;
using System.Linq;
using UnityEngine;

public class Quad {
    // Geometry-related fields
    protected int _sideCount;
    protected GameObject[] _gameObjects;
    protected Vector3[] _vertices;
    protected MeshRenderer[] _meshRenderers;
    protected Mesh[] _meshes;

    // Winding for triangles and UV coordinates
        // 1 --> 2
        // |  /  |
        // 0 <-- 3
    public static int[] QuadTriangles {
        get => new int[]{
            0, 1, 2,
            2, 3, 0
        };
    }
    public static Vector2[] QuadUVCoords {
        get => new Vector2[]{
            Vector2.zero, Vector2.up,
            Vector2.one, Vector2.right
        };
    }

    public int SideCount {
        set {
            if (value < 1 || value > 2)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Side count range is between 1 and 2.");
            _sideCount = value;
        }
    }
    protected float _Scale {
        get => Vector3.Magnitude(_vertices[0] - _vertices[2]);
    }

    // Constructor for Invalid Quads
    public Quad() {}

    // Normal constructor
    public Quad(
        Vector3[] vertices,
        int sideCount,
        bool collision = true
    ) {
        SideCount = sideCount;

        _gameObjects = new GameObject[sideCount];
        _meshRenderers = new MeshRenderer[sideCount];
        _meshes = new Mesh[sideCount];
        _vertices = vertices;
    
        for (int i = 0; i < sideCount; i++) {
            _gameObjects[i] = new GameObject();

            _meshRenderers[i] = _gameObjects[i].AddComponent<MeshRenderer>();
            Mesh mesh = _gameObjects[i].AddComponent<MeshFilter>().mesh;
            _meshes[i] = mesh;

            mesh.vertices = _vertices;
            mesh.triangles = QuadTriangles;
            mesh.uv = QuadUVCoords;

            if (i == 0) {
                // Reverse winding
                mesh.triangles = mesh.triangles.Reverse().ToArray();
                mesh.uv = mesh.uv.Reverse().ToArray();
            }

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            
            if (collision) {
                MeshCollider collider = _gameObjects[i].AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }
        }
    }

    // Updates mesh(es) with provided vertices
    public virtual void UpdateVertices(
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3
    ) {
        _vertices = new Vector3[]{
            vert0, vert1,
            vert2, vert3
        };
        for (int i = 0; i < _sideCount; i++) {
            _meshes[i].vertices = _vertices;
            _meshes[i].RecalculateBounds();
            _meshes[i].RecalculateNormals();
            _meshes[i].RecalculateTangents();

            _gameObjects[i].TryGetComponent<MeshCollider>(out MeshCollider collider);
            if (collider is not null) collider.sharedMesh = _meshes[i];
        }
    }

    protected Vector3[] OffsetVertices(Vector3 offset) {
        return new Vector3[]{
            _vertices[0] + offset,
            _vertices[1] + offset,
            _vertices[2] + offset,
            _vertices[3] + offset
        };
    }

    // Sets material
    public virtual void SetMaterial(Material material) {
        for (int i = 0; i < _sideCount; i++)
            _gameObjects[i].GetComponent<MeshRenderer>().material = material;
    }

    public virtual void DestroySelf() {
        Complex.DestroyGOs(_gameObjects);
    }
}
