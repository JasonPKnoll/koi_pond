
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Food : UdonSharpBehaviour
{
    public float movementChangeInterval = 1.5f;
    private float lastMovementChangeTime;
    private bool isUp = true;
    public float positionY = 0.03f;

    private Rigidbody _rigidBody;

    [UdonSynced] bool isON;
    void Start()
    {
        gameObject.name = "Food";
        _rigidBody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        jiggle();
        if (Time.time > lastMovementChangeTime + movementChangeInterval )
        {
            changeDirection();
            lastMovementChangeTime = Time.time;
        }

    }

    public void changeDirection()
    {
        if (isUp == true)
        {
            isUp = false;
        }
        else
        {
            isUp = true;
        }
        lastMovementChangeTime = Time.time;
    }

    public void jiggle()
    {
        if (isUp == true)
        {
            transform.position += new Vector3(0, positionY, 0) * Time.deltaTime;
        }
        else
        {
            transform.position -= new Vector3(0, positionY, 0) * Time.deltaTime;
        }
    }

    public override void OnPickup()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        //SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "turnONorOFF");
    }

    public override void OnDrop()
    {
        _rigidBody.isKinematic = false;
        _rigidBody.useGravity = true;
    }

    //public override void Interact()
    //{
    //    if (!Networking.IsOwner(gameObject))
    //    {
    //        Networking.SetOwner(Networking.LocalPlayer, gameObject);
    //    }
    //    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "turnONorOFF");
    //}

    //public void turnONorOFF()
    //{
    //isON = !food.activeSelf;
    //food.SetActive(isON);
    //}

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name == "Water")
        {
            _rigidBody.useGravity = false;
            _rigidBody.isKinematic = true;
        }    
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.name == "Water")
        {
            _rigidBody.useGravity = true;
            _rigidBody.isKinematic = false;
        }
    }

    public override void OnDeserialization()
    {
        //gameObject.SetActive(isON);
    }
}
