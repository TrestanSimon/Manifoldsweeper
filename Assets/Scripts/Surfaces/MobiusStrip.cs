using UnityEngine;

using static Unity.Mathematics.math;

public class MobiusStrip : Complex {
    public float R, tau;

    public override void Setup(int resU, int resV) {
        sideCount = 2;
        this.resU = resU;
        this.resV = resV;
        R = resU / 72f;
        tau = 16f / (float)resV;
        planar = false;
        GenerateVertices();
        GenerateQuads();
    }

    protected override void GenerateVertices() {
        vertices = new Vector3[resU+1, resV+1];
        for (int p = 0; p <= resU; p++) {
            sincos(2*PI*p / resU, out float sinp, out float cosp);
            sincos(PI*p / resU, out float sinp2, out float cosp2);

            for (int q = 0; q <= resV; q++) {
                float q1 = q / (float)resV - 0.5f;
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
        int u1 = u >= 0 ? u % resU : u + resU;
        int v1 = v;

        if (u % (2*resU) >= resU || u % (-2*resU) < 0) {
            // Flips v when wrapping
            v1 = resV - (v1 + 1);
        }

        if (u1 >= 0 && u1 < resU && v1 >= 0 && v1 < resV) { return quads[u1,v1]; }
        else { return new Quad(); }
    }
}
