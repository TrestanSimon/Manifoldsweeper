using UnityEngine;

using static Unity.Mathematics.math;

public class KleinBottle : Complex {
    // public new int ResU = 48, ResV = 16;

    public override void Setup(Camera cam, int ResU, int ResV) {
        sideCount = 2;
        this.ResU = ResU;
        this.ResV = ResV;
        planar = false;
    }

    public override void GenerateVertices() {
        vertices = new Vector3[ResU+1, ResV+1];
        for (int p = 0; p <= ResU; p++) {
            float p1 = 4f*PI*(float)p / (float)ResU;
            sincos(p1, out float sinp, out float cosp);
            for (int q = 0; q <= ResV; q++) {
                float q1 = 2f*PI*(float)q / (float)ResV;
                sincos(q1, out float sinq, out float cosq);

                if (p1 < PI) {
                    vertices[p,q] = new Vector3(
                        (2.5f - 1.5f*cosp) * cosq,
                        (2.5f - 1.5f*cosp) * sinq,
                        -2.5f * sinp
                    );
                } else if (p1 < 2f*PI) {
                    vertices[p,q] = new Vector3(
                        (2.5f - 1.5f*cosp) * cosq,
                        (2.5f - 1.5f*cosp) * sinq,
                        3f*p1 - 3f*PI
                    );
                } else if (p1 < 3f*PI) {
                    vertices[p,q] = new Vector3(
                        -2f + (2f + cosq) * cosp,
                        sinq,
                        (2f + cosq) * sinp + 3f*PI
                    );
                } else {
                    vertices[p,q] = new Vector3(
                        -2f + 2f*cosp - cosq,
                        sinq,
                        -3f*p1 + 12f*PI
                    );
                }
            }
        }
    }

    public override Quad GetNeighbor(int u, int v) {
        // Wraps around u and v
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v >= 0 ? v % ResV : v + ResV;

        if (u % (2*ResU) >= ResU || u % (-2*ResU) < 0) {
            // Flips v when wrapping
            v1 = ResV - (v1 + 1);
        }

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) { return quads[u1,v1]; }
        else { return new Quad(); }
    }
}
