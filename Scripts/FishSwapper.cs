
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class FishSwapper : UdonSharpBehaviour
{
    private VRCObjectSync sync;
    private Rigidbody _rigidBody;

    private const byte  OutOfWater = 2;

    [SerializeField] FishSpawner _fishSpawner;
    [SerializeField] GameObject _fish;
    [SerializeField] Koi _koi;

    void Start() {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
    }

    public void SwapFish(GameObject fish) {
        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _fish = fish;
        _fishSpawner.availableObjects.Return(_fish);
        fish = _fishSpawner.availableObjects.TryToSpawn();
        _koi = fish.GetComponent<Koi>();
        _koi.SpawnOutOfWater();
        _koi.currentState = OutOfWater;
        fish.transform.position = transform.position + new Vector3(0, 0.50f, 0);
        _koi.RequestSerialization();
    }
}