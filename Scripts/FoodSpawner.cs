
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class FoodSpawner : UdonSharpBehaviour
{
    [SerializeField] 
    public VRCObjectPool availableObjects;
    public GameObject spawnedObject;

    private Rigidbody _rigidBody;

    void Start()
    {
        
    }

    public override void Interact()
    {
        SpawnObject();
    }
    public void SpawnObject()
    {
        if (!Networking.IsOwner(gameObject)) return;
        spawnedObject = availableObjects.TryToSpawn();
        _rigidBody = spawnedObject.GetComponent<Rigidbody>();
        spawnedObject.transform.position = transform.position + new Vector3(0, 0.5f, 0);
        _rigidBody.isKinematic = true;
        _rigidBody.useGravity = false;
    }
}
