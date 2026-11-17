using UnityEngine;
using UnityEngine.UI;

namespace VertigoShift.UI
{
    public static class UIFactory
    {
        public static Canvas BuatCanvas(string nama, RenderMode mode = RenderMode.ScreenSpaceOverlay)
        {
            var go = new GameObject(nama);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = mode;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform BuatPanel(Transform induk, string nama, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color? latar = null)
        {
            var go = new GameObject(nama);
            go.transform.SetParent(induk, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            if (latar.HasValue)
            {
                var img = go.AddComponent<Image>();
                img.color = latar.Value;
                img.raycastTarget = false;
            }

            return rt;
        }

        // ---------------------------------------------------------------- Text

        public static Text BuatText(Transform induk, string nama, string teks, Font font, int ukuran,
            Color warna, TextAnchor rata, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, bool raycast = false)
        {
            var go = new GameObject(nama);
            go.transform.SetParent(induk, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var txt = go.AddComponent<Text>();
            txt.text = teks;
            txt.font = font;
            txt.fontSize = ukuran;
            txt.color = warna;
            txt.alignment = rata;
            txt.raycastTarget = raycast;

            return txt;
        }

        public static void SetText(Text txt, string teks)
        {
            if (txt != null) txt.text = teks;
        }

        // ---------------------------------------------------------------- Image

        public static Image BuatImage(Transform induk, string nama, Sprite sprite, Color warna,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, bool raycast = false)
        {
            var go = new GameObject(nama);
            go.transform.SetParent(induk, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = warna;
            img.raycastTarget = raycast;

            return img;
        }

        public static Image BuatGaris(Transform induk, string nama, Color warna, float tebal,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(nama);
            go.transform.SetParent(induk, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, tebal);

            var img = go.AddComponent<Image>();
            img.color = warna;
            img.raycastTarget = false;

            return img;
        }

        // ---------------------------------------------------------------- Button

        public static Button BuatTombol(Transform induk, string nama, string teks, Font font, int ukuran,
            Color warnaTeks, Color latar, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(nama);
            go.transform.SetParent(induk, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var bg = go.AddComponent<Image>();
            bg.color = latar;
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.ColorTint;
            btn.colors = new ColorBlock
            {
                normalColor = latar,
                highlightedColor = latar * 1.2f,
                pressedColor = latar * 0.8f,
                disabledColor = latar * 0.5f,
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            var txtGo = new GameObject("Teks");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = txtGo.AddComponent<Text>();
            txt.text = teks;
            txt.font = font;
            txt.fontSize = ukuran;
            txt.color = warnaTeks;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            return btn;
        }

        // ---------------------------------------------------------------- Slider

        public static Slider BuatSlider(Transform induk, string nama, float nilai, float min, float maks,
            Color latar, Color isi, Color handle, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(nama);
            go.transform.SetParent(induk, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = maks;
            slider.value = nilai;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;

            // Background bar
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = latar;
            bgImg.raycastTarget = false;
            slider.targetGraphic = bgImg;

            // Fill area
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = isi;
            fillImg.raycastTarget = false;
            slider.fillRect = fillRt;

            // Handle
            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(go.transform, false);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.anchorMin = new Vector2(0f, 0.5f);
            handleRt.anchorMax = new Vector2(0f, 0.5f);
            handleRt.sizeDelta = new Vector2(20f, 30f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = handle;
            handleImg.raycastTarget = true;
            slider.handleRect = handleRt;

            return slider;
        }
    }
}
