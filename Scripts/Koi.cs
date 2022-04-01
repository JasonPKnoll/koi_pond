

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon;


public class Koi : UdonSharpBehaviour
{
    public float rotationSpeed = 1.0f;
    public float directionChangeInterval = 2.0f;
    private bool swappable = false;
    public bool outOfWater = false;

    [UdonSynced] public float speed = 0.0f;
    [UdonSynced] public float fishSize = 0.04f;

    // Color Values
    [UdonSynced] public float r = 0f, g = 0f, b = 0f;
    [UdonSynced] public float r2 = 0f, g2 = 0f, b2 = 0f;
    private float _r = 0f, _g = 0f, _b = 0f;
    private float _r2 = 0f, _g2 = 0f, _b2 = 0f;

    private float lastDirectionChangeTime;

    private VRCObjectSync sync;

    [UdonSynced]
    private Quaternion heading;

    private Renderer _renderer;
    private Rigidbody _rigidBody;
    public Collider _collider;
    [SerializeField] 
    MaterialPropertyBlock _propBlock;

    // State Machine
    public const byte fishSwimming = 1, fishTurningLeft = 2, fishTuringRight = 3, 
        fishResting = 4, seekingFood = 5, seekingMate = 6;

    // For RayCasting
    private float rayDistance = 0.5f;
    public LayerMask mask;
    private LayerMask foodMask = 0;

    // Access to other Classes
    [SerializeField] CookFish _cookFish;
    [SerializeField] FishSwapper _fishSwapper;
    [SerializeField] FoodSpawner _foodSpawner;
    [SerializeField] FishSpawner _fishSpawner;
    [SerializeField] Food _food;


    void Start() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        //pickupMask = LayerMask.GetMask("Pickup");
        if (outOfWater == false)
            CreateNewKoi(false, true); // Possibly redundent
            ChooseHeading();
    }

    public void OnEnable() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        if (outOfWater == false)
            CreateNewKoi(false, true);
            ChooseHeading();
    }

    private void Update() {
        if (target) {
            MoveToTarget();
        } else if (_rigidBody.useGravity == false) {
            DrawRays();
        }
        if (transform.position.y < -10) {
            _fishSpawner.RespawnFromFall(gameObject);
        }
    }

    void DrawRays() {
        // swim towards y position 0
        if (-0.01f <= transform.position.y && transform.position.y >= 0.1f) {
            Vector3 target = new Vector3(transform.position.x, 0.0f, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }

        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        Vector3 left = transform.TransformDirection(Vector3.left);


        if (!Physics.Raycast(transform.position, fwd, rayDistance, mask)) {
            transform.rotation = Quaternion.Lerp(transform.rotation, heading, Time.deltaTime * rotationSpeed);
            transform.position += transform.forward * speed * Time.deltaTime;

            Debug.DrawRay(transform.position, fwd * rayDistance, Color.green);

            if (Time.time > lastDirectionChangeTime + directionChangeInterval) {
                ChooseHeading();
            }
        }

        if (Physics.Raycast(transform.position, fwd, rayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * rayDistance, Color.red);
            TryRight(right, left);
            //TryLeft(left, right);
        }
    }

    void TryRight(Vector3 right, Vector3 left) {
        if (!Physics.Raycast(transform.position, right, rayDistance, mask)) {
            heading = Quaternion.Euler(0.0f, heading.eulerAngles.y + 90.0f, 0.0f);
            transform.Rotate(0, 90 * Time.deltaTime, 0);

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * rayDistance, Color.green);
        }
        if (Physics.Raycast(transform.position, right, rayDistance, mask)) {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * rayDistance, Color.red);
            TryLeft(left, right);
        }

    }

    void TryLeft(Vector3 left, Vector3 right) {
        if (!Physics.Raycast(transform.position, left, rayDistance, mask)) {
            heading = Quaternion.Euler(0.0f, heading.eulerAngles.y - 90.0f, 0.0f);
            transform.Rotate(0, -90 * Time.deltaTime, 0);

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * rayDistance, Color.green);
        }
        if (Physics.Raycast(transform.position, left, rayDistance, mask)) {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * rayDistance, Color.red);
            TryRight(right, left);
        }
    }

    public void SetTarget(GameObject new_target) {
        target = new_target;
    }

    public void MoveToTarget() {
        TryToEat();

        Quaternion lookAtLocation = Quaternion.LookRotation(target.transform.position - transform.position);

        transform.rotation = Quaternion.Slerp(transform.rotation, lookAtLocation, rotationSpeed * 3f * Time.deltaTime);
        if (Vector3.Distance(transform.position, target.transform.position) > 0.1f) {
            transform.position += transform.forward * speed * 1.2f * Time.deltaTime;
        } else {
            transform.position = Vector3.Lerp(transform.position, target.transform.position, speed * Time.deltaTime);
        }

        if (!target.name.StartsWith("In Water") || target.activeSelf == false) {
            target = null;
        }
    }

    private void TryToEat() {
        Debug.DrawRay(transform.position, (target.transform.position - transform.position).normalized * 0.5f, Color.cyan);
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance < 0.12f) {
            EatFood(target.gameObject);
        }
    }

    private void OnTriggerEnter(Collider collider) {
        if (collider.gameObject.name == "Fire") {
            _cookFish.Cook(gameObject);
        }

        if (collider.gameObject.name == "FishSwapper") {
            if (swappable == true) {
                _fishSwapper.SwapFish(gameObject);
            }
        }

        if (collider.gameObject.name == "Water") {
            outOfWater = false;
            sync.SetGravity(false);
            sync.SetKinematic(true);
            //_propBlock.SetFloat("_Speed", 5f); // Not working
            gameObject.transform.position += new Vector3(0f, 0f, 0f);
        }

        if (collider.gameObject.name == "Food") {
            EatFood(collider.gameObject);
        }
        if (collider.gameObject.name == "FriedKoi") {
            EatFood(collider.gameObject);
        }
    }

    private void EatFood(GameObject food) {
        if (fishSize < 0.08f) {
            _food = food.gameObject.GetComponent<Food>();
            RespawnFood(_food);
            fishSize += 0.01f;
            speed += 0.2f;
            transform.localScale = new Vector3(fishSize, fishSize, fishSize);
            RequestSerialization();
        } else {
            _food = food.gameObject.GetComponent<Food>();
            RespawnFood(_food);
        }
    }
        }
    }
    
    private void OnTriggerExit(Collider collider) {
        if (collider.gameObject.name == "Water") {
            outOfWater = true;
            sync.SetGravity(true);
            sync.SetKinematic(false);
            _propBlock.SetFloat("_Speed", 20f);
        }
    }

    public void SpawnOutOfWater() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        CreateNewKoi(true, false);
    }

    public void CreateNewKoi(bool gravity, bool kinematic) {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        speed = 0.2f;
        fishSize = 0.04f;
        swappable = false;
        transform.localScale = new Vector3(fishSize, fishSize, fishSize);

        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _rigidBody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        sync.SetGravity(gravity);
        sync.SetKinematic(kinematic);

        r = Random.Range(0f, 1f);
        g = Random.Range(0f, 1f);
        b = Random.Range(0f, 1f);

        r2 = Random.Range(0f, 1f);
        g2 = Random.Range(0f, 1f);
        b2 = Random.Range(0f, 1f);

        _propBlock.SetColor("_Color", new Color(r, g, b));
        _propBlock.SetColor("_Color2", new Color(r2, g2, b2));

        _renderer.SetPropertyBlock(_propBlock);
        RequestSerialization();
    }    

    public override void OnPickup() {
        if (!Networking.IsOwner(gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        swappable = true;
    }

    void ChooseHeading() {
        if(Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
            heading = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
            lastDirectionChangeTime = Time.time;
        }
    }

    public override void OnDeserialization() {
        if (r != _r || b != _b || b2 != _b2 || g2 != _g2) {
            syncNewFish();
        }
        if (fishSize != _fishSize) {
            syncFishGrowwth();
        }
    }

    void syncFishGrowwth() {
        _fishSize = fishSize;
        _speed = speed;
        transform.localScale = new Vector3(fishSize, fishSize, fishSize);
    }


    void syncNewFish() {
        _r = r; _b = b; _g = g;
        _r2 = r2; _b2 = b2; _g2 = g2;

        speed = 0.2f;
        fishSize = 0.04f;
        swappable = false;
        transform.localScale = new Vector3(fishSize, fishSize, fishSize);

        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _rigidBody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        _propBlock.SetColor("_Color", new Color(r, g, b));
        _propBlock.SetColor("_Color2", new Color(r2, g2, b2));

        _renderer.SetPropertyBlock(_propBlock);
    }
}
