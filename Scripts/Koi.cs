
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon;


public class Koi : UdonSharpBehaviour
{
    private float rotationSpeed = 1.0f;
    private bool swappable = false;
    private bool desireOffspring = false;
    private bool _desireOffspring = false;
    public GameObject target;
    private Food _foodTarget;

    [UdonSynced] public float speed = 0.0f;
    public float fishSpeedIncrement = 0.2f;
    private float _speed = 0.0f;

    [UdonSynced] public float fishSize = 0.04f;
    public float fishSizeIncrement = 0.01f;
    public float fishSizeMax = 0.08f;
    public float _fishSize = 0.04f;

    // Color Values
    [UdonSynced] public float r = 0f, g = 0f, b = 0f;
    [UdonSynced] public float r2 = 0f, g2 = 0f, b2 = 0f;
    public float _r = 0f, _g = 0f, _b = 0f;
    public float _r2 = 0f, _g2 = 0f, _b2 = 0f;

    // Intervals
    private float directionChangeInterval = 2.0f;
    private float restTime = 8f;
    private float restCheckTimer = 10f;

    // Timers
    private float lastDirectionChangeTime;
    private float startRestTime;
    private float lastRestCheck;

    private VRCObjectSync sync;

    [UdonSynced]
    private Quaternion heading;

    // Components
    private Renderer _renderer;
    public Rigidbody _rigidBody;
    public Collider _collider;
    [SerializeField] MaterialPropertyBlock _propBlock;

    // State Machine
    // Enum is not compatible with Udon
    public const byte Swimming = 1, OutOfWater = 2, AvoidingLeft = 3, AvoidingRight = 4,
        Resting = 5, SeekingFood = 6, SeekingMate = 7, InWater = 8;
    [UdonSynced] public byte currentState;
    public byte _currentState;

    // For RayCasting
    private float rayDistance = 0.3f;
    public LayerMask mask;
    private LayerMask foodMask = 0;

    // Access to other Classes
    [SerializeField] CookFish _cookFish;
    [SerializeField] FishSwapper _fishSwapper;
    [SerializeField] FoodSpawner _foodSpawner;
    [SerializeField] FoodSpawner _vPillSpawner;
    [SerializeField] FishSpawner _fishSpawner;
    [SerializeField] Food _food;
    [SerializeField] KoiColor _koiColor;

