
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
    public Koi _koiTarget;

    [UdonSynced] public float speed = 0.0f;
    public float fishSpeedIncrement = 0.1f;
    private float _speed = 0.0f;

    [UdonSynced] public float fishSize = 0.04f;
    public float fishSizeIncrement = 0.01f;
    public float fishSizeMax = 0.07f;
    public float _fishSize = 0.03f;

    public AudioSource audioMakeOffspring;

    public ParticleSystem particleHearts;

    // Color Values
    [UdonSynced] public float r = 0f, g = 0f, b = 0f;
    [UdonSynced] public float r2 = 0f, g2 = 0f, b2 = 0f;
    public float _r = 0f, _g = 0f, _b = 0f;
    public float _r2 = 0f, _g2 = 0f, _b2 = 0f;

    // Intervals
    private float directionChangeInterval = 3.0f;
    private float restTime = 8f;
    private float restCheckTimer = 10f;
    private float timeToProduceOffspring = 5f;
    private float outOfWaterInterval = 0.03f;

    // Timers
    private float lastDirectionChangeTime;
    private float startRestTime;
    private float lastRestCheck;
    private float startCreatingOffspring;
    private float lastOutOfWaterTime;

    private VRCObjectSync sync;

    [UdonSynced]
    private Quaternion heading;
    private Quaternion avoidDirection;

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
    [SerializeField] AudioManager _audioManager;

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

        if (transform.position.y < -8) {
            //_fishSpawner.RespawnFromFall(gameObject);
            _rigidBody.velocity = Vector3.zero;
            _fishSpawner.ThisSpawnAtLocation(this);
        }
    }

    public void SetState(byte changedState) {
        currentState = changedState;
        RequestSerialization();
    }

    private void UpdateSwimming() {

        //float rayDistancePlus = rayDistance + fishSize * 5f;
        CheckDepth();

        if (desireOffspring == true) {
            SearchForMate();
            if (_koiTarget) SetState(SeekingMate);
        }

        if (Time.time > lastRestCheck + restCheckTimer) {
            AttemptRest();
        }

        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        if (!Physics.Raycast(transform.position, fwd, rayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * rayDistance, Color.green);

            transform.rotation = Quaternion.Lerp(transform.rotation, heading, Time.deltaTime * rotationSpeed);
            transform.position += transform.forward * speed * Time.deltaTime;

            if (Time.time > lastDirectionChangeTime + directionChangeInterval) {
                ChooseHeading();
            }
        }

        if (Physics.Raycast(transform.position, fwd, rayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * rayDistance, Color.red);

            float roll = Random.Range(0f, 1f);
           
            if (roll >= 0.90f) {
                startRestTime = Time.time;
                SetState(Resting);
            } else if (roll >= 0.45f) {
                CheckLeftFirst();
            } else {
                CheckRightFirst();
            }
        }
    }

    private void AttemptRest() {
        float roll = Random.Range(0f, 1f);
        lastRestCheck = Time.time;
        if (roll > 0.8f) {
            startRestTime = Time.time;
            SetState(Resting);
        } 
    }

    private void CheckDepth() {
        if (-0.001f >= transform.position.y || transform.position.y >= 0.1f) {
            Vector3 depthTarget = new Vector3(transform.position.x, 0.005f, transform.position.z);
            Quaternion depthTargetQuanternion = Quaternion.Euler(depthTarget.x, depthTarget.y, depthTarget.z);

            transform.rotation = Quaternion.Slerp(transform.rotation, depthTargetQuanternion, rotationSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, depthTarget, speed * Time.deltaTime);
        }
    }

    private void HeartsAnimation() {
        if (desireOffspring == true) {
            particleHearts.Play();
        }
    }

    private void UpdateOutOfWater() {
        //Debug.Log("VELOCITY: "+ (_rigidBody.velocity.magnitude > 0.0000001f));
        //Debug.Log("DISTANCE BETWEEN: "+(Vector3.Distance(gameObject.transform.position, Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head))));

        //if ( _rigidBody.velocity.magnitude > 0.01f && Vector3.Distance(gameObject.transform.position, Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head)) < 1.0f) {
        if (Vector3.Distance(gameObject.transform.position, Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head)) <= 0.3f) {
            float audioPitch = Random.Range(0.8f, 1.2f);
            _audioManager.PlayOnce(_audioManager.audioBonk, gameObject, audioPitch);
        }
    }


    private void UpdateAvoidingLeft() {
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        Vector3 left = transform.TransformDirection(Vector3.left);

        float increasedRayDistance = (rayDistance + fishSize * 5f) * 1.2f;

        if (Physics.Raycast(transform.position, fwd, increasedRayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * increasedRayDistance, Color.red);

            if (!Physics.Raycast(transform.position, left, rayDistance, mask)) {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * rayDistance, Color.green);

                avoidDirection = Quaternion.Euler(transform.position.x, transform.eulerAngles.y - 90.0f, transform.position.z);
                transform.localRotation = Quaternion.Lerp(transform.rotation, avoidDirection, rotationSpeed * 1.25f * Time.deltaTime);

            //else if (Physics.Raycast(transform.position, left, rayDistance, mask)) {
            //    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * rayDistance, Color.red);
            //    SetState(AvoidingRight);
            //}
            }
        }

        if (!Physics.Raycast(transform.position, fwd, increasedRayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * increasedRayDistance, Color.green);
            transform.localRotation = Quaternion.Lerp(transform.rotation, avoidDirection, rotationSpeed * 1.25f * Time.deltaTime);
            heading = Quaternion.Euler(0f, transform.eulerAngles.y - 90f, 0f);
            lastRestCheck = Time.time;
            lastDirectionChangeTime = Time.time;
            SetState(Swimming);
        }
    }

    private void UpdateAvoidingRight() {
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float increasedRayDistance = (rayDistance + fishSize * 5f) * 1.2f;

        if (Physics.Raycast(transform.position, fwd, increasedRayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * increasedRayDistance, Color.red);

            if (!Physics.Raycast(transform.position, right, rayDistance, mask)) {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * rayDistance, Color.green);

                avoidDirection = Quaternion.Euler(transform.position.x, transform.eulerAngles.y + 90f, transform.position.z);
                transform.localRotation = Quaternion.Lerp(transform.rotation, avoidDirection, rotationSpeed * 1.25f * Time.deltaTime);

                //    } else if (Physics.Raycast(transform.position, right, rayDistance, mask)) {
                //        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * rayDistance, Color.red);
                //        SetState(AvoidingLeft);
            }
        } 

        if (!Physics.Raycast(transform.position, fwd, increasedRayDistance, mask)) {
            Debug.DrawRay(transform.position, fwd * increasedRayDistance, Color.green);
            transform.localRotation = Quaternion.Lerp(transform.rotation, avoidDirection, rotationSpeed * 1.25f * Time.deltaTime);
            heading = Quaternion.Euler(0f, transform.eulerAngles.y + 90f, 0f);
            lastRestCheck = Time.time;
            lastDirectionChangeTime = Time.time;
            SetState(Swimming);
        }
    }

    private void CheckLeftFirst() {
        Vector3 left = transform.TransformDirection(Vector3.left);

        // If left raycast is not hitting a wall than rotate left
        if (!Physics.Raycast(transform.position, left, rayDistance, mask)) {
            SetState(AvoidingLeft);
        } else {
            SetState(AvoidingRight);
        }
    }

    private void CheckRightFirst() {
        Vector3 right = transform.TransformDirection(Vector3.right);

        // If right raycast is not hitting a wall than rotate right
        if (!Physics.Raycast(transform.position, right, rayDistance, mask)) {
            SetState(AvoidingRight);
        } else {
            SetState(AvoidingLeft);
        }
    }

    private void UpdateResting() {
        if (Time.time > restTime + startRestTime) {
            float roll = Random.Range(0f, 1f);
            
            if (roll >= 0.50f) {
                CheckLeftFirst();
            } else {
                CheckRightFirst();
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

        if (_koiTarget.currentState == OutOfWater || target.activeSelf == false || _koiTarget._koiTarget != this) {
            ResetFromSeekingMate();
            SetState(Swimming);
            return;
        }

        Quaternion lookAtLocation = Quaternion.LookRotation(target.transform.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookAtLocation, rotationSpeed * 3f * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.transform.position) > 0.15f) {
            transform.position += transform.forward * speed * 1.2f * Time.deltaTime;
            startCreatingOffspring = 0;
            if (audioMakeOffspring != null) {
                audioMakeOffspring.Stop();
                audioMakeOffspring = null;
            }

        } else {
            if (startCreatingOffspring == 0) {
                if (createsOffspring == true) {
                    audioMakeOffspring = _audioManager.GetAudio(_audioManager.audioMakingOffspring, gameObject);
                    if (audioMakeOffspring != null) audioMakeOffspring.Play();
                    startCreatingOffspring = Time.time;
                }
            } else if (Time.time > startCreatingOffspring + timeToProduceOffspring) {
                Procreate();
            }
            // Do Nothing
        }
    }

    public void ResetFromSeekingMate() {
        startCreatingOffspring = 0;
        createsOffspring = false;
        target = null;
        _koiTarget = null;
        if (audioMakeOffspring != null)
        {
            audioMakeOffspring.Stop();
            audioMakeOffspring = null;
        }
    }

    public void Procreate() {
        if (createsOffspring == true) {
            GameObject newFish = _fishSpawner.availableObjects.TryToSpawn();
            if (newFish == null) {
                _koiTarget.ResetFromProcreate();
                ResetFromProcreate();
                return;
            } else {
                newFish.transform.position = transform.position + Vector3.forward * 0.075f;
                Koi newKoi = newFish.GetComponent<Koi>();
                newKoi.CreateKoiFromParents(this, _koiTarget, InWater);
                _koiTarget.ResetFromProcreate();
                ResetFromProcreate();
            }
        }
    }

    public void ResetFromProcreate() {
        desireOffspring = false;
        createsOffspring = false;
        target = null;
        _koiTarget = null;
        SetState(Swimming);
        particleHearts.Stop();
        if (audioMakeOffspring != null) {
            audioMakeOffspring.Stop();
            audioMakeOffspring = null;
        }
        RequestSerialization();
    }

    private void SearchForMate()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 3f, fishMask);
        foreach (var hitCollider in hitColliders) {
            if (hitCollider.gameObject == this.gameObject) return;
            Koi foundKoi = hitCollider.gameObject.GetComponent<Koi>();
            if (foundKoi == null) return;
            if (foundKoi.currentState != OutOfWater && foundKoi.desireOffspring == true && foundKoi.target == null) {
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

    private void OnTriggerExit(Collider collider) {
        if (Time.time > lastOutOfWaterTime + outOfWaterInterval && collider.gameObject.name == "Water") {
            float audioPitch = Random.Range(0.6f, 0.9f);
            _audioManager.PlayOnce(_audioManager.audioSplash, gameObject, audioPitch);
            if (Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
                if (currentState == SeekingMate) {
                    ResetFromSeekingMate();
                }
                SetState(OutOfWater);
                target = null;
                sync.SetGravity(true);
                sync.SetKinematic(false);
                lastOutOfWaterTime = Time.time;
            }
            //_propBlock.SetFloat("_Speed", 20f);
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

        if (Time.time > lastOutOfWaterTime + outOfWaterInterval && collider.gameObject.name == "Water") {
            float audioPitch = Random.Range(1.0f, 1.2f);
            _audioManager.PlayOnce(_audioManager.audioSplash, gameObject, audioPitch);
            SetState(Swimming);
            sync.SetKinematic(true);
            sync.SetGravity(false);
            lastRestCheck = Time.time;
            lastOutOfWaterTime = Time.time;
            //_propBlock.SetFloat("_Speed", 5f); // Not working
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
            particleHearts.Play();
        }
    }

    private void EatPill(GameObject pill) {
        _food = pill.gameObject.GetComponent<Food>();
        float audioPitch = Random.Range(1.0f, 1.2f);
        _audioManager.PlayOnce(_audioManager.audioEat, gameObject, audioPitch);
        desireOffspring = true;
        particleHearts.Play();
        RespawnFood(_food);
        RequestSerialization();
    }

    private void EatFood(GameObject food) {
        if (Networking.IsOwner(Networking.LocalPlayer, gameObject)) {
            float audioPitch = Random.Range(1.0f, 1.2f);
            _audioManager.PlayOnce(_audioManager.audioEat, gameObject, audioPitch);
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

    public override void OnPickup() {
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
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