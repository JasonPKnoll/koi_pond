
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class FishSpawner : UdonSharpBehaviour

{
    [SerializeField]
    public VRCObjectPool availableObjects;
    public GameObject spawnedObject;

    void Start()
    {
        SpawnTen();
    }

    void SpawnTen()
    {
        for (int i = 0; i < 10; i++)
        {
            spawnedObject = availableObjects.TryToSpawn();
            spawnedObject.transform.position = transform.position + new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-1.2f, 1.2f));
        }
    }

    public void RespawnFromFall(GameObject fallenObject)
    {

        //for (int i = 0; i < availableObjects.Pool.Length; i++)
        //{
        //if (availableObjects.Pool[i].name == fallenObject)
        //spawnedObject = availableObjects.Pool[i];
        //}
        spawnedObject = fallenObject;
        spawnedObject.transform.position = transform.position + new Vector3(0f, 10f, 0f);
    }    
}
