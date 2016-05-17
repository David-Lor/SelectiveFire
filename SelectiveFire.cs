using GTA;
using GTA.Native;
using System;
using System.Windows.Forms;
using System.Drawing; //print external images on game

public class SelectiveFire : Script
{
    //LOCAL VARIABLES for the whole script
        private int fireMode = 1, //1= full auto, 2= semi-auto, 3=burst (3 shots)
        ammo, /*prevammo,*/ ammocount;
        private bool capableWeapon = false, firemodechanged = false, showImg = false, stealth, stealthLaunchIfPlayerAiming = false, playerreloaded = false, breathshaking = false, aimshaking = false;
        private string firemodeImgRoot = AppDomain.CurrentDomain.BaseDirectory + "\\SelectiveFire\\", firemodeImg;
        private Weapon previousWeapon;
        private Ped player;
        private DateTime imgTimer;

    //VARIABLES FROM INI CONFIG FILE
        private ScriptSettings config;
        private Keys ChangeFireModeHotkey_Keys;
        private bool ActivateSelectiveFire, ShowImage, AutoHideImage, StealthIfPlayerAiming, StealthAutoDisable, WasteAmmo, ShowNotif, BreathAimMovement, ShakeWhenAim;
        private int ImageShownTime, ImageWidth, ImageHeight, BreathMovementRate, ShakeWhenAimMovementRate;

    public SelectiveFire()
    {
        Tick += OnTick;
        KeyUp += OnKeyUp;
        Interval = 10;

        //Pick up configs from INI file
            config = ScriptSettings.Load("scripts\\SelectiveFire.ini");
            string ChangeFireModeHotkey_String = config.GetValue<string>("HOTKEYS", "ChangeFireMode", "N");
            string StealthModeHotkey_String = config.GetValue<string>("HOTKEYS", "StealthMode", "P");
            ActivateSelectiveFire = config.GetValue<bool>("SELECTIVEFIRE", "SelectiveFire", true);
            ShowImage = config.GetValue<bool>("NOTIFICATIONS", "ShowImage", true);
            AutoHideImage = config.GetValue<bool>("NOTIFICATIONS", "AutoHideImage", true);
            ImageShownTime = config.GetValue<int>("NOTIFICATIONS", "ImageShownTime", 3);
            ShowNotif = config.GetValue<bool>("NOTIFICATIONS", "ShowNotif", true);
            ImageWidth = config.GetValue<int>("NOTIFICATIONS", "ImageWidth", 136);
            ImageHeight = config.GetValue<int>("NOTIFICATIONS", "ImageHeight", 27);
            StealthIfPlayerAiming = config.GetValue<bool>("STEALTHMODE", "StealthIfPlayerAiming", false);
            StealthAutoDisable = config.GetValue<bool>("STEALTHMODE", "StealthAutoDisable", true);
            if (ShowNotif)  {UI.Notify("SelectiveFire loaded."); }
            WasteAmmo = config.GetValue<bool>("REALLISTICMAGS", "WasteAmmo", false);
            BreathAimMovement = config.GetValue<bool>("REALLISTICAIMING", "BreathAimMovement", false);
            BreathMovementRate = config.GetValue<int>("REALLISTICAIMING", "BreathMovementRate", 1);
            ShakeWhenAim = config.GetValue<bool>("REALLISTICAIMING", "ShakeWhenAim", false);
            ShakeWhenAimMovementRate = config.GetValue<int>("REALLISTICAIMING", "ShakeWhenAimMovementRate", 1);


        //Hotkeys (String from INI to Enum-Keys)
            Enum.TryParse(ChangeFireModeHotkey_String, out ChangeFireModeHotkey_Keys);

    }

