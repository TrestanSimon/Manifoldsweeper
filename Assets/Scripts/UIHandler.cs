using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHandler : MonoBehaviour {
    private GameObject board;
    private Transform panel, topPanel;
    private Transform manifoldsPanel;
    private Transform mapsPanel;
    private Transform manifoldMaps;
    private Transform gamePanel;
    private Transform customInputs;

    
    private int selectedManifold, selectedMap;
    private int selectedDifficulty;
    private int ResU, ResV, mineCount;
    private (int u, int v, int mines)[,] manifoldDifficulties;

    private Game game;
    private Complex complex;
    private Button mapButton;
    private TMP_Text timerText, flagText;
    private TMP_InputField[] inputFields;
    private TMP_Dropdown manifoldDropdown, difficultyDropdown, mapDropdown;
    private Animator animator;
    private bool panelOpen;

    private CameraHandler cameraHandler;
    private List<Coroutine> coroutines;

    private void Awake() {
        coroutines = new List<Coroutine>();

        // Panel data
        panel = transform.Find("Panel");

        manifoldsPanel = panel.Find("Manifolds Panel");
        manifoldDropdown = manifoldsPanel.Find("Manifold Dropdown").gameObject.GetComponent<TMP_Dropdown>();

        mapsPanel = panel.Find("Maps Panel");
        manifoldMaps = mapsPanel.Find("Maps Dropdowns");
        mapButton = mapsPanel.Find("Map Button").GetComponent<Button>();

        gamePanel = panel.Find("Game Panel");
        difficultyDropdown = gamePanel.Find("Difficulty Dropdown").GetComponent<TMP_Dropdown>();
        customInputs = gamePanel.Find("Custom Inputs");
        inputFields = customInputs.GetComponentsInChildren<TMP_InputField>();

        animator = panel.GetComponent<Animator>();

        // Top panel data
        topPanel = transform.Find("Top Panel");
        timerText = topPanel.Find("Timer Label").GetComponent<TMP_Text>();
        flagText = topPanel.Find("Flag Label").GetComponent<TMP_Text>();

        cameraHandler = Camera.main.GetComponent<CameraHandler>();

        manifoldDifficulties = new (int,int,int)[5,3];

        // Plane
        manifoldDifficulties[0,0] = (8, 8, 12);
        manifoldDifficulties[0,1] = (16, 16, 42);
        manifoldDifficulties[0,2] = (16, 32, 98);

        // Cylinder
        manifoldDifficulties[1,0] = (8, 8, 12);
        manifoldDifficulties[1,1] = (16, 16, 42);
        manifoldDifficulties[1,2] = (32, 16, 98);

        // Torus
        manifoldDifficulties[2,0] = (8, 16, 12);
        manifoldDifficulties[2,1] = (16, 24, 42);
        manifoldDifficulties[2,2] = (16, 32, 98);

        // Mobius strip
        manifoldDifficulties[3,0] = (16, 4, 12);
        manifoldDifficulties[3,1] = (32, 8, 42);
        manifoldDifficulties[3,2] = (32, 16, 98);

        // Klein bottle
        manifoldDifficulties[4,0] = (8, 8, 12);
        manifoldDifficulties[4,1] = (16, 16, 42);
        manifoldDifficulties[4,2] = (16, 32, 98);

        mineCount = 0;
    }

    private void Start() {
        UpdateSelectedManifold();
        UpdateActiveMaps();
        UpdateDifficulty();
    }

    private void Update() {
        if (game != null) {
            flagText.text = $"{game.FlagCount}/{game.MineCount}";
            timerText.text = game.Timer.ToString();
        }
    }

    public void Clear() {
        Destroy(board);
    }

    public void PanelOpenClose() {
        panelOpen = animator.GetBool("panelOpen");
        panelOpen = !panelOpen;
        animator.SetBool("panelOpen", panelOpen);
    }

    public void Generate() {
        UpdateSelectedManifold();
        UpdateActiveMaps();
        UpdateDifficulty();

        if (board != null) {
            if (complex != null)
                Destroy(complex);
            if (game != null)
                Destroy(game);
            Destroy(board);
        }
        board = new GameObject();
        board.name = "Board";

        switch (selectedManifold) {
            case 0: complex = board.AddComponent<Plane>(); break;
            case 1: complex = board.AddComponent<Cylinder>(); break;
            case 2: complex = board.AddComponent<Torus>(); break;
            case 3: complex = board.AddComponent<MobiusStrip>(); break;
            case 4: complex = board.AddComponent<KleinBottle>(); break;
            default: break;
        }
        game = board.AddComponent<Game>();

        complex.Setup(ResU, ResV, Complex.Map.Flat);
        game.Setup(complex, ResU, ResV, mineCount);
        cameraHandler.Target = complex;

        game.NewGame(false);
    }

    // Ran On Value Changed of Manifold Toggles
    public void UpdateSelectedManifold() {
        selectedManifold = manifoldDropdown.value;

        UpdateActiveMaps();
        UpdateDifficulty();
    }

    public void UpdateActiveMaps() {
        for (int i = 0; i < 5; i++) {
            if (i == selectedManifold) {
                manifoldMaps.GetChild(i).gameObject.SetActive(true);
            } else {
                manifoldMaps.GetChild(i).gameObject.SetActive(false);
            }
        }

        mapDropdown = manifoldMaps.GetChild(selectedManifold).gameObject.GetComponent<TMP_Dropdown>();
        UpdateSelectedMap();
    }

    // Ran On Value Changed of Mapping Toggles
    public void UpdateSelectedMap() {
        selectedMap = mapDropdown.value;
    }

    // Ran On Value Changed of Difficulty Toggles
    public void UpdateDifficulty() {
        selectedDifficulty = difficultyDropdown.value;

        // If custom difficulty is selected
        if (selectedDifficulty == 3) {
            UpdateActiveCustomDifficulty(true);
        } else {
            UpdateActiveCustomDifficulty(false);
            ResU = manifoldDifficulties[selectedManifold,selectedDifficulty].u;
            ResV = manifoldDifficulties[selectedManifold,selectedDifficulty].v;
            mineCount = manifoldDifficulties[selectedManifold,selectedDifficulty].mines;

            inputFields[0].text = ResU.ToString();
            inputFields[1].text = ResV.ToString();
            inputFields[2].text = mineCount.ToString();
        }
    }

    private void UpdateActiveCustomDifficulty(bool active) {
        for (int i = 0; i < inputFields.Length; i++) {
            inputFields[i].interactable = active;
        }
    }

    // Ran On Value Changed of Custom Difficulty Text Inputs
    public void UpdateCustomDifficulty() {
        int.TryParse(inputFields[0].text, out ResU);
        int.TryParse(inputFields[1].text, out ResV);
        int.TryParse(inputFields[2].text, out mineCount);
    }

    public void MapToPlane() {
        StartCoroutine(MapToPlane2());
    }

    private IEnumerator MapToPlane2() {
        mapButton.interactable = false;
        yield return StartCoroutine(complex.ToPlane());
        mapButton.interactable = true;
    }
}
