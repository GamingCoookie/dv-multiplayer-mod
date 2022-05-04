﻿using DV;
using DV.CabControls;
using DVMultiplayer;
using DVMultiplayer.DTO.Train;
using DVMultiplayer.DTO.Player;
using DVMultiplayer.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using UnityEngine;
using UnityEngine.UI;

internal class NetworkTrainSync : MonoBehaviour
{
    public TrainCar loco;
    public bool listenToLocalPlayerInputs = false;
    private LocoControllerBase baseController;
    private bool isAlreadyListening = false;

    public void ListenToTrainInputEvents()
    {
        if (!loco.IsLoco || isAlreadyListening)
            return;

        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen to base loco controller");
        baseController = loco.GetComponent<LocoControllerBase>();
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen throttle change on base loco controller");
        baseController.ThrottleUpdated += OnTrainThrottleChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen brake change on base loco controller");
        baseController.BrakeUpdated += OnTrainBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen indepBrake change on base loco controller");
        baseController.IndependentBrakeUpdated += OnTrainIndependentBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen reverser change on base loco controller");
        baseController.ReverserUpdated += OnTrainReverserStateChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen sander change on base loco controller");
        baseController.SandersUpdated += OnTrainSanderChanged;

        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Listen to specific train events");
        switch (loco.carType)
        {
            case TrainCarType.LocoShunter:
                ShunterDashboardControls shunterDashboard = loco.interior.GetComponentInChildren<ShunterDashboardControls>();
                FuseBoxPowerController fuseBox = shunterDashboard.fuseBoxPowerController;
                for (int i = 0; i < fuseBox.sideFusesObj.Length; i++)
                {
                    ToggleSwitchBase sideFuse = fuseBox.sideFusesObj[i].GetComponent<ToggleSwitchBase>();
                    switch (i)
                    {
                        case 0:
                            sideFuse.ValueChanged += OnTrainSideFuse_1Changed;
                            break;

                        case 1:
                            sideFuse.ValueChanged += OnTrainSideFuse_2Changed;
                            break;
                    }
                }
                fuseBox.mainFuseObj.GetComponent<ToggleSwitchBase>().ValueChanged += OnTrainMainFuseChanged;
                shunterDashboard.hornObj.GetComponent<ControlImplBase>().ValueChanged += HornUsed;
                SingletonBehaviour<CoroutineManager>.Instance.Run(ShunterRotaryAmplitudeCheckerStartListen(fuseBox));
                break;

            case TrainCarType.LocoDiesel:
                DieselDashboardControls dieselDashboard = loco.interior.GetComponentInChildren<DieselDashboardControls>();
                Main.Log($"dashboard found {dieselDashboard != null}");
                FuseBoxPowerControllerDiesel dieselFuseBox = dieselDashboard.fuseBoxPowerControllerDiesel;
                Main.Log($"fusebox found {dieselFuseBox != null}");
                DoorsAndWindowsControllerDiesel doorsAndWindows = loco.interior.GetComponentInChildren<DoorsAndWindowsControllerDiesel>();
                Main.Log($"doorsAndWindows found {doorsAndWindows != null}");
                for (int i = 0; i < dieselFuseBox.sideFusesObj.Length; i++)
                {
                    ControlImplBase sideFuse = dieselFuseBox.sideFusesObj[i].GetComponentInChildren<ControlImplBase>();
                    Main.Log($"{sideFuse}");
                    switch (i)
                    {
                        case 0:
                            sideFuse.ValueChanged += OnTrainSideFuse_1Changed;
                            break;

                        case 1:
                            sideFuse.ValueChanged += OnTrainSideFuse_2Changed;
                            break;

                        case 2:
                            sideFuse.ValueChanged += OnTrainSideFuse_3Changed;
                            break;
                    }
                }
                foreach (ControlImplBase thing in loco.interior.GetComponentsInChildren<ControlImplBase>())
                {
                    Main.Log($"Listen change: {thing.name}");
                    switch (thing.name)
                    {
                        case "C bell button":
                            thing.ValueChanged += OnBellChanged;
                            break;
                        case "C dynamic_brake_lever":
                            thing.ValueChanged += OnDynamicBrakeChanged;
                            break;
                        case "C engine_bay_door01":
                            thing.ValueChanged += OnEngineBayDoor1Changed;
                            break;
                        case "C engine_bay_door02":
                            thing.ValueChanged += OnEngineBayDoor2Changed;
                            break;
                        case "C engine_thottle":
                            thing.ValueChanged += OnEngineThrottleChanged;
                            break;
                        case "C engine_ignition":
                            thing.ValueChanged += OnEngineIgnitionChanged;
                            break;
                        case "C fuse_panel_door":
                            thing.ValueChanged += OnFusePanelDoorChanged;
                            break;
                    }
                }
                Main.Log($"Cab light");
                dieselDashboard.cabLightRotary.GetComponentInChildren<ControlImplBase>().ValueChanged += OnCabLightChanged;
                Main.Log($"Door 1");
                doorsAndWindows.door1Lever.ValueChanged += OnDoor1Changed;
                Main.Log($"Door 2");
                doorsAndWindows.door2Lever.ValueChanged += OnDoor2Changed;
                Main.Log($"Emergency Off");
                dieselDashboard.emergencyEngineOffBtn.GetComponentInChildren<ControlImplBase>().ValueChanged += OnEmergencyOffChanged;
                Main.Log($"Fan Switch");
                dieselDashboard.fanSwitchButton.GetComponentInChildren<ControlImplBase>().ValueChanged += OnFanSwitchChanged;
                Main.Log($"Headlight");
                dieselDashboard.headlightsRotary.GetComponentInChildren<ControlImplBase>().ValueChanged += OnHeadlightSwitchChanged;
                Main.Log($"Window 1");
                doorsAndWindows.windows1Puller.ValueChanged += OnWindow1Changed;
                Main.Log($"Window 2");
                doorsAndWindows.windows2Puller.ValueChanged += OnWindow2Changed;
                Main.Log($"Window 3");
                doorsAndWindows.windows3Puller.ValueChanged += OnWindow3Changed;
                Main.Log($"Window 4");
                doorsAndWindows.windows4Puller.ValueChanged += OnWindow4Changed;
                Main.Log($"MainFuse");
                dieselFuseBox.mainFuseObj.GetComponent<ControlImplBase>().ValueChanged += OnTrainMainFuseChanged;
                Main.Log($"Horn");
                dieselDashboard.hornControl.ValueChanged += HornUsed;
                Main.Log($"Rotary Amplitude");
                SingletonBehaviour<CoroutineManager>.Instance.Run(DieselRotaryAmplitudeCheckerStartListen(dieselFuseBox));
                break;
            case TrainCarType.LocoSteamHeavy:
            case TrainCarType.LocoSteamHeavyBlue:
                RotaryBase[] valves;
                valves = loco.interior.GetComponentsInChildren<RotaryBase>();
                foreach (RotaryBase valve in valves)
                {
                    Main.Log($"Listen valve change: {valve.name}");
                    switch (valve.name)
                    {
                        case "C valve 1":
                            valve.ValueChanged += OnWaterDumpChanged;
                            break;
                        case "C valve 2":
                            valve.ValueChanged += OnSteamReleaseChanged;
                            break;
                        case "C valve 3":
                            valve.ValueChanged += OnBlowerChanged;
                            break;
                        case "C valve 4":
                            valve.ValueChanged += OnBlankValveChanged;
                            break;
                        case "C valve 5 FireOn":
                            valve.ValueChanged += OnFireOutChanged;
                            break;
                        case "C injector":
                            valve.ValueChanged += OnInjectorChanged;
                            break;
                    }
                }
                LeverBase[] levers;
                levers = loco.interior.GetComponentsInChildren<LeverBase>();
                foreach (LeverBase lever in levers)
                {
                    Main.Log($"Listen lever change: {lever.name}");
                    switch (lever.name)
                    {
                        case "C firebox handle invisible":
                            lever.ValueChanged += OnFireDoorChanged;
                            break;
                        case "C sand valve":
                            lever.ValueChanged += OnSteamSanderChanged;
                            break;
                        case "C light lever":
                            lever.ValueChanged += OnLightLeverChanged;
                            break;
                    }
                }

                ButtonBase[] buttons = loco.interior.GetComponentsInChildren<ButtonBase>();
                foreach (ButtonBase button in buttons) 
                {
                    Main.Log($"Listen button change: {button.name}");
                    switch(button.name)
                    {
                        case "C inidactor light switch":
                            button.ValueChanged += OnLightSwitchChanged;
                            break;
                    }
                }
                PullerBase draft = loco.interior.GetComponentInChildren<PullerBase>();
                Main.Log($"Listen puller change: C draft");
                draft.ValueChanged += OnDraftChanged;

                break;
        }
        isAlreadyListening = true;
    }

