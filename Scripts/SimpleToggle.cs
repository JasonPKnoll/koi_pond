
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SimpleToggle : UdonSharpBehaviour
{

    public UdonBehaviour eventTarget;
    void Start() {
        
    }

    public override void Interact() {
        eventTarget.SendCustomEvent("ClickButtonToggle");
    }
}
