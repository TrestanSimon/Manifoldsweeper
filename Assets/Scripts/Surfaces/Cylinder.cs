using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Cylinder : Complex {
    public new static Dictionary<string, Map> MapDict {
        get => new Dictionary<string, Map>(){
            {"Flat", Map.Flat},
            {"Cylinder", Map.Cylinder}
        };
    }
    
    public float radius;
    private float angleOffset = PI/2f; // Added to -2*PI*p/resU
    // Necessary so that the p-u seam is at the top of the cylinder
    // and the mapping to a plane breaks the cylinder at this seam
    // cf minorOffset in Torus.cs

    public override void Setup(int resU, int resV, Map initMap) {
        sideCount = 2;
        this.resU = resU;
        this.resV = resV;
        radius = resU/(4f*PI);
        currentMap = initMap;
        GenerateVertices(initMap);
        GenerateQuads();
    }

    protected override void GenerateVertices(Map map) {
        vertices = new Vector3[resU+1, resV+1];
        for (int p = 0; p <= resU; p++) {
            // Reversed sign is necessary so that tile orientation matches
            // that of the torus when it is mapped to a cylinder
            sincos(-2*PI*p/resU + angleOffset, out float sinp, out float cosp);

            for (int q = 0; q <= resV; q++) {
                vertices[p,q] = new Vector3(
                    radius * cosp,
                    radius * sinp,
                    (q - resV/2f) / 2f
                );
            }
        }
    }

    public override Quad GetNeighbor(int u, int v) {
        // Wraps around u
        int u1 = u >= 0 ? u % resU : u + resU;
        int v1 = v;

        if (u1 >= 0 && u1 < resU && v1 >= 0 && v1 < resV) return quads[u1,v1];
        else return new Quad();
    }

    public override void RepeatComplex() {
        // if (!planar) return;
        Instantiate(gameObject,
            2f*(vertices[0,resV/2] + radius*Vector3.up),
            Quaternion.identity);
        Instantiate(gameObject,
            -2f*(vertices[0,resV/2] + radius*Vector3.up),
            Quaternion.identity);
    }

    public IEnumerator CylinderToPlane(bool reverse = false) {
        float time = 0f;
        float duration = 1f;
        float progress;

        while (time < duration) {
            progress = time / duration;
            UpdateVertices(CylinderToPlaneMap(
                (reverse ? 1f - progress : progress), radius));

            time += Time.deltaTime;
            yield return null;
        }
        // Finalize mapping
        vertices = CylinderToPlaneMap((reverse ? 0f : 1f), radius);
        UpdateVertices(vertices);
        if (!reverse)
            RepeatComplex();
    }

    public override IEnumerator ToPlane() {
        if (currentMap == Map.Flat) {
            yield return StartCoroutine(CylinderToPlane(true));
            currentMap = Map.Cylinder;
        }
        else {
            yield return StartCoroutine(CylinderToPlane(false));
            currentMap = Map.Flat;
        }
    }
}
