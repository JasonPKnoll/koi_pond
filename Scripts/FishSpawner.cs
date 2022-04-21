
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.Udon.Common.Interfaces;
using VRC.SDKBase;
using VRC.Udon;

public class FishSpawner : UdonSharpBehaviour

{
    [SerializeField]
    public VRCObjectPool availableObjects;
    public Transform fishSpawnLocation;
    public GameObject spawnedObject;
    private float spawnSpeed = 2.0f;
    public Koi _koi;
    public float spawnRadius = 3f;

    private const byte Swimming = 1;
    public VRCObjectSync sync;

    void Start() {
        //sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        for (int i = 0; i < 10; i++) {
            SpawnNearSpawner();
        }
    }


    void SpawnNearSpawner() {
        spawnedObject = availableObjects.TryToSpawn();
        _koi = spawnedObject.GetComponent<Koi>();
        _koi.transform.position = transform.position + new Vector3(Random.Range(-spawnRadius, spawnRadius), 0f, Random.Range(-spawnRadius, spawnRadius));
        _koi.currentState = Swimming;
    }


    public void RespawnFromFall(GameObject fallenObject) {
        spawnedObject = fallenObject;
        Rigidbody _rigidbody = spawnedObject.GetComponent<Rigidbody>();
        _rigidbody.velocity = Vector3.zero;
        spawnedObject.transform.position = transform.position + new Vector3(0f, 8f, 0f);
    }

    public void PushButtonToggle() {
        this.SendCustomNetworkEvent(NetworkEventTarget.Owner, "SpawnAtLocation");
    }

    public void SpawnAtLocation() {
        if (availableObjects == null) return;
        spawnedObject = availableObjects.TryToSpawn();
        spawnedObject.transform.rotation = fishSpawnLocation.transform.localRotation;
        spawnedObject.transform.position = fishSpawnLocation.position;
        _koi = spawnedObject.GetComponent<Koi>();
        _koi.SpawnOutOfWater();
        _koi._rigidBody.velocity = _koi.transform.forward * spawnSpeed;
    }
}