using System;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(InitializationGameGroup))]
public class AiDataSys : ComponentSystem
{
    private UtilityAiDto data;
    private AiData nativeData;
    private bool isLoaded;
    private bool reloadRequired;

    public static UtilityAiDto Data { get; private set; }

    public static AiData NativeData { get; private set; }

    protected override void OnCreate()
    {
        isLoaded = false;
        reloadRequired = false;
        nativeData = new AiData();
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

        try
        {
            string json = File.ReadAllText(Config.AiPath);
            data = JsonUtility.FromJson<UtilityAiDto>(json);
        }
        catch (Exception e)
        {
            Logger.LogError($"Unable to load AI from: \"{Config.AiPath}\". ({e.Message})");
            return;
        }

        if (isLoaded)
        {
            nativeData.Decisions.Dispose();
            nativeData.Choices.Dispose();
            nativeData.Considerations.Dispose();
            nativeData.RecordedScores.Dispose();
        }

        isLoaded = true;
        reloadRequired = false;

        // find out how many choices and considerations there are
        int totalChoiceCount = 0;
        int considerationCount = 0;
        for (int i = 0; i < data.Decisions.Length; i++)
        {
            DecisionDto d = data.Decisions[i];
            totalChoiceCount += d.Choices.Length;
            for (int j = 0; j < d.Choices.Length; j++)
            {
                considerationCount += d.Choices[j].Considerations.Length;
            }
        }

        nativeData.Decisions = new NativeArray<Decision>(data.Decisions.Length, Allocator.Persistent);
        nativeData.Choices = new NativeArray<Choice>(totalChoiceCount, Allocator.Persistent);
        nativeData.Considerations = new NativeArray<Consideration>(considerationCount, Allocator.Persistent);

        // create the decisions
        ushort nextChoiceStartIndex = 0;
        ushort nextConsiderationStartIndex = 0;
        int maxScoresToRecord = 0;
        int scoresToRecord = 0;
        for (int i = 0; i < data.Decisions.Length; i++)
        {
            DecisionDto d = data.Decisions[i];
            nativeData.Decisions[i] = d.ToDecision(nextChoiceStartIndex);

            // add the choices from this decision to the choices array
            for (int j = 0; j < d.Choices.Length; j++)
            {
                scoresToRecord++;
                ChoiceDto cd = d.Choices[j];

                // populate choices
                nativeData.Choices[nextChoiceStartIndex + j] = cd.ToChoice(nextConsiderationStartIndex);

                // add the considerations for this choice to the considerations array
                for (int k = 0; k < cd.Considerations.Length; k++)
                {
                    scoresToRecord += 2; // one for the score, one for the input value
                    ConsiderationDto con = cd.Considerations[k];
                    nativeData.Considerations[nextConsiderationStartIndex + k] = con.ToConsideration();
                }

                nextConsiderationStartIndex += (ushort)cd.Considerations.Length;
            }

            nextChoiceStartIndex += (ushort)d.Choices.Length;
            maxScoresToRecord = math.max(scoresToRecord, maxScoresToRecord);
        }

        nativeData.RecordedScores = new NativeArray<float>(maxScoresToRecord, Allocator.Persistent);
        Data = data;
        NativeData = nativeData;
        Messenger.Global.Post(Msg.AiLoaded);
    }
}