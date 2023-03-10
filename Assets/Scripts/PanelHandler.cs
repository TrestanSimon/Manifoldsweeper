using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelHandler : MonoBehaviour {
    private GameObject board;
    private Transform manifoldsPanel;
    private Transform mapsPanel;
    private Transform manifoldMaps;
    private Transform gamePanel;
    private Transform customColumn;

    
    private int selectedManifold, selectedMap;
    private int selectedDifficulty;
    private int ResU, ResV, mineCount;
    private (int u, int v, int mines)[,] manifoldDifficulties;

    private Game game;
    private Complex complex;
    private Toggle[] manifoldToggles, mapToggles, difficultyToggles;
    private Button mapButton;
    private TMP_InputField[] inputFields;
    private (TMP_Text size, TMP_Text mines)[] difficultyText;
    private Animator animator;
    private bool panelOpen;

    private CameraHandler cameraHandler;
    private List<Coroutine> coroutines;

    private void Awake() {
        coroutines = new List<Coroutine>();
        Transform difficultyTogglesHolder;
        Transform easyColumn, medColumn, hardColumn;
        difficultyText = new (TMP_Text, TMP_Text)[3];

        manifoldsPanel = transform.Find("Manifolds Panel");
        manifoldToggles = manifoldsPanel.Find("Manifold Toggles").gameObject.GetComponentsInChildren<Toggle>();

        mapsPanel = transform.Find("Maps Panel");
        manifoldMaps = mapsPanel.Find("Maps Toggles");
        mapButton = mapsPanel.Find("Map Button").GetComponent<Button>();

        gamePanel = transform.Find("Game Panel");
        difficultyTogglesHolder = gamePanel.Find("Difficulty Toggles");
        difficultyToggles = difficultyTogglesHolder.gameObject.GetComponentsInChildren<Toggle>();

        // Get labels for different difficulties
        easyColumn = difficultyTogglesHolder.Find("Easy Column");
        difficultyText[0] = (
            easyColumn.Find("Easy Size").GetComponent<TMP_Text>(),
            easyColumn.Find("Easy Mines").GetComponent<TMP_Text>()
        );
        medColumn = difficultyTogglesHolder.Find("Medium Column");
        difficultyText[1] = (
            medColumn.Find("Medium Size").GetComponent<TMP_Text>(),
            medColumn.Find("Medium Mines").GetComponent<TMP_Text>()
        );
        hardColumn = difficultyTogglesHolder.Find("Hard Column");
        difficultyText[2] = (
            hardColumn.Find("Hard Size").GetComponent<TMP_Text>(),
            hardColumn.Find("Hard Mines").GetComponent<TMP_Text>()
        );

        customColumn = difficultyTogglesHolder.Find("Custom Column");
        inputFields = customColumn.GetComponentsInChildren<TMP_InputField>();

        animator = gameObject.GetComponent<Animator>();
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
    }

    private void Start() {
        UpdateSelectedManifold();
        UpdateActiveMaps();
        UpdateDifficulty();
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

        complex.Setup(ResU, ResV);
        game.Setup(complex, ResU, ResV, mineCount);
        cameraHandler.Target = complex;

        game.NewGame(false);
    }

    // Ran On Value Changed of Manifold Toggles
    public void UpdateSelectedManifold() {
        for (int i = 0; i < manifoldToggles.Length; i++) {
            if (manifoldToggles[i].isOn) {
                selectedManifold = i;
                break;
            }
        }
        UpdateActiveMaps();
        UpdateActiveDifficulties();
    }

    public void UpdateActiveMaps() {
        for (int i = 0; i < manifoldToggles.Length; i++) {
            if (i == selectedManifold) {
                manifoldMaps.GetChild(i).gameObject.SetActive(true);
            } else {
                manifoldMaps.GetChild(i).gameObject.SetActive(false);
            }
        }

        mapToggles = manifoldMaps.GetChild(selectedManifold).gameObject.GetComponentsInChildren<Toggle>();
        UpdateSelectedMap();
    }

    // Ran On Value Changed of Mapping Toggles
    public void UpdateSelectedMap() {
        for (int i = 0; i < mapToggles.Length; i++) {
            if (mapToggles[i].isOn) {
                selectedMap = i;
                break;
            }
        }
    }

    private void UpdateActiveDifficulties() {
        int j = selectedManifold;
        for (int i = 0; i < 3; i++) {
            difficultyText[i].size.text
                = $"{manifoldDifficulties[j,i].u} x {manifoldDifficulties[j,i].v}";
            difficultyText[i].mines.text
                = $"{manifoldDifficulties[j,i].mines}";
        }
        UpdateDifficulty();
    }

    // Ran On Value Changed of Difficulty Toggles
    public void UpdateDifficulty() {
        for (int i = 0; i < difficultyToggles.Length; i++) {
            if (difficultyToggles[i].isOn) {
                selectedDifficulty = i;
                break;
            }
        }

        // If custom difficulty is selected
        if (selectedDifficulty == 3) {
            UpdateActiveCustomDifficulty(true);
        } else {
            UpdateActiveCustomDifficulty(false);
            ResU = manifoldDifficulties[selectedManifold,selectedDifficulty].u;
            ResV = manifoldDifficulties[selectedManifold,selectedDifficulty].v;
            mineCount = manifoldDifficulties[selectedManifold,selectedDifficulty].mines;
        }
    }

    public void UpdateActiveCustomDifficulty(bool active) {
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