    public void StopListeningToTrainInputEvents()
    {
        if (!loco || !loco.IsLoco)
            return;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening throttle change on base loco controller");
        baseController.ThrottleUpdated -= OnTrainThrottleChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening brake change on base loco controller");
        baseController.BrakeUpdated -= OnTrainBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening indepBrake change on base loco controller");
        baseController.IndependentBrakeUpdated -= OnTrainIndependentBrakeChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening reverser change on base loco controller");
        baseController.ReverserUpdated -= OnTrainReverserStateChanged;
        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening sander change on base loco controller");
        baseController.SandersUpdated -= OnTrainSanderChanged;

        if (loco.logicCar != null)
            Main.Log($"[{loco.ID}] Stop listening to train specific events");
        switch (loco.carType)
        {
            case TrainCarType.LocoShunter:
                FuseBoxPowerController fuseBox = loco.interior.GetComponentInChildren<ShunterDashboardControls>().fuseBoxPowerController;
                for (int i = 0; i < fuseBox.sideFusesObj.Length; i++)
                {
                    ToggleSwitchBase sideFuse = fuseBox.sideFusesObj[i].GetComponent<ToggleSwitchBase>();
                    switch (i)
                    {
                        case 0:
                            sideFuse.ValueChanged -= OnTrainSideFuse_1Changed;
                            break;

                        case 1:
                            sideFuse.ValueChanged -= OnTrainSideFuse_2Changed;
                            break;
                    }
                }
                fuseBox.mainFuseObj.GetComponent<ToggleSwitchBase>().ValueChanged -= OnTrainMainFuseChanged;
                fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged -= OnTrainFusePowerStarterStateChanged;
                break;

            case TrainCarType.LocoDiesel:
                DieselDashboardControls dieselDashboard = loco.interior.GetComponentInChildren<DieselDashboardControls>();
                FuseBoxPowerControllerDiesel dieselFuseBox = dieselDashboard.fuseBoxPowerControllerDiesel;
                DoorsAndWindowsControllerDiesel doorsAndWindows = loco.interior.GetComponentInChildren<DoorsAndWindowsControllerDiesel>();
                for (int i = 0; i < dieselFuseBox.sideFusesObj.Length; i++)
                {
                    ToggleSwitchBase sideFuse = dieselFuseBox.sideFusesObj[i].GetComponent<ToggleSwitchBase>();
                    switch (i)
                    {
                        case 0:
                            sideFuse.ValueChanged -= OnTrainSideFuse_1Changed;
                            break;

                        case 1:
                            sideFuse.ValueChanged -= OnTrainSideFuse_2Changed;
                            break;

                        case 2:
                            sideFuse.ValueChanged -= OnTrainSideFuse_3Changed;
                            break;
                    }
                }
                foreach (ControlImplBase thing in loco.interior.GetComponentsInChildren<ControlImplBase>())
                {
                    Main.Log($"Listen change: {thing.name}");
                    switch (thing.name)
                    {
                        case "C bell button":
                            thing.ValueChanged -= OnBellChanged;
                            break;
                        case "C dynamic_brake_lever":
                            thing.ValueChanged -= OnDynamicBrakeChanged;
                            break;
                        case "C engine_bay_door01":
                            thing.ValueChanged -= OnEngineBayDoor1Changed;
                            break;
                        case "C engine_bay_door02":
                            thing.ValueChanged -= OnEngineBayDoor2Changed;
                            break;
                        case "C engine_throttle":
                            thing.ValueChanged -= OnEngineThrottleChanged;
                            break;
                        case "C engine_ignition":
                            thing.ValueChanged -= OnEngineIgnitionChanged;
                            break;
                        case "C fuse_panel_door":
                            thing.ValueChanged -= OnFusePanelDoorChanged;
                            break;
                    }
                }
                dieselDashboard.cabLightRotary.GetComponentInChildren<ControlImplBase>().ValueChanged -= OnCabLightChanged;
                doorsAndWindows.door1Lever.ValueChanged -= OnDoor1Changed;
                doorsAndWindows.door2Lever.ValueChanged -= OnDoor2Changed;
                dieselDashboard.emergencyEngineOffBtn.GetComponentInChildren<ControlImplBase>().ValueChanged -= OnEmergencyOffChanged;
                dieselDashboard.fanSwitchButton.GetComponentInChildren<ControlImplBase>().ValueChanged -= OnFanSwitchChanged;
                dieselDashboard.headlightsRotary.GetComponentInChildren<ControlImplBase>().ValueChanged -= OnHeadlightSwitchChanged;
                doorsAndWindows.windows1Puller.ValueChanged -= OnWindow1Changed;
                doorsAndWindows.windows2Puller.ValueChanged -= OnWindow2Changed;
                doorsAndWindows.windows3Puller.ValueChanged -= OnWindow3Changed;
                doorsAndWindows.windows4Puller.ValueChanged -= OnWindow4Changed;
                dieselFuseBox.mainFuseObj.GetComponent<ToggleSwitchBase>().ValueChanged -= OnTrainMainFuseChanged;
                dieselFuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged -= OnTrainFusePowerStarterStateChanged;
                break;
            case TrainCarType.LocoSteamHeavy:
            case TrainCarType.LocoSteamHeavyBlue:
                RotaryBase[] valves;
                valves = loco.interior.GetComponentsInChildren<RotaryBase>();
                foreach (RotaryBase valve in valves)
                {
                    Main.Log($"Stop listening valve change: {valve.name}");
                    switch (valve.name)
                    {
                        case "C valve 1":
                            valve.ValueChanged -= OnWaterDumpChanged;
                            break;
                        case "C valve 2":
                            valve.ValueChanged -= OnSteamReleaseChanged;
                            break;
                        case "C valve 3":
                            valve.ValueChanged -= OnBlowerChanged;
                            break;
                        case "C valve 4":
                            valve.ValueChanged -= OnBlankValveChanged;
                            break;
                        case "C valve 5 FireOn":
                            valve.ValueChanged -= OnFireOutChanged;
                            break;
                        case "C injector":
                            valve.ValueChanged -= OnInjectorChanged;
                            break;
                    }
                }
                LeverBase[] levers;
                levers = loco.interior.GetComponentsInChildren<LeverBase>();
                foreach (LeverBase lever in levers)
                {
                    Main.Log($"Stop listening lever change: {lever.name}");
                    switch (lever.name)
                    {
                        case "C firebox handle invisible":
                            lever.ValueChanged -= OnFireDoorChanged;
                            break;
                        case "C sand valve":
                            lever.ValueChanged -= OnSteamSanderChanged;
                            break;
                        case "C light lever":
                            lever.ValueChanged -= OnLightLeverChanged;
                            break;
                    }
                }

                ButtonBase[] buttons = loco.interior.GetComponentsInChildren<ButtonBase>();
                foreach (ButtonBase button in buttons)
                {
                    Main.Log($"Listen button change: {button.name}");
                    switch (button.name)
                    {
                        case "C inidactor light switch":
                            button.ValueChanged -= OnLightSwitchChanged;
                            break;
                    }
                }
                PullerBase draft = loco.interior.GetComponentInChildren<PullerBase>();
                Main.Log($"Listen puller change: C draft");
                draft.ValueChanged -= OnDraftChanged;
                break;
        }
    }

