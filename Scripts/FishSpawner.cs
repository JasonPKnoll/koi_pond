
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
    public Vector3 fishSpawnLocation;
    public GameObject spawnedObject;
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

    public void SpawnInteract() {
        this.SendCustomNetworkEvent(NetworkEventTarget.Owner, "SpawnAtLocation");
    }

    public void SpawnAtLocation() {
        spawnedObject = availableObjects.TryToSpawn();
        _koi = spawnedObject.GetComponent<Koi>();
        _koi.transform.position = fishSpawnLocation;
    }
}