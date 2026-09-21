using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class Unmask : Image
{
    // 마스크 가능한 객체임을 알림
    public override Material materialForRendering
    {
        get
        {
            Material material = new Material(base.materialForRendering);
            // 스텐실 버퍼 설정: 이 이미지가 그려지는 곳의 값을 변경하여 
            // 부모 마스크가 그리지 못하게 함
            material.SetInt("_StencilComp", (int)CompareFunction.Always);
            material.SetInt("_StencilOp", (int)StencilOp.Zero); // 0으로 만들어 구멍 뚫기
            return material;
        }
    }
}