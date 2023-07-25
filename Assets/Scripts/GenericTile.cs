using UnityEngine;

public class GenericTile : Quad {
    protected GameObject[] _flags;
    protected Cloud[] _clouds;
    protected GameObject _revealPS; // Particle system for tile reveal

    // Constructor for Invalid Tiles
    public GenericTile() {}

    // Normal constructor
    public GenericTile(
        int u, int v, int sideCount,
        Vector3[] vertices,
        Transform parent
    ) : base(vertices) {
        _clouds = new Cloud[_sideCount];

        for (int i = 0; i < sideCount; i++) {
            _gameObjects[i].name = $"Quad {i} ({u}, {v})";
            
            // Make tiles child of Complex GameObject
            _gameObjects[i].transform.parent = parent;

            // For identifying tile instance from GameObject
            Tag tag = _gameObjects[i].AddComponent<Tag>();
            tag.u = u; tag.v = v;

            GenerateCloud(i);
        }
    }

    // Updates mesh(es) with provided vertices
    public override void UpdateVertices(
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3
    ) {
        base.UpdateVertices(vert0, vert1, vert2, vert3);
        foreach (Cloud cloud in _clouds) UpdateClouds();
    }

    public override void SetMaterial(Material material) {
        base.SetMaterial(material);
    }

    private void GenerateCloud(int i) {
        Vector3 altitude = _meshes[i].normals[0] * _Scale/10f;
        _clouds[i] = new Cloud(
            new Vector3[] {
                _vertices[0] + altitude,
                _vertices[1] + altitude,
                _vertices[2] + altitude,
                _vertices[3] + altitude
            }
        );
        _clouds[i].Parent(_gameObjects[i]);
    }

    private void UpdateClouds() {
        for (int i = 0; i < _clouds.Length; i++) {
            Vector3 altitude = _meshes[i].normals[0] * _Scale/10f;
            _clouds[i].UpdateVertices(
                    _vertices[0] + altitude,
                    _vertices[1] + altitude,
                    _vertices[2] + altitude,
                    _vertices[3] + altitude
            );
        }
    }

    public void Reveal(Material material, GameObject breakPS = null) {
        SetMaterial(material);

        // Deactivate clouds
        foreach (Cloud cloud in _clouds)
            cloud.Active(false);

        if (breakPS != null) {
            _revealPS = Complex.CreateGO(breakPS, _vertices[0], Quaternion.identity, _Scale);
            _revealPS.transform.parent = _gameObjects[0].transform;
        }
    }

    public void PlaceFlags(GameObject flagPrefab, Material materialFlag) {
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

    public virtual void UpdateFlags() {
        Vector3 stake = (_vertices[0] + _vertices[2]) / 2f;
        for (int i = 0; i < _sideCount; i++) {
            _flags[i].transform.position = stake + _meshes[i].normals[0] * _Scale/2f;
            _flags[i].transform.rotation = Quaternion.LookRotation(_meshes[i].normals[0])
                * Quaternion.AngleAxis(90, Vector3.up);
        }
    }
}