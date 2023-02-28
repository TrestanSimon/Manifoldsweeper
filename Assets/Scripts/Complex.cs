using UnityEngine;

public abstract class Complex : MonoBehaviour {
    public int ResU = 16, ResV = 16*3;

    public Vector3[,] vertices;
    public Vector3[,] normals;
    public Quad[,] quads;
    public int sideCount;

    public float minFOV = 10f;
    public float maxFOV = 90f;
    public float sensitivity = 1f;
    public float FOV = 60f;
    public Vector3 mousePos;
    public Vector3 dmousePos;
    public float scroll;

    // Setup() method needed to set sideCount
    public abstract void Setup(Camera cam, int ResU, int ResV);

    public Game Gamify(int mineCount) {
        Game game;
        gameObject.TryGetComponent<Game>(out game);
        if (game == null) {
            game = gameObject.AddComponent<Game>();
        }
        game.Setup(this, mineCount);
        return game;
    }

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

        for (int v = 0; v < ResV; v++) {
            for (int u = 0; u < ResU; u++) {
                quads[u,v] = new Quad(
                    u, v, sideCount,
                    vertices[u,v], vertices[u+1,v],
                    vertices[u+1,v+1], vertices[u,v+1]
                );
                // Make quads child of Complex GameObject
                foreach (GameObject quad in quads[u,v].gameObjects) {
                    quad.transform.parent = gameObject.transform;
                }
            }
        }
    }

    // Transforms vertices and quads to UV plane
    public void MapToPlane() {
        // Transforms vertices
        for (int u = 0; u <= ResU; u++) {
            for (int v = 0; v <= ResV; v++) {
                vertices[u,v] = new Vector3(
                    u, 0, v
                );
            }
        }
        // Transforms quads
        for (int u = 0; u < ResU; u++) {
            for (int v = 0; v < ResV; v++) {
                quads[u,v].UpdateVertices(
                    vertices[u,v], vertices[u+1,v],
                    vertices[u+1,v+1], vertices[u,v+1]
                );
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

    public static void DestroyFlags(GameObject[] gos) {
        foreach (GameObject go in gos) { Destroy(go); }
    }

    public static GameObject CreateFlag(GameObject go, Vector3 pos, Quaternion rot, float scale){
        GameObject flag = Instantiate(go, pos, rot);
        flag.transform.localScale *= scale;
        return flag;
    }

    // Default camera
    public virtual void UpdateCamera(Camera cam, bool force = false) {
        scroll = Input.mouseScrollDelta.y * sensitivity * -1f;
        if (Input.GetMouseButtonDown(0)) {
            mousePos = Input.mousePosition;
        }
        if ((Input.GetMouseButton(0) && mousePos != null) || force) {
            dmousePos = Input.mousePosition - mousePos;
            cam.transform.RotateAround(Vector3.zero, Vector3.up, dmousePos.x/2f*Time.deltaTime);
            cam.transform.RotateAround(Vector3.zero, Camera.main.transform.right, -dmousePos.y/2f*Time.deltaTime);
        }
        if (scroll != 0f) {
            cam.transform.position -= scroll * cam.transform.forward.normalized;
        }
    }

    // A top-down camera
    // if force is true, camera resets position
    public void UpdateTopDownCamera(Camera cam, bool force = false) {
        if (force) {
            cam.transform.position = new Vector3(ResU/2f, 30f, ResV/2f);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 270f);
        }

        scroll = Input.mouseScrollDelta.y * sensitivity * -1f;
        if (Input.GetMouseButtonDown(0)) {
            mousePos = Input.mousePosition;
        }

        if ((Input.GetMouseButton(0) && mousePos != null) || force) {
            dmousePos = Input.mousePosition - mousePos;
            cam.transform.position += dmousePos.x/10f*Time.deltaTime * Vector3.forward;
            cam.transform.position += dmousePos.y/10f*Time.deltaTime * Vector3.left;
        }

        if (scroll != 0f) {
            cam.transform.position += scroll * Vector3.up;
        }
    }
}
