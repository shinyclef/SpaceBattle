using System;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationGameGroup))]
public class AiLoadSys : ComponentSystem
{
    private AiData data;
    private bool isLoaded;
    private bool reloadRequired;

    public static AiData Data { get; private set; }

    protected override void OnCreate()
    {
        isLoaded = false;
        reloadRequired = false;
        data = new AiData();
        Messenger.Global.AddListener(Msg.AnyKeyDown, OnAnyKeyDown);
    }

    protected override void OnStartRunning()
    {
        LoadAi();
    }

    protected override void OnUpdate()
    {
        if (reloadRequired)
        {
            Logger.Log("Reloading AI");
            LoadAi();
        }
    }

    private void OnAnyKeyDown()
    {
        if (GInput.GetButtonDown(Cmd.ReloadAi))
        {
            reloadRequired = true;
        }
    }

    private void LoadAi()
    {
        if (!File.Exists(Config.AiPath))
        {
            Logger.LogError($"Unable to load AI. File does not exist at: \"{Config.AiPath}\"");
            return;
        }

        UtilityAiDto dto;
        try
        {
            string json = File.ReadAllText(Config.AiPath);
            dto = JsonUtility.FromJson<UtilityAiDto>(json);
        }
        catch (Exception e)
        {
            Logger.LogError($"Unable to load AI from: \"{Config.AiPath}\". ({e.Message})");
            return;
        }

        if (isLoaded)
        {
            data.Decisions.Dispose();
            data.Choices.Dispose();
            data.ConsiderationIndecies.Dispose();
            data.Considerations.Dispose();
        }

        isLoaded = true;
        reloadRequired = false;

        // find out how many choices and consideration indecies there are
        int totalChoiceCount = 0;
        int considerationIndexCount = 0;
        for (int i = 0; i < dto.Decisions.Length; i++)
        {
            DecisionDto d = dto.Decisions[i];
            totalChoiceCount += d.Choices.Length;
            for (int j = 0; j < d.Choices.Length; j++)
            {
                considerationIndexCount += d.Choices[j].ConsiderationIndecies.Length;
            }
        }

        data.Decisions = new NativeArray<Decision>(dto.Decisions.Length, Allocator.Persistent);
        data.Choices = new NativeArray<Choice>(totalChoiceCount, Allocator.Persistent);
        data.ConsiderationIndecies = new NativeArray<ushort>(considerationIndexCount, Allocator.Persistent);

        // create the decisions
        ushort nextChoiceStartIndex = 0;
        ushort nextConsiderationStartIndex = 0;
        for (int i = 0; i < dto.Decisions.Length; i++)
        {
            DecisionDto d = dto.Decisions[i];
            data.Decisions[i] = new Decision()
            {
                DecisionType = d.DecisionType,
                ChoiceIndexStart = nextChoiceStartIndex,
                MinimumRequiredOfBest = new half(d.MinimumRequiredOfBest)
            };

            // add the choices from this decision to the choices array
            for (int j = 0; j < d.Choices.Length; j++)
            {
                ChoiceDto cd = d.Choices[j];

                // populate choices
                data.Choices[nextChoiceStartIndex + j] = new Choice
                {
                    ChoiceType = cd.ChoiceType,
                    ConsiderationIndexStart = nextConsiderationStartIndex,
                    Weight = cd.Weight,
                    MomentumFactor = cd.Momentum
                };

                // add the consideration indecies for this choice to the consideration indecies array
                for (int k = 0; k < cd.ConsiderationIndecies.Length; k++)
                {
                    data.ConsiderationIndecies[nextConsiderationStartIndex + k] = (ushort)cd.ConsiderationIndecies[k];
                }

                nextConsiderationStartIndex += (ushort)cd.ConsiderationIndecies.Length;
            }

            nextChoiceStartIndex += (ushort)d.Choices.Length;
        }

        int considerationCount = dto.Considerations.Length;
        data.Considerations = new NativeArray<Consideration>(considerationCount, Allocator.Persistent);
        for (int i = 0; i < dto.Considerations.Length; i++)
        {
            ConsiderationDto cd = dto.Considerations[i];
            data.Considerations[i] = new Consideration
            {
                FactType = cd.FactType,
                GraphType = cd.GraphType,
                Slope = cd.Slope,
                Exp = cd.Exp,
                XShift = new half(cd.XShift),
                YShift = new half(cd.YShift),
                InputMin = cd.InputMin,
                InputMax = cd.InputMax
            };
        }

        Data = data;
    }
}