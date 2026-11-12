using System.Collections.Generic;
using UnityEngine;

public class SewingButtonManager : MonoBehaviour
{
    [Header("Button Spawning")]
    [SerializeField] private Transform buttonParent;
    [SerializeField] private List<ButtonSpawnOption> buttonTypes = new List<ButtonSpawnOption>();
    [SerializeField] private float buttonDistanceThreshold = 2f;

    private readonly Dictionary<GameObject, ButtonCollector> buttons = new Dictionary<GameObject, ButtonCollector>();
    private Sewing sewing;
    private int buttonsCollected;
    private int score;

    public int ButtonsCollected => buttonsCollected;
    public int Score => score;
    public event System.Action<Vector2> DynamiteCaptured;

    public int AliveButtonCount
    {
        get
        {
            int count = 0;
            foreach (GameObject button in buttons.Keys)
            {
                if (button != null) count++;
            }
            return count;
        }
    }

    public void Initialize(Sewing sewingOwner)
    {
        sewing = sewingOwner;
        if (buttonParent == null && sewingOwner != null)
        {
            buttonParent = sewingOwner.transform;
        }
    }

    public int AliveButtonCountOf(string typeId)
    {
        int count = 0;
        foreach (KeyValuePair<GameObject, ButtonCollector> entry in buttons)
        {
            if (entry.Key == null || entry.Value == null) continue;
            if (entry.Value.ButtonTypeId == typeId) count++;
        }
        return count;
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
                buttons.Remove(button);
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

            bool dynamiteButton = collector.ButtonTypeId == "dynamite";
            if (dynamiteButton)
            {
                if (sewing != null)
                {
                    sewing.DamageNeedle(1, Vector2.zero);
                }
                DynamiteCaptured?.Invoke(button.transform.position);
            }
            else if (collector.ButtonTypeId == "heart")
            {
                if (sewing != null)
                {
                    sewing.RestoreNeedleHealth(1);
                }
            }
            else if (collector.ButtonTypeId == "strike")
            {
                // slowTime();
                //bonusPoints = true;
            }

            buttons.Remove(button);
            Destroy(button);
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

    public void SpawnRandomButton(Vector2 position)
    {
        List<ButtonSpawnOption> availableOptions = new List<ButtonSpawnOption>();
        float totalWeight = 0f;
        int aliveCount = Mathf.Max(1, AliveButtonCount);

        foreach (ButtonSpawnOption option in buttonTypes)
        {
            if (option == null || option.prefab == null || option.targetShare <= 0f) continue;

            ButtonCollector collector = option.prefab.GetComponent<ButtonCollector>();
            if (collector == null || string.IsNullOrWhiteSpace(collector.ButtonTypeId))
            {
                Debug.LogError($"Button prefab '{option.prefab.name}' needs a ButtonCollector with a type ID.", option.prefab);
                continue;
            }

            float share = AliveButtonCountOf(collector.ButtonTypeId) / (float)aliveCount;
            float weight = option.targetShare / (0.35f + share);
            if (weight <= 0f) continue;
            availableOptions.Add(option);
            totalWeight += weight;
        }

        if (availableOptions.Count == 0)
        {
            Debug.LogError("No valid button prefabs are configured on SewingButtonManager.", this);
            return;
        }

        float roll = Random.value * totalWeight;
        foreach (ButtonSpawnOption option in availableOptions)
        {
            ButtonCollector collector = option.prefab.GetComponent<ButtonCollector>();
            float share = AliveButtonCountOf(collector.ButtonTypeId) / (float)aliveCount;
            roll -= option.targetShare / (0.35f + share);
            if (roll <= 0f)
            {
                SpawnButton(position, option);
                return;
            }
        }

        SpawnButton(position, availableOptions[availableOptions.Count - 1]);
    }

    private void SpawnButton(Vector2 position, ButtonSpawnOption option)
    {
        if (option == null || option.prefab == null)
        {
            Debug.LogError("A button spawn option has no prefab assigned.", this);
            return;
        }

        Transform parent = buttonParent != null ? buttonParent : sewing != null ? sewing.transform : transform;
        GameObject newButton = Instantiate(option.prefab, position, Quaternion.identity, parent);
        ButtonCollector collector = newButton.GetComponent<ButtonCollector>();
        if (collector == null || string.IsNullOrWhiteSpace(collector.ButtonTypeId))
        {
            Debug.LogError($"Spawned button '{newButton.name}' needs a ButtonCollector with a type ID.", newButton);
            Destroy(newButton);
            return;
        }

        collector.Initialize();
        MonoBehaviour[] components = newButton.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component is IButtonSpawnInitializer initializer)
            {
                initializer.Initialize(sewing, sewing != null ? sewing.GetNeedleTransform() : null);
            }
        }

        buttons.Add(newButton, collector);
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
                buttons.Remove(button);
                continue;
            }

            if (button.transform.position.y < worldY)
            {
                buttons.Remove(button);
                Destroy(button);
            }
        }
    }

    [System.Serializable]
    public class ButtonSpawnOption
    {
        public GameObject prefab;
        [Min(0f)] public float targetShare = 0.25f;
    }
}
