using UnityEngine;

public class CloseAnimationButton : MonoBehaviour
{
    // 綁在叉叉 Button 的 OnClick 事件
    public void OnClosePressed()
    {
        ARInteractiveDot.CloseCurrentFromButton();
    }
}
