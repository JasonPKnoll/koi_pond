
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class FishSwapper : UdonSharpBehaviour
{ 
    //[SerializeField]
    //public VRCObjectPool availableObjects;
    [SerializeField]
    FishSpawner _fishSpawner;
    [SerializeField]
    GameObject _fish;
    [SerializeField]
    Koi _koi;

    void Start()
    {
        
    }

    public void SwapFish(GameObject fish)
    {
        _fish = fish;
        _fishSpawner.availableObjects.Return(_fish);
        _fish = _fishSpawner.availableObjects.TryToSpawn();
        //_koi = GetComponent<Koi>();
        //_koi.SpawnOutOfWater();
        _koi = _fish.GetComponent<Koi>();
        _koi.SpawnOutOfWater();
        _koi.transform.position = transform.position + new Vector3(0, 0.5f, 0);
    }    
}