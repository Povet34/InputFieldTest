using UnityEngine;
using UnityEngine.UI;

public class ScreenShot : MonoBehaviour
{
    [SerializeField] RawImage target;
    [SerializeField] Camera Camera;

    [Header("RenderTexture Settings")]
    [SerializeField] int width = 1024;
    [SerializeField] int height = 1024;
    [Tooltip("카메라가 자동으로 RenderTexture에 렌더링하게 할지(활성화시 매프레임)")]
    [SerializeField] bool cameraAutoRender = true;

    // 현재 RawImage에 할당된 임시 RenderTexture (GetTemporary으로 생성)
    RenderTexture currentTempRT;

    private void OnEnable()
    {
        if (Camera == null || target == null) return;

        // 카메라 배경을 투명으로 설정
        Camera.clearFlags = CameraClearFlags.SolidColor;
        Camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
    }

    private void OnDisable()
    {
        ReleaseCurrentTempRT();
    }

    private void OnDestroy()
    {
        ReleaseCurrentTempRT();
    }

    private void Update()
    {
        if (cameraAutoRender)
        {
            CreateNewTempRTAndRender(); // 매프레임 새로운 RT 생성 및 렌더
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            CaptureOnce(); // 수동 캡처 (한 프레임만)
        }
    }

    // 매 프레임: 새 임시 RT를 만들고 카메라로 렌더, 기존 RT 해제
    void CreateNewTempRTAndRender()
    {
        if (Camera == null || target == null) return;

        // 새 임시 RT 생성
        RenderTexture newRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        newRT.filterMode = FilterMode.Bilinear;
        newRT.wrapMode = TextureWrapMode.Clamp;

        // 카메라에 임시로 할당해서 렌더 -> 바로 복구
        Camera.targetTexture = newRT;
        Camera.Render();
        Camera.targetTexture = null;

        // RawImage에 연결 (이제 UI는 newRT를 참조)
        target.texture = newRT;

        // 이전 RT가 있으면 반환
        if (currentTempRT != null)
        {
            RenderTexture.ReleaseTemporary(currentTempRT);
            currentTempRT = null;
        }

        // 현재 RT 갱신 (다음 프레임에 해제됨)
        currentTempRT = newRT;
    }

    // 수동 캡처: 한 프레임만 새로운 RT를 만들어 렌더하고 RawImage에 연결
    public void CaptureOnce()
    {
        if (Camera == null || target == null) return;

        // 새 임시 RT 생성
        RenderTexture newRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        newRT.filterMode = FilterMode.Bilinear;
        newRT.wrapMode = TextureWrapMode.Clamp;

        // 임시 할당하여 렌더
        Camera.targetTexture = newRT;
        Camera.Render();
        Camera.targetTexture = null;

        // RawImage에 연결 및 이전 RT 해제
        if (currentTempRT != null)
        {
            RenderTexture.ReleaseTemporary(currentTempRT);
            currentTempRT = null;
        }

        target.texture = newRT;
        currentTempRT = newRT;
    }

    // 현재 보관중인 임시 RT 해제
    void ReleaseCurrentTempRT()
    {
        if (currentTempRT != null)
        {
            RenderTexture.ReleaseTemporary(currentTempRT);
            currentTempRT = null;
        }

        if (target != null)
            target.texture = null;
    }
}