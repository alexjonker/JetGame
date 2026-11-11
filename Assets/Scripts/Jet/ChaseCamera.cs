using UnityEngine;

// Follows behind the target jet with a bit of lag and rolls along with it.
public class ChaseCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -22f);
    [SerializeField] private float lookAhead = 30f;
    [SerializeField] private float positionSharpness = 8f;
    [SerializeField] private float rotationSharpness = 6f;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public void Snap()
    {
        if (target == null)
        {
            return;
        }

        transform.position = DesiredPosition();
        transform.rotation = DesiredRotation(transform.position);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float positionBlend = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
        float rotationBlend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

        Vector3 desiredPosition = DesiredPosition();
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionBlend);
        transform.rotation = Quaternion.Slerp(transform.rotation, DesiredRotation(desiredPosition), rotationBlend);
    }

    private Vector3 DesiredPosition()
    {
        return target.TransformPoint(offset);
    }

    private Quaternion DesiredRotation(Vector3 fromPosition)
    {
        Vector3 lookPoint = target.position + target.forward * lookAhead;
        return Quaternion.LookRotation(lookPoint - fromPosition, target.up);
    }
}
