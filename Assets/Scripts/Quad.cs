using System;
using System.Linq;
using System.Collections;
using UnityEngine;

public class Quad {
    public enum Type {
        Invalid,
        Empty,
        Number,
        Mine
    }

    // Geometry-related fields
    private int _u, _v, _sideCount;
    private GameObject[] _gameObjects;
    private Vector3[] _vertices;
    private Mesh[] _meshes;

    // Game-related fields
    private Type _type;
    private int _number; // Number of mines in neighborhood
    private bool _revealed, _flagged, _exploded;
    private GameObject[] _flags;
    private GameObject _revealPS; // Particle system for tile reveal

    public int U {
        get => _u;
        private set {
            if (value < 0 || value > 99)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "U index range is between 0 and 99.");
            else _u = value;
        }
    }
    public int V {
        get => _v;
        private set {
            if (value < 0 || value > 99)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "V index range is between 0 and 99.");
            else _v = value;
        }
    }
    public int SideCount {
        set {
            if (value < 1 || value > 2)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Side count range is between 1 and 2.");
            _sideCount = value;
        }
    }
    private float _Scale {
        get => Vector3.Magnitude(_vertices[0] - _vertices[2]);
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
            if (value && _flagged)
                _flagged = false;
            _revealed = value;
        }
    }
    public bool Flagged {
        get => _flagged;
        set {
            if (!_revealed) {
                if (value && _flags == null)
                    _flags = new GameObject[_sideCount];
                else if (!value && _flags != null)
                    Complex.DestroyGOs(_flags);
                _flagged = value;
            }
        }
    }
    public bool Exploded {
        get => _exploded;
        set {
            if (_type == Quad.Type.Mine)
                _exploded = value;
        }
    }
    public bool Visited { get; set; }
    public int Depth { get; set; }

    // Constructor for Invalid Quads
    public Quad() {}

    // Normal constructor
    public Quad(
        int u, int v, int sideCount,
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3,
        Complex complex
    ) {
        U = u; V = v;
        SideCount = sideCount;

        _gameObjects = new GameObject[sideCount];
        _meshes = new Mesh[sideCount];
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
            _gameObjects[i].transform.parent = complex.transform;

            // For identifying Quad instance from GameObject
            Tag tag = _gameObjects[i].AddComponent<Tag>();
            tag.u = U; tag.v = V;

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

    // Delay revealing based on flood depth
    public IEnumerator DelayedReveal(Material material, GameObject breakPS = null) {
        if (type == Type.Invalid) yield break;
        yield return new WaitForSeconds(0.02f * Depth);

        SetMaterial(material);

        if (breakPS != null) {
            _revealPS = Complex.CreateGO(breakPS, _vertices[0], Quaternion.identity, _Scale);
            _revealPS.transform.parent = _gameObjects[0].transform;
        }
    }

    // Sets material
    public void SetMaterial(Material material) {
        for (int i = 0; i < _sideCount; i++)
            _gameObjects[i].GetComponent<MeshRenderer>().material = material;
    }

    // Places flag(s)
    public void Flag(GameObject flagPrefab,
        Material materialFlag, Material materialUnknown
    ) {
        if (type == Type.Invalid || Revealed) return;

        if (Flagged) { // Manual unflag
            Flagged = false;
            SetMaterial(materialUnknown);
        } else { // Manual flag
            Flagged = true;

            // Points to where flag will be planted
            Vector3 stake = (_vertices[0] + _vertices[2]) / 2f;

            // Create flag for each side
            for (int i = 0; i < _sideCount; i++) {
                Vector3 flagPos = stake + _meshes[i].normals[0] * _Scale/2f;
                Quaternion flagRot = Quaternion.LookRotation(_meshes[i].normals[0])
                    * Quaternion.AngleAxis(90, Vector3.up);

                _flags[i] = Complex.CreateGO(flagPrefab, flagPos, flagRot, _Scale);
                _flags[i].transform.parent = _gameObjects[i].transform;
                _flags[i].name = "Flag";
            }
            SetMaterial(materialFlag);
        }
    }
}
