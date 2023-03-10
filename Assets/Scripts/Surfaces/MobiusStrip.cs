using UnityEngine;

using static Unity.Mathematics.math;

public class MobiusStrip : Complex {
    public enum Map {
        Planar,
        Strip,
        CrossCap,
        Sudanese
    }

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

        // Flips v when wrapping
        if (u % (2*resU) >= resU || u % (-2*resU) < 0)
            v1 = resV - (v1 + 1);

        if (u1 >= 0 && u1 < resU && v1 >= 0 && v1 < resV) return quads[u1,v1];
        else return new Quad();
    }

    private void SudaneseVertices() {
        float x, y, z, w, ys, ws;
        vertices = new Vector3[resU+1, resV+1];
        for (int p = 0; p <= resU; p++) {
            sincos(2*PI*p / resU, out float sin2p, out float cos2p);
            sincos(PI*p / resU, out float sinp, out float cosp);

            for (int q = 0; q <= resV; q++) {
                sincos(PI*q / resV, out float sinq, out float cosq);

                // Embed a Mobius strip in the 3-sphere
                x = sinq * cos2p;
                y = sinq * sin2p;
                z = cosq * cosp;
                w = cosq * sinp;

                // Stereographic projection
                ys = (w + y) / sqrt(2f);
                ws = (w - y) / sqrt(2f);
                vertices[p,q] = new Vector3(
                    x / (1 - ws),
                    ys / (1 - ws),
                    z / (1 - ws)
                );
            }
        }
    }
}
