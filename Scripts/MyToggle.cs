
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MyToggle : UdonSharpBehaviour
{
    public GameObject target;
    public GameObject[] targets;
    void Start()
    {

    }

    public override void Interact()
    {
        if (targets != null)
        {
            foreach (var target in targets)
            {
                target.SetActive(!target.activeSelf);
            }
        }

        if (target != null)
        {
            target.SetActive(!target.activeSelf);
        }
    }
}
