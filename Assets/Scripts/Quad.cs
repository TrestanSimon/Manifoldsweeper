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

    private int _u, _v;
    private GameObject[] _gameObjects;
    private Vector3[] _vertices;
    private Vector3[] _normals;
    private Mesh[] _meshes;
    private int _sideCount;
    private float _scale;

    private Type _type;

    
    private int _number;
    private bool _revealed;
    private bool _flagged;
    private bool _exploded;
    private bool _visited;
    private int _depth;

    private GameObject[] _flags;
    private GameObject _ps;

    public int U {
        get => _u;
        private set => _u = value;
    }
    public int V {
        get => _v;
        private set => _v = value;
    }

    public Type type {
        get => _type;
        set => _type = value;
    }

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
    public bool Revealed {
        get => _revealed;
        set {
            if (value && _flagged) {
                _flagged = false;
                Complex.DestroyGOs(_flags);
            }
            _revealed = value;
        }
    }
    public bool Flagged { 
        get => _flagged;
        set { if (!_revealed) _flagged = value; }
    }
    public bool Exploded {
        get => _exploded;
        set => _exploded = value;
    }
    public bool Visited {
        get => _visited;
        set => _visited = value;
    }
    public int Depth {
        get => _depth;
        set => _depth = value;
    }

    // Constructor for Invalid Quads
    public Quad() {}

    public Quad(
        int u, int v, int sideCount,
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3,
        Transform parent
    ) {
        _sideCount = sideCount;
        _gameObjects = new GameObject[sideCount];
        _meshes = new Mesh[sideCount];
        _normals =  new Vector3[sideCount];
        _flags = new GameObject[sideCount];

        _vertices = new Vector3[]{
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
            _gameObjects[i] = new GameObject();
            _gameObjects[i].name = "Quad" + i.ToString() + " " + u.ToString() + ", " + v.ToString();
            
            // Make quads child of Complex GameObject
            _gameObjects[i].transform.parent = parent;

            // For identifying Quad instance from GameObject
            Tag tag = _gameObjects[i].AddComponent<Tag>();
            _u = tag.u = u;
            _v = tag.v = v;

            MeshFilter filter = _gameObjects[i].AddComponent<MeshFilter>();
            Mesh mesh = filter.mesh;
            _meshes[i] = mesh;

            _gameObjects[i].AddComponent<MeshRenderer>();

            mesh.vertices = _vertices;
            mesh.triangles = winding;
            mesh.uv = uvCoords;
            _normals[i] = Vector3.Cross(vert0 - vert1, vert0 - vert2).normalized;

            if (i % 2 == 1) {
                // Reverse winding
                mesh.triangles = mesh.triangles.Reverse().ToArray();
                mesh.uv = mesh.uv.Reverse().ToArray();
                _normals[i] *= -1f;
            }

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            
            MeshCollider collider = _gameObjects[i].AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            _scale = Vector3.Magnitude(_vertices[0] - _vertices[2]);
        }
    }

    // Updates mesh(es) with provided vertices
    public void UpdateVertices(
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
            MeshCollider collider = _gameObjects[i].GetComponent<MeshCollider>();
            collider.sharedMesh = _meshes[i];
        }
    }

    public IEnumerator Reveal(Material material, GameObject breakPS) {
        if (type == Type.Invalid) { yield break; }
        yield return new WaitForSeconds(0.02f * _depth);
        SetMaterial(material);
        _ps = Complex.CreateGO(breakPS, _vertices[0], Quaternion.identity, _scale);
        _ps.transform.parent = _gameObjects[0].transform;
    }

    // Sets material
    public void SetMaterial(Material material) {
        if (type == Type.Invalid) return;
        for (int i = 0; i < _sideCount; i++) {
            MeshRenderer meshRenderer = _gameObjects[i].GetComponent<MeshRenderer>();
            meshRenderer.material = material;
        }
    }

    // Places flag(s)
    public void Flag(Dictionary<Vector2Int, GameObject[]> flags, GameObject flag = null) {
        if (type == Type.Invalid || _revealed) return;
        if (_flagged) {
            flags.Remove(new Vector2Int(_u,_v));
            Complex.DestroyGOs(_flags);
        } else if (flag != null) {
            // Points to where flag will be planted
            Vector3 stake = (_vertices[0] + _vertices[2]) / 2f;

            // Create flag for each side
            for (int i = 0; i < _sideCount; i++) {
                Vector3 flagPos = stake + _normals[i] * _scale/2f;
                Quaternion flagRot = Quaternion.LookRotation(_normals[i]) * Quaternion.AngleAxis(90, Vector3.up);
                _flags[i] = Complex.CreateGO(flag, flagPos, flagRot, _scale);
                _flags[i].transform.parent = _gameObjects[i].transform;
                _flags[i].name = "Flag";
            }
            flags.Add(new Vector2Int(_u,_v), _flags);
        }
        _flagged = !_flagged;
    }

    public void UpdateFlags() {
        if (type == Type.Invalid || _revealed || !_flagged) return;

    }
}
