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
    }

    private void Update()
    {
        Debug.Log($"Update wasESC : {wasESC}  keyboadESC : {keyboadESC} wasCanceled : {wasCanceled}");
    }

    void Log(string text)
    {
        wasESC = Input.GetKeyDown(KeyCode.Escape);

        TouchScreenKeyboard kbd = inputField != null ? inputField.touchScreenKeyboard : null;
        keyboadESC = kbd != null && kbd.status == TouchScreenKeyboard.Status.Canceled;
        wasCanceled = inputField.wasCanceled;

        Debug.Log($"UpdateLog wasESC : {wasESC}  keyboadESC : {keyboadESC} wasCanceled : {wasCanceled}");
    }
}
