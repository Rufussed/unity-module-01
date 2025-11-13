using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
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

    private void Start()
    {
        SetActivePlayer(startingPlayerName);
    }

    private void Update()
    {
        HandlePlayerSelectionInput();
        ReportGoalStates();
    }

    private void HandlePlayerSelectionInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SetActivePlayer(key1PlayerName);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SetActivePlayer(key2PlayerName);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            SetActivePlayer(key3PlayerName);
        }
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