    void Start() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
    }

    public void OnEnable() {
        //sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        if (currentState == OutOfWater) {
            sync.SetGravity(true);
            sync.SetKinematic(false);
        } else {
            CreateNewKoi(false, true, Swimming);
            ChooseHeading();
        }
    }

    private void Update() {
        if (Networking.IsOwner(gameObject)) { // Only the owner of this gameobject will execute update loops, everyone else will sync
            switch (currentState) {
                case Swimming:
                    UpdateSwimming();
                    break;
                case OutOfWater:
                    UpdateOutOfWater();
                    break;
                case AvoidingLeft:
                    UpdateAvoidingLeft();
                    break;
                case AvoidingRight:
                    UpdateAvoidingRight();
                    break;
                case Resting:
                    UpdateResting();
                    break;
                case SeekingFood:
                    UpdateSeekingFood();
                    break;
                case SeekingMate:
                    UpdateSeekingMate();
                    break;
            }
        }

        if (transform.position.y < -10) {
            _fishSpawner.RespawnFromFall(gameObject);
        }
    }

    public void SetState(byte changedState) {
        currentState = changedState;
        RequestSerialization();
    }

    private void UpdateSwimming() {
        // example addition of fishSize 0.06f * 5f = 0.3f
        float rayDistancePlus = rayDistance + fishSize * 5f;

        CheckDepth();

        if (Time.time > lastRestCheck + restCheckTimer) {
            float roll = Random.Range(0f, 1f);
            lastRestCheck = Time.time;
            if (roll > 0.8f) {
                startRestTime = Time.time;
                SetState(Resting);
            } 
        }

        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        Vector3 left = transform.TransformDirection(Vector3.left);

        if (!Physics.Raycast(transform.position, fwd, rayDistancePlus, mask)) {
            transform.rotation = Quaternion.Lerp(transform.rotation, heading, Time.deltaTime * rotationSpeed);
            transform.position += transform.forward * speed * Time.deltaTime;

            Debug.DrawRay(transform.position, fwd * rayDistancePlus, Color.green);

            if (Time.time > lastDirectionChangeTime + directionChangeInterval) {
                ChooseHeading();
            }
        }

        if (Physics.Raycast(transform.position, fwd, rayDistancePlus, mask)) {
            Debug.DrawRay(transform.position, fwd * rayDistancePlus, Color.red);

            //CheckDepth();

            float roll = Random.Range(0f, 1f);
           
            if (roll >= 0.90f) {
                startRestTime = Time.time;
                SetState(Resting);
            } else if (roll >= 0.45f) {
                SetState(AvoidingLeft);
            } else {
                SetState(AvoidingRight);
            }
        }
    }

    private void CheckDepth() {
        if (-0.001f >= transform.position.y || transform.position.y >= 0.1f) {
            Vector3 target = new Vector3(transform.position.x, 0.005f, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }
    }

    private void UpdateOutOfWater() {
    }

    private void UpdateAvoidingLeft() {
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        Vector3 left = transform.TransformDirection(Vector3.left);

        float increasedRayDistance = (rayDistance + fishSize * 5f) * 1.2f;

        if (Physics.Raycast(transform.position, fwd, increasedRayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * increasedRayDistance, Color.red);

            if (!Physics.Raycast(transform.position, left, increasedRayDistance, mask)) {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * increasedRayDistance, Color.green);

                heading = Quaternion.Euler(0.0f, heading.eulerAngles.y - 90.0f, 0.0f);
                transform.Rotate(0, -90 * Time.deltaTime, 0);
            } else if (Physics.Raycast(transform.position, left, increasedRayDistance, mask)) {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * increasedRayDistance, Color.red);
                SetState(AvoidingRight);
            }
        }

        if (!Physics.Raycast(transform.position, fwd, increasedRayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * increasedRayDistance, Color.green);
            SetState(Swimming);
        }
    }

    private void UpdateAvoidingRight() {
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float increasedRayDistance = (rayDistance + fishSize * 5f) * 1.2f;

        if (Physics.Raycast(transform.position, fwd, increasedRayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * increasedRayDistance, Color.red);

            if (!Physics.Raycast(transform.position, right, increasedRayDistance, mask)) {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * increasedRayDistance, Color.green);

                heading = Quaternion.Euler(0.0f, heading.eulerAngles.y + 90.0f, 0.0f);
                transform.Rotate(0, 90 * Time.deltaTime, 0);
            } else if (Physics.Raycast(transform.position, right, increasedRayDistance, mask)) {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * increasedRayDistance, Color.red);
                SetState(AvoidingLeft);
            }
        } 

        if (!Physics.Raycast(transform.position, fwd, increasedRayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * increasedRayDistance, Color.green);
            SetState(Swimming);
        }
    }

    private void UpdateResting() {
        if (Time.time > restTime + startRestTime) {
            float roll = Random.Range(0f, 1f);
            
            if (roll >= 0.50f) {
                SetState(AvoidingLeft);
            } else {
                SetState(AvoidingRight);
            }
        }
    }

    private void UpdateSeekingFood() {
        _foodTarget = target.GetComponent<Food>();
        TryToEat();

        Quaternion lookAtLocation = Quaternion.LookRotation(target.transform.position - transform.position);

        transform.rotation = Quaternion.Slerp(transform.rotation, lookAtLocation, rotationSpeed * 3f * Time.deltaTime);
        if (Vector3.Distance(transform.position, target.transform.position) > 0.1f) {
            transform.position += transform.forward * speed * 1.2f * Time.deltaTime;
        } else {
            transform.position = Vector3.Lerp(transform.position, target.transform.position, speed * Time.deltaTime);
        }

        if (_foodTarget.currentState != InWater || target.activeSelf == false) {
            target = null;
            _foodTarget = null;
            SetState(Swimming);
        }
    }

    private void UpdateSeekingMate() {
        UpdateSwimming();
    }

    public void SetTarget(GameObject new_target) {
        target = new_target;
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
            SetState(Swimming);
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
        if (collider.gameObject.name  == "vPill"  && fishSize == fishSizeMax) {
            EatPill(collider.gameObject);
            SetState(SeekingMate);
        }
    }

    private void EatPill(GameObject food) {
        _food = food.gameObject.GetComponent<Food>();
        desireOffspring = true;
        RespawnFood(_food);
        RequestSerialization();
    }

    private void EatFood(GameObject food) {
        if (Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
            if (fishSize < fishSizeMax) {
                _food = food.gameObject.GetComponent<Food>();
                RespawnFood(_food);
                fishSize += fishSizeIncrement;
                speed += fishSpeedIncrement;
                transform.localScale = new Vector3(fishSize, fishSize, fishSize);
                RequestSerialization();
            } else {
                _food = food.gameObject.GetComponent<Food>();
                RespawnFood(_food);
            }
        }
    }

    private void RespawnFood(Food _food) {
        if (_food.name == "Food") {
            // _food.name = "Food";
            _food.SetState(OutOfWater);
            _foodSpawner.availableObjects.Return(_food.gameObject);
        } else if (_food.name == "FriedKoi") {
            // _food.name = "FriedKoi";
            _food.SetState(InWater);
            _cookFish.availableObjects.Return(_food.gameObject);
        } else {
            _food.SetState(OutOfWater);
            _vPillSpawner.availableObjects.Return(_food.gameObject);
        }
    }
    
    private void OnTriggerExit(Collider collider) {
        if (collider.gameObject.name == "Water") {
            //outOfWater = true;
            SetState(OutOfWater);
            sync.SetGravity(true);
            sync.SetKinematic(false);
            _propBlock.SetFloat("_Speed", 20f);
        }
    }

    public void SpawnOutOfWater() {
        CreateNewKoi(true, false, OutOfWater);
    }

    public void CreateNewKoi(bool gravity, bool kinematic, byte state) {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        speed = 0.2f;
        fishSize = 0.04f;
        swappable = false;
        transform.localScale = new Vector3(fishSize, fishSize, fishSize);

        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _rigidBody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        //_rigidBody.useGravity = gravity;
        //_rigidBody.isKinematic = kinematic;
        sync.SetGravity(gravity);
        sync.SetKinematic(kinematic);
        currentState = state;

        Color primaryColor = _koiColor.AssignPrimaryColor();
        Color secondaryColor = _koiColor.AssignSecondaryColor();

        r = primaryColor.r;
        g = primaryColor.g;
        b = primaryColor.b;

        r2 = secondaryColor.r;
        g2 = secondaryColor.g;
        b2 = secondaryColor.b;

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

    // FOR PLAYER SYNCHRONIZATION
    public override void OnDeserialization() {
        if (currentState != _currentState) {
            _currentState = currentState;
        }
        if (r != _r || b != _b || b2 != _b2 || g2 != _g2) {
            syncNewFish();
        }
        if (fishSize != _fishSize) {
            syncFishGrowth();
        }
        if (desireOffspring != _desireOffspring) {
            _desireOffspring = desireOffspring;
        }
    }

    void syncFishGrowth() {
        _fishSize = fishSize;
        _speed = speed;
        transform.localScale = new Vector3(fishSize, fishSize, fishSize);
    }


    void syncNewFish() {
        _r = r; _b = b; _g = g;
        _r2 = r2; _b2 = b2; _g2 = g2;

        _renderer = GetComponent<Renderer>();

        _propBlock.SetColor("_Color", new Color(r, g, b));
        _propBlock.SetColor("_Color2", new Color(r2, g2, b2));

        _renderer.SetPropertyBlock(_propBlock);
    }
}
