using System.Globalization;
using TMPro;
using UnityEngine;

public class InputFieldSizeHelper : MonoBehaviour
{
    [SerializeField] TMP_InputField inputField;

    float fontSize;
    [SerializeField] float spacePerLine;
    [SerializeField] bool isOn;
    [SerializeField] float offset;

    private void Awake()
    {
        fontSize = inputField.textComponent.fontSize;
        spacePerLine = inputField.fontAsset.faceInfo.lineHeight / inputField.fontAsset.faceInfo.pointSize * fontSize;
    }

    void Update()
    {
        if(isOn)
        {
            int count = inputField.textComponent.textInfo.lineCount;
            inputField.GetComponent<RectTransform>().sizeDelta = new Vector2(inputField.GetComponent<RectTransform>().sizeDelta.x, spacePerLine * count + offset);
        }
    }
}
