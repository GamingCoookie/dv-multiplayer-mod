﻿using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV;
using DV.ServicePenalty;
using DV.TerrainSystem;
using DVMultiplayer;
using DVMultiplayer.DTO.Savegame;
using DVMultiplayer.Networking;
using DVMultiplayer.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;

internal class NetworkSaveGameManager : SingletonBehaviour<NetworkSaveGameManager>
{
    private OfflineSaveGame offlineSave;
    public bool IsHostSaveReceived { get; private set; }
    public bool IsHostSaveLoadedFailed { get; internal set; }
    public bool IsHostSaveLoaded { get; private set; }
    public bool IsOfflineSaveLoaded { get; private set; }

    protected override void Awake()
    {
        base.Awake();
    }

    public void PlayerDisconnect()
    {
        IsOfflineSaveLoaded = false;
        if(offlineSave != null)
        {
            Main.Log("Reading offline save into SaveGameManager");
            SaveGameManager.data.SetJObject(SaveGameKeys.Cars, JObject.Parse(offlineSave.SaveDataCars));
            SaveGameManager.data.SetObject(SaveGameKeys.Jobs, offlineSave.SaveDataJobs, JobSaveManager.serializeSettings);
            SaveGameManager.data.SetJObject(SaveGameKeys.Junctions, JObject.Parse(offlineSave.SaveDataSwitches));
            SaveGameManager.data.SetJObject(SaveGameKeys.Turntables, JObject.Parse(offlineSave.SaveDataTurntables));
            SaveGameManager.data.SetJObject("Debt_deleted_locos", JObject.Parse(offlineSave.SaveDataDestroyedLocoDebt));
            SaveGameManager.data.SetJObject("Debt_staged_jobs", JObject.Parse(offlineSave.SaveDataStagedJobDebt));
            SaveGameManager.data.SetJObject("Debt_jobless_cars", JObject.Parse(offlineSave.SaveDataDeletedJoblessCarsDebt));
            SaveGameManager.data.SetJObject("Debt_insurance", JObject.Parse(offlineSave.SaveDataInsuranceDebt));
            SaveGameManager.data.SetVector3("Player_position", offlineSave.SaveDataPosition);
            offlineSave = null;
            SaveGameUpgrader.Upgrade();
            SingletonBehaviour<CoroutineManager>.Instance.Run(LoadOfflineSave());
        }
        else if (NetworkManager.IsHost())
        {
            CarSpawner.useCarPooling = true;
            SingletonBehaviour<SaveGameManager>.Instance.disableAutosave = false;
            IsOfflineSaveLoaded = true;
        }
    }

