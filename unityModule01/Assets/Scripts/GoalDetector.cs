using UnityEngine;

public class GoalDetector : MonoBehaviour
{
    [SerializeField] private GameManager.ShapeId targetShape = GameManager.ShapeId.Claire;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float tolerance = 0.3f;

    private Transform targetTransform;

    private bool lastReportedState;
    private bool hasReportedState;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        TryResolveTargetTransform();
    }

    private void Update()
    {
        if (gameManager == null)
        {
            return;
        }

        if (!TryResolveTargetTransform())
        {
            return;
        }

        var inside = IsWithinTolerance();
        if (!hasReportedState || inside != lastReportedState)
        {
            lastReportedState = inside;
            hasReportedState = true;
            gameManager.SetShapeInGoal(targetShape, inside);
        }
    }

    private bool TryResolveTargetTransform()
    {
        if (targetTransform != null)
        {
            return true;
        }

        var targetObject = GameObject.Find(targetShape.ToString());
        if (targetObject == null)
        {
            return false;
        }

        targetTransform = targetObject.transform;
        return targetTransform != null;
    }

    private bool IsWithinTolerance()
    {
        var goalPos = transform.position;
        var targetPos = targetTransform.position;

        return Mathf.Abs(goalPos.x - targetPos.x) <= tolerance &&
               Mathf.Abs(goalPos.y - targetPos.y) <= tolerance;
    }
}
