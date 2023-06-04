#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[Serializable]
public struct KeyImage
{
    public string bindingPath;
    public string groups;
    public Sprite image;
}

public class PhysicalButtonManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiAsset = null!;
    [SerializeField] private PlayerInput playerInput = null!;
    [SerializeField] private KeyImage[] keyImages = new KeyImage[] { };

    private List<(VisualElement element, InputAction action, KeyImage[] keyImages)> elements = new();
    private int? lastDeviceId = null;

    void Awake()
    {
        var uiElements = VisualElementUtils.FindAndFilterAllChildren(uiAsset.rootVisualElement, e => e.name == "PhysicalButton");

        foreach (var uiElement in uiElements)
        {
            string actionName = uiElement.viewDataKey;
            InputAction? action = playerInput.currentActionMap.actions.FirstOrDefault(action => action.name == actionName);

            if (action == null)
            {
                Debug.LogError($"Cannot find corresponding action for PhysicalButton named \"{uiElement.name}\"");
                continue;
            };

            KeyImage[] elementKeyImages = action.bindings.SelectMany((binding, idx) =>
                keyImages.Where(keyImage => keyImage.bindingPath == binding.path)
            ).ToArray();

            if (elementKeyImages.Length == 0)
            {
                Debug.LogError($"Cannot find KeyImage that match bindings of action named \"{action.name}\"");
                continue;
            };

            elements.Add((uiElement, action, elementKeyImages));
        }

        playerInput.onControlsChanged += OnControlsChanged;
        SetUIImages();
    }

    private void OnControlsChanged(PlayerInput playerInput)
    {
        InputDevice device = playerInput.devices[0];

        if (lastDeviceId == device.deviceId) return;
        lastDeviceId = device.deviceId;

        SetUIImages();
    }

    private void SetUIImages()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            var e = elements[i];
            var bindingMask = InputBinding.MaskByGroup(playerInput.currentControlScheme);

            KeyImage? keyImage = null;
            foreach (var currentKeyImage in e.keyImages)
            {
                InputBinding binding = new InputBinding(currentKeyImage.bindingPath, null, currentKeyImage.groups);
                if (!bindingMask.Matches(binding)) continue;

                keyImage = currentKeyImage;
                break;
            }

            if (keyImage == null)
            {
                Debug.LogError("PhysicalButton component cannot find image to show.");
                continue;
            }

            Background background = Background.FromSprite(keyImage.Value.image);
            e.element.Children().First().Q("Image").style.backgroundImage = background;
        }
    }
}
