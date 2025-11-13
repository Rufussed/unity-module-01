using UnityEngine;

public class GoalDetector : MonoBehaviour
{
    [SerializeField] private GameManager.ShapeId targetShape = GameManager.ShapeId.Claire;
    [SerializeField] private float tolerance = 0.3f;
    [SerializeField] private Transform targetTransform;

    private bool lastReportedState;
    private bool hasReportedState;

    private void Awake()
    {
        TryResolveTargetTransform();
    }

    private void Update()
    {
        if (!TryResolveTargetTransform())
        {
            return;
        }

        var manager = GameManager.Instance;
        if (manager == null)
        {
            return;
        }

        var inside = IsWithinTolerance();
        if (!hasReportedState || inside != lastReportedState)
        {
            lastReportedState = inside;
            hasReportedState = true;
            manager.SetShapeInGoal(targetShape, inside);
        }
    }

    private bool missingTargetLogged;

    private bool TryResolveTargetTransform()
    {
        if (targetTransform != null)
        {
            return true;
        }

        var desiredName = targetShape.ToString();
        foreach (var controller in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (controller.name == desiredName)
            {
                targetTransform = controller.transform;
                return true;
            }
        }

        var targetObject = GameObject.Find(desiredName);
        if (targetObject == null)
        {
            if (!missingTargetLogged)
            {
                Debug.LogWarning($"GoalDetector on {name} could not find a target named {desiredName}.");
                missingTargetLogged = true;
            }

            return false;
        }

        var controllerOnObject = targetObject.GetComponentInParent<PlayerController>();
        if (controllerOnObject != null)
        {
            targetTransform = controllerOnObject.transform;
            return true;
        }

        // Fall back to the found transform, but log so duplicates can be spotted.
        Debug.LogWarning($"GoalDetector on {name} is using object '{targetObject.name}' without a PlayerController. Consider assigning the transform explicitly.");
        targetTransform = targetObject.transform;
        missingTargetLogged = false;
        return true;
    }

    private bool IsWithinTolerance()
    {
        if (targetTransform == null)
        {
            return false;
        }

        var goalPos = transform.position;
        var targetPos = targetTransform.position;

        return Mathf.Abs(goalPos.x - targetPos.x) <= tolerance &&
               Mathf.Abs(goalPos.y - targetPos.y) <= tolerance;
    }
}