    private void OnTick(object sender, EventArgs e)
    {
        player = Game.Player.Character;
        Weapon actualWeapon = player.Weapons.Current;

        if (ActivateSelectiveFire && (actualWeapon.Hash == GTA.Native.WeaponHash.AdvancedRifle || actualWeapon.Hash == GTA.Native.WeaponHash.APPistol || actualWeapon.Hash == GTA.Native.WeaponHash.AssaultRifle || actualWeapon.Hash == GTA.Native.WeaponHash.AssaultSMG || actualWeapon.Hash == GTA.Native.WeaponHash.BullpupRifle || actualWeapon.Hash == GTA.Native.WeaponHash.CarbineRifle || actualWeapon.Hash == GTA.Native.WeaponHash.CombatPDW || actualWeapon.Hash == GTA.Native.WeaponHash.Gusenberg || actualWeapon.Hash == GTA.Native.WeaponHash.MachinePistol || actualWeapon.Hash == GTA.Native.WeaponHash.MicroSMG || actualWeapon.Hash == GTA.Native.WeaponHash.SMG || actualWeapon.Hash == GTA.Native.WeaponHash.SpecialCarbine))
        { //just run if using an automatic weapon (from the upper "if" list)
            ammo = Game.Player.Character.Weapons.Current.Ammo;
            capableWeapon = true;

            //LAUNCH Selective Fire Modes
                if (fireMode == 1) {
                    firemodeImg = firemodeImgRoot + "fullauto.png";
                }
                else if (fireMode == 2) {
                    firemodeImg = firemodeImgRoot + "semiauto.png";
                    SemiAutoMode();
                }
                else if (fireMode == 3) {
                    firemodeImg = firemodeImgRoot + "burst3.png";
                    BurstMode();
                }

            //Draw on screen a image showing the actual fire mode for X seconds, if fire mode changed
                if (ShowImage && AutoHideImage) { //if show image and autohide it
                    if (actualWeapon != previousWeapon) { //"fire mode changed" if weapon changed, too
                        firemodechanged = true;
                    }
                    if (firemodechanged) { //launch the startup timer
                        imgTimer = DateTime.Now; //take startup time
                        firemodechanged = false;
                        showImg = true;
                    }
                    if ((DateTime.Now - imgTimer).TotalSeconds > ImageShownTime) { //if X seconds passed, stop showing img
                        showImg = false;
                    }
                    if (showImg) {
                        UI.DrawTexture(firemodeImg, 1, 1, 100, new Point(1, 1), new Size(ImageWidth, ImageHeight));
                    }
                }
                if (ShowImage && !AutoHideImage) { //if always show the image (no autohide)
                    UI.DrawTexture(firemodeImg, 1, 1, 100, new Point(1, 1), new Size(ImageWidth, ImageHeight));
                }

            //Restart ammocount when player is reloading or has changed weapon (avoiding firing after that)
                if (player.IsReloading || actualWeapon != previousWeapon) {
                    Game.Player.DisableFiringThisFrame();
                    ammocount = 0;
                }
        }
        //END OF "IF USING AUTO WEAPON"
        else { //if not using an automatic weapon from the list
            capableWeapon = false;
        }

        //STEALTH MODULE
        if (StealthIfPlayerAiming) {
            stealth = GTA.Native.Function.Call<bool>(GTA.Native.Hash.GET_PED_STEALTH_MOVEMENT, player);
            if (StealthIfPlayerAiming && !stealthLaunchIfPlayerAiming && Game.Player.IsAiming) {
                StealthMode(true);
                stealthLaunchIfPlayerAiming = true; //just launch stealth order once
            }
            else if (stealthLaunchIfPlayerAiming && !Game.Player.IsAiming) {
                stealthLaunchIfPlayerAiming = false;
                if (StealthAutoDisable && stealth) { //auto-disable stealth option
                    StealthMode(false);
                }
            }
        }

        //REALLISTIC MAGS MODULE
        if (WasteAmmo && (actualWeapon.Hash == GTA.Native.WeaponHash.AdvancedRifle || actualWeapon.Hash == GTA.Native.WeaponHash.APPistol || actualWeapon.Hash == GTA.Native.WeaponHash.AssaultRifle || actualWeapon.Hash == GTA.Native.WeaponHash.AssaultSMG || actualWeapon.Hash == GTA.Native.WeaponHash.BullpupRifle || actualWeapon.Hash == GTA.Native.WeaponHash.CarbineRifle || actualWeapon.Hash == GTA.Native.WeaponHash.CombatMG || actualWeapon.Hash == GTA.Native.WeaponHash.CombatPDW || actualWeapon.Hash == GTA.Native.WeaponHash.CombatPistol || actualWeapon.Hash == GTA.Native.WeaponHash.CompactRifle || actualWeapon.Hash == GTA.Native.WeaponHash.GrenadeLauncher || actualWeapon.Hash == GTA.Native.WeaponHash.GrenadeLauncherSmoke || actualWeapon.Hash == GTA.Native.WeaponHash.Gusenberg || actualWeapon.Hash == GTA.Native.WeaponHash.HeavyPistol || actualWeapon.Hash == GTA.Native.WeaponHash.HeavyShotgun || actualWeapon.Hash == GTA.Native.WeaponHash.HeavySniper || actualWeapon.Hash == GTA.Native.WeaponHash.MachinePistol || actualWeapon.Hash == GTA.Native.WeaponHash.MarksmanRifle || actualWeapon.Hash == GTA.Native.WeaponHash.MG || actualWeapon.Hash == GTA.Native.WeaponHash.MicroSMG || actualWeapon.Hash == GTA.Native.WeaponHash.Pistol || actualWeapon.Hash == GTA.Native.WeaponHash.Pistol50 || actualWeapon.Hash == GTA.Native.WeaponHash.Revolver || actualWeapon.Hash == GTA.Native.WeaponHash.SMG || actualWeapon.Hash == GTA.Native.WeaponHash.SniperRifle || actualWeapon.Hash == GTA.Native.WeaponHash.SNSPistol || actualWeapon.Hash == GTA.Native.WeaponHash.SpecialCarbine || actualWeapon.Hash == GTA.Native.WeaponHash.VintagePistol || actualWeapon.Hash == GTA.Native.WeaponHash.AssaultShotgun)) 
        {
            if (player.IsReloading && !playerreloaded) {
                playerreloaded = true;
                if (player.Weapons.Current.AmmoInClip > 0) {
                    player.Weapons.Current.AmmoInClip = 0;
                }
            }
            if (!player.IsReloading && player.Weapons.Current.AmmoInClip == player.Weapons.Current.DefaultClipSize) {
                playerreloaded = false;
            }
        }

        //REALLISTICAIMING MODULE
        if (BreathAimMovement) { BreathAimModule(); }
        if (ShakeWhenAim) { ShakeWhenAimModule(); }

        previousWeapon = actualWeapon;
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        //CHANGE FIRE MODE
        if (capableWeapon && e.KeyCode == ChangeFireModeHotkey_Keys) {
            if (fireMode == 1) { //FULL-AUTO to SEMI-AUTO
                fireMode = 2;
                if (ShowNotif) {UI.Notify("Fire mode: SEMI-AUTO");}
                firemodechanged = true;
            }
            else if (fireMode == 2) { //SEMI-AUTO to BURST
                fireMode = 3;
                if (ShowNotif) {UI.Notify("Fire mode: BURST");}
                firemodechanged = true;
            }
            else if (fireMode == 3) { //BURST to FULL-AUTO
                fireMode = 1;
                if (ShowNotif) {UI.Notify("Fire mode: FULL-AUTO");}
                firemodechanged = true;
            }
        }
    }

