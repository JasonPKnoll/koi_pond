
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class Food : UdonSharpBehaviour
{
    private bool isUp = true;
    public bool inWater = false;
    public Vector3 spawnOffset = new Vector3 (0f, 0.2f, 0f); 
    public float positionY = 0.03f;
    public int fishSeeking = 0;
    public int maxSeeking = 100;

    private VRCObjectSync sync;
    private Rigidbody _rigidBody;
    public UdonBehaviour eventTarget;
    public LayerMask mask;
    public int fishMask = 1 << 23;

    // Intervals
    public float movementChangeInterval = 1.5f;
    private float outOfWaterInterval = 0.03f;

    // timers
    private float lastMovementChangeTime;
    private float lastOutOfWaterTime;

    // State Machine
    public const byte Swimming = 1, OutOfWater = 2, AvoidingLeft = 3, AvoidingRight = 4,
    Resting = 5, SeekingFood = 6, SeekingMate = 7, InWater = 8;

    [SerializeField] AudioManager _audioManager;
    [SerializeField] FoodSpawner _foodspawner;
    [SerializeField] FishSpawner _fishspawner;
    [SerializeField] CookFish _cookFish;

    [UdonSynced] public byte currentState;
    public byte _currentState;

    [UdonSynced] public Vector3 spawnLocation;
    public Vector3 _spawnLocation;



    void Start() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));

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
    }


    public void OnEnable() {
        if (!Networking.IsOwner(gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        ResetSpawnPosition();
    }

    void Update() {
        if (Networking.IsOwner(gameObject)) {
            if (gameObject.activeSelf == false) {
                ResetSpawnPosition();
            }

            if (_rigidBody.useGravity == false) {
                jiggle();
                if (Time.time > lastMovementChangeTime + movementChangeInterval) {
                    changeDirection();
                }
            }

            if (currentState == InWater && fishSeeking <= maxSeeking) {
                if (name == "vPill") {
                    findNearbyFish(7.0f);
                } else {
                    findNearbyFish(0f);
                }
            }
        }
    }

    public void SetState(byte changedState) {
        currentState = changedState;
        RequestSerialization();
    }

    public void findNearbyFish(float fishSizeMultiplier) {
        Collider[]hitColliders = Physics.OverlapSphere(transform.position, 2f, fishMask);
        foreach (var hitCollider in hitColliders) {
            if (fishSeeking <= maxSeeking) {
                Koi fish = hitCollider.gameObject.GetComponent<Koi>();
                if (fish == null) return;
                float minFishSize = fish.fishSizeIncrement * fishSizeMultiplier;
                if (fish.fishSize > minFishSize && fish.currentState == Swimming || fish.fishSize > minFishSize && fish.currentState == Resting) {
                    fishSeeking += 1;
                    fish.SetTarget(gameObject);
                    fish.SetState(SeekingFood);
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
        if (collider.gameObject.name == "Water" && Time.time > lastOutOfWaterTime + outOfWaterInterval) {
            float pitch = Random.Range(0.5f, 0.6f); 
            _audioManager.PlayOnce(_audioManager.audioSplash, gameObject, pitch);
            SetState(InWater);
            sync.SetGravity(false);
            sync.SetKinematic(true);
            lastOutOfWaterTime = Time.time;
        }    
    }

    private void OnTriggerExit(Collider collider) {
        if (collider.gameObject.name == "Water" && Time.time > lastOutOfWaterTime + outOfWaterInterval) {
            float pitch = Random.Range(0.5f, 0.6f);
            _audioManager.PlayOnce(_audioManager.audioSplash, gameObject, pitch);
            fishSeeking = 0;
            SetState(OutOfWater);
            sync.SetGravity(true);
            lastOutOfWaterTime = Time.time;
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

