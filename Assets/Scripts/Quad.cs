using UnityEngine;
using System.Linq;
using System.Collections.Generic;

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
    public Vector3 normal;
    public Mesh[] meshes;
    public int sideCount = 1;

    public Type type;
    public int number;
    public bool revealed;
    public bool flagged;
    public bool exploded;

    public GameObject flag;

    public Quad() {}

    public Quad(
        int u, int v, int sideCount,
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3
    ) {
        this.sideCount = sideCount;
        gameObjects = new GameObject[sideCount];
        meshes = new Mesh[sideCount];

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
            gameObjects[i].AddComponent<MeshRenderer>();

            mesh.vertices = vertices;
            mesh.triangles = winding;
            mesh.uv = uvCoords;

            if (i % 2 == 1) {
                // Reverse winding
                mesh.triangles = mesh.triangles.Reverse().ToArray();
                mesh.uv = mesh.uv.Reverse().ToArray();
            }

            normal = Vector3.Cross(vert0 - vert1, vert0 - vert2).normalized;

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh.RecalculateNormals();
            
            MeshCollider collider = gameObjects[i].AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }
    }

    public virtual void SetMaterial(Material material) {
        for (int i = 0; i < sideCount; i++) {
            MeshRenderer meshRenderer = gameObjects[i].GetComponent<MeshRenderer>();
            meshRenderer.material = material;
        }
    }

    public void Flag(Dictionary<Vector2Int, GameObject> flags, GameObject flag = null) {
        if (type == Type.Invalid || revealed) {return;}
        if (flagged) {
            flags.Remove(new Vector2Int(u,v));
            Complex.DestroyFlag(this.flag);
        } else if (flag != null) {
            Vector3 stake = (vertices[0] + vertices[2]) / 2f;
            Vector3 flagPos = stake + normal*0.2f;
            Quaternion flagRot = Quaternion.LookRotation(normal) * Quaternion.AngleAxis(90, Vector3.up);
            this.flag = Complex.CreateFlag(flag, flagPos, flagRot);
            flags.Add(new Vector2Int(u,v), this.flag);
        }
        flagged = !flagged;
    }
}
