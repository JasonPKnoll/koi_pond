
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Koi : UdonSharpBehaviour
{
    public float speed = 1.0f;
    public float rotationSpeed = 1.0f;
    public float directionChangeInterval = 1.0f;
    private float lastDirectionChangeTime;
    private Quaternion heading;

    void Start()
    {
        ChooseHeading();
    }

    private void Update()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, heading, Time.deltaTime * rotationSpeed);
        transform.position += transform.forward * speed * Time.deltaTime;
        if(Time.time > lastDirectionChangeTime + directionChangeInterval)
        {
            ChooseHeading();
        }
    }

    void ChooseHeading()
    {
        heading = Random.rotation;
        lastDirectionChangeTime = Time.time;
    }
}