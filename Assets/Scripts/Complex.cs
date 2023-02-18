using UnityEngine;

public abstract class Complex : MonoBehaviour {
    public int ResU = 16, ResV = 16*3;

    public Vector3[,] vertices;
    public Vector3[,] normals;
    public Quad[,] quads;
    public Vector3[,] quadNormals;

    public float minFOV = 10f;
    public float maxFOV = 90f;
    public float sensitivity = 1f;
    public float FOV = 60f;
    public Vector3 mousePos;
    public Vector3 dmousePos;
    public float scroll;
    public Camera cam;

    public void GenerateComplex() {
        GenerateVertices();
        GenerateQuads();
    }

    // Generates array of 3-vectors pointing to vertices
    // Position of vertices depend on the surface
    public abstract void GenerateVertices();

    // Generates quads from vertices
    private void GenerateQuads() {
        quads = new Quad[ResU, ResV];
        quadNormals = new Vector3[ResU, ResV];

        for (int v = 0; v < ResV; v++) {
            for (int u = 0; u < ResU; u++) {
                quadNormals[u,v] = (normals[u,v] + normals[u+1,v] + normals[u+1,v+1] + normals[u,v+1])/4;
                quads[u,v] = new Quad(
                    u, v,
                    vertices[u,v], vertices[u+1,v],
                    vertices[u+1,v+1], vertices[u,v+1],
                    quadNormals[u,v]
                );
                quads[u,v].go.transform.parent = gameObject.transform;
            }
        }
    }

    // Returns a Quad given a coordinate neighboring another Quad
    // Can be overridden to glue edges together
    public virtual Quad GetNeighbor(int u, int v) {
        if (u >= 0 && u < ResU && v >= 0 && v < ResV) { return quads[u,v]; }
        else { return new Quad(); } // returns Invalid Quad
    }

    // Identifies the Quad instance the cursor is over
    // returns null if there is no Quad
    public Quad MouseIdentify() {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            return Identify(hit.collider.gameObject);
        } else {
            return null;
        }
    }

    // Identifies the Quad instance associated with a GameObject
    private Quad Identify(GameObject go) {
        Tag tag = go.GetComponent<Tag>();
        return quads[tag.u, tag.v];
    }

    public static void DestroyFlag(GameObject go) {
        Destroy(go);
    }
    public static GameObject CreateFlag(GameObject go, Vector3 pos, Quaternion rot){
        return Instantiate(go, pos, rot);
    }

    public virtual void UpdateCamera(bool force = false) {
        cam = Camera.main;
        scroll = Input.mouseScrollDelta.y * sensitivity * -1f;
        if (Input.GetMouseButtonDown(2)) {
            mousePos = Input.mousePosition;
        }
        if ((Input.GetMouseButton(2) && mousePos != null) || force) {
            dmousePos = Input.mousePosition - mousePos;
            cam.transform.RotateAround(Vector3.zero, Vector3.up, dmousePos.x/2f*Time.deltaTime);
            cam.transform.RotateAround(Vector3.zero, Camera.main.transform.right, -dmousePos.y/2f*Time.deltaTime);
        }
        if (scroll != 0f) {
            FOV += scroll;
            FOV = Mathf.Clamp(FOV, minFOV, maxFOV);
            cam.fieldOfView = FOV;
        }
    }
}
