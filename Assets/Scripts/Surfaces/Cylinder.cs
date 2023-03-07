using System.Collections;
using UnityEngine;

using static Unity.Mathematics.math;

public class Cylinder : Complex {
    // public new int ResU = 72, ResV = 8;
    public float radius;
    private float angleOffset = PI/2f; // Added to -2*PI*p/ResU
    // Necessary so that the p-u seam is at the top of the cylinder
    // and the mapping to a plane breaks the cylinder at this seam
    // cf minorOffset in Torus.cs

    public override void Setup(Camera cam, int ResU, int ResV) {
        sideCount = 2;
        this.ResU = ResU;
        this.ResV = ResV;
        radius = ResU/(4f*PI);
        planar = false;
    }

    public override void GenerateVertices() {
        vertices = new Vector3[ResU+1, ResV+1];
        for (int p = 0; p <= ResU; p++) {
            // Reversed sign is necessary so that tile orientation matches
            // that of the torus when it is mapped to a cylinder
            sincos(-2*PI*p/ResU + angleOffset, out float sinp, out float cosp);

            for (int q = 0; q <= ResV; q++) {
                vertices[p,q] = new Vector3(
                    radius * cosp,
                    radius * sinp,
                    (q - ResV/2f) / 2f
                );
            }
        }
    }

    public override Quad GetNeighbor(int u, int v) {
        // Wraps around u
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v;

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) { return quads[u1,v1]; }
        else { return new Quad(); }
    }

    public IEnumerator CylinderToPlane() {
        float time = 0f;
        float duration = 1f;
        float progress = 0f;

        while (time < duration) {
            progress = time / duration;
            UpdateVertices(CylinderToPlaneMap(progress, radius));

            time += Time.deltaTime;
            yield return null;
        }
        // Finalize mapping
        vertices = CylinderToPlaneMap(1, radius);
        UpdateVertices(vertices);
    }

    public override IEnumerator ToPlane() {
        yield return StartCoroutine(CylinderToPlane());
        planar = true;
    }
}