    private void BurstMode()
    { //Script for Burst Fire Mode
        if (player.IsShooting) {
            ammocount++;
        }
        if (ammocount == 1 || ammocount == 2) {
            Game.SetControlNormal(0, GTA.Control.Attack, 1.0f);
            if (player.IsAimingFromCover) {
                Game.SetControlNormal(0, GTA.Control.Aim, 1.0f);
            }
        }
        if (ammocount == 3) {
            Game.Player.DisableFiringThisFrame();
            if (GTA.Game.IsControlJustReleased(0, GTA.Control.Attack)) {
                ammocount = 0;
            }
        }
        //prevammo = ammo;
    }

    private void SemiAutoMode()
    { //Script for Semi-Auto Fire Mode
        if (GTA.Game.IsControlPressed(0, GTA.Control.Attack)) {
            Game.Player.DisableFiringThisFrame();
        }
    }

    private void StealthMode(bool sth)
    {
        GTA.Native.Function.Call(GTA.Native.Hash.SET_PED_STEALTH_MOVEMENT, player, sth, 0);
    }

    private void BreathAimModule()
    { //Shake "Hand" effect - when you're aiming the camera moves like if breathing
        if (Game.Player.IsAiming && !breathshaking) {
            GameplayCamera.Shake(GTA.CameraShake.Hand, (0.1f * BreathMovementRate));
            breathshaking = true;
        }
        else if (!Game.Player.IsAiming) { //avoid shake running more than once
            GameplayCamera.StopShaking();
            breathshaking = false;
        }
    }

    private void ShakeWhenAimModule() 
    { //Shake "Jolt" effect - just applies when start aiming, moving the camera
        if (Game.Player.IsAiming && !aimshaking) {
            GameplayCamera.Shake(GTA.CameraShake.Jolt, (0.1f * ShakeWhenAimMovementRate));
            aimshaking = true;
        }
        else if (!Game.Player.IsAiming) { //avoid shake running more than once
            aimshaking = false;
        }
    }
}