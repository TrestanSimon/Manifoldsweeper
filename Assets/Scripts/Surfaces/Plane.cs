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
        planar = true;
    }
    
    public override void GenerateVertices() {
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
        for (int u = 0; u <= ResU; u++) {
            for (int v = 0; v <= ResV; v++) {
                vertices[u,v] = new Vector3(
                    -u + ResU/2f,
                    0,
                    -v + ResV/2f
                ) / 2f;
            }
        }
    }

    // Already a plane...
    public override IEnumerator ToPlane() {
        yield return null;
    }
}
