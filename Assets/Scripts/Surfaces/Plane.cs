using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Plane : Complex {
    public Vector3 center;

    public override void Setup(Camera cam, int ResU, int ResV) {
        sideCount = 2;
        this.ResU = ResU;
        this.ResV = ResV;
        UpdateCamera(cam, true);
    }
    
    public override void GenerateVertices() {
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
        for (int u = 0; u <= ResU; u++) {
            for (int v = 0; v <= ResV; v++) {
                vertices[u,v] = new Vector3(
                    u, 0, v
                );
            }
        }
    }

    public override void UpdateCamera(Camera cam, bool force = false) {
        UpdateTopDownCamera(cam, force);
    }
}
