using UnityEngine;

using static Unity.Mathematics.math;

public class MobiusStrip : Complex {
    // public new int ResU = 72, ResV = 8;
    public float R, tau;

    public override void Setup(Camera cam, int ResU, int ResV) {
        sideCount = 2;
        this.ResU = ResU;
        this.ResV = ResV;
        R = ResU / 72f;
        tau = 16f / (float)ResV;
        planar = false;
    }

    public override void GenerateVertices() {
        vertices = new Vector3[ResU+1, ResV+1];
        for (int p = 0; p <= ResU; p++) {
            sincos(2*PI*p / ResU, out float sinp, out float cosp);
            sincos(PI*p / ResU, out float sinp2, out float cosp2);

            for (int q = 0; q <= ResV; q++) {
                float q1 = q / (float)ResV - 0.5f;
                float minor = R + q1/tau * cosp2;

                vertices[p,q] = 5f * new Vector3(
                    minor * cosp,
                    q1/tau * sinp2,
                    minor * sinp
                );
            }
        }
    }

    public override Quad GetNeighbor(int u, int v) {
        // Wraps around u
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v;

        if (u % (2*ResU) >= ResU || u % (-2*ResU) < 0) {
            // Flips v when wrapping
            v1 = ResV - (v1 + 1);
        }

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) { return quads[u1,v1]; }
        else { return new Quad(); }
    }
}
