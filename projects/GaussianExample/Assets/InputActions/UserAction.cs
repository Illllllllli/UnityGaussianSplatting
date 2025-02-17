//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.7.0
//     from Assets/InputActions/UserAction.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @UserAction: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @UserAction()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""UserAction"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""234de8d5-9777-461f-9c84-b48c61534fd0"",
            ""actions"": [
                {
                    ""name"": ""Mouse Left Click"",
                    ""type"": ""Button"",
                    ""id"": ""a1d942e8-d6be-44c4-9f65-ad4f4f00ed7e"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Mouse Movement"",
                    ""type"": ""Value"",
                    ""id"": ""9b94e6bb-ac6b-49d7-8606-0ce6d9ed2088"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Switch View Mode"",
                    ""type"": ""Button"",
                    ""id"": ""e5e6af57-96d9-4479-894a-9de835e2487f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""SwitchSelectMode"",
                    ""type"": ""Button"",
                    ""id"": ""65d9935d-b6f5-4b62-82e1-dc8317e07640"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Switch Edit Mode"",
                    ""type"": ""Button"",
                    ""id"": ""eb2ec678-f0ac-45f4-9dd7-b8084be5abe0"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""W key"",
                    ""type"": ""Value"",
                    ""id"": ""3d36303a-d956-4ea2-9c04-37b4b048e493"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""A key"",
                    ""type"": ""Button"",
                    ""id"": ""fd6960b1-0b11-4450-b0b8-3b70a883ad35"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""S key"",
                    ""type"": ""Button"",
                    ""id"": ""df7017af-3b9d-4ec7-8523-968130c77d5c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""D key"",
                    ""type"": ""Button"",
                    ""id"": ""061e6b56-764d-40f1-9555-2c11af0c9683"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""E key"",
                    ""type"": ""Button"",
                    ""id"": ""57a624c3-4785-499a-bd52-1822424a5d57"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Q key"",
                    ""type"": ""Button"",
                    ""id"": ""97c3cfab-7ad9-42b5-9ac4-57499a71fa11"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""31ddaf57-b22b-4553-86a1-6d8963533ad7"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Mouse Left Click"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5fa3497a-6c90-4e24-bfce-28369b1d346b"",
                    ""path"": ""<Keyboard>/1"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Switch View Mode"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""28b9dbe3-6883-482b-b041-2dffd7edda5d"",
                    ""path"": ""<Keyboard>/2"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""SwitchSelectMode"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a6b630a6-a8f7-4c86-958d-437764c4d94d"",
                    ""path"": ""<Keyboard>/3"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Switch Edit Mode"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c1febc2e-fa42-4d70-9014-f2ab6d21af63"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Mouse Movement"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""fd934779-f8c5-4eba-a3dc-455f1de32f97"",
                    ""path"": ""<Keyboard>/#(A)"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""A key"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c2705ea8-5815-4803-8334-a04cddd6caef"",
                    ""path"": ""<Keyboard>/#(S)"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""S key"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b1505cae-173e-4027-9d0e-00d365a3fb9f"",
                    ""path"": ""<Keyboard>/#(D)"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""D key"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1ef8fe0c-c36d-4d35-a9f6-889e90537589"",
                    ""path"": ""<Keyboard>/#(W)"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""W key"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""25d77d08-9562-486a-9f69-e550d520146f"",
                    ""path"": ""<Keyboard>/#(E)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""E key"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a87fbe42-3af8-4d06-a001-289913b40053"",
                    ""path"": ""<Keyboard>/#(Q)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Q key"",
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
        m_Player_MouseLeftClick = m_Player.FindAction("Mouse Left Click", throwIfNotFound: true);
        m_Player_MouseMovement = m_Player.FindAction("Mouse Movement", throwIfNotFound: true);
        m_Player_SwitchViewMode = m_Player.FindAction("Switch View Mode", throwIfNotFound: true);
        m_Player_SwitchSelectMode = m_Player.FindAction("SwitchSelectMode", throwIfNotFound: true);
        m_Player_SwitchEditMode = m_Player.FindAction("Switch Edit Mode", throwIfNotFound: true);
        m_Player_Wkey = m_Player.FindAction("W key", throwIfNotFound: true);
        m_Player_Akey = m_Player.FindAction("A key", throwIfNotFound: true);
        m_Player_Skey = m_Player.FindAction("S key", throwIfNotFound: true);
        m_Player_Dkey = m_Player.FindAction("D key", throwIfNotFound: true);
        m_Player_Ekey = m_Player.FindAction("E key", throwIfNotFound: true);
        m_Player_Qkey = m_Player.FindAction("Q key", throwIfNotFound: true);
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

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Player
    private readonly InputActionMap m_Player;
    private List<IPlayerActions> m_PlayerActionsCallbackInterfaces = new List<IPlayerActions>();
    private readonly InputAction m_Player_MouseLeftClick;
    private readonly InputAction m_Player_MouseMovement;
    private readonly InputAction m_Player_SwitchViewMode;
    private readonly InputAction m_Player_SwitchSelectMode;
    private readonly InputAction m_Player_SwitchEditMode;
    private readonly InputAction m_Player_Wkey;
    private readonly InputAction m_Player_Akey;
    private readonly InputAction m_Player_Skey;
    private readonly InputAction m_Player_Dkey;
    private readonly InputAction m_Player_Ekey;
    private readonly InputAction m_Player_Qkey;
    public struct PlayerActions
    {
        private @UserAction m_Wrapper;
        public PlayerActions(@UserAction wrapper) { m_Wrapper = wrapper; }
        public InputAction @MouseLeftClick => m_Wrapper.m_Player_MouseLeftClick;
        public InputAction @MouseMovement => m_Wrapper.m_Player_MouseMovement;
        public InputAction @SwitchViewMode => m_Wrapper.m_Player_SwitchViewMode;
        public InputAction @SwitchSelectMode => m_Wrapper.m_Player_SwitchSelectMode;
        public InputAction @SwitchEditMode => m_Wrapper.m_Player_SwitchEditMode;
        public InputAction @Wkey => m_Wrapper.m_Player_Wkey;
        public InputAction @Akey => m_Wrapper.m_Player_Akey;
        public InputAction @Skey => m_Wrapper.m_Player_Skey;
        public InputAction @Dkey => m_Wrapper.m_Player_Dkey;
        public InputAction @Ekey => m_Wrapper.m_Player_Ekey;
        public InputAction @Qkey => m_Wrapper.m_Player_Qkey;
        public InputActionMap Get() { return m_Wrapper.m_Player; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
        public void AddCallbacks(IPlayerActions instance)
        {
            if (instance == null || m_Wrapper.m_PlayerActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_PlayerActionsCallbackInterfaces.Add(instance);
            @MouseLeftClick.started += instance.OnMouseLeftClick;
            @MouseLeftClick.performed += instance.OnMouseLeftClick;
            @MouseLeftClick.canceled += instance.OnMouseLeftClick;
            @MouseMovement.started += instance.OnMouseMovement;
            @MouseMovement.performed += instance.OnMouseMovement;
            @MouseMovement.canceled += instance.OnMouseMovement;
            @SwitchViewMode.started += instance.OnSwitchViewMode;
            @SwitchViewMode.performed += instance.OnSwitchViewMode;
            @SwitchViewMode.canceled += instance.OnSwitchViewMode;
            @SwitchSelectMode.started += instance.OnSwitchSelectMode;
            @SwitchSelectMode.performed += instance.OnSwitchSelectMode;
            @SwitchSelectMode.canceled += instance.OnSwitchSelectMode;
            @SwitchEditMode.started += instance.OnSwitchEditMode;
            @SwitchEditMode.performed += instance.OnSwitchEditMode;
            @SwitchEditMode.canceled += instance.OnSwitchEditMode;
            @Wkey.started += instance.OnWkey;
            @Wkey.performed += instance.OnWkey;
            @Wkey.canceled += instance.OnWkey;
            @Akey.started += instance.OnAkey;
            @Akey.performed += instance.OnAkey;
            @Akey.canceled += instance.OnAkey;
            @Skey.started += instance.OnSkey;
            @Skey.performed += instance.OnSkey;
            @Skey.canceled += instance.OnSkey;
            @Dkey.started += instance.OnDkey;
            @Dkey.performed += instance.OnDkey;
            @Dkey.canceled += instance.OnDkey;
            @Ekey.started += instance.OnEkey;
            @Ekey.performed += instance.OnEkey;
            @Ekey.canceled += instance.OnEkey;
            @Qkey.started += instance.OnQkey;
            @Qkey.performed += instance.OnQkey;
            @Qkey.canceled += instance.OnQkey;
        }

        private void UnregisterCallbacks(IPlayerActions instance)
        {
            @MouseLeftClick.started -= instance.OnMouseLeftClick;
            @MouseLeftClick.performed -= instance.OnMouseLeftClick;
            @MouseLeftClick.canceled -= instance.OnMouseLeftClick;
            @MouseMovement.started -= instance.OnMouseMovement;
            @MouseMovement.performed -= instance.OnMouseMovement;
            @MouseMovement.canceled -= instance.OnMouseMovement;
            @SwitchViewMode.started -= instance.OnSwitchViewMode;
            @SwitchViewMode.performed -= instance.OnSwitchViewMode;
            @SwitchViewMode.canceled -= instance.OnSwitchViewMode;
            @SwitchSelectMode.started -= instance.OnSwitchSelectMode;
            @SwitchSelectMode.performed -= instance.OnSwitchSelectMode;
            @SwitchSelectMode.canceled -= instance.OnSwitchSelectMode;
            @SwitchEditMode.started -= instance.OnSwitchEditMode;
            @SwitchEditMode.performed -= instance.OnSwitchEditMode;
            @SwitchEditMode.canceled -= instance.OnSwitchEditMode;
            @Wkey.started -= instance.OnWkey;
            @Wkey.performed -= instance.OnWkey;
            @Wkey.canceled -= instance.OnWkey;
            @Akey.started -= instance.OnAkey;
            @Akey.performed -= instance.OnAkey;
            @Akey.canceled -= instance.OnAkey;
            @Skey.started -= instance.OnSkey;
            @Skey.performed -= instance.OnSkey;
            @Skey.canceled -= instance.OnSkey;
            @Dkey.started -= instance.OnDkey;
            @Dkey.performed -= instance.OnDkey;
            @Dkey.canceled -= instance.OnDkey;
            @Ekey.started -= instance.OnEkey;
            @Ekey.performed -= instance.OnEkey;
            @Ekey.canceled -= instance.OnEkey;
            @Qkey.started -= instance.OnQkey;
            @Qkey.performed -= instance.OnQkey;
            @Qkey.canceled -= instance.OnQkey;
        }

        public void RemoveCallbacks(IPlayerActions instance)
        {
            if (m_Wrapper.m_PlayerActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IPlayerActions instance)
        {
            foreach (var item in m_Wrapper.m_PlayerActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_PlayerActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public PlayerActions @Player => new PlayerActions(this);
    public interface IPlayerActions
    {
        void OnMouseLeftClick(InputAction.CallbackContext context);
        void OnMouseMovement(InputAction.CallbackContext context);
        void OnSwitchViewMode(InputAction.CallbackContext context);
        void OnSwitchSelectMode(InputAction.CallbackContext context);
        void OnSwitchEditMode(InputAction.CallbackContext context);
        void OnWkey(InputAction.CallbackContext context);
        void OnAkey(InputAction.CallbackContext context);
        void OnSkey(InputAction.CallbackContext context);
        void OnDkey(InputAction.CallbackContext context);
        void OnEkey(InputAction.CallbackContext context);
        void OnQkey(InputAction.CallbackContext context);
    }
}
