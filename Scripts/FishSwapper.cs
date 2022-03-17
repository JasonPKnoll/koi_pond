
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class FishSwapper : UdonSharpBehaviour
{
    private VRCObjectSync sync;
    private Rigidbody _rigidBody;

    [SerializeField] FishSpawner _fishSpawner;
    [SerializeField] GameObject _fish;
    [SerializeField] Koi _koi;

    void Start()
    {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
    }

    public void OnEnable()
    {
        //sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
    }

    public void SwapFish(GameObject fish) {
        sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
        if (!Networking.IsOwner(gameObject))
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _fishSpawner.availableObjects.Return(fish);
        fish = _fishSpawner.availableObjects.TryToSpawn();
        _koi = fish.GetComponent<Koi>();
        _koi.SpawnOutOfWater();
        _koi.outOfWater = true;
        fish.transform.position = transform.position + new Vector3(0, 0.50f, 0);
        _koi.RequestSerialization();
    }
}