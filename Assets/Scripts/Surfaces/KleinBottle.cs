using System.Collections;
using System.Collections.Generic;
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
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
        for (int u = 0; u <= ResU; u++) {
            float u1 = 4f*PI*(float)u / (float)ResU;
            sincos(u1, out float sinu, out float cosu);
            for (int v = 0; v <= ResV; v++) {
                float v1 = 2f*PI*(float)v / (float)ResV;
                sincos(v1, out float sinv, out float cosv);

                if (u1 < PI) {
                    vertices[u,v] = new Vector3(
                        (2.5f - 1.5f*cosu) * cosv,
                        (2.5f - 1.5f*cosu) * sinv,
                        -2.5f * sinu
                    );
                } else if (u1 < 2f*PI) {
                    vertices[u,v] = new Vector3(
                        (2.5f - 1.5f*cosu) * cosv,
                        (2.5f - 1.5f*cosu) * sinv,
                        3f*u1 - 3f*PI
                    );
                } else if (u1 < 3f*PI) {
                    vertices[u,v] = new Vector3(
                        -2f + (2f + cosv) * cosu,
                        sinv,
                        (2f + cosv) * sinu + 3f*PI
                    );
                } else {
                    vertices[u,v] = new Vector3(
                        -2f + 2f*cosu - cosv,
                        sinv,
                        -3f*u1 + 12f*PI
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
