
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
    private float fishSearchFrequency = 5f;
    private float lastFishSearch;

    private VRCObjectSync sync;
    private Rigidbody _rigidBody;
    public FoodSpawner _foodspawner;
    public FishSpawner _fishspawner;
    public CookFish _cookFish;
    public LayerMask mask;
    public int fishMask = 1 << 23;

    private Koi[] allFish;

    [UdonSynced] public Vector3 spawnLocation;
    public Vector3 _spawnLocation;

    void Start() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        allFish = _fishspawner.transform.GetComponentsInChildren<Koi>(true);

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
        if (Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
            sync.SetKinematic(kinematic);
            sync.SetGravity(gravity);
        }
    }



    public void OnEnable() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        ResetSpawnPosition();
        if (gameObject.name.StartsWith("Foo")) {
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
        if (inWater == true) {
            //if (Time.time > lastFishSearch + fishSearchFrequency) {
                findNearbyFish();
                Debug.Log("Food it layer: " + gameObject.layer);
                lastFishSearch = Time.time;
            //}
        }
    }

    public void findNearbyFish() {
        Collider[]hitColliders = Physics.OverlapSphere(transform.position, 2f, fishMask);
        foreach (var hitCollider in hitColliders) {
            Debug.Log("Hit" + hitCollider.name);
            if (hitCollider.name.StartsWith("koi")) {
                var fish = hitCollider.gameObject.GetComponent<Koi>();
                fish.SetTarget(gameObject);
            }
        }
        lastFishSearch = Time.time;
    }

    public bool checkFishViability(Koi fish) {
        if (fish.gameObject.activeSelf == false) {
            return false;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position, (fish.transform.position - transform.position).normalized, out hit, 2f, mask)) {
            if (hit.collider == fish._collider) {
                return true;
            }
                Debug.DrawRay(transform.position, (fish.transform.position - transform.position).normalized * 2f, Color.green);
        } else {
            Debug.DrawRay(transform.position, (fish.transform.position - transform.position).normalized * 2f, Color.yellow);
        }
        return false;
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
        if (collider.gameObject.name == "Water") {
            gameObject.name = gameObject.name.Insert(0, "In Water ");
            inWater = true;
            sync.SetGravity(false);
            sync.SetKinematic(true);
        }    
    }

    private void OnTriggerExit(Collider collider) {
        if (collider.gameObject.name == "Water") {
            gameObject.name = gameObject.name.Remove(0, 9);
            inWater = false;
            sync.SetGravity(true);
            sync.SetKinematic(false);
        }
    }

    public void ResetSpawnPosition() {
        if (gameObject.name.StartsWith("Foo") || gameObject.name.StartsWith("In Water Foo")) {
            gameObject.transform.position = _foodspawner.transform.position + spawnOffset;
        } else {
            gameObject.transform.position = _cookFish.transform.position + spawnOffset;
        }
    }

    public override void OnDeserialization() {
    }
}