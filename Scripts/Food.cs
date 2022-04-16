
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
    public bool inWater = false;
    public Vector3 spawnOffset = new Vector3 (0f, 0.2f, 0f); 
    public float positionY = 0.03f;
    private int fishSeeking = 0;

    private VRCObjectSync sync;
    private Rigidbody _rigidBody;
    public FoodSpawner _foodspawner;
    public FishSpawner _fishspawner;
    public CookFish _cookFish;
    public LayerMask mask;
    public int fishMask = 1 << 23;

    // State Machine
    public const byte Swimming = 1, OutOfWater = 2, AvoidingLeft = 3, AvoidingRight = 4,
    Resting = 5, SeekingFood = 6, SeekingMate = 7, InWater = 8;

    [UdonSynced] public byte currentState;
    public byte _currentState;

    [UdonSynced] public Vector3 spawnLocation;
    public Vector3 _spawnLocation;

    void Start() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        currentState = OutOfWater;

        if (gameObject.name.StartsWith("Foo")) {
            gameObject.name = "Food";
            FirstSpawn(true, false);
        } else if (gameObject.name.StartsWith("Fried")) {
            gameObject.name = "FriedKoi";
            FirstSpawn(false, true);
        } else {
            gameObject.name = "vPill";
            FirstSpawn(true, false);
        }
        _rigidBody = GetComponent<Rigidbody>();
    }

    private void FirstSpawn(bool kinematic, bool gravity) {
        if (_cookFish) {
            gameObject.transform.position = _cookFish.transform.position + spawnOffset;
        } else {
            gameObject.transform.position = _foodspawner.transform.position + spawnOffset;
        }
        if (Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
            sync.SetKinematic(kinematic);
            sync.SetGravity(gravity);
        }
    }


    public void OnEnable() {
        if (!Networking.IsOwner(gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        ResetSpawnPosition();
        //fishSeeking = 0;
        if (gameObject.name.StartsWith("Food")) {
            sync.SetKinematic(true);
            sync.SetGravity(false);
        } else {
            sync.SetKinematic(false);
            sync.SetGravity(true);
        }
    }

    void Update() {
        if (gameObject.activeSelf == false) {
            ResetSpawnPosition();
        }

        if (_rigidBody.useGravity == false) {
            jiggle();
            if (Time.time > lastMovementChangeTime + movementChangeInterval) {
                changeDirection();
            }
        }

        //Debug.Log(fishSeeking);
        if (currentState == InWater && fishSeeking <= 2) {
            if (name == "vPill") {
                findNearbyAdultFish();
            } else {
                findNearbyFish();
            }
        }
    }

    public void SetState(byte changedState) {
        currentState = changedState;
        RequestSerialization();
    }


    public void findNearbyFish() {
        Collider[]hitColliders = Physics.OverlapSphere(transform.position, 2f, fishMask);
        if (fishSeeking <= 2) {
            foreach (var hitCollider in hitColliders) {
                if (hitCollider.name.StartsWith("koi") && fishSeeking <= 2) {
                    var fish = hitCollider.gameObject.GetComponent<Koi>();
                    if (fish.currentState == Swimming || fish.currentState == Resting) {
                        fishSeeking =+ 1;
                        Debug.Log("Hit :" + fishSeeking);
                        fish.SetTarget(gameObject);
                        fish.SetState(SeekingFood);
                    }
                }
            }
        }
    }

    public void findNearbyAdultFish() {
        Collider[]hitColliders = Physics.OverlapSphere(transform.position, 2f, fishMask);
        if (fishSeeking <= 2) {
            foreach (var hitCollider in hitColliders) {
                if (hitCollider.name.StartsWith("koi") && fishSeeking <= 2) {
                    Koi fish = hitCollider.gameObject.GetComponent<Koi>();
                    if (fish.fishSize >= fish.fishSizeIncrement*7 && fish.currentState == Swimming || fish.currentState == Resting) {
                        fishSeeking += 1;
                        fish.SetTarget(gameObject);
                        fish.SetState(SeekingFood);
                    }
                }
            }
        }
    }


    public void changeDirection() {
        if (isUp == true) {
            isUp = false;
        } else {
            isUp = true;
        }
        lastMovementChangeTime = Time.time;
    }

    public void jiggle() {
        if (isUp == true) {
            transform.position += new Vector3(0, positionY, 0) * Time.deltaTime;
        } else {
            transform.position -= new Vector3(0, positionY, 0) * Time.deltaTime;
        }
    }

    public override void OnPickup() {
        if (!Networking.IsOwner(gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        sync.SetGravity(true);
        sync.SetKinematic(false);
    }

    public override void OnDrop() {
        if (!Networking.IsOwner(gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        sync.SetGravity(true);
        sync.SetKinematic(false);
    }

    private void OnTriggerEnter(Collider collider) {
        if (collider.gameObject.name == "Water" && currentState == OutOfWater) {
            SetState(InWater);
            Debug.Log("IN WATER");
            //fishSeeking = 0;
            sync.SetGravity(false);
            sync.SetKinematic(true);
        }    
    }

    private void OnTriggerExit(Collider collider) {
        if (collider.gameObject.name == "Water" && currentState == InWater) {
            SetState(OutOfWater);
            Debug.Log("OUT OF WATER");
            //fishSeeking = 0;
            sync.SetGravity(true);
            sync.SetKinematic(false);
        }
    }

    public void ResetSpawnPosition() {
        if (gameObject.name.StartsWith("Fried")) {
            gameObject.transform.position = _cookFish.transform.position + spawnOffset;
        } else {
            gameObject.transform.position = _foodspawner.transform.position + spawnOffset;
        }
    }

    public override void OnDeserialization() {
    }
}
