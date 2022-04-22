
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon;


public class Koi : UdonSharpBehaviour
{
    private float rotationSpeed = 1.0f;
    private bool swappable = false;
    [UdonSynced] public bool desireOffspring = false;
    public bool _desireOffspring = false;
    public bool createsOffspring = false;

    public GameObject target;
    private Food _foodTarget;
    private Koi _koiTarget;

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
    private float timeToProduceOffspring = 3f;

    // Timers
    private float lastDirectionChangeTime;
    private float startRestTime;
    private float lastRestCheck;
    private float startCreatingOffspring;

    private VRCObjectSync sync;

    [UdonSynced]
    private Quaternion heading;

    // Components
    private Renderer _renderer;
    public Rigidbody _rigidBody;
    public Collider _collider;
    [SerializeField] MaterialPropertyBlock _propBlock;

    // State Machine :: Enum is not compatible with Udon
    public const byte Swimming = 1, OutOfWater = 2, AvoidingLeft = 3, AvoidingRight = 4,
        Resting = 5, SeekingFood = 6, SeekingMate = 7, InWater = 8;
    [UdonSynced] public byte currentState;
    public byte _currentState;

    // For RayCasting
    private float rayDistance = 0.3f;
    public LayerMask mask;
    public int fishMask = 1 << 23;

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

        float rayDistancePlus = rayDistance + fishSize * 5f;
        CheckDepth();

        if (desireOffspring == true) {
            SearchForMate();
        }

        if (Time.time > lastRestCheck + restCheckTimer) {
            float roll = Random.Range(0f, 1f);
            lastRestCheck = Time.time;
            if (roll > 0.8f) {
                startRestTime = Time.time;
                SetState(Resting);
            } 
        }

        Vector3 fwd = transform.TransformDirection(Vector3.forward);
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
        if (_koiTarget.currentState == OutOfWater || target.activeSelf == false) {
            startCreatingOffspring = 0;
            target = null;
            _koiTarget = null;
            SetState(Swimming);
        }

        Quaternion lookAtLocation = Quaternion.LookRotation(target.transform.position - transform.position);

        transform.rotation = Quaternion.Slerp(transform.rotation, lookAtLocation, rotationSpeed * 3f * Time.deltaTime);
        if (Vector3.Distance(transform.position, target.transform.position) > 0.15f) {
            transform.position += transform.forward * speed * 1.2f * Time.deltaTime;
            startCreatingOffspring = 0;
        } else {
            if (startCreatingOffspring == 0) {
                startCreatingOffspring = Time.time;
            } else {
                if (Time.time > startCreatingOffspring + timeToProduceOffspring) {
                    Procreate();
               }
            }
            // stop transform.position
        }
    }

    public void Procreate() {
        if (createsOffspring == true) {
            GameObject newFish = _fishSpawner.availableObjects.TryToSpawn();
            newFish.transform.position = transform.position;
            if (newFish == null) return;
            Koi newKoi = newFish.GetComponent<Koi>();
            newKoi.CreateKoiFromParents(this, _koiTarget, InWater);
        }
        desireOffspring = false;
        target = null;
        _koiTarget = null;
        SetState(Swimming);
        RequestSerialization();
    }

    public void CreateKoiFromParents(Koi firstParent, Koi secondParent, byte state) {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));

        float rollPrimary = Random.Range(0,1);
        float rollSecondary = Random.Range(0, 1);

        if (rollPrimary > 0.5f) {
            r = firstParent.r;
            g = firstParent.g;
            b = firstParent.b;
        } else {
            r = secondParent.r;
            g = secondParent.g;
            b = secondParent.b;
        }

        if (rollSecondary > 0.5f) {
            r2 = firstParent.r2;
            g2 = firstParent.g2;
            b2 = firstParent.b2;
        } else {
            r2 = firstParent.r2;
            g2 = firstParent.g2;
            b2 = firstParent.b2;
        }

        speed = 0.2f;
        fishSize = 0.04f;
        transform.localScale = new Vector3(fishSize, fishSize, fishSize);

        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _rigidBody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        sync.SetGravity(false);
        sync.SetKinematic(true);
        currentState = state;

        _propBlock.SetColor("_Color", new Color(r, g, b));
        _propBlock.SetColor("_Color2", new Color(r2, g2, b2));

        _renderer.SetPropertyBlock(_propBlock);
        RequestSerialization();
    }

    private void SearchForMate()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 3f, fishMask);
        foreach (var hitCollider in hitColliders) {
            if (hitCollider.gameObject == this.gameObject) return;
            Koi foundKoi = hitCollider.gameObject.GetComponent<Koi>();
            if (foundKoi.desireOffspring == true && foundKoi.target == null) {
                foundKoi.SetTargetFish(gameObject);
                SetTarget(foundKoi.gameObject);
                _koiTarget = foundKoi;
                SetState(SeekingMate);
                createsOffspring = true;
            }
        }
    }

    public void SetTargetFish(GameObject fish) {
        Koi koi = fish.GetComponent<Koi>();
        SetTarget(fish);
        _koiTarget = koi;
        SetState(SeekingMate);
        createsOffspring = false;
    }

    public void SetTarget(GameObject new_target) {
        target = new_target;
    }

    private void TryToEat() {
        Debug.DrawRay(transform.position, (target.transform.position - transform.position).normalized * 0.5f, Color.cyan);
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance < 0.12f) {
            if (target.name == "vPill")
                EatPill(target.gameObject);
            else
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
        if (collider.gameObject.name  == "vPill"  && fishSize >= fishSizeMax-fishSizeIncrement) {
            EatPill(collider.gameObject);
            desireOffspring = true;
        }
    }

    private void EatPill(GameObject pill) {
        _food = pill.gameObject.GetComponent<Food>();
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
            _food.SetState(OutOfWater);
            _foodSpawner.availableObjects.Return(_food.gameObject);
        } else if (_food.name == "FriedKoi") {
            _food.SetState(InWater);
            _cookFish.availableObjects.Return(_food.gameObject);
        } else {
            _food.SetState(OutOfWater);
            _vPillSpawner.availableObjects.Return(_food.gameObject);
        }
    }
    
    private void OnTriggerExit(Collider collider) {
        if (collider.gameObject.name == "Water") {
            SetState(OutOfWater);
            target = null;
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