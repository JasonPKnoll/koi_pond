
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.Udon.Common.Interfaces;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class FoodSpawner : UdonSharpBehaviour
{
    [SerializeField]  public VRCObjectPool availableObjects;
    private GameObject spawnedObject;

    [SerializeField] Food _food;

    private Rigidbody _rigidBody;
    private VRCObjectSync sync;

    void Start() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
    }

    public void PushButtonToggle() {
        this.SendCustomNetworkEvent(NetworkEventTarget.Owner, "SpawnObject");
    }

    public override void Interact() {
        this.SendCustomNetworkEvent(NetworkEventTarget.Owner, "SpawnObject");
    }
    public void SpawnObject() {
        if (availableObjects == null) return;
        availableObjects.TryToSpawn();
    }
}

