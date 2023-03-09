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

    // Geometry-related fields
    private int _u, _v;
    private GameObject[] _gameObjects;
    private Vector3[] _vertices;
    private Mesh[] _meshes;
    private int _sideCount;
    private float _scale;

    // Game-related fields
    private Type _type;
    private int _number, _depth;
    private bool _revealed, _flagged, _exploded, _visited;
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
        set {
            if (_type == Quad.Type.Mine)
                _exploded = value;
        }
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

    // Normal constructor
    public Quad(
        int u, int v, int sideCount,
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3,
        Transform parent
    ) {
        if (1 > sideCount ||  sideCount > 2)
            throw new ArgumentOutOfRangeException(nameof(sideCount),
                "Quads must have either 1 or 2 sides.");

        _sideCount = sideCount;
        _gameObjects = new GameObject[sideCount];
        _meshes = new Mesh[sideCount];
        _flags = new GameObject[sideCount];

        _vertices = new Vector3[]{
            vert0, vert1,
            vert2, vert3
        };

        // Winding for triangles and UV coordinates
        // 1 --> 2
        // |  /  |
        // 0 <-- 3
        int[] triangles = new int[]{
            0, 1, 2,
            2, 3, 0
        };
        Vector2[] uvCoords = new Vector2[]{
            Vector2.zero, Vector2.up,
            Vector2.one, Vector2.right
        };
    
        for (int i = 0; i < sideCount; i++) {
            _gameObjects[i] = new GameObject();
            _gameObjects[i].name = $"Quad {i} ({u}, {v})";
            
            // Make quads child of Complex GameObject
            _gameObjects[i].transform.parent = parent;

            // For identifying Quad instance from GameObject
            Tag tag = _gameObjects[i].AddComponent<Tag>();
            _u = tag.u = u;
            _v = tag.v = v;

            _gameObjects[i].AddComponent<MeshRenderer>();
            Mesh mesh = _gameObjects[i].AddComponent<MeshFilter>().mesh;
            _meshes[i] = mesh;

            mesh.vertices = _vertices;
            mesh.triangles = triangles;
            mesh.uv = uvCoords;

            if (i == 1) {
                // Reverse winding
                mesh.triangles = mesh.triangles.Reverse().ToArray();
                mesh.uv = mesh.uv.Reverse().ToArray();
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

    public IEnumerator DelayedReveal(Material material, GameObject breakPS) {
        if (type == Type.Invalid) yield break;
        yield return new WaitForSeconds(0.02f * _depth);

        SetMaterial(material);
        _ps = Complex.CreateGO(breakPS, _vertices[0], Quaternion.identity, _scale);
        _ps.transform.parent = _gameObjects[0].transform;
    }

    // Sets material
    public void SetMaterial(Material material) {
        if (type == Type.Invalid) return;
        for (int i = 0; i < _sideCount; i++)
            _gameObjects[i].GetComponent<MeshRenderer>().material = material;
    }

    // Places flag(s)
    public void Flag(Dictionary<(int u, int v), GameObject[]> flags, GameObject flag = null) {
        if (type == Type.Invalid || _revealed) return;
        if (_flagged) {
            flags.Remove((_u, _v));
            Complex.DestroyGOs(_flags);
        } else if (flag != null) {
            // Points to where flag will be planted
            Vector3 stake = (_vertices[0] + _vertices[2]) / 2f;

            // Create flag for each side
            for (int i = 0; i < _sideCount; i++) {
                Vector3 flagPos = stake + _meshes[i].normals[0] * _scale/2f;
                Quaternion flagRot = Quaternion.LookRotation(_meshes[i].normals[0])
                    * Quaternion.AngleAxis(90, Vector3.up);

                _flags[i] = Complex.CreateGO(flag, flagPos, flagRot, _scale);
                _flags[i].transform.parent = _gameObjects[i].transform;
                _flags[i].name = "Flag";
            }
            flags.Add((_u, _v), _flags);
        }
        _flagged = !_flagged;
    }
}
