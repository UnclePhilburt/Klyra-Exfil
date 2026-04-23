using UnityEngine;

/// <summary>
/// Component on the player that handles dragging bodies.
/// Automatically added by BodyDrag script.
/// </summary>
public class BodyDragger : MonoBehaviour
{
    private BodyDrag currentDraggedBody;
    private float dragDistance = 2f;
    private bool isDragging = false;

    void FixedUpdate()
    {
        if (isDragging && currentDraggedBody != null)
        {
            // Calculate position behind player
            Vector3 dragPosition = transform.position - transform.forward * dragDistance;

            // Use the player's current Y position (keep body at player height)
            dragPosition.y = transform.position.y;

            Debug.Log($"FixedUpdate: isDragging={isDragging}, body={currentDraggedBody.gameObject.name}, dragPos={dragPosition}");

            // Tell the body to move to this position
            currentDraggedBody.DragToPosition(dragPosition);
        }
        else
        {
            if (isDragging)
            {
                Debug.LogWarning("isDragging is true but currentDraggedBody is null!");
            }
        }
    }

    public void StartDragging(BodyDrag body)
    {
        currentDraggedBody = body;
        isDragging = true;

        Debug.Log($"Started dragging {body.gameObject.name}");
    }

    public void StopDragging()
    {
        if (currentDraggedBody != null)
        {
            Debug.Log($"Stopped dragging {currentDraggedBody.gameObject.name}");
        }
        currentDraggedBody = null;
        isDragging = false;
    }

    public bool IsDragging()
    {
        return isDragging && currentDraggedBody != null;
    }

    public BodyDrag GetDraggedBody()
    {
        return currentDraggedBody;
    }
}
