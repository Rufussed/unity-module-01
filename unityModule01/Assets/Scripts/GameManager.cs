using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [System.Serializable]
    private class PlayerSlot
    {
        public string playerName;
        public PlayerController controller;
    }

    [SerializeField] private PlayerSlot[] players;
    [SerializeField] private string startingPlayerName = "Thomas";
    [SerializeField] private string key1PlayerName = "Claire";
    [SerializeField] private string key2PlayerName = "Thomas";
    [SerializeField] private string key3PlayerName = "John";
    [SerializeField] private bool[] shapesInGoal = new bool[3];
    [SerializeField] private TMPro.TextMeshProUGUI headlineText;
    [SerializeField] private GameObject[] shapeGoalLabels = new GameObject[3];
    [SerializeField] private LayerMask selectableLayers = ~0;

    public enum ShapeId
    {
        Claire = 0,
        Thomas = 1,
        John = 2
    }

    private readonly Dictionary<string, PlayerController> playerLookup = new();
    private PlayerController activePlayer;
    public PlayerController ActivePlayer => activePlayer;
    public Transform ActivePlayerTransform => activePlayer != null ? activePlayer.transform : null;

#if UNITY_EDITOR
    private void Reset()
    {
        players = new PlayerSlot[]
        {
            new PlayerSlot { playerName = "Claire" },
            new PlayerSlot { playerName = "Thomas" },
            new PlayerSlot { playerName = "John" }
        };

        startingPlayerName = "Thomas";
        key1PlayerName = "Clair";
        key2PlayerName = "Thomas";
        key3PlayerName = "John";
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        playerLookup.Clear();

        if (players == null || players.Length == 0)
        {
            Debug.LogWarning("GameManager has no player slots assigned.");
        }
        else
        {
            foreach (var slot in players)
            {
                RegisterSlot(slot);
            }
        }

        RegisterScenePlayers();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        SetActivePlayer(startingPlayerName);
        RefreshShapeIndicators();
        ShowStageHeadlineForScene();
        UpdateLevelCompleteIndicator();
    }

    private void Update()
    {
        HandlePlayerSelectionInput();
        ReportGoalStates();
    }

    private void HandlePlayerSelectionInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (WasPressed(keyboard.digit1Key, keyboard.numpad1Key))
            {
                SetActivePlayer(key1PlayerName);
            }
            else if (WasPressed(keyboard.digit2Key, keyboard.numpad2Key))
            {
                SetActivePlayer(key2PlayerName);
            }
            else if (WasPressed(keyboard.digit3Key, keyboard.numpad3Key))
            {
                SetActivePlayer(key3PlayerName);
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ReloadActiveScene();
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            TrySelectPlayerWithMouse(mouse.position.ReadValue());
        }
    }

    private void TrySelectPlayerWithMouse(Vector2 screenPosition)
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        var ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, selectableLayers))
        {
            return;
        }

        var controller = hitInfo.collider.GetComponentInParent<PlayerController>();
        if (controller == null)
        {
            return;
        }

        SetActivePlayer(controller.gameObject.name);
    }

    private static bool WasPressed(ButtonControl primary, ButtonControl secondary)
    {
        var primaryPressed = primary != null && primary.wasPressedThisFrame;
        var secondaryPressed = secondary != null && secondary.wasPressedThisFrame;
        return primaryPressed || secondaryPressed;
    }

    private float goalReportTimer;
    private const float GoalReportInterval = 0.5f;

    private void ReportGoalStates()
    {
        goalReportTimer += Time.deltaTime;
        if (goalReportTimer < GoalReportInterval)
        {
            return;
        }

        goalReportTimer = 0f;

        var statusBuilder = new System.Text.StringBuilder();
        statusBuilder.Append("Goals: ");

        for (int i = 0; i < shapesInGoal.Length; i++)
        {
            var shapeName = ((ShapeId)i).ToString();
            statusBuilder.Append(shapeName);
            statusBuilder.Append(shapesInGoal[i] ? "=IN " : "=OUT ");
        }

        Debug.Log(statusBuilder.ToString());
    }

    private void SetActivePlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return;
        }

        var nextPlayer = ResolvePlayer(playerName);
        if (nextPlayer == null)
        {
            return;
        }

        if (activePlayer == nextPlayer)
        {
            return;
        }

        if (activePlayer != null)
        {
            activePlayer.SetControlState(false);
        }

        activePlayer = nextPlayer;
        activePlayer.SetControlState(true);
    }

    private void RegisterSlot(PlayerSlot slot)
    {
        if (slot == null || string.IsNullOrWhiteSpace(slot.playerName))
        {
            return;
        }

        if (playerLookup.ContainsKey(slot.playerName))
        {
            Debug.LogWarning($"Duplicate player name detected: {slot.playerName}. Only the first instance will be controllable.");
            return;
        }

        var controller = slot.controller ?? ResolvePlayer(slot.playerName);

        if (controller == null)
        {
            return;
        }

        playerLookup[slot.playerName] = controller;
        controller.SetControlState(false);
    }

    public void SetShapeInGoal(ShapeId shape, bool inGoal)
    {
        var index = (int)shape;
        if (index < 0 || index >= shapesInGoal.Length)
        {
            Debug.LogWarning($"ShapesInGoal index {index} ({shape}) is out of range.");
            return;
        }

        shapesInGoal[index] = inGoal;
        UpdateShapeIndicator(shape, inGoal);
        UpdateLevelCompleteIndicator();
    }

    public bool GetShapeInGoal(ShapeId shape)
    {
        var index = (int)shape;
        if (index < 0 || index >= shapesInGoal.Length)
        {
            Debug.LogWarning($"ShapesInGoal index {index} ({shape}) is out of range.");
            return false;
        }

        return shapesInGoal[index];
    }

    private void UpdateLevelCompleteIndicator() // check if all are in and turn on or off ui text
    {
        var allInGoal = true;
        for (int i = 0; i < shapesInGoal.Length; i++)
        {
            if (!shapesInGoal[i])
            {
                allInGoal = false;
                break;
            }
        }

        if (allInGoal)
        {
            if (!levelCompletionTriggered)
            {
                levelCompletionTriggered = true;
                headlineLocked = false;
                if (headlineText != null)
                {
                    headlineText.text = stageCompleteMessage;
                    headlineText.gameObject.SetActive(true);
                }

                CancelInvoke(nameof(LoadNextStage));
                if (!string.IsNullOrEmpty(nextStageName))
                {
                    Invoke(nameof(LoadNextStage), 4f);
                }
            }
        }
        else if (!headlineLocked && !levelCompletionTriggered)
        {
            if (headlineText != null)
            {
                headlineText.gameObject.SetActive(false);
            }

            CancelInvoke(nameof(LoadNextStage));
        }
    }

    private void ShowStageHeadline(string message, float duration)
    {
        if (headlineText == null)
        {
            return;
        }

        headlineLocked = true;
        headlineText.text = message;
        headlineText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideHeadline));
        if (duration > 0f)
        {
            Invoke(nameof(HideHeadline), duration);
        }
    }

    private void HideHeadline()
    {
        headlineLocked = false;
        if (headlineText != null && !AllShapesInGoal())
        {
            headlineText.gameObject.SetActive(false);
        }
    }

    private bool AllShapesInGoal()
    {
        for (int i = 0; i < shapesInGoal.Length; i++)
        {
            if (!shapesInGoal[i])
            {
                return false;
            }
        }

        return true;
    }

    private bool headlineLocked;
    private bool levelCompletionTriggered;
    private string nextStageName;
    private string stageCompleteMessage = "Level Complete!";

    private void LoadNextStage()
    {
        if (string.IsNullOrEmpty(nextStageName))
        {
            return;
        }

        SceneManager.LoadScene(nextStageName);
    }

    private void ShowStageHeadlineForScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            return;
        }

        stageCompleteMessage = "Level Complete!";
        nextStageName = null;
        levelCompletionTriggered = false;

        string message = null;
        switch (scene.name)
        {
            case "Stage2":
                message = "Stage 2";
                stageCompleteMessage = "You Win!";
                break;
            case "Stage1":
                message = "Stage 1";
                nextStageName = "Stage2";
                break;
        }

        if (!string.IsNullOrEmpty(message))
        {
            ShowStageHeadline(message, 5f);
        }
    }

    public void ReloadActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            SceneManager.LoadScene(scene.name);
        }
    }

    private void RefreshShapeIndicators()
    {
        for (int i = 0; i < shapesInGoal.Length; i++)
        {
            UpdateShapeIndicator((ShapeId)i, shapesInGoal[i]);
        }
    }

    private void UpdateShapeIndicator(ShapeId shape, bool inGoal)
    {
        var index = (int)shape;
        if (index < 0 || index >= shapeGoalLabels.Length)
        {
            return;
        }

        var label = shapeGoalLabels[index];
        if (label != null)
        {
            label.SetActive(inGoal);
        }
    }

    private PlayerController ResolvePlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return null;
        }

        if (playerLookup.TryGetValue(playerName, out var controller) && controller != null)
        {
            return controller;
        }

        controller = FindControllerByName(playerName);
        if (controller == null)
        {
            Debug.LogWarning($"No controllable player named '{playerName}' was found in the scene.");
            return null;
        }

        playerLookup[playerName] = controller;
        controller.SetControlState(false);
        return controller;
    }

    private PlayerController FindControllerByName(string playerName)
    {
        var targetObject = GameObject.Find(playerName);

        if (targetObject != null)
        {
            var controller = targetObject.GetComponent<PlayerController>();
            if (controller != null)
            {
                return controller;
            }
        }

        foreach (var controller in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (controller.name == playerName)
            {
                return controller;
            }
        }

        return null;
    }

    private void RegisterScenePlayers()
    {
        foreach (var controller in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            var name = controller.gameObject.name;

            if (playerLookup.ContainsKey(name))
            {
                continue;
            }

            playerLookup.Add(name, controller);
            controller.SetControlState(false);
        }
    }
}
