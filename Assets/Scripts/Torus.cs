using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Torus : Complex {
    public float r = 1f, R = 3f;
    
    public override void GenerateVertices() {
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
        for (int u = 0; u <= ResU; u++) {
            sincos(2*PI*u / ResU, out float sinu, out float cosu);
            float minor = R + r*cosu;

            for (int v = 0; v <= ResV; v++) {
                sincos(2*PI*v / ResV, out float sinv, out float cosv);

                vertices[u,v] = new Vector3(
                    minor * cosv,
                    r * sinu,
                    minor * sinv
                );
                normals[u,v] = new Vector3(
                    cosu * cosv,
                    sinu,
                    cosu * sinv
                );
            }
        }
    }

    public override Quad GetNeighbor(int u, int v) {
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v >= 0 ? v % ResV : v + ResV;

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) { return quads[u1,v1]; }
        else { return null; }
    }
}