    public void Awake()
    {
        Main.Log($"NetworkTrainSync.Awake()");
        loco = GetComponent<TrainCar>();
        loco.LoadInterior();
        loco.keepInteriorLoaded = true;
        StartCoroutine(ListenToTrainInputEvents(loco));
#if DEBUG
        AddInfotagAboveTrain();
#endif
    }

    public void FixedUpdate()
    {
        //Main.Log($"{SingletonBehaviour<NetworkTrainManager>.Instance} {loco} {SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork} {listenToLocalPlayerInputs}");
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || !loco || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoValue(loco);
    }

    public void Update()
    {
        WorldTrain serverState = SingletonBehaviour<NetworkTrainManager>.Instance.serverCarStates.First(s => s.Guid == loco.CarGUID);
        //Main.Log($"{serverState.AuthorityPlayerId}");
#if DEBUG
        NetworkPlayerSync playerSync = new NetworkPlayerSync();
        if (SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync().Id == serverState.AuthorityPlayerId)
        {
            playerSync = SingletonBehaviour<NetworkPlayerManager>.Instance.GetLocalPlayerSync();
        }
        else
        {
            playerSync = SingletonBehaviour<NetworkPlayerManager>.Instance.GetPlayerSyncById(serverState.AuthorityPlayerId);
        }
        if (playerSync != null)
        {
            //Main.Log($"{playerSync.Username}");
            loco.GetComponentInChildren<Text>().text = $"Authority: {playerSync.Username}";
        }
#endif
    }

