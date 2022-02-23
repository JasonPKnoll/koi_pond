

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


public class Koi : UdonSharpBehaviour
{
    public float speed = 0.0f;
    public float rotationSpeed = 1.0f;
    public float directionChangeInterval = 2.0f;
    private float lastDirectionChangeTime;
    private Quaternion heading;
    public LayerMask mask;
    RaycastHit hit;

    void Start()
    {
        ChooseHeading();
    }

    private void Update()
    {
        DrawRays();
    }

    void DrawRays()
    {
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        Vector3 left = transform.TransformDirection(Vector3.left);

        if (Physics.Raycast(transform.position, fwd, 2, mask))
        {
            if (Physics.Raycast(transform.position, right, 2, mask))
            {
                //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * 2, Color.red);
                if (Physics.Raycast(transform.position, left, 2, mask))
                {
                    //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * 2, Color.red);
                }
                else
                {
                    heading = Quaternion.Euler(0.0f, heading.eulerAngles.y - 90.0f, 0.0f);
                    transform.Rotate(0, -90 * Time.deltaTime, 0);

                    //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * 2, Color.green);
                    //Debug.Log("" + heading.eulerAngles.y + " Turning Left" + heading);
                }
            }
            else
            {

                heading = Quaternion.Euler(0.0f, heading.eulerAngles.y + 90.0f, 0.0f);
                transform.Rotate(0, 90 * Time.deltaTime, 0);

                //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * 2, Color.green);
                //Debug.Log("" + heading.eulerAngles.y + " Turning Right" + heading);
            }

            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 2, Color.red);
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, heading, Time.deltaTime * rotationSpeed);
            transform.position += transform.forward * speed * Time.deltaTime;

            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 2, Color.green);
            if (Time.time > lastDirectionChangeTime + directionChangeInterval)
            {
                ChooseHeading();
            }
        }
    }

    void FoodRay()
    {
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        if (Physics.Raycast(transform.position, fwd, 1))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1, Color.red);
            var objectHit = hit.collider.gameObject;
            if (objectHit.name == "Food")
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1, Color.yellow);
            }
            else
            {
            }
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1, Color.green);
        }
    }
    void ChooseHeading()
    {
        heading = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
        lastDirectionChangeTime = Time.time;
    }
}
