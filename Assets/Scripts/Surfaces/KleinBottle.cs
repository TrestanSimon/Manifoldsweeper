using UnityEngine;
using static Unity.Mathematics.math;

public class KleinBottle : Complex {
    public override void Setup(int resU, int resV, Map initMap) {
        sideCount = 2;
        this.resU = resU;
        this.resV = resV;
        planar = false;
        GenerateVertices(initMap);
        GenerateQuads();
    }

    protected override void GenerateVertices(Map map) {
        vertices = new Vector3[resU+1, resV+1];
        for (int p = 0; p <= resU; p++) {
            float p1 = 4f*PI*(float)p / (float)resU;
            sincos(p1, out float sinp, out float cosp);
            for (int q = 0; q <= resV; q++) {
                float q1 = 2f*PI*(float)q / (float)resV;
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
        int u1 = u >= 0 ? u % resU : u + resU;
        int v1 = v >= 0 ? v % resV : v + resV;

        // Flips v when wrapping
        if (u % (2*resU) >= resU || u % (-2*resU) < 0)    
            v1 = resV - (v1 + 1);

        if (u1 >= 0 && u1 < resU && v1 >= 0 && v1 < resV) return quads[u1,v1];
        else return new Quad();
    }
}
