using System;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace XrayUI.Helpers
{
    /// <summary>
    /// Composition-driven cross-fade for the main↔mini window-mode switch.
    ///
    /// The outgoing panel just snaps away (so the click stays
    /// as responsive as a plain switch); the incoming panel scales + fades in as a
    /// short, non-blocking entrance flourish.
    ///
    /// The animation runs on the Composition (render) thread, so it stays smooth
    /// even while the UI thread is busy resizing.
    /// </summary>
    internal static class WindowModeTransition
    {

        private const float ShrunkScale = 0.90f;


        private static readonly TimeSpan InDuration = TimeSpan.FromMilliseconds(160);

        /// <summary>
        /// Stage a panel in its hidden (invisible + shrunk) state synchronously, so it
        /// doesn't flash at full opacity when its bound Visibility flips to Visible.
        /// </summary>
        public static void PrepareHidden(FrameworkElement element)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.Opacity = 0f;
            visual.Scale = new Vector3(ShrunkScale, ShrunkScale, 1f);
        }

        /// <summary>
        /// Fade + scale the newly visible panel up to its resting state. Fire-and-forget:
        /// the animation lives on the render thread, and nothing awaits its completion, so
        /// there's no Task/ScopedBatch plumbing to signal one.
        /// </summary>
        public static void FadeIn(FrameworkElement element)
        {
            // The window just resized; force a layout pass so CenterPoint uses the
            // panel's new size rather than the stale pre-resize one.
            element.UpdateLayout();

            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            // Pivot the scale around the panel's center, not its top-left corner.
            visual.CenterPoint = new Vector3(
                (float)(element.ActualWidth  / 2),
                (float)(element.ActualHeight / 2),
                0f);

            // Ease-out (Fluent "Decelerate"): arrive fast, settle softly.
            var ease = compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f));

            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(1f, 1f, ease);
            fade.Duration = InDuration;

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(1f, Vector3.One, ease);
            scale.Duration = InDuration;

            visual.StartAnimation("Opacity", fade);
            visual.StartAnimation("Scale", scale);
        }
    }
}
