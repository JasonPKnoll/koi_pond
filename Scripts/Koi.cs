

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

    [UdonSynced] public float r = 0f;
    [UdonSynced] public float g = 0f;
    [UdonSynced] public float b = 0f;

    [UdonSynced] public float r2 = 0f;
    [UdonSynced] public float g2 = 0f;
    [UdonSynced] public float b2 = 0f;

    public float _speed = 0.0f;
    public float _fishSize = 0.04f;

    public float _r = 0f;
    public float _g = 0f;
    public float _b = 0f;
                 
    public float _r2 = 0f;
    public float _g2 = 0f;
    public float _b2 = 0f;

    private float lastDirectionChangeTime;

    private VRCObjectSync sync;

    [UdonSynced]
    private Quaternion heading;

    private Renderer _renderer;
    private Rigidbody _rigidBody;
    private Collider _collider;
    [SerializeField] 
    MaterialPropertyBlock _propBlock;

    // For RayCasting
    private float rayDistance = 0.5f;
    public LayerMask mask;
    RaycastHit hit;

    // Access to other Classes
    [SerializeField] CookFish _cookFish;
    [SerializeField] FishSwapper _fishSwapper;
    [SerializeField] FoodSpawner _foodSpawner;
    [SerializeField] FishSpawner _fishSpawner;
    [SerializeField] Food _food;


    void Start()
    {
        ChooseHeading();
        speed = 0.2f;
        transform.localScale = new Vector3(fishSize, fishSize, fishSize);

        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _rigidBody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        _propBlock.SetColor("_Color", new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
        _propBlock.SetColor("_Color2", new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));

        _renderer.SetPropertyBlock(_propBlock);
    }

    private void Update()
    {
        if (_rigidBody.useGravity == false)
        {
            DrawRays();
        }
        if (transform.position.y < -10)
        {
            //_fishSpawner.spawnedObject = gameObject;
            _fishSpawner.RespawnFromFall(gameObject);
        }
        //FoodRay();
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

        if (Physics.Raycast(transform.position, fwd, rayDistance, mask)) {
            if (Physics.Raycast(transform.position, right, rayDistance, mask)) {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * rayDistance, Color.red);

                // If left path not clear draw raycast as red
                if (Physics.Raycast(transform.position, left, rayDistance, mask)) {
                    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * rayDistance, Color.red);

                // Check left and move left if path is clear
                } else {
                    heading = Quaternion.Euler(0.0f, heading.eulerAngles.y - 90.0f, 0.0f);
                    transform.Rotate(0, -90 * Time.deltaTime, 0);

                    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * rayDistance, Color.green);
                }

            // Check right and move right if path is clear
            } else {
                heading = Quaternion.Euler(0.0f, heading.eulerAngles.y + 90.0f, 0.0f);
                transform.Rotate(0, 90 * Time.deltaTime, 0);

                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * rayDistance, Color.green);
            }

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * rayDistance, Color.red);

        // if raycast forward is good continue as normal
        } else {
            transform.rotation = Quaternion.Lerp(transform.rotation, heading, Time.deltaTime * rotationSpeed);
            transform.position += transform.forward * speed * Time.deltaTime;

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * rayDistance, Color.green);

            if (Time.time > lastDirectionChangeTime + directionChangeInterval) {
                ChooseHeading();
            }
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
            _propBlock.SetFloat("_Speed", 5f); // Not working
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
            if (_food.name == "Food") {
                _foodSpawner.availableObjects.Return(_food.gameObject);
            } else {
                _cookFish.availableObjects.Return(food.gameObject);
            }
            fishSize += 0.01f;
            speed += 0.2f;
            transform.localScale = new Vector3(fishSize, fishSize, fishSize);
            RequestSerialization();
        } else {
        }
    }
    
    private void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.name == "Water")
        {
            _rigidBody.useGravity = true;
            _rigidBody.isKinematic = false;
            //_collider.isTrigger = false;
            _propBlock.SetFloat("_Speed", 20f);
        }
    }

    void ToggleFertility()
    {
        if (fertility == false)
        {
            fertility = true;
        }
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

    public override void OnPickup()
    {
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        swappable = true;
    }

    void ChooseHeading()
    {
        heading = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
        lastDirectionChangeTime = Time.time;
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
        _r = r;
        _b = b;
        _g = g;
        _r2 = r2;
        _b2 = b2;
        _g2 = g2;

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
