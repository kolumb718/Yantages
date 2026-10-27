using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CourierAction { Interact, Descend, Cancel, Camera, Sprint }

// Input System actions for the courier, built in code so keyboard and gamepad live in one place.
// Bindings carry Keyboard/Gamepad groups: prompts show the keys of whichever device was used last.
public sealed class CourierInput : IDisposable
{
    const string KeyboardGroup = "Keyboard", GamepadGroup = "Gamepad";

    readonly InputActionMap map = new InputActionMap("Courier");
    readonly InputAction move;
    readonly InputAction mouseLook, stickLook, orbitHold, scroll;
    readonly InputAction click, pointer;
    readonly InputAction[] buttons;

    public bool UsingGamepad { get; private set; }

    public CourierInput()
    {
        move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w", KeyboardGroup).With("Down", "<Keyboard>/s", KeyboardGroup)
            .With("Left", "<Keyboard>/a", KeyboardGroup).With("Right", "<Keyboard>/d", KeyboardGroup);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow", KeyboardGroup).With("Down", "<Keyboard>/downArrow", KeyboardGroup)
            .With("Left", "<Keyboard>/leftArrow", KeyboardGroup).With("Right", "<Keyboard>/rightArrow", KeyboardGroup);
        move.AddBinding("<Gamepad>/leftStick", groups: GamepadGroup);
        move.AddBinding("<Gamepad>/dpad", groups: GamepadGroup);
        mouseLook = map.AddAction("MouseLook",InputActionType.Value,"<Mouse>/delta");
        stickLook = map.AddAction("StickLook",InputActionType.Value,"<Gamepad>/rightStick");
        orbitHold = map.AddAction("OrbitHold",InputActionType.Button,"<Mouse>/rightButton");
        scroll = map.AddAction("Zoom",InputActionType.Value,"<Mouse>/scroll/y");
        click = map.AddAction("Click",InputActionType.Button,"<Mouse>/leftButton");
        pointer = map.AddAction("Pointer",InputActionType.Value,"<Mouse>/position");

        // Order matches CourierAction.
        buttons = new[]
        {
            Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth", "<Keyboard>/enter"),
            Button("Descend", "<Keyboard>/r", "<Gamepad>/buttonWest"),
            Button("Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast"),
            Button("Camera", "<Keyboard>/c", "<Gamepad>/buttonNorth"),
            Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftTrigger", "<Keyboard>/rightShift"),
        };
        map.actionTriggered += context =>
        {
            if (context.control != null) UsingGamepad = context.control.device is Gamepad;
        };
        map.Enable();
    }

    InputAction Button(string name, string key, string pad, string extraKey = null)
    {
        var action = map.AddAction(name, InputActionType.Button);
        action.AddBinding(key, groups: KeyboardGroup);
        action.AddBinding(pad, groups: GamepadGroup);
        if (extraKey != null) action.AddBinding(extraKey, groups: KeyboardGroup);
        return action;
    }

    // Screen-space intent: x to the right, y away from the camera; diagonals are normalised.
    public Vector2 Move => Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1);
    public Vector2 LookDelta(float deltaTime) =>
        (orbitHold.IsPressed() ? mouseLook.ReadValue<Vector2>()*.12f : Vector2.zero)
        + stickLook.ReadValue<Vector2>()*(120*deltaTime);
    public float ZoomDelta => -scroll.ReadValue<float>()*.0025f;
    public bool ClickPressed => click.WasPressedThisFrame();
    public Vector2 PointerPosition => pointer.ReadValue<Vector2>();
    public bool Held(CourierAction action) => buttons[(int)action].IsPressed();
    public bool Pressed(CourierAction action) => buttons[(int)action].WasPressedThisFrame();

    public string MoveLabel => UsingGamepad ? "L-стик" : "WASD";

    // Display name of the first binding for the active device, e.g. "E" or "A".
    public string Label(CourierAction action)
    {
        var input = buttons[(int)action];
        string group = UsingGamepad ? GamepadGroup : KeyboardGroup;
        for (int i = 0; i < input.bindings.Count; i++)
            if (input.bindings[i].groups == group) return input.GetBindingDisplayString(i);
        return "";
    }

    public void Dispose()
    {
        map.Disable();
        map.Dispose();
    }
}
