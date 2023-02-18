using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleQuad : Quad {
    public GameObject goFlip;
    public Vector3 normalFlip;
    public Mesh meshFlip;

    public DoubleQuad() {}

    public DoubleQuad(
        int u, int v,
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3,
        Vector3 normal
    ) {
        go = new GameObject();
        goFlip = new GameObject();

        go.name = "Quad " + u.ToString() + ", " + v.ToString();
        goFlip.name = "Quad2 " + u.ToString() + ", " + v.ToString();
        
        MeshFilter filter = go.AddComponent<MeshFilter>();
        MeshFilter filterFlip = goFlip.AddComponent<MeshFilter>();
        Mesh mesh = filter.mesh;
        Mesh meshFlip = filterFlip.mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        MeshRenderer rendererFlip = goFlip.AddComponent<MeshRenderer>();

        // For identifying Quad instance from GameObject
        Tag tag = go.AddComponent<Tag>();
        Tag tagFlip = goFlip.AddComponent<Tag>();
        this.u = tag.u = tagFlip.u = u;
        this.v = tag.v = tagFlip.v = v;
        this.normal = normal;
        this.normalFlip = -1f * normal;

        vertices = new Vector3[]{
            vert0, vert1,
            vert2, vert3
        };
        mesh.vertices = meshFlip.vertices = vertices;

        // 1 --> 2
        // |  /  |
        // 0 <-- 3
        mesh.triangles = new int[]{
            0, 1, 2,
            2, 3, 0
        };
        meshFlip.triangles = new int[]{
            0, 3, 2,
            2, 1, 0
        };
        
        mesh.uv = meshFlip.uv = new Vector2[]{
            Vector2.zero, Vector2.up,
            Vector2.one, Vector2.right
        };

        mesh.RecalculateBounds();
        meshFlip.RecalculateBounds();
        mesh.RecalculateTangents();
        meshFlip.RecalculateTangents();
        mesh.RecalculateNormals();
        meshFlip.RecalculateNormals();
        
        MeshCollider collider = go.AddComponent<MeshCollider>();
        MeshCollider colliderFlip = goFlip.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        colliderFlip.sharedMesh = meshFlip;
    }

    public override void SetMaterial(Material material) {
        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        MeshRenderer meshRendererFlip = goFlip.GetComponent<MeshRenderer>();
        meshRenderer.material = meshRendererFlip.material = material;
    }
}
