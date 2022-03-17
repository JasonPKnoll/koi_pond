
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class Food : UdonSharpBehaviour
{
    public float movementChangeInterval = 1.5f;
    private float lastMovementChangeTime;
    private bool isUp = true;
    public Vector3 spawnOffset = new Vector3 (0f, 0.2f, 0f); 
    public float positionY = 0.03f;

    private VRCObjectSync sync;
    private Rigidbody _rigidBody;
    public FoodSpawner _foodspawner;
    public CookFish _cookFish;

    [UdonSynced] public Vector3 spawnLocation;
    public Vector3 _spawnLocation;

    void Start() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        if (gameObject.name.StartsWith("Foo")) {
            gameObject.name = "Food";
            FirstSpawn(true, false);
        } else {
            gameObject.name = "FriedKoi";
            FirstSpawn(false, true);
        }
        _rigidBody = GetComponent<Rigidbody>();
    }

    private void FirstSpawn(bool kinematic, bool gravity) {
        if (_cookFish) {
            gameObject.transform.position = _cookFish.transform.position + spawnOffset;
        } else {
            gameObject.transform.position = _foodspawner.transform.position + spawnOffset;
        }
        if (Networking.IsOwner(Networking.LocalPlayer, gameObject))
        {
            sync.SetKinematic(kinematic);
            sync.SetGravity(gravity);
        }
    }



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
