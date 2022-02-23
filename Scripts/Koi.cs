

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


public class Koi : UdonSharpBehaviour
{
    public float speed = 0.0f;
    public float rotationSpeed = 1.0f;
    public float directionChangeInterval = 2.0f;
    private bool fertility = false;
    public float fishSize = 0.04f;
    private bool swappable = false;
    private float lastDirectionChangeTime;
    private Quaternion heading;

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private Rigidbody _rigidBody;
    private Collider _collider;

    // For RayCasting
    private float rayDistance = 0.5f;
    public LayerMask mask;
    RaycastHit hit;

    // Access to other Classes
    [SerializeField]
    CookFish _cookFish;
    [SerializeField]
    FishSwapper _fishSwapper;
    [SerializeField]
    FoodSpawner _foodSpawner;
    [SerializeField]
    FishSpawner _fishSpawner;
    [SerializeField]
    GameObject _food;

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

    void DrawRays()
    {
        if (gameObject.transform.eulerAngles.y != 0f)
        {
            gameObject.transform.position += Vector3.up * 0;
        }
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        Vector3 left = transform.TransformDirection(Vector3.left);

        if (Physics.Raycast(transform.position, fwd, rayDistance, mask))
        {
            if (Physics.Raycast(transform.position, right, rayDistance, mask))
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * rayDistance, Color.red);
                if (Physics.Raycast(transform.position, left, rayDistance, mask))
                {
                    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * rayDistance, Color.red);
                }
                else
                {
                    heading = Quaternion.Euler(0.0f, heading.eulerAngles.y - 90.0f, 0.0f);
                    transform.Rotate(0, -90 * Time.deltaTime, 0);

                    Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.left) * rayDistance, Color.green);
                }
            }
            else
            {

                heading = Quaternion.Euler(0.0f, heading.eulerAngles.y + 90.0f, 0.0f);
                transform.Rotate(0, 90 * Time.deltaTime, 0);

                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * rayDistance, Color.green);
            }

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * rayDistance, Color.red);
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, heading, Time.deltaTime * rotationSpeed);
            transform.position += transform.forward * speed * Time.deltaTime;

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * rayDistance, Color.green);
            if (Time.time > lastDirectionChangeTime + directionChangeInterval)
            {
                ChooseHeading();
            }
        }
    }

    void FoodRay()
    {
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        if (Physics.Raycast(transform.position, fwd, 1))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1, Color.red);
            var objectHit = hit.collider.gameObject;
            if (objectHit.name == "Food")
            {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1, Color.yellow);
            }
            else
            {
            }
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1, Color.green);
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name == "Fire")
        {
            _cookFish.Cook(gameObject);
        }

        if (collider.gameObject.name == "FishSwapper")
        {
            if (swappable == true)
                _fishSwapper.SwapFish(gameObject);
        }

        if (collider.gameObject.name == "Water")
        {
            _rigidBody.useGravity = false;
            _rigidBody.isKinematic = true;
            //_collider.isTrigger = true;
            _propBlock.SetFloat("_Speed", 5f);
            gameObject.transform.position += new Vector3(0f, 0f, 0f);
        }

        if (collider.gameObject.name == "Food")
        {
            if (fishSize < 0.08f)
            {
                _food = collider.gameObject;
                _foodSpawner.availableObjects.Return(_food);
                fishSize += 0.01f;
                speed += 0.2f;
                transform.localScale = new Vector3(fishSize, fishSize, fishSize);
            }
            else
            {
                ToggleFertility();
            }
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

    public void SpawnOutOfWater()
    {
        ChooseHeading();
        speed = 0.2f;
        swappable = false;
        transform.localScale = new Vector3(fishSize, fishSize, fishSize);

        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _rigidBody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();

        _rigidBody.useGravity = true;
        _rigidBody.isKinematic = false;
        _propBlock.SetColor("_Color", new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
        _propBlock.SetColor("_Color2", new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));

        _renderer.SetPropertyBlock(_propBlock);
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
    }
}
