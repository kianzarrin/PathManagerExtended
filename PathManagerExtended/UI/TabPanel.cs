﻿using ColossalFramework.UI;
using PathManagerExtended.Util;
using PathManagerExtended.UI.Editors;
using UnityEngine;

namespace PathManagerExtended.UI
{
    public class CustomUITabstrip : UITabstrip
    {
        public CustomUITabstrip()
        {
            atlas = TextureUtil.InGameAtlas;
            backgroundSprite = "";
        }
        public void AddTab(string name, float textScale = 0.85f)
        {
            var tabButton = base.AddTab(name);
            tabButton.autoSize = true;
            tabButton.textPadding = new RectOffset(5, 5, 2, 2);
            tabButton.textScale = textScale;
            tabButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            tabButton.verticalAlignment = UIVerticalAlignment.Middle;

            tabButton.atlas = TextureUtil.InGameAtlas;

            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = "SubBarButtonBasePressed";

            tabButton.Invalidate();

            FitChildrenVertically();
        }
        protected override void OnSizeChanged() => FitChildrenVertically();
    }
}
