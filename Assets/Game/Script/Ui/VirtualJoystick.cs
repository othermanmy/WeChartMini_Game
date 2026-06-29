using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Script.Ui
{
    public class VirtualJoystick : MonoBehaviour,IDragHandler,IPointerDownHandler,IPointerUpHandler
    {
        [Header("摇杆UI")]
        [SerializeField]private RectTransform joystickBg;
        [SerializeField]private RectTransform joystick;
        
        [Tooltip("0-1，小于该比例将视为 0")]
        [Range(0f, 1f)] [SerializeField] private float deadZone = 0.1f;
        [Tooltip("是否在 pointer up 重置位置为中心")]
        [SerializeField] private bool resetOnPointerUp = true;

        // 当前归一化方向（x,y），长度在 [0,1]
        [SerializeField]
        private Vector2 input = Vector2.zero;
        // 当前 pointer id，当没有按下时为 int.MinValue
        private int activePointerId = int.MinValue;

        // 外部可读属性
        public Vector2 Direction => input; // 已归一化（包含长度信息）
        public float Magnitude => input.magnitude; // 0..1
        
        public Action<Vector2> OnValueChanged;

        // 计算的摇杆最大半径（像素），基于 joystickBg 的 sizeDelta
        private float radius => (joystickBg) ? (Mathf.Min(joystickBg.rect.width, joystickBg.rect.height) * 0.5f) : 0f;

        private void Reset()
        {
            // 尝试自动绑定
            if (!joystickBg)
            {
                var rt = transform.GetChild(0).GetComponent<RectTransform>();
                if (rt) joystickBg = rt;
            }
            if (!joystick && joystickBg && joystickBg.childCount > 0)
            {
                var child = joystickBg.GetChild(0) as RectTransform;
                if (child) joystick = child;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 记录开始的 pointer id，并即刻处理这次按下
            if (activePointerId != int.MinValue) return; // 已有活跃指针则忽略
            activePointerId = eventData.pointerId;
            ProcessDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 只有同一指针可以控制当前摇杆
            if (eventData == null) return;
            if (eventData.pointerId != activePointerId) return;
            ProcessDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null) return;
            if (eventData.pointerId != activePointerId) return;

            // 复位
            if (resetOnPointerUp && joystick)
            {
                joystick.anchoredPosition = Vector2.zero;
            }

            SetInput(Vector2.zero);
            activePointerId = int.MinValue;
        }

        private void ProcessDrag(PointerEventData eventData)
        {
            if (!joystickBg || !joystick) return;

            // 将屏幕点映射到 joystickBg 的本地坐标（pivot 为参考）
            RectTransformUtility.ScreenPointToLocalPointInRectangle
                (joystickBg, eventData.position, eventData.pressEventCamera, out var localPoint);

            // localPoint 是相对 joystickBg pivot 的局部坐标（以像素为单位）
            // 限制到半径内
            Vector2 clamped = Vector2.ClampMagnitude(localPoint, radius);

            // 将 joystick 的 anchoredPosition 设置成 clamped（注意：需要 joystick 的锚点与父元素一致以正常工作）
            joystick.anchoredPosition = clamped;

            // 归一化到 [-1,1] 之间
            Vector2 normalized = (radius > 0f) ? (clamped / radius) : Vector2.zero;

            // 应用死区
            if (normalized.magnitude < deadZone)
            {
                normalized = Vector2.zero;
            }

            // 保持长度不超过 1
            normalized = Vector2.ClampMagnitude(normalized, 1f);

            SetInput(normalized);
        }

        private void SetInput(Vector2 val)
        {
            if (input != val)
            {
                input = val;
                OnValueChanged?.Invoke(input);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑器中保证引用不丢
            if (!joystickBg)
            {
                var rt = transform.GetChild(0).GetComponent<RectTransform>();
                if (rt) joystickBg = rt;
            }
        }
#endif
    }
}
