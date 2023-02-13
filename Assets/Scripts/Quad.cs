using UnityEngine;

public class Quad {
    public enum Type {
        Invalid,
        Empty,
        Mine,
        Number
    }

    public GameObject go = new GameObject();
    public int u, v;
    public Vector3[] vertices;
    public Type type;
    public int number;
    public bool revealed;
    public bool flagged;
    public bool exploded;

    public GameObject flag;

    public Quad() {
        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
    }

    public void GenerateMesh(
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3
    ) {
        Mesh mesh = go.GetComponent<MeshFilter>().mesh;

        vertices = new Vector3[]{
            vert0, vert1,
            vert2, vert3
        };
        mesh.vertices = vertices;

        mesh.triangles = new int[]{
            0, 1, 2,
            2, 3, 0
        };

        mesh.uv = new Vector2[]{
            Vector2.zero, Vector2.up,
            Vector2.one, Vector2.right
        };

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        mesh.RecalculateNormals();
        
        MeshCollider collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }

    public void SetMaterial(Material material) {
        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.material = material;
    }
}