    private IEnumerator LoadOfflineSave()
    {
        Main.Log("Loading offline save");
        TutorialController.movementAllowed = false;
        Vector3 vector3_1 = SaveGameManager.data.GetVector3("Player_position").Value;
        //SingletonBehaviour<WorldMover>.Instance.movingEnabled = true;
        AppUtil.Instance.UnpauseGame();
        yield return new WaitUntil(() => !AppUtil.IsPaused);
        yield return new WaitForEndOfFrame();
        PlayerManager.TeleportPlayer(vector3_1 + WorldMover.currentMove, PlayerManager.PlayerTransform.rotation, null, false);
        UUI.UnlockMouse(true);
        yield return new WaitUntil(() => SingletonBehaviour<TerrainGrid>.Instance.IsInLoadedRegion(PlayerManager.PlayerTransform.position));
        SingletonBehaviour<CarsSaveManager>.Instance.DeleteAllExistingCars();
        yield return new WaitForSecondsRealtime(1);
        CustomUI.OpenPopup("Disconnecting", "Loading offline save");
        yield return new WaitUntil(() => CustomUI.currentScreen == CustomUI.PopupUI);
        AppUtil.Instance.PauseGame();
        yield return new WaitUntil(() => AppUtil.IsPaused);
        CarSpawner.useCarPooling = true;
        bool carsLoadedSuccessfully = false;
        ResetDebts();
        JObject jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Turntables);
        if (jObject != null)
        {
            Main.Log("Loading Turntables");
            TurntableRailTrack.SetSaveData(jObject);
        }
        else
        {
            Main.Log("[WARNING] Turntables data not found!");
        }
        jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Junctions);
        if (jObject != null)
        {
            Main.Log("Loading Junctions");
            JunctionsSaveManager.Load(jObject);
        }
        else
        {
            Main.Log("[WARNING] Junctions save not found!");
        }

        jObject = SaveGameManager.data.GetJObject(SaveGameKeys.Cars);
        if (jObject != null)
        {
            Main.Log("Loading Cars");
            carsLoadedSuccessfully = SingletonBehaviour<CarsSaveManager>.Instance.Load(jObject);
            if (!carsLoadedSuccessfully)
                Main.Log("[WARNING] Cars not loaded successfully!");
        }
        else
            Main.Log("[WARNING] Cars save not found!");

        if (carsLoadedSuccessfully)
        {
            JobsSaveGameData saveData = SaveGameManager.data.GetObject<JobsSaveGameData>(SaveGameKeys.Jobs, JobSaveManager.serializeSettings);
            if (saveData != null)
            {
                Main.Log("Loading Jobs");
                SingletonBehaviour<JobSaveManager>.Instance.LoadJobSaveGameData(saveData);
            }
            else
                Main.Log("[WARNING] Jobs save not found!");
            SingletonBehaviour<JobSaveManager>.Instance.MarkAllNonJobCarsAsUnused();
        }

        jObject = SaveGameManager.data.GetJObject("Debt_deleted_locos");
        if (jObject != null)
        {
            SingletonBehaviour<LocoDebtController>.Instance.LoadDestroyedLocosDebtsSaveData(jObject);
            Main.Log("Loaded destroyed locos debt");
        }
        jObject = SaveGameManager.data.GetJObject("Debt_staged_jobs");
        if (jObject != null)
        {
            SingletonBehaviour<JobDebtController>.Instance.LoadStagedJobsDebtsSaveData(jObject);
            Main.Log("Loaded staged jobs debt");
        }
        jObject = SaveGameManager.data.GetJObject("Debt_jobless_cars");
        if (jObject != null)
        {
            SingletonBehaviour<JobDebtController>.Instance.LoadDeletedJoblessCarDebtsSaveData(jObject);
            Main.Log("Loaded jobless cars debt");
        }
        jObject = SaveGameManager.data.GetJObject("Debt_insurance");
        if (jObject != null)
        {
            SingletonBehaviour<CareerManagerDebtController>.Instance.feeQuota.LoadSaveData(jObject);
            Main.Log("Loaded insurance fee data");
        }
        
        UUI.UnlockMouse(false);
        CustomUI.Close();
        yield return new WaitUntil(() => !CustomUI.currentScreen);
        TutorialController.movementAllowed = true;
        AppUtil.Instance.UnpauseGame();
        SingletonBehaviour<SaveGameManager>.Instance.disableAutosave = false;
        IsOfflineSaveLoaded = true;
    }

    internal void CreateOfflineBackup()
    {
        offlineSave = new OfflineSaveGame()
        {
            SaveDataCars = SaveGameManager.data.GetJObject(SaveGameKeys.Cars).ToString(Formatting.None),
            SaveDataJobs = SaveGameManager.data.GetObject<JobsSaveGameData>(SaveGameKeys.Jobs, JobSaveManager.serializeSettings),
            SaveDataSwitches = SaveGameManager.data.GetJObject(SaveGameKeys.Junctions).ToString(Formatting.None),
            SaveDataTurntables = SaveGameManager.data.GetJObject(SaveGameKeys.Turntables).ToString(Formatting.None),
            SaveDataDestroyedLocoDebt = SaveGameManager.data.GetJObject("Debt_deleted_locos").ToString(Formatting.None),
            SaveDataStagedJobDebt = SaveGameManager.data.GetJObject("Debt_staged_jobs").ToString(Formatting.None),
            SaveDataDeletedJoblessCarsDebt = SaveGameManager.data.GetJObject("Debt_jobless_cars").ToString(Formatting.None),
            SaveDataInsuranceDebt = SaveGameManager.data.GetJObject("Debt_insurance").ToString(Formatting.None),
            SaveDataPosition = PlayerManager.PlayerTransform.position - WorldMover.currentMove
        };
        Main.Log($"Offline save exists now? {offlineSave != null}");
    }

    public void ResetDebts()
    {
        if (SingletonBehaviour<NetworkDebtManager>.Exists)
            SingletonBehaviour<NetworkDebtManager>.Instance.IsChangeByNetwork = true;
        Main.Log("Clearing Loco Debts");
        SingletonBehaviour<LocoDebtController>.Instance.ClearLocoDebts();
        Main.Log("Clearing Job Debts");
        SingletonBehaviour<JobDebtController>.Instance.ClearJobDebts();
        Main.Log("Clearing Debts Via Insurance Quota Reached");
        SingletonBehaviour<CareerManagerDebtController>.Instance.ClearDebtsViaInsuranceQuotaReached();
        Main.Log("Clearing Rest of the Payable Debts");
        SingletonBehaviour<CareerManagerDebtController>.Instance.ClearRestOfThePayableDebts();
        Main.Log("Resetting fee quota");
        SingletonBehaviour<CareerManagerDebtController>.Instance.feeQuota.Quota = LicenseManager.InsuranceFeeQuota;
        Main.Log("Registering insurance fee quota updating");
        SingletonBehaviour<CareerManagerDebtController>.Instance.RegisterInsuranceFeeQuotaUpdating();
        Main.Log("Clearing paid quota");
        SingletonBehaviour<CareerManagerDebtController>.Instance.feeQuota.ClearPaidQuota();
        if (SingletonBehaviour<NetworkDebtManager>.Exists)
            SingletonBehaviour<NetworkDebtManager>.Instance.IsChangeByNetwork = false;
    }
}