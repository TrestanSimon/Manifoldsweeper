using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Torus : Complex {
    public new static Dictionary<string, Map> MapDict {
        get => new Dictionary<string, Map>(){
            {"Flat", Map.Flat},
            {"Torus", Map.Torus}
        };
    }
    
    private float r, R;
    private float minorOffset = PI/2f; // Added to 2*PI*p/ResU
    // Necessary so that the p-u seam is at the top of the torus
    // and the mapping to a plane breaks the cylinder at this seam

    public override int ResU {
        get => resU;
        protected set {
            resU = Mathf.Clamp(value, 3, 99);
        }
    }
    public override int ResV {
        get => resV;
        protected set {
            resV = Mathf.Clamp(value, 3, 99);
        }
    }

    public override void Setup(int resU, int resV, Map initMap) {
        sideCount = 2;
        this.ResU = Mathf.Min(resU, resV);
        this.ResV = Mathf.Max(resU, resV);
        r = this.ResU / 16f;
        R = this.ResV / 16f;
        CurrentMap = initMap;
        InitVertices(initMap);
        InitTiles();
    }
    
    protected override void InitVertices(Map map) {
        switch(map) {
            case Map.Flat: vertices = CylinderInvoluteMap(1, r); break;
            case Map.Torus: vertices = TorusInvolutesMap(0, R, r, minorOffset); break;
        }
    }

    public override Tile GetNeighbor(int u, int v) {
        // Wraps around u and v
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v >= 0 ? v % ResV : v + ResV;

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) return tiles[u1,v1];
        else return new Tile();
    }

    public override IEnumerator ReMap(Map newMap) {
        if (newMap == CurrentMap) yield return null;
        else if (newMap == Map.Flat) {
            yield return StartCoroutine(TorusToCylinder());
            yield return StartCoroutine(CylinderToPlane());
        } else if (newMap == Map.Torus) {
            yield return StartCoroutine(CylinderToPlane(true));
            yield return StartCoroutine(TorusToCylinder(true));
        }
        CurrentMap = newMap;
    }

    public override void RepeatU() {
        CopyDepthU++;
        for (int v = 0; v < resV; v++) {
            for (int u = 0; u < resU; u++) {
                Vector3 extraOffset = new Vector3();
                for (int index = -CopyDepthV; index <= CopyDepthV; index++) {
                    extraOffset = Offset[1] * index;

                    tiles[u,v].CreateClone(Offset[0] * CopyDepthU + extraOffset);
                    tiles[u,v].CreateClone(-1*Offset[0] * CopyDepthU + extraOffset);
                }
            }
        }

        CalculateCorners(CopyDepthU, CopyDepthV);
    }

    public override void RepeatV() {
        CopyDepthV++;
        for (int v = 0; v < resV; v++) {
            for (int u = 0; u < resU; u++) {
                Vector3 extraOffset = new Vector3();
                for (int index = -CopyDepthU; index <= CopyDepthU; index++) {
                    extraOffset = Offset[0] * index;

                    tiles[u,v].CreateClone(Offset[1] * CopyDepthV + extraOffset);
                    tiles[u,v].CreateClone(-1*Offset[1] * CopyDepthV + extraOffset);
                }
            }
        }

        CalculateCorners(CopyDepthU, CopyDepthV);
    }

    public override void CalculateCorners(int depthU, int depthV) {
        Vector3 offset = Offset[0] * depthU + Offset[1] * depthV;
        _corners = new Vector3[]{
            vertices[0,ResV] + offset,
            vertices[ResU,ResV] - offset,
            vertices[ResU,0] - offset,
            vertices[0,0] + offset,
        };
    }

    public IEnumerator TorusToCylinder(bool reverse = false) {
        float time = 0f;
        float duration = 1f;
        float t;

        while (time < duration) {
            t = reverse ? 1f - time/duration : time/duration;
            t = t * t * (3f - 2f * t);
            UpdateVertices(TorusInvolutesMap(t, R, r, minorOffset));

            time += Time.deltaTime;
            yield return null;
        }

        // Finalize mapping
        vertices = TorusInvolutesMap(reverse ? 0f : 1f, R, r, minorOffset);
        UpdateVertices(vertices);
    }

    public IEnumerator CylinderToPlane(bool reverse = false) {
        float time = 0f;
        float t;
        float duration = 1f;

        while (time < duration) {
            t = reverse ? 1f - time / duration : time / duration;
            t = t * t * (3f - 2f * t);
            UpdateVertices(CylinderInvoluteMap(t, r));

            time += Time.deltaTime;
            yield return null;
        }
        // Finalize mapping
        vertices = CylinderInvoluteMap((reverse ? 0f : 1f), r);
        UpdateVertices(vertices);
    }
}