    private IEnumerator ListenToTrainInputEvents(TrainCar car)
    {
        yield return new WaitUntil(() => car.IsInteriorLoaded);
        NetworkTrainSync trainSync = car.GetComponent<NetworkTrainSync>();
        trainSync.ListenToTrainInputEvents();
        trainSync.listenToLocalPlayerInputs = true;
    }

    private IEnumerator ShunterRotaryAmplitudeCheckerStartListen(FuseBoxPowerController fuseBox)
    {
        yield return new WaitUntil(() => fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>() != null);
        fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged += OnTrainFusePowerStarterStateChanged;
    }
    private IEnumerator DieselRotaryAmplitudeCheckerStartListen(FuseBoxPowerControllerDiesel fuseBox)
    {
        yield return new WaitUntil(() => fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>() != null);
        fuseBox.powerRotaryObj.GetComponent<RotaryAmplitudeChecker>().RotaryStateChanged += OnTrainFusePowerStarterStateChanged;
    }

    private void HornUsed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        float val = e.newValue;
        if (val < .7f && val > .3f)
            val = 0;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Horn, val);
    }

    private void OnBellChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Bell, e.newValue);
    }

    private void OnDynamicBrakeChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.DynamicBrake, e.newValue);
    }

    private void OnEngineBayDoor1Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.EngineBayDoor1, e.newValue);
    }

    private void OnEngineBayDoor2Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.EngineBayDoor2, e.newValue);
    }

    private void OnEngineThrottleChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.EngineBayThrottle, e.newValue);
    }

    private void OnEngineIgnitionChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.EngineIgnition, e.newValue);
    }

    private void OnFusePanelDoorChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.FusePanelDoor, e.newValue);
    }

    private void OnCabLightChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.CabLightSwitch, e.newValue);
    }

    private void OnDoor1Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Door1, e.newValue);
    }

    private void OnDoor2Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Door2, e.newValue);
    }

    private void OnEmergencyOffChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.EmergencyOff, e.newValue);
    }

    private void OnFanSwitchChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.FanSwitch, e.newValue);
    }

    private void OnHeadlightSwitchChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.HeadlightSwitch, e.newValue);
    }

    private void OnWindow1Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Window1, e.newValue);
    }

    private void OnWindow2Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Window2, e.newValue);
    }

    private void OnWindow3Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Window3, e.newValue);
    }

    private void OnWindow4Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Window4, e.newValue);
    }

    private void OnTrainFusePowerStarterStateChanged(int state)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        float val = .5f;
        if (state == -1)
            val = 0;
        else if (state == 1)
            val = 1;
        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.FusePowerStarter, val);
    }

    private void OnTrainSideFuse_3Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.SideFuse_3, e.newValue);
    }

    private void OnTrainSideFuse_2Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.SideFuse_2, e.newValue);
    }

    private void OnTrainSideFuse_1Changed(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.SideFuse_1, e.newValue);
    }

    private void OnTrainMainFuseChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.MainFuse, e.newValue);
    }

    private void OnTrainSanderChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;
        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Sander, value);
    }

    private void OnTrainReverserStateChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Reverser, value);
    }

    private void OnTrainIndependentBrakeChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.IndependentBrake, value);
    }

    private void OnTrainBrakeChanged(float value)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Brake, value);
    }

    private void OnTrainThrottleChanged(float newThrottle)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Throttle, newThrottle);
    }

    private void OnFireDoorChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.FireDoor, e.newValue);
    }

    private void OnWaterDumpChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.WaterDump, e.newValue);
    }

    private void OnSteamReleaseChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.SteamRelease, e.newValue);
    }

    private void OnBlowerChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Blower, e.newValue);
    }

    private void OnBlankValveChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.BlankValve, e.newValue);
    }

    private void OnFireOutChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.FireOut, e.newValue);
    }

    private void OnInjectorChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Injector, e.newValue);
    }

    private void OnSteamSanderChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Sander, e.newValue);
    }

    private void OnLightLeverChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.LightLever, e.newValue);
    }

    private void OnLightSwitchChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.HeadlightSwitch, e.newValue);
    }

    private void OnDraftChanged(ValueChangedEventArgs e)
    {
        if (!SingletonBehaviour<NetworkTrainManager>.Instance || SingletonBehaviour<NetworkTrainManager>.Instance.IsChangeByNetwork || !loco || !listenToLocalPlayerInputs)
            return;

        SingletonBehaviour<NetworkTrainManager>.Instance.SendNewLocoLeverValue(loco, Levers.Draft, e.newValue);
    }
    private void AddInfotagAboveTrain()
    {
        GameObject infotagCanvas = new GameObject("Infotag canvas");
        infotagCanvas.transform.parent = loco.transform;
        infotagCanvas.transform.localPosition = new Vector3(0, 5, 0);
        infotagCanvas.AddComponent<Canvas>();
        infotagCanvas.AddComponent<RotateTowardsPlayer>();

        RectTransform rectTransform = infotagCanvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1920, 1080);
        rectTransform.localScale = new Vector3(0.004f, .001f, 0);

        GameObject infotagBackground = new GameObject("Infotag BG");
        infotagBackground.transform.parent = infotagCanvas.transform;
        infotagBackground.transform.localPosition = new Vector3(0, 0, 0);

        RawImage bg = infotagBackground.AddComponent<RawImage>();
        bg.color = new Color(69 / 255, 69 / 255, 69 / 255, .45f);

        rectTransform = infotagBackground.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(1f, 1f, 0);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);

        GameObject infotag = new GameObject("Infotag");
        infotag.transform.parent = infotagCanvas.transform;
        infotag.transform.localPosition = new Vector3(775, 0, 0);

        Text tag = infotag.AddComponent<Text>();
        tag.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        tag.fontSize = 300;
        tag.alignment = TextAnchor.MiddleCenter;
        tag.resizeTextForBestFit = true;
        tag.text = "Authority: Nobody";

        rectTransform = infotag.GetComponent<RectTransform>();
        rectTransform.localScale = new Vector3(2f, 5f, 0);
        rectTransform.anchorMin = new Vector2(0, .5f);
        rectTransform.anchorMax = new Vector2(0, .5f);
        rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, 350);
        rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -350);
        rectTransform.sizeDelta = new Vector2(1575, 350);
    }
}