using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ShipCountsUi : MonoBehaviour
{
    [SerializeField] private ShipSpawnerComp[] MonoComponents = default;

    [SerializeField] private NumberInputField[] spawnRateInputs = default;
    [SerializeField] private NumberInputField[] maxShipsInputs = default;
    [SerializeField] private TextMeshProUGUI[] activeShipsTexts = default;

    [SerializeField] private TextMeshProUGUI spawnRateTotalText = default;
    [SerializeField] private TextMeshProUGUI maxShipsTotalText = default;
    [SerializeField] private TextMeshProUGUI activeShipsTotalText = default;

    private int[] activeShipsVals;
    private ShipSpawnerSpawnSys spawnerSys;
    private EntityQuery spawnerQuery;

    private void Awake()
    {
        activeShipsVals = new int[3];
        spawnerSys = World.Active.GetExistingSystem<ShipSpawnerSpawnSys>();
        spawnerQuery = World.Active.EntityManager.CreateEntityQuery(typeof(Faction), typeof(ShipSpawner));
    }

    private void Start()
    {
        SetupInputValues();
    }

    private void SetupInputValues()
    {
        spawnRateInputs[0].SetValueWithoutNotify(MonoComponents[0].SpawnRatePerSecond);
        spawnRateInputs[1].SetValueWithoutNotify(MonoComponents[1].SpawnRatePerSecond);
        spawnRateInputs[2].SetValueWithoutNotify(MonoComponents[2].SpawnRatePerSecond);
        UpdateSpawnRateTotal();

        maxShipsInputs[0].SetValueWithoutNotify(MonoComponents[0].MaxShips);
        maxShipsInputs[1].SetValueWithoutNotify(MonoComponents[1].MaxShips);
        maxShipsInputs[2].SetValueWithoutNotify(MonoComponents[2].MaxShips);
        UpdateMaxShipsTotal();
    }

    private void LateUpdate()
    {
        UpdateActiveShips();
    }

    #region events

    public void OnSpawnRateGreenInputChangedValid(NumberInputField numberField) => OnSpawnRateInputChanged(1, (int)numberField.GetValue());

    public void OnSpawnRateRedInputChangedValid(NumberInputField numberField) => OnSpawnRateInputChanged(2, (int)numberField.GetValue());

    public void OnSpawnRateBlueInputChangedValid(NumberInputField numberField) => OnSpawnRateInputChanged(3, (int)numberField.GetValue());

    public void OnMaxShipsGreenInputChangedValid(NumberInputField numberField) => OnMaxShipsInputChanged(1, (int)numberField.GetValue());

    public void OnMaxShipsRedInputChangedValid(NumberInputField numberField) => OnMaxShipsInputChanged(2, (int)numberField.GetValue());

    public void OnMaxShipsBlueInputChangedValid(NumberInputField numberField) => OnMaxShipsInputChanged(3, (int)numberField.GetValue());

    public void OnSpawnRateInputChanged(int faction, int value)
    {
        var em = World.Active.EntityManager;
        NativeArray <Entity> entities = spawnerQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; i++)
        {
            Faction f = em.GetComponentData<Faction>(entities[i]);
            if ((int)f.Value == faction)
            {
                ShipSpawner s = em.GetComponentData<ShipSpawner>(entities[i]);
                s.SpawnRatePerSecond = value;
                em.SetComponentData(entities[i], s);
                break;
            }
        }

        entities.Dispose();
        UpdateSpawnRateTotal();
    }

    public void OnMaxShipsInputChanged(int faction, int value)
    {
        var em = World.Active.EntityManager;
        NativeArray<Entity> entities = spawnerQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < entities.Length; i++)
        {
            Faction f = em.GetComponentData<Faction>(entities[i]);
            if ((int)f.Value == faction)
            {
                ShipSpawner s = em.GetComponentData<ShipSpawner>(entities[i]);
                s.MaxShips = value;
                em.SetComponentData(entities[i], s);
                break;
            }
        }

        entities.Dispose();
        UpdateMaxShipsTotal();
    }

    #endregion

    private void UpdateActiveShips()
    {
        var counts = spawnerSys.ActiveShipCounts;
        SetActiveShips(1, counts[1]);
        SetActiveShips(2, counts[2]);
        SetActiveShips(3, counts[3]);
        UpdateActiveShipsTotal();
    }

    private void SetActiveShips(int faction, int val)
    {
        activeShipsVals[faction - 1] = val;
        activeShipsTexts[faction - 1].text = val.ToString();
    }

    private void UpdateSpawnRateTotal()
    {
        int total = (int)(spawnRateInputs[0].GetValue() + spawnRateInputs[1].GetValue() + spawnRateInputs[2].GetValue());
        spawnRateTotalText.text = total.ToString();
    }

    private void UpdateMaxShipsTotal()
    {
        int total = (int)(maxShipsInputs[0].GetValue() + maxShipsInputs[1].GetValue() + maxShipsInputs[2].GetValue());
        maxShipsTotalText.text = total.ToString();
    }

    private void UpdateActiveShipsTotal()
    {
        int total = activeShipsVals[0] + activeShipsVals[1] + activeShipsVals[2];
        activeShipsTotalText.text = total.ToString();
    }
}