using TMPro;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] TMP_InputField inputField;
    bool wasESC;
    bool keyboadESC;
    bool wasCanceled;

    private void Awake()
    {
        inputField.onSubmit.AddListener(Log);
        inputField.onTouchScreenKeyboardStatusChanged.AddListener(HandleKeyboardStatusChange);

    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            wasESC = true;
        }

        if(inputField.touchScreenKeyboard?.status == TouchScreenKeyboard.Status.Canceled)
        {
            keyboadESC = true;
        }

        if(inputField.wasCanceled)
        {
            wasCanceled = true;
        }

        Debug.Log($"Update wasESC : {wasESC}  keyboadESC : {keyboadESC} wasCanceled : {wasCanceled}");
    }

    void Log(string text)
    {

        Debug.Log($"UpdateLog wasESC : {wasESC}  keyboadESC : {keyboadESC} wasCanceled : {wasCanceled}");

        wasESC = false;
        keyboadESC = false;
        wasCanceled = false;

    }

    private void HandleKeyboardStatusChange(TouchScreenKeyboard.Status status)
    {
        if(TouchScreenKeyboard.visible)
            Debug.Log("TouchScreenKeyboard is visible.");
        else
            Debug.Log("TouchScreenKeyboard is not visible.");

        if (status == TouchScreenKeyboard.Status.Canceled)
        {
            keyboadESC = true;
            Debug.Log("TouchScreenKeyboard was canceled by the user.");
        }
        else if (status == TouchScreenKeyboard.Status.Done)
        {
            Debug.Log("TouchScreenKeyboard input was submitted.");
        }
        else
        {
            Debug.Log("Keyboard status changed to: " + status);
        }
    }
}
