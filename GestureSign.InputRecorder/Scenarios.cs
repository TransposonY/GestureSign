using System.Collections.Generic;

namespace GestureSign.InputRecorder
{
    internal sealed class Scenario
    {
        public Scenario(string id, string instruction)
        {
            Id = id;
            Instruction = instruction;
        }

        /// <summary>File name stem: &lt;id&gt;.gsrec.json.</summary>
        public string Id { get; }

        /// <summary>Shown to the user and stored as the recording's Description.</summary>
        public string Instruction { get; }

        public override string ToString() => Id;
    }

    internal static class Scenarios
    {
        public static readonly IReadOnlyList<Scenario> All = new[]
        {
            new Scenario("touchpad-1finger-L", "Touchpad: with ONE finger draw an 'L' (straight down, then right), then lift the finger."),
            new Scenario("touchpad-1finger-circle", "Touchpad: with ONE finger draw a full circle, then lift the finger."),
            new Scenario("touchpad-2finger-swipe-down", "Touchpad: put TWO fingers down together, swipe straight down, lift both."),
            new Scenario("touchpad-3finger-tap", "Touchpad: tap once with THREE fingers at the same time, no movement."),
            new Scenario("touchpad-finger-replaced", "Touchpad: start a ONE-finger stroke to the right, lift the finger briefly mid-stroke, put it back down and continue to the right, then lift."),
            new Scenario("touchpad-2finger-swipe-up", "Touchpad: put TWO fingers down together, swipe straight up, lift both."),
            new Scenario("touchpad-2finger-swipe-left", "Touchpad: put TWO fingers down together, swipe straight left, lift both."),
            new Scenario("touchpad-2finger-swipe-right", "Touchpad: put TWO fingers down together, swipe straight right, lift both."),
            new Scenario("touchpad-2finger-L", "Touchpad: with TWO fingers together draw an 'L' (straight down, then right), then lift both."),
            new Scenario("touchpad-3finger-swipe-up", "Touchpad: put THREE fingers down together, swipe straight up, lift all."),
            new Scenario("touchpad-3finger-swipe-down", "Touchpad: put THREE fingers down together, swipe straight down, lift all."),
            new Scenario("touchpad-3finger-swipe-left", "Touchpad: put THREE fingers down together, swipe straight left, lift all."),
            new Scenario("touchpad-3finger-swipe-right", "Touchpad: put THREE fingers down together, swipe straight right, lift all."),
            new Scenario("touchpad-3finger-L", "Touchpad: with THREE fingers together draw an 'L' (straight down, then right), then lift all."),
            new Scenario("touchpad-4finger-swipe-down", "Touchpad: put FOUR fingers down together, swipe straight down, lift all."),
            new Scenario("touchpad-4finger-swipe-up", "Touchpad: put FOUR fingers down together, swipe straight up, lift all."),
            new Scenario("touchpad-4finger-tap", "Touchpad: tap once with FOUR fingers at the same time, no movement."),
            new Scenario("touchpad-1finger-click-drag", "Touchpad: press the pad down until it clicks, keep it pressed and drag ONE finger to the right, release."),

            new Scenario("touchscreen-1finger-stroke", "Touch screen: with ONE finger draw a stroke from left to right, then lift."),
            new Scenario("touchscreen-2finger-stroke", "Touch screen: with TWO fingers together draw a stroke downwards, then lift both."),
            new Scenario("touchscreen-3finger-stroke", "Touch screen: with THREE fingers together draw a stroke upwards, then lift all."),
            new Scenario("touchscreen-quick-tap", "Touch screen: tap once quickly with ONE finger, no movement."),

            new Scenario("pen-tip-stroke", "Pen: touch the screen with the pen tip and draw a 'Z', then lift the pen away from the screen."),
            new Scenario("pen-hover-barrel-stroke", "Pen: hover the pen just above the screen (not touching), press and hold the barrel button, draw a stroke to the right while hovering, release the button, move the pen away."),
            new Scenario("pen-eraser-stroke", "Pen: turn the pen around and draw a stroke with the eraser end (or hold the invert/eraser button), then lift."),

            new Scenario("mouse-drawing-button-gesture", "Mouse: hold the mouse button configured as GestureSign's drawing button (see Settings.DrawingButton, usually Right) and draw a 'Z', then release."),
            new Scenario("mouse-right-click", "Mouse: right-click once without moving the mouse."),
        };
    }
}
