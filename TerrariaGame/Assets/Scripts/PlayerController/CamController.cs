using UnityEngine;

public class CamController : MonoBehaviour
{
    [Range(0, 1)]
    public float smoothTime;

    public Transform playerTransform;
    private float orthoSize;

    [HideInInspector]
    public int worldSize;

    public void Spawn(Vector3 pos)
    {
        GetComponent<Transform>().position = pos;
        orthoSize = GetComponent<Camera>().orthographicSize;
    }

    private void FixedUpdate()
    {
        Vector3 pos = GetComponent<Transform>().position;

        pos.x = Mathf.Lerp(pos.x, playerTransform.position.x, smoothTime);
        pos.y = Mathf.Lerp(pos.y, playerTransform.position.y, smoothTime);

        pos.x = Mathf.Clamp(pos.x, 0 + (orthoSize * 2), worldSize - (orthoSize * 2));

        GetComponent<Transform>().position = pos;
    }
}