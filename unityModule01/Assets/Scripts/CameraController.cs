using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float followSpeed = 5f; //interpolate
    [SerializeField] private float xOffset = 0f;
    [SerializeField] private bool snapOnStart = true;

    private Transform currentTarget;

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        currentTarget = ResolveActiveTarget();
        if (snapOnStart && currentTarget != null)
        {
            SnapToTarget();
        }
    }

    private void LateUpdate()
    {
        var target = GetTarget();
        if (target == null)
        {
            return;
        }

        currentTarget = target;
        FollowTargetX();
    }

    private Transform GetTarget()
    {
        if (gameManager != null && gameManager.ActivePlayerTransform != null)
        {
            return gameManager.ActivePlayerTransform;
        }

        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            var controller = currentTarget.GetComponent<PlayerController>();
            if (controller != null && controller.IsControlEnabled)
            {
                return currentTarget;
            }
        }

        currentTarget = ResolveActiveTarget();
        return currentTarget;
    }

    private Transform ResolveActiveTarget()
    {
        if (gameManager != null && gameManager.ActivePlayerTransform != null)
        {
            return gameManager.ActivePlayerTransform;
        }

        foreach (var controller in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (controller.IsControlEnabled)
            {
                return controller.transform;
            }
        }

        return null;
    }

    private void FollowTargetX()
    {
        var position = transform.position;
        var desiredX = currentTarget.position.x + xOffset;
        position.x = Mathf.Lerp(position.x, desiredX, followSpeed * Time.deltaTime);
        transform.position = position;
    }

    private void SnapToTarget()
    {
        var position = transform.position;
        position.x = currentTarget.position.x + xOffset;
        transform.position = position;
    }

    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
        currentTarget = ResolveActiveTarget();
    }
}
