using System.Collections.Generic;
using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    // Softens population balancing so rare button types are favored without overwhelming other types.
    private const float SpawnBalanceSmoothing = 0.35f;

    [Header("Button Spawning")]
    [SerializeField] private Transform buttonParent;
    [SerializeField] private List<ButtonCollector> buttonTypes = new List<ButtonCollector>();
    [SerializeField] private float buttonDistanceThreshold = 2f;

    private readonly Dictionary<GameObject, ButtonCollector> buttons = new Dictionary<GameObject, ButtonCollector>();
    private readonly Dictionary<ButtonType, int> activeButtonCountsByType = new Dictionary<ButtonType, int>();
    private NeedleManager needleManager;
    private int buttonsCollected;
    private int score;

    public int ButtonsCollected => buttonsCollected;
    public int Score => score;
    public event System.Action<Vector2> DynamiteCaptured;

    public int ActiveButtonCount
    {
        get
        {
            int count = 0;
            foreach (int typeCount in activeButtonCountsByType.Values)
            {
                count += typeCount;
            }
            return count;
        }
    }

    public void Initialize(NeedleManager needleManagerOwner)
    {
        needleManager = needleManagerOwner;
        if (buttonParent == null && needleManagerOwner != null)
        {
            buttonParent = needleManagerOwner.transform;
        }
    }

    private int GetActiveButtonCountByType(ButtonType type)
    {
        return activeButtonCountsByType.TryGetValue(type, out int count) ? count : 0;
    }

    public void ProcessButtonCollection(
        Vector2 previousNeedlePosition,
        Vector2 currentNeedlePosition,
        bool threadCrossingStarted)
    {
        if (buttons.Count == 0) return;

        List<GameObject> buttonKeys = new List<GameObject>(buttons.Keys);
        for (int i = 0; i < buttonKeys.Count; i++)
        {
            GameObject button = buttonKeys[i];
            if (button == null ||
                !buttons.TryGetValue(button, out ButtonCollector collector) ||
                collector == null)
            {
                RemoveAndDestroyButton(button);
                continue;
            }

            if (!collector.TryCollect(
                    previousNeedlePosition,
                    currentNeedlePosition,
                    threadCrossingStarted,
                    buttonDistanceThreshold))
            {
                continue;
            }

            bool dynamiteButton = collector.Type == ButtonType.Dynamite;
            if (dynamiteButton)
            {
                if (needleManager != null)
                {
                    needleManager.DamageNeedle(1, Vector2.zero);
                }
                DynamiteCaptured?.Invoke(button.transform.position);
            }
            else if (collector.Type == ButtonType.Heart)
            {
                if (needleManager != null)
                {
                    needleManager.RestoreNeedleHealth(1);
                }
            }
            else if (collector.Type == ButtonType.Strike)
            {
                // slowTime();
                //bonusPoints = true;
            }

            RemoveAndDestroyButton(button);
            if (!dynamiteButton)
            {
                OnButtonCollected(collector.ScorePoints);
            }
            break;
        }
    }

    private void OnButtonCollected(int scorePoints)
    {
        buttonsCollected++;
        score += scorePoints;
    }

    private void RemoveAndDestroyButton(GameObject button)
    {
        if (buttons.TryGetValue(button, out ButtonCollector collector))
        {
            buttons.Remove(button);
            if (collector != null)
            {
                DecrementActiveButtonCount(collector.Type);
            }
            else
            {
                RebuildActiveButtonCountsByType();
            }
        }

        // Destroy the button.
        if (button != null)
        {
            Destroy(button);
        }
    }

    private void DecrementActiveButtonCount(ButtonType type)
    {
        int typeCount = GetActiveButtonCountByType(type) - 1;
        if (typeCount <= 0)
        {
            activeButtonCountsByType.Remove(type);
        }
        else
        {
            activeButtonCountsByType[type] = typeCount;
        }
    }

    private void RebuildActiveButtonCountsByType()
    {
        activeButtonCountsByType.Clear();
        foreach (KeyValuePair<GameObject, ButtonCollector> entry in buttons)
        {
            if (entry.Key == null || entry.Value == null) continue;

            ButtonType type = entry.Value.Type;
            activeButtonCountsByType[type] = GetActiveButtonCountByType(type) + 1;
        }
    }

    public void SpawnRandomButton(Vector2 position)
    {
        ButtonCollector selectedButtonType = PickRandomButton();
        if (selectedButtonType != null)
        {
            SpawnButton(position, selectedButtonType);
        }
    }

    private ButtonCollector PickRandomButton()
    {
        float totalWeight = 0f;
        int activeCount = Mathf.Max(1, ActiveButtonCount);
        ButtonCollector selectedButtonType = null;

        // Favor underrepresented types while maintaining a weighted random choice.
        foreach (ButtonCollector buttonType in buttonTypes)
        {
            if (buttonType == null || buttonType.SpawnWeight <= 0f) continue;
            if (buttonType.Type == ButtonType.None)
            {
                Debug.LogError($"Button prefab '{buttonType.name}' needs a valid button type.", buttonType);
                continue;
            }

            // Calculate this type's share of the currently active buttons.
            float share = GetActiveButtonCountByType(buttonType.Type) / (float)activeCount;
            float weight = buttonType.SpawnWeight / (SpawnBalanceSmoothing + share);
            if (weight <= 0f) continue;

            totalWeight += weight;
            // Replace the current choice with probability proportional to this type's weight.
            if (Random.value * totalWeight < weight)
            {
                selectedButtonType = buttonType;
            }
        }

        if (selectedButtonType == null)
        {
            Debug.LogError("No valid button prefabs are configured on ButtonManager.", this);
            return null;
        }

        return selectedButtonType;
    }

    private void SpawnButton(Vector2 position, ButtonCollector selectedButtonType)
    {
        // Instantiate the selected prefab and initialize its collector and spawn-specific behaviors.
        Transform parent = buttonParent != null ? buttonParent : needleManager != null ? needleManager.transform : transform;
        GameObject newButton = Instantiate(selectedButtonType.gameObject, position, Quaternion.identity, parent);
        ButtonCollector collector = newButton.GetComponent<ButtonCollector>();
        if (collector == null || collector.Type == ButtonType.None)
        {
            Debug.LogError($"Spawned button '{newButton.name}' needs a valid ButtonCollector type.", newButton);
            Destroy(newButton);
            return;
        }

        collector.Initialize();
        MonoBehaviour[] components = newButton.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component is IButtonSpawnInitializer initializer)
            {
                initializer.Initialize(needleManager, needleManager != null ? needleManager.GetNeedleTransform() : null);
            }
        }

        buttons.Add(newButton, collector);
        activeButtonCountsByType[collector.Type] = GetActiveButtonCountByType(collector.Type) + 1;
    }

    public void DespawnButtonsBelow(float worldY)
    {
        if (buttons == null || buttons.Count == 0) return;

        List<GameObject> buttonKeys = new List<GameObject>(buttons.Keys);
        for (int i = 0; i < buttonKeys.Count; i++)
        {
            GameObject button = buttonKeys[i];
            if (button == null)
            {
                RemoveAndDestroyButton(button);
                continue;
            }

            if (button.transform.position.y < worldY)
            {
                RemoveAndDestroyButton(button);
            }
        }
    }

}
