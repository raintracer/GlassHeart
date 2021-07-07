// GENERATED AUTOMATICALLY FROM 'Assets/Input/ControlMap.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @ControlMap : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @ControlMap()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""ControlMap"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""0a9dc1b0-1487-40c8-bbfd-9957e885f224"",
            ""actions"": [
                {
                    ""name"": ""MoveCursor"",
                    ""type"": ""PassThrough"",
                    ""id"": ""f64962f1-7cd0-4eeb-8380-48fc47f49524"",
                    ""expectedControlType"": ""Dpad"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""SwitchAtCursor"",
                    ""type"": ""Button"",
                    ""id"": ""2be2e319-79f3-45b0-afd2-380202eb39ae"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ScrollBoost"",
                    ""type"": ""Button"",
                    ""id"": ""b9cc71c3-1981-4521-9659-c25516a5351e"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)""
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""WASD"",
                    ""id"": ""cdaa7303-9f58-4761-acd9-e447b6ad4565"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""83873efb-5558-4101-8396-2131fbafdc20"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""15958c25-a3b0-43ac-a854-4d844c65f656"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""0ad708fd-0813-4ca5-aff5-fb063ce7e6e5"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""ead85771-08be-45fc-a1ae-ae090e2b016c"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Arrows"",
                    ""id"": ""d64c084d-6f02-46b2-9985-29d45fd67c85"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""318b7bcb-362a-4802-acb0-dc60494990f2"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""f5b431d6-0085-41a6-a1c7-e8f223fcb4df"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""ee4b09fc-36fa-4a9f-82ad-2bb5a3c0596a"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""96d54070-48b3-40d2-b97c-4414eb41404c"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Gamepad"",
                    ""id"": ""f0bdf56b-b2da-4155-819d-4289b977ead8"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""fc88ead1-d119-436a-8e7c-d046aa957ab4"",
                    ""path"": ""<Gamepad>/dpad/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""977933c4-ca35-45cc-9e68-cadd4ae06128"",
                    ""path"": ""<Gamepad>/dpad/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""b3b73466-9dcf-42a8-b1e7-9b4b506453aa"",
                    ""path"": ""<Gamepad>/dpad/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""8d7884b3-ea5d-4376-9cd2-b7d0d8ca38ca"",
                    ""path"": ""<Gamepad>/dpad/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""f3d952b8-a5d6-4b54-819e-6adce7c4e52f"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""SwitchAtCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""79279d19-a4a4-4a45-bdc4-22c9926eb93f"",
                    ""path"": ""<Gamepad>/buttonWest"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""SwitchAtCursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""7394b86e-61ac-4187-bd6c-595c0774849e"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ScrollBoost"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Player
        m_Player = asset.FindActionMap("Player", throwIfNotFound: true);
        m_Player_MoveCursor = m_Player.FindAction("MoveCursor", throwIfNotFound: true);
        m_Player_SwitchAtCursor = m_Player.FindAction("SwitchAtCursor", throwIfNotFound: true);
        m_Player_ScrollBoost = m_Player.FindAction("ScrollBoost", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Player
    private readonly InputActionMap m_Player;
    private IPlayerActions m_PlayerActionsCallbackInterface;
    private readonly InputAction m_Player_MoveCursor;
    private readonly InputAction m_Player_SwitchAtCursor;
    private readonly InputAction m_Player_ScrollBoost;
    public struct PlayerActions
    {
        private @ControlMap m_Wrapper;
        public PlayerActions(@ControlMap wrapper) { m_Wrapper = wrapper; }
        public InputAction @MoveCursor => m_Wrapper.m_Player_MoveCursor;
        public InputAction @SwitchAtCursor => m_Wrapper.m_Player_SwitchAtCursor;
        public InputAction @ScrollBoost => m_Wrapper.m_Player_ScrollBoost;
        public InputActionMap Get() { return m_Wrapper.m_Player; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
        public void SetCallbacks(IPlayerActions instance)
        {
            if (m_Wrapper.m_PlayerActionsCallbackInterface != null)
            {
                @MoveCursor.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveCursor;
                @MoveCursor.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveCursor;
                @MoveCursor.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveCursor;
                @SwitchAtCursor.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSwitchAtCursor;
                @SwitchAtCursor.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSwitchAtCursor;
                @SwitchAtCursor.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSwitchAtCursor;
                @ScrollBoost.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScrollBoost;
                @ScrollBoost.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScrollBoost;
                @ScrollBoost.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnScrollBoost;
            }
            m_Wrapper.m_PlayerActionsCallbackInterface = instance;
            if (instance != null)
            {
                @MoveCursor.started += instance.OnMoveCursor;
                @MoveCursor.performed += instance.OnMoveCursor;
                @MoveCursor.canceled += instance.OnMoveCursor;
                @SwitchAtCursor.started += instance.OnSwitchAtCursor;
                @SwitchAtCursor.performed += instance.OnSwitchAtCursor;
                @SwitchAtCursor.canceled += instance.OnSwitchAtCursor;
                @ScrollBoost.started += instance.OnScrollBoost;
                @ScrollBoost.performed += instance.OnScrollBoost;
                @ScrollBoost.canceled += instance.OnScrollBoost;
            }
        }
    }
    public PlayerActions @Player => new PlayerActions(this);
    public interface IPlayerActions
    {
        void OnMoveCursor(InputAction.CallbackContext context);
        void OnSwitchAtCursor(InputAction.CallbackContext context);
        void OnScrollBoost(InputAction.CallbackContext context);
    }
}
