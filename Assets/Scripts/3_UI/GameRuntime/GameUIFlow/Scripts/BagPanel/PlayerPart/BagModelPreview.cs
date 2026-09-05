using MmInventory;
using UnityEngine;
using UnityEngine.UI;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// 背包模型区 TP 预览 挂在 RawImage 上 负责 RT 与旋转滑条
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class BagModelPreview : MonoBehaviour
    {
        /// <summary> 预览 RT 短边像素 </summary>
        private const int PreviewRtShortSide = 512;

        /// <summary> 预览显示 </summary>
        private RawImage rawImage;

        /// <summary> 旋转滑条 </summary>
        private Slider rotateSlider;

        /// <summary> 背包模型预览 RT </summary>
        private RenderTexture modelPreviewRt;

        /// <summary> 是否已初始化 </summary>
        private bool isInited;

        /// <summary>
        /// 由 BagPanel 在面板就绪后调用一次
        /// </summary>
        public void InitComponents()
        {
            if (isInited)
                return;

            rawImage = GetComponent<RawImage>();
            rotateSlider = GetComponentInChildren<Slider>(true);

            if (rawImage != null)
                rawImage.raycastTarget = false;

            if (rotateSlider != null)
                rotateSlider.onValueChanged.AddListener(OnRotateSliderChanged);

            isInited = true;
        }

        /// <summary>
        /// 注销滑条监听
        /// </summary>
        public void Unbind()
        {
            if (!isInited)
                return;

            if (rotateSlider != null)
                rotateSlider.onValueChanged.RemoveListener(OnRotateSliderChanged);

            isInited = false;
        }

        /// <summary>
        /// 打开预览渲染与旋转
        /// </summary>
        public void BeginPreview()
        {
            BindAndOpenTpPreview();
            BeginModelRotation();
        }

        /// <summary>
        /// 关闭预览渲染并还原旋转
        /// </summary>
        public void EndPreview()
        {
            EndModelRotation();
            CloseTpPreview();
        }

        /// <summary>
        /// 释放预览 RT
        /// </summary>
        public void Release()
        {
            CloseTpPreview();
            if (modelPreviewRt == null)
                return;

            modelPreviewRt.Release();
            Object.Destroy(modelPreviewRt);
            modelPreviewRt = null;
        }

        /// <summary>
        /// 打开时重置滑条并缓存相机偏航
        /// </summary>
        private void BeginModelRotation()
        {
            if (rotateSlider != null)
                rotateSlider.SetValueWithoutNotify(0f);

            PlayerTpPreviewHub.Current?.BeginPreviewYaw();
        }

        /// <summary>
        /// 关闭时还原预览相机偏航
        /// </summary>
        private void EndModelRotation()
        {
            PlayerTpPreviewHub.Current?.EndPreviewYaw();
        }

        /// <summary>
        /// 创建 RT 绑到 TP 相机与 RawImage
        /// </summary>
        private void BindAndOpenTpPreview()
        {
            var preview = PlayerTpPreviewHub.Current;
            if (preview == null || rawImage == null)
                return;

            EnsurePreviewRtMatchesRawImage();
            ApplyRawImageStretchFill();

            preview.BindTargetTexture(modelPreviewRt);
            rawImage.texture = modelPreviewRt;
            rawImage.color = Color.white;
            preview.OpenTPCamera(true);
        }

        /// <summary>
        /// RawImage 全向拉伸铺满父节点
        /// </summary>
        private void ApplyRawImageStretchFill()
        {
            var rect = rawImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 按 RawImage 宽高比重建 RT 避免竖框拉伸方形画面
        /// </summary>
        private void EnsurePreviewRtMatchesRawImage()
        {
            Rect rect = rawImage.rectTransform.rect;
            float width = Mathf.Abs(rect.width);
            float height = Mathf.Abs(rect.height);
            if (width < 1f)
                width = 1f;
            if (height < 1f)
                height = 1f;

            float aspect = width / height;
            int rtWidth;
            int rtHeight;
            if (aspect >= 1f)
            {
                rtHeight = PreviewRtShortSide;
                rtWidth = Mathf.Max(1, Mathf.RoundToInt(PreviewRtShortSide * aspect));
            }
            else
            {
                rtWidth = PreviewRtShortSide;
                rtHeight = Mathf.Max(1, Mathf.RoundToInt(PreviewRtShortSide / aspect));
            }

            if (modelPreviewRt != null
                && modelPreviewRt.width == rtWidth
                && modelPreviewRt.height == rtHeight)
                return;

            if (modelPreviewRt != null)
            {
                modelPreviewRt.Release();
                Object.Destroy(modelPreviewRt);
                modelPreviewRt = null;
            }

            modelPreviewRt = new RenderTexture(rtWidth, rtHeight, 16);
            modelPreviewRt.name = "BagModelPreviewRT";
        }

        /// <summary>
        /// 关闭 TP 预览相机
        /// </summary>
        private void CloseTpPreview()
        {
            PlayerTpPreviewHub.Current?.OpenTPCamera(false);
        }

        /// <summary>
        /// 滑条回调 0~1 映射为绕世界 Y 的 0~360 度
        /// </summary>
        private void OnRotateSliderChanged(float value)
        {
            float yawDegrees = value * 360f;
            PlayerTpPreviewHub.Current?.SetPreviewYaw(yawDegrees);
        }
    }
}
