
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class CookFish : UdonSharpBehaviour
{

    private float fishSize;

    [SerializeField]
    public VRCObjectPool availableObjects;
    [SerializeField]
    FishSpawner _fishSpawner;
    [SerializeField]
    GameObject _fish;
    [SerializeField]
    Koi _koi;
    GameObject _cookedFish;

    void Start()
    {
        
    }

    public void Cook(GameObject fish)
    {
        _fish = fish;
        _koi = _fish.GetComponent<Koi>();
        fishSize = _koi.fishSize;
        _fishSpawner.availableObjects.Return(_fish);
        _cookedFish = availableObjects.TryToSpawn();
        _cookedFish.transform.localScale = new Vector3(fishSize, fishSize, fishSize);
        _cookedFish.transform.position = transform.position + new Vector3(0, 0.1f, 0);
    }    
}
