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

    void Log(string text)
    {
        wasESC = Input.GetKeyDown(KeyCode.Escape);
        keyboadESC = TouchScreenKeyboard;
        wasCanceled = inputField.wasCanceled;

        Debug.Log($"wasESC : {wasESC}  keyboadESC : {keyboadESC} wasCanceled : {wasCanceled}");
    }
}
