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
    public Vector3 normal;
    public Mesh mesh;
    public Type type;
    public int number;
    public bool revealed;
    public bool flagged;
    public bool exploded;

    public GameObject flag;

    public Quad(
        int u, int v,
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3,
        Vector3 normal
    ) {
        MeshFilter filter = go.AddComponent<MeshFilter>();
        Mesh mesh = filter.mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        // For identifying Quad instance from GameObject
        Tag tag = go.AddComponent<Tag>();
        this.u = tag.u = u;
        this.v = tag.v = v;
        this.normal = normal;

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

    public void Flag(GameObject flag = null) {
        if (type == Type.Invalid || revealed) {return;}
        if (flagged) {
            QuadHandler.flags.Remove(new Vector2Int(u,v));
            QuadHandler.DestroyFlag(this.flag);
        } else if (flag != null) {
            Vector3 stake = (vertices[0] + vertices[2]) / 2f;
            Vector3 flagPos = stake + normal*0.2f;
            Quaternion flagRot = Quaternion.LookRotation(normal) * Quaternion.AngleAxis(90, Vector3.up);
            this.flag = QuadHandler.CreateFlag(flag, flagPos, flagRot);
            QuadHandler.flags.Add(new Vector2Int(u,v), this.flag);
        }
        flagged = !flagged;
    }
}
