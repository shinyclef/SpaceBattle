using System;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationGameGroup))]
public class AiDataSys : ComponentSystem
{
    private UtilityAiDto savedData;
    private AiData nativeData;
    private bool isLoaded;

    private bool reloadFromDiskRequired;
    private bool revertRequired;
    private bool updateRequired;

    public static AiDataSys I { get; private set; }
    public static UtilityAiDto Data { get; private set; }
    public static AiData NativeData { get; private set; }
    public bool DataIsDirty => !Data.Equals(savedData);

    public void SaveAiData()
    {
        if (!DataIsDirty)
        {
            return;
        }

        try
        {
            savedData = Data.Clone();
            string json = JsonUtility.ToJson(savedData, true);
            File.WriteAllText(Config.AiPath, json);
        }
        catch (Exception e)
        {
            Logger.LogError(string.Format(Localizer.Strings.Error.AiErrorSaving, Config.AiPath, e.Message));
            return;
        }
    }

    public void RevertAiData()
    {
        revertRequired = true;
    }

    public void ReloadAiData()
    {
        reloadFromDiskRequired = true;
    }

    public void UpdateAiData()
    {
        updateRequired = true;
    }

    protected override void OnCreate()
    {
        I = this;
        isLoaded = false;
        reloadFromDiskRequired = false;
        nativeData = new AiData();
    }

    protected override void OnStartRunning()
    {
        LoadAiFromDisk();
    }

    protected override void OnDestroy()
    {
        if (isLoaded)
        {
            nativeData.Decisions.Dispose();
            nativeData.Choices.Dispose();
            nativeData.Considerations.Dispose();
            nativeData.RecordedScores.Dispose();
        }
    }

    protected override void OnUpdate()
    {
        if (reloadFromDiskRequired)
        {
            updateRequired = false;
            LoadAiFromDisk();
        }
        else if (revertRequired)
        {
            updateRequired = false;
            RevertAi();
        }
        else if (updateRequired)
        {
            UpdateAi();
        }

        updateRequired = false;
        revertRequired = false;
        reloadFromDiskRequired = false;
    }

    private void LoadAiFromDisk()
    {
        if (!File.Exists(Config.AiPath))
        {
            Logger.LogError(string.Format(Localizer.Strings.Error.AiFileNotExists, Config.AiPath));
            return;
        }

        try
        {
            string json = File.ReadAllText(Config.AiPath);
            savedData = JsonUtility.FromJson<UtilityAiDto>(json);
        }
        catch (Exception e)
        {
            Logger.LogError(string.Format(Localizer.Strings.Error.AiErrorLoading, Config.AiPath, e.Message));
            return;
        }

        Data = savedData.Clone();
        GenerateNativeArraysFromDto();
        Messenger.Global.Post(Msg.AiLoadedFromDisk);
    }

    private void RevertAi()
    {
        Data.CopyValuesFrom(savedData);
        GenerateNativeArraysFromDto();
        Messenger.Global.Post(Msg.AiRevertedUnsavedChanges);
    }

    private void UpdateAi()
    {
        GenerateNativeArraysFromDto();
    }

    private void GenerateNativeArraysFromDto()
    {
        if (isLoaded)
        {
            nativeData.Decisions.Dispose();
            nativeData.Choices.Dispose();
            nativeData.Considerations.Dispose();
            nativeData.RecordedScores.Dispose();
        }

        isLoaded = true;

        // find out how many choices and considerations there are
        int totalChoiceCount = 0;
        int considerationCount = 0;
        for (int i = 0; i < Data.Decisions.Length; i++)
        {
            DecisionDto d = Data.Decisions[i];
            totalChoiceCount += d.Choices.Length;
            for (int j = 0; j < d.Choices.Length; j++)
            {
                considerationCount += d.Choices[j].Considerations.Length;
            }
        }

        nativeData.Decisions = new NativeArray<Decision>(Data.Decisions.Length, Allocator.Persistent);
        nativeData.Choices = new NativeArray<Choice>(totalChoiceCount, Allocator.Persistent);
        nativeData.Considerations = new NativeArray<Consideration>(considerationCount, Allocator.Persistent);

        // create the decisions
        ushort nextChoiceStartIndex = 0;
        ushort nextConsiderationStartIndex = 0;
        int maxScoresToRecord = 0;
        int scoresToRecord = 0;
        for (int i = 0; i < Data.Decisions.Length; i++)
        {
            DecisionDto d = Data.Decisions[i];
            nativeData.Decisions[i] = d.ToDecision(nextChoiceStartIndex);

            // add the choices from this decision to the choices array
            for (int j = 0; j < d.Choices.Length; j++)
            {
                ChoiceDto cd = d.Choices[j];
                scoresToRecord += cd.TargetCount;

                // populate choices
                nativeData.Choices[nextChoiceStartIndex + j] = cd.ToChoice(nextConsiderationStartIndex);

                // add the considerations for this choice to the considerations array
                for (int k = 0; k < cd.Considerations.Length; k++)
                {
                    scoresToRecord += cd.TargetCount * 2; // one for the score, one for the input value
                    ConsiderationDto con = cd.Considerations[k];
                    nativeData.Considerations[nextConsiderationStartIndex + k] = con.ToConsideration();
                }

                nextConsiderationStartIndex += (ushort)cd.Considerations.Length;
            }

            nextChoiceStartIndex += (ushort)d.Choices.Length;
            maxScoresToRecord = math.max(scoresToRecord, maxScoresToRecord);
        }

        nativeData.RecordedScores = new NativeHashMap<int, float>(maxScoresToRecord, Allocator.Persistent);
        NativeData = nativeData;
        Messenger.Global.Post(Msg.AiNativeArrayssGenerated);
    }
}