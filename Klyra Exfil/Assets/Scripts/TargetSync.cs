using UnityEngine;
using Photon.Pun;

/// <summary>
/// Syncs an existing shooting target over the network.
/// Add this alongside your existing target script.
/// </summary>
public class TargetSync : MonoBehaviourPun, IPunObservable
{
    [Header("Sync Settings")]
    [Tooltip("The target transform that rotates")]
    public Transform targetTransform;

    [Tooltip("Sync rotation?")]
    public bool syncRotation = true;

    [Tooltip("Sync position? (if target moves)")]
    public bool syncPosition = false;

    private Vector3 networkPosition;
    private Quaternion networkRotation;

    void Start()
    {
        if (targetTransform == null)
        {
            targetTransform = transform;
        }

        networkPosition = targetTransform.position;
        networkRotation = targetTransform.rotation;
    }

    void Update()
    {
        // Only interpolate on non-owned objects
        if (photonView != null && !photonView.IsMine)
        {
            if (syncPosition)
            {
                targetTransform.position = Vector3.Lerp(targetTransform.position, networkPosition, Time.deltaTime * 10f);
            }

            if (syncRotation)
            {
                targetTransform.rotation = Quaternion.Lerp(targetTransform.rotation, networkRotation, Time.deltaTime * 10f);
            }
        }
    }

    /// <summary>
    /// Photon serialization - sends data over network
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this target - send data to others
            if (syncPosition)
            {
                stream.SendNext(targetTransform.position);
            }

            if (syncRotation)
            {
                stream.SendNext(targetTransform.rotation);
            }
        }
        else
        {
            // Network data received
            if (syncPosition)
            {
                networkPosition = (Vector3)stream.ReceiveNext();
            }

            if (syncRotation)
            {
                networkRotation = (Quaternion)stream.ReceiveNext();
            }
        }
    }
}
