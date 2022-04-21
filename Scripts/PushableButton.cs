
using UdonSharp;
using UnityEngine;
//using UnityEngine.Events;
using VRC.SDKBase;
using VRC.Udon;

public class PushableButton : UdonSharpBehaviour
{
    public Transform buttonTop;
    public Transform buttonLowerLimit;
    public Transform buttonUpperLimit;
    public float thresHold;
    public float force = 10;
    private float upperLowerDiff;
    public bool isPressed;
    private bool prevPressedState;
    public AudioSource pressedSound;
    public AudioSource releasedSound;
    public UdonBehaviour eventTarget;
    public Rigidbody _buttonTopRigidBody;

    void Start() {
        Physics.IgnoreCollision(GetComponent<Collider>(), buttonTop.GetComponent<Collider>());
        _buttonTopRigidBody = buttonTop.GetComponent<Rigidbody>();
        if (transform.eulerAngles != Vector3.zero) {
            Vector3 savedAngle = transform.eulerAngles;
            upperLowerDiff = buttonUpperLimit.position.y - buttonLowerLimit.position.y;
            transform.eulerAngles = savedAngle;
        } else {
            upperLowerDiff = buttonUpperLimit.position.y - buttonLowerLimit.position.y;
        }
    }

    private void Update() {
        buttonTop.transform.localPosition = new Vector3(0, buttonTop.transform.localPosition.y, 0);
        buttonTop.transform.localEulerAngles = new Vector3(0, 0, 0);

        if (buttonTop.localPosition.y >= 0) {
            buttonTop.transform.position = new Vector3(buttonUpperLimit.position.x, buttonUpperLimit.position.y, buttonUpperLimit.position.z);
        } else {
            _buttonTopRigidBody.AddForce(buttonTop.transform.up * force * Time.deltaTime);
        }

        if (buttonTop.localPosition.y <= buttonLowerLimit.localPosition.y) {
            buttonTop.transform.position = new Vector3(buttonLowerLimit.position.x, buttonLowerLimit.position.y, buttonLowerLimit.position.z);
        }

        if (Vector3.Distance(buttonTop.position, buttonLowerLimit.position) < upperLowerDiff * thresHold) {
            isPressed = true;
        } else {
            isPressed = false;
        }

        if (isPressed && prevPressedState != isPressed) {
            Pressed();
        }
        if (!isPressed && prevPressedState != isPressed) {
            Released();
        }
    }

    public void Pressed() {
        prevPressedState = isPressed;
        pressedSound.pitch = 1;
        pressedSound.Play();
    }

    public void Released() {
        prevPressedState = isPressed;
        releasedSound.pitch = Random.Range(1.1f, 1.2f);
        releasedSound.Play();
        eventTarget.SendCustomEvent("PushButtonToggle");
    }
}