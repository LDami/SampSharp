// SampSharp
// Copyright 2022 Tim Potze
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using System;
using SampSharp.Core.Logging;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.Pools;

namespace SampSharp.GameMode.World;

/// <summary>Represents a Open.MP NPC component.</summary>
public partial class Npc : IdentifiedPool<Npc>, IWorldObject
{

    public const int InvalidId = -1;

    /// <summary>Maximum number of NPC which can exist.</summary>
    public const int Max = 1000;

    public const int MaxNodes = 64;

    public const float MoveSpeedAuto = -1.0f;
    public const float MoveSpeedWalk = 0.1552086f;
    public const float MoveSpeedJog = 0.56444f;
    public const float MoveSpeedSprint = 0.926784f;

    /// <summary>
    /// Initialize the instance
    /// </summary>
    public Npc()
    {
    }

    /// <summary>
    /// Returns a string with ID and Name
    /// </summary>
    public override string ToString()
    {
        AssertNotDisposed();

        return $"ID: {Id} ; Name: {Name}";
    }

    /// <summary>Gets or sets the name of this NPC.</summary>
    public virtual string Name
    {
        get;
        private set;
    }

    /// <summary>Gets or sets the interior of this NPC.</summary>
    public virtual int Interior
    {
        get => NpcInternal.Instance.NPC_GetInterior(Id);
        set => NpcInternal.Instance.NPC_SetInterior(Id, value);
    }

    /// <summary>Gets or sets the health of this NPC.</summary>
    public virtual float Health
    {
        get => NpcInternal.Instance.NPC_GetHealth(Id);
        set => NpcInternal.Instance.NPC_SetHealth(Id, value);
    }

    /// <summary>Gets or sets the armor of this NPC.</summary>
    public virtual float Armour
    {
        get => NpcInternal.Instance.NPC_GetArmour(Id);
        set => NpcInternal.Instance.NPC_SetArmour(Id, value);
    }

    /// <summary>Gets or sets the invulnerability of this NPC.</summary>
    public virtual bool Invulnerable
    {
        get => NpcInternal.Instance.NPC_IsInvulnerable(Id);
        set => NpcInternal.Instance.NPC_SetInvulnerable(Id, value);
    }



    /// <summary>Occurs when the <see cref="OnFinishMove" /> is being called. This callback is triggered when the npc reached it's destination.</summary>
    public event EventHandler<EventArgs> FinishedMove;

    /// <summary>Occurs when the <see cref="OnCreate" /> is being called. This callback is called when a npc connects to the server.</summary>
    public event EventHandler<EventArgs> Created;

    /// <summary>Occurs when the <see cref="OnDestroy" /> is being called. This callback is called when a npc disconnects from the server.</summary>
    public event EventHandler<EventArgs> Destroyed;

    /// <summary>Occurs when the <see cref="OnSpawn" /> is being called. This callback is called when a npc spawns.</summary>
    public event EventHandler<SpawnEventArgs> Spawned;

    /// <summary>Occurs when the <see cref="OnRespawn" /> is being called. This callback is called when a npc respawns.</summary>
    public event EventHandler<SpawnEventArgs> Respawned;

    /// <summary>Occurs when the <see cref="OnWeaponStateChange" /> is being called. This callback is called when the weapon state changes.</summary>
    public event EventHandler<WeaponStateChangeEventArgs> WeaponStateChanged;

    /// <summary>Occurs when the <see cref="OnTakeDamage" /> is being called. This callback is called when a npc takes damages.</summary>
    public event EventHandler<DamageEventArgs> TakeDamage;

    /// <summary>Occurs when the <see cref="OnGiveDamage" /> is being called. This callback is called when a npc gives damage.</summary>
    public event EventHandler<DamageEventArgs> GiveDamage;

    /// <summary>Occurs when the <see cref="OnDeath" /> is being called. This callback is triggered when the npc dies.</summary>
    public event EventHandler<DeathEventArgs> Died;

    /// <summary>Occurs when the <see cref="OnPlaybackStart" /> is being called. This callback is triggered when the playback starts.</summary>
    public event EventHandler<NpcPlaybackEventArgs> PlaybackStarted;

    /// <summary>Occurs when the <see cref="OnPlaybackEnd" /> is being called. This callback is triggered when the playback ends.</summary>
    public event EventHandler<NpcPlaybackEventArgs> PlaybackEnded;

    /// <summary>Occurs when the <see cref="OnWeaponShot" /> is being called. This callback is triggered when the npc shots with a weapon.</summary>
    public event EventHandler<WeaponShotEventArgs> WeaponShot;

    /// <summary>Occurs when the <see cref="OnFinishNodePoint" /> is being called.</summary>
    public event EventHandler<NpcFinishNodePointEventArgs> FinishNodePoint;

    /// <summary>Occurs when the <see cref="OnFinishNode" /> is being called.</summary>
    public event EventHandler<NpcFinishNodeEventArgs> FinishNode;

    /// <summary>Occurs when the <see cref="OnChangeNode" /> is being called.</summary>
    public event EventHandler<NpcChangeNodeEventArgs> ChangeNode;

    /// <summary>Occurs when the <see cref="OnFinishMovePath" /> is being called.</summary>
    public event EventHandler<NpcFinishMovePathEventArgs> FinishMovePath;

    /// <summary>Occurs when the <see cref="OnFinishMovePathPoint" /> is being called.</summary>
    public event EventHandler<NpcFinishMovePathPointEventArgs> FinishMovePathPoint;

    #region Core

    /// <summary>Creates the NPC with the given name</summary>
    /// <param name="name">The name of the NPC</param>
    public static Npc Create(string name)
    {
        var r = Npc.Create(-1); // Get an instance of Npc class by using IdentifiedPool's create method
        var id = NpcInternal.Instance.NPC_Create(name); // Create the Npc on the server and get it's id
        if (id == InvalidId)
        {
            r.Dispose();
            return null;
        }
        r.Id = id;
        r.Name = name;
        return r;
    }

    /// <summary>
    /// Destroy this NPC
    /// </summary>
    public virtual void Destroy()
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_Destroy(Id);
    }

    /// <summary>
    /// Checks if the NPC is valid
    /// </summary>
    public virtual bool IsValid => NpcInternal.Instance.NPC_IsValid(Id);

    /// <summary>Gets a value indicating whether this Player is alive.</summary>
    public virtual bool IsAlive => !NpcInternal.Instance.NPC_IsDead(Id);

    /// <summary>Spawns a NPC.</summary>
    public virtual void Spawn()
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_Spawn(Id);
    }

    /// <summary>Respawns a NPC.</summary>
    public virtual void Respawn()
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_Respawn(Id);
    }

    /// <summary>Gets or sets the position of this NPC.</summary>
    public virtual Vector3 Position
    {
        get
        {
            NpcInternal.Instance.NPC_GetPos(Id, out var x, out var y, out var z);
            return new Vector3(x, y, z);
        }
        set => NpcInternal.Instance.NPC_SetPos(Id, value.X, value.Y, value.Z);
    }

    /// <summary>Gets or sets the rotation of this NPC.</summary>
    /// <remarks>Only the Z angle can be set!</remarks>
    public virtual Vector3 Rotation
    {
        get
        {
            NpcInternal.Instance.NPC_GetRot(Id, out var x, out var y, out var z);
            return new Vector3(x, y, z);
        }
        set => NpcInternal.Instance.NPC_SetRot(Id, value.X, value.Y, value.Z);
    }

    /// <summary>Gets or sets the facing angle of this NPC.</summary>
    public virtual float Angle
    {
        get
        {
            NpcInternal.Instance.NPC_GetFacingAngle(Id, out var angle);
            return angle;
        }
        set => NpcInternal.Instance.NPC_SetFacingAngle(Id, value);
    }

    /// <summary>Gets or sets the virtual world of this NPC.</summary>
    public virtual int VirtualWorld
    {
        get => NpcInternal.Instance.NPC_GetVirtualWorld(Id);
        set => NpcInternal.Instance.NPC_SetVirtualWorld(Id, value);
    }


    /// <summary>
    /// Moves the NPC to a specific position
    /// </summary>
    /// <param name="target">The position to move to</param>
    /// <param name="moveType">The <see cref="NPCMoveType" /> (jog, sprint, walk, ...)</param>
    /// <param name="moveSpeed">The move speed</param>
    /// <param name="stopRange">The stop range (default = 0.2f)</param>
    public virtual void Move(Vector3 target, NPCMoveType moveType = NPCMoveType.Jog, float moveSpeed = MoveSpeedAuto, float stopRange = 0.2f)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_Move(Id, target.X, target.Y, target.Z, (int)moveType, moveSpeed, stopRange);
    }

    /// <summary>
    /// Moves the NPC to a specific player
    /// </summary>
    /// <param name="target">The <see cref="BasePlayer" /> to move to</param>
    /// <param name="moveType">The move type (jog, sprint, walk, ...)</param>
    /// <param name="moveSpeed">The move speed</param>
    /// <param name="stopRange">The stop range (default = 0.2f)</param>
    /// <param name="updateDelayMS">The delay in MS between 2 updates</param>
    /// <param name="autoRestart">If True, the NPC will keep following the <see cref="BasePlayer"/></param>
    public virtual void MoveToPlayer(BasePlayer target, NPCMoveType moveType = NPCMoveType.Jog, float moveSpeed = MoveSpeedAuto, float stopRange = 0.2f,
        int updateDelayMS = 500, bool autoRestart = false)
    {
        AssertNotDisposed();

        ArgumentNullException.ThrowIfNull(target);

        NpcInternal.Instance.NPC_MoveToPlayer(Id, target.Id, (int)moveType, moveSpeed, stopRange, updateDelayMS, autoRestart);
    }

    /// <summary>
    /// Stops the NPC
    /// </summary>
    /// <returns>True if the NPC has been stopped</returns>
    public virtual bool StopMove()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_StopMove(Id);
    }

    /// <summary>
    /// Returns true if the NPC is moving
    /// </summary>
    /// <returns>True if the NPC is moving</returns>
    public virtual bool IsMoving => NpcInternal.Instance.NPC_IsMoving(Id);

    #endregion
    #region Weapons & Combat
    /// <summary>Gets the WeaponState of the Weapon this NPC is currently holding.</summary>
    public virtual WeaponState WeaponState => (WeaponState)NpcInternal.Instance.NPC_GetWeaponState(Id);

    /// <summary>Gets or sets the Weapon this NPC is currently holding.</summary>
    public virtual Weapon Weapon
    {
        get => (Weapon)NpcInternal.Instance.NPC_GetWeapon(Id);
        set => NpcInternal.Instance.NPC_SetWeapon(Id, (int)value);
    }

    /// <summary>Gets or sets the ammo of the Weapon this NPC is currently holding.</summary>
    /// <remarks>This method does not need parameter "weapon" unlike BasePlayer, I assume it sets the ammo of the current hold weapon</remarks>
    public virtual int WeaponAmmo
    {
        get => NpcInternal.Instance.NPC_GetAmmo(Id);
        set => NpcInternal.Instance.NPC_SetAmmo(Id, value);
    }

    /// <summary>Gets or sets the ammo in the clip of the Weapon this NPC is currently holding.</summary>
    /// <remarks>This method does not need parameter "weapon" unlike BasePlayer, I assume it sets the ammo of the current hold weapon</remarks>
    public virtual int WeaponAmmoInClip
    {
        get => NpcInternal.Instance.NPC_GetAmmoInClip(Id);
        set => NpcInternal.Instance.NPC_SetAmmoInClip(Id, value);
    }

    /// <summary>
    /// Gets the weapon accuracy
    /// </summary>
    public virtual float GetWeaponAccuracy(Weapon weapon)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_GetWeaponAccuracy(Id, (int)weapon);
    }

    /// <summary>
    /// Sets the weapon accuracy
    /// </summary>
    /// <param name="weapon">The weapon to set the accuracy for</param>
    /// <param name="value">The accuracy value (between 0.0 and 1.0)</param>
    public virtual void SetWeaponAccuracy(Weapon weapon, float value)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_SetWeaponAccuracy(Id, (int)weapon, value);
    }

    /// <summary>
    /// Gets the weapon reload time
    /// </summary>
    public virtual int GetWeaponReloadTime(Weapon weapon)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_GetWeaponReloadTime(Id, (int)weapon);
    }

    /// <summary>
    /// Sets the weapon reload time
    /// </summary>
    public virtual void SetWeaponReloadTime(Weapon weapon, int value)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_SetWeaponReloadTime(Id, (int)weapon, value);
    }

    /// <summary>
    /// Gets the weapon actual reload time
    /// </summary>
    public virtual int GetWeaponActualReloadTime(Weapon weapon)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_GetWeaponActualReloadTime(Id, (int)weapon);
    }

    /// <summary>
    /// Gets the weapon shoot time
    /// </summary>
    public virtual int GetWeaponShootTime(Weapon weapon)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_GetWeaponShootTime(Id, (int)weapon);
    }

    /// <summary>
    /// Sets the weapon shoot time
    /// </summary>
    public virtual void SetWeaponShootTime(Weapon weapon, int value)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_SetWeaponShootTime(Id, (int)weapon, value);
    }

    /// <summary>
    /// Gets the weapon shoot time
    /// </summary>
    public virtual int GetWeaponClipSize(Weapon weapon)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_GetWeaponClipSize(Id, (int)weapon);
    }

    /// <summary>
    /// Sets the weapon shoot time
    /// </summary>
    public virtual void SetWeaponClipSize(Weapon weapon, int value)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_SetWeaponClipSize(Id, (int)weapon, value);
    }


    /// <summary>Gets or sets the FightStyle of this NPC.</summary>
    public virtual FightStyle FightStyle
    {
        get => (FightStyle)NpcInternal.Instance.NPC_GetFightingStyle(Id);
        set => NpcInternal.Instance.NPC_SetFightingStyle(Id, (int)value);
    }

    /// <summary>
    /// Makes this Npc melee attack
    /// </summary>
    /// <param name="time">Time in ms</param>
    /// <param name="secondaryAttack">Use the secondary attack key instead of fire key</param>
    public virtual bool MeleeAttack(int time, bool secondaryAttack = false)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_MeleeAttack(Id, time, secondaryAttack);
    }

    /// <summary>
    /// Makes this Npc stop melee attack
    /// </summary>
    public virtual bool StopMeleeAttack()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_StopMeleeAttack(Id);
    }

    /// <summary>
    /// Return true if this Npc is melee attacking
    /// </summary>
    public virtual bool IsMeleeAttacking => NpcInternal.Instance.NPC_IsMeleeAttacking(Id);
    
    /// <summary>
    /// Enables or disabled the weapon reloading for this Npc
    /// </summary>
    public virtual bool EnableReloading(bool enable)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_EnableReloading(Id, enable);
    }

    /// <summary>
    /// Returns true if the reload is enabled
    /// </summary>
    public virtual bool IsReloadEnabled => NpcInternal.Instance.NPC_IsReloadEnabled(Id);

    /// <summary>
    /// Returns true if the Npc is reloading
    /// </summary>
    public virtual bool IsReloading => NpcInternal.Instance.NPC_IsReloading(Id);

    /// <summary>
    /// Enables or disabled the weapon infinite ammo for this Npc
    /// </summary>
    public virtual bool EnableInfiniteAmmo(bool enable)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_EnableInfiniteAmmo(Id, enable);
    }

    /// <summary>
    /// Returns true if the weapon infinite ammo is enabled
    /// </summary>
    public virtual bool IsInfiniteAmmoEnabled => NpcInternal.Instance.NPC_IsInfiniteAmmoEnabled(Id);

    /// <summary>
    /// Makes this NPC fire a weapon shoot
    /// </summary>
    /// <remarks>The <paramref name="weapon"/> must throw bullets, otherwise this method does nothing</remarks>
    /// <param name="weapon">The weapon id to use for shooting</param>
    /// <param name="targetId">The ID of target entity being shot</param>
    /// <param name="targetType">The type of entity being hit (player, NPC, vehicle, etc.)</param>
    /// <param name="endPoint">Coordinate of the bullet end point</param>
    /// <param name="offset">Offset from the hit point</param>
    /// <param name="isHit">Whether the shot actually hits the target</param>
    /// <param name="checkInBetweenFlags">Entity check flags</param>
    /// <returns>True on success, false on failure</returns>
    public virtual bool Shoot(Weapon weapon, int targetId, BulletHitType targetType, Vector3 endPoint, Vector3 offset, bool isHit, NPCEntityCheck checkInBetweenFlags = NPCEntityCheck.All)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_Shoot(Id, (int)weapon, targetId, (int)targetType, endPoint.X, endPoint.Y, endPoint.Z, offset.X, offset.Y, offset.Z, isHit, (int)checkInBetweenFlags);
    }

    /// <summary>
    /// Returns true if the NPC is shooting
    /// </summary>
    /// <returns>True if the NPC is shooting</returns>
    public virtual bool IsShooting => NpcInternal.Instance.NPC_IsShooting(Id);

    /// <summary>
    /// Makes the NPC aim at a specific point
    /// </summary>
    /// <param name="point">The coordinate to aim at</param>
    /// <param name="shoot">If true, the NPC will fire a weapon shot</param>
    /// <param name="shootDelay">Delay to fire a weapon shot</param>
    /// <param name="updateAngle">No idea what it means</param>
    /// <param name="offsetFrom">No idea what it means</param>
    /// <param name="checkInBetweenFlags">Still no idea what it means</param>
    public virtual void AimAt(Vector3 point, bool shoot, int shootDelay, bool updateAngle, Vector3 offsetFrom, NPCEntityCheck checkInBetweenFlags = NPCEntityCheck.All)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_AimAt(Id, point.X, point.Y, point.Z, shoot, shootDelay, updateAngle, offsetFrom.X, offsetFrom.Y, offsetFrom.Z, 
            (int)checkInBetweenFlags);
    }

    /// <summary>
    /// Makes the NPC aim at a <see cref="BasePlayer"/>
    /// </summary>
    /// <param name="player"></param>
    /// <param name="shoot">If true, the NPC will fire a weapon shot</param>
    /// <param name="shootDelay">Delay to fire a weapon shot</param>
    /// <param name="updateAngle">No idea what it means</param>
    /// <param name="offset">No idea what it means</param>
    /// <param name="offsetFrom">No idea what it means</param>
    /// <param name="checkInBetweenFlags">Still no idea what it means</param>
    public virtual void AimAtPlayer(BasePlayer player, bool shoot, int shootDelay, bool updateAngle, Vector3 offset, Vector3 offsetFrom, NPCEntityCheck checkInBetweenFlags = NPCEntityCheck.All)
    {
        AssertNotDisposed();

        ArgumentNullException.ThrowIfNull(player);

        NpcInternal.Instance.NPC_AimAtPlayer(Id, player.Id, shoot, shootDelay, updateAngle, offset.X, offset.Y, offset.Z, 
            offsetFrom.X, offsetFrom.Y, offsetFrom.Z, (int)checkInBetweenFlags);
    }

    /// <summary>
    /// Stop the aim animation
    /// </summary>
    public virtual bool StopAim()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_StopAim(Id);
    }

    /// <summary>
    /// Returns true if the Npc is aiming
    /// </summary>
    public virtual bool IsAiming()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_IsAiming(Id);
    }

    /// <summary>
    /// Returns true if the Npc is aiming at <paramref name="player"/>
    /// </summary>
    /// <param name="player">The player</param>
    public virtual bool IsAimingAtPlayer(BasePlayer player)
    {
        AssertNotDisposed();

        ArgumentNullException.ThrowIfNull(player);

        return NpcInternal.Instance.NPC_IsAimingAtPlayer(Id, player.Id);
    }

    #endregion
    #region Vehicles

    /// <summary>Gets the vehicle seat this NPC sits on.</summary>
    public virtual int VehicleSeat => NpcInternal.Instance.NPC_GetVehicleSeat(Id);

    /// <summary>Gets whether this NPC is currently in any vehicle.</summary>
    public virtual bool InAnyVehicle => NpcInternal.Instance.NPC_GetVehicle(Id) != -1;


    /// <summary>Gets the Vehicle this NPC is currently in.</summary>
    public virtual BaseVehicle Vehicle
    {
        get
        {
            var vehicleid = NpcInternal.Instance.NPC_GetVehicleID(Id);
            return vehicleid == 0
                ? null
                : BaseVehicle.Find(vehicleid);
        }
    }

    /// <summary>Puts this <see cref="Npc" /> in a <see cref="BaseVehicle" />.</summary>
    /// <param name="vehicle">The vehicle for the NPC to be put in.</param>
    /// <param name="seatid">The ID of the seat to put the NPC in (default = 0).</param>
    public virtual bool PutInVehicle(BaseVehicle vehicle, int seatid = 0)
    {
        AssertNotDisposed();

        ArgumentNullException.ThrowIfNull(vehicle);

        return NpcInternal.Instance.NPC_PutInVehicle(Id, vehicle.Id, seatid);
    }

    /// <summary>Removes this <see cref="Npc" /> from his vehicle.</summary>
    public virtual bool RemoveFromVehicle()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_RemoveFromVehicle(Id);
    }

    /// <summary>Ejects without animation this <see cref="Npc" /> from his vehicle.</summary>
    public virtual bool EnterInVehicle(BaseVehicle vehicle, int seatId, NPCMoveType moveType)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_EnterVehicle(Id, vehicle.Id, seatId, (int)moveType);
    }

    /// <summary>Ejects without animation this <see cref="Npc" /> from his vehicle.</summary>
    public virtual bool ExitFromVehicle()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_ExitVehicle(Id);
    }

    /// <summary>
    /// Get the vehicle this Npc is currently entering in.
    /// </summary>
    /// <returns>A <see cref="BaseVehicle"/> corresponding to the vehicle the NPC is currently entering in.</returns>
    public virtual BaseVehicle GetEnteringVehicle()
    {
        AssertNotDisposed();

        return BaseVehicle.Find(NpcInternal.Instance.NPC_GetEnteringVehicle(Id));
    }

    /// <summary>
    /// Get the vehicle seat this NPC is currently entering in.
    /// </summary>
    /// <returns>The seat the NPC is currently seating on.</returns>
    public virtual int GetEnteringVehicleSeat()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_GetEnteringVehicleSeat(Id);
    }

    /// <summary>
    /// Returns true if the NPC is entering a vehicle.
    /// </summary>
    /// <returns>True if the NPC is entering a vehicle.</returns>
    public virtual bool IsEnteringVehicle()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_IsEnteringVehicle(Id);
    }

    /// <summary>
    /// Enables or disables the vehicle siren.
    /// </summary>
    /// <returns>True if the siren has been enabled.</returns>
    public virtual bool UseVehicleSiren(bool use = true)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_UseVehicleSiren(Id, use);
    }

    /// <summary>
    /// Returns true if vehicle siren is enabled.
    /// </summary>
    /// <returns>True if vehicle siren is enabled.</returns>
    public virtual bool IsVehicleSirenUsed()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_IsVehicleSirenUsed(Id);
    }

    /// <summary>
    /// Gets the hydra thrusters direction
    /// </summary>
    /// <returns></returns>
    public virtual int GetHydraThrusters()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_GetVehicleHydraThrusters(Id);
    }

    /// <summary>
    /// Sets the hydra thrusters direction
    /// </summary>
    /// <param name="updown">0 = forward, 1 = backward</param>
    /// <returns></returns>
    public virtual void SetHydraThrusters(int updown)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_SetVehicleHydraThrusters(Id, updown);
    }

    /// <summary>
    /// Gets the hydra thrusters direction
    /// </summary>
    /// <returns>The gear state (0 = down, 1 = up)</returns>
    public virtual int GetGearState()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_GetVehicleGearState(Id);
    }

    /// <summary>
    /// Sets the vehicle's gear state
    /// </summary>
    /// <param name="updown">0 = down, 1 = up</param>
    /// <returns></returns>
    public virtual void SetGearState(int updown)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_SetVehicleGearState(Id, updown);
    }

    /// <summary>
    /// Gets the speed of the train
    /// </summary>
    public virtual float GetVehicleTrainSpeed()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_GetVehicleTrainSpeed(Id);
    }

    /// <summary>
    /// Sets the speed of the train
    /// </summary>
    /// <param name="speed">Velocity to apply to the train</param>
    public virtual void SetVehicleTrainSpeed(float speed)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_SetVehicleTrainSpeed(Id, speed);
    }
    #endregion
    #region Animations
    /// <summary>
    /// Resets the NPC animation
    /// </summary>
    public virtual void ResetAnimation()
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_ResetAnimation(Id);
    }

    /// <summary>
    /// Sets the animation for this NPC
    /// </summary>
    /// <param name="animId">The animation ID to set</param>
    /// <param name="delta"></param>
    /// <param name="loop">Sets if the animation should loop or run only once</param>
    /// <param name="lockX"></param>
    /// <param name="lockY"></param>
    /// <param name="freeze"></param>
    /// <param name="time"></param>
    public virtual void SetAnimation(int animId, float delta, bool loop, bool lockX, bool lockY, bool freeze, int time)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_SetAnimation(Id, animId, delta, loop, lockX, lockY, freeze, time);
    }

    /// <summary>
    /// Gets the animation for this NPC
    /// </summary>
    /// <param name="animId">The animation ID to set</param>
    /// <param name="delta"></param>
    /// <param name="loop">Gets if the animation should loop or run only once</param>
    /// <param name="lockX"></param>
    /// <param name="lockY"></param>
    /// <param name="freeze"></param>
    /// <param name="time"></param>
    public virtual void GetAnimation(out int animId, out float delta, out bool loop, out bool lockX, out bool lockY, out bool freeze, out int time)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_GetAnimation(Id, out animId, out delta, out loop, out lockX, out lockY, out freeze, out time);
    }

    /// <summary>
    /// Apply an animation from the SA animation library
    /// </summary>
    /// <param name="animlib">The animation library where the animation to apply is</param>
    /// <param name="animname">The animation name (must be in the <paramref name="animlib"/>)</param>
    /// <param name="delta"></param>
    /// <param name="loop">Sets if the animation should loop or run only once</param>
    /// <param name="lockX"></param>
    /// <param name="lockY"></param>
    /// <param name="freeze"></param>
    /// <param name="time"></param>
    public virtual void ApplyAnimation(string animlib, string animname, float delta, bool loop, bool lockX, bool lockY, bool freeze, int time)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_ApplyAnimation(Id, animlib, animname, delta, loop,lockX, lockY, freeze, time);
    }

    /// <summary>
    /// Clear all the animations from this NPC
    /// </summary>
    public virtual void ClearAnimations()
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_ClearAnimations(Id);
    }

    /// <summary>
    /// Gets or sets the <see cref="Definitions.SpecialAction"/> for this NPC
    /// </summary>
    public virtual SpecialAction SpecialAction
    {
        get => (SpecialAction)NpcInternal.Instance.NPC_GetSpecialAction(Id);
        set => NpcInternal.Instance.NPC_SetSpecialAction(Id, (int)value);
    }
    #endregion
    #region Playback
    /// <summary>
    /// Starts a pre-recorded playback
    /// </summary>
    /// <param name="recordName">The record name</param>
    /// <param name="autoUnload">If true, the record will be unloaded after finished</param>
    /// <param name="start">The position where the NPC should start the playback</param>
    /// <param name="rotation">The rotation the NPC should have at the start of the playback</param>
    public virtual bool StartPlayback(string recordName, bool autoUnload, Vector3 start, Vector3 rotation)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_StartPlayback(Id, recordName, autoUnload, start.X, start.Y, start.Z, rotation.X, rotation.Y, rotation.Z);
    }
    /// <summary>
    /// Starts a pre-recorded playback
    /// </summary>
    /// <param name="recordId">The record ID</param>
    /// <param name="autoUnload">If true, the record will be unloaded after finished</param>
    /// <param name="start">The position where the NPC should start the playback</param>
    /// <param name="rotation">The rotation the NPC should have at the start of the playback</param>
    public virtual bool StartPlayback(int recordId, bool autoUnload, Vector3 start, Vector3 rotation)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_StartPlaybackEx(Id, recordId, autoUnload, start.X, start.Y, start.Z, rotation.X, rotation.Y, rotation.Z);
    }

    /// <summary>
    /// Stops the playback
    /// </summary>
    /// <returns>True if the playback has been stopped</returns>
    public virtual bool StopPlayback()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_StopPlayback(Id);
    }

    /// <summary>
    /// Pauses the playback
    /// </summary>
    /// <returns>True if the playback has been paused</returns>
    public virtual bool PausePlayback(bool paused)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_PausePlayback(Id, paused);
    }

    /// <summary>
    /// Returns true if the NPC is playing a playback
    /// </summary>
    public virtual bool IsPlayingPlayback => NpcInternal.Instance.NPC_IsPlayingPlayback(Id);

    /// <summary>
    /// Returns true if the playback of the  NPC is paused
    /// </summary>
    public virtual bool IsPlaybackPaused => NpcInternal.Instance.NPC_IsPlaybackPaused(Id);
    #endregion
    #region Paths

    /// <summary>
    /// Contains all the path methods, used for NPC navigation
    /// </summary>
    public class Path
    {
        /// <summary>
        /// Path ID of this instance
        /// </summary>
        public virtual int PathId
        {
            get;
            private set;
        }
        /// <summary>
        /// Creates a new path that can be used for NPC navigation
        /// </summary>
        /// <remarks>The path is created empty</remarks>
        /// <returns>A <see cref="Npc.Path"/> instance of the created path</returns>
        public Path()
        {
            PathId = NpcInternal.Instance.NPC_CreatePath();
        }
        /// <summary>
        /// Destroy the specified path
        /// </summary>
        /// <returns>True if the path has been destroyed, false otherwise</returns>
        public virtual bool Destroy()
        {
            return NpcInternal.Instance.NPC_DestroyPath(PathId);
        }
        /// <summary>
        /// Destroys all paths
        /// </summary>
        /// <remarks>All NPC following paths will stop moving</remarks>
        /// <returns>True on success</returns>
        public static bool DestroyAll()
        {
            return NpcInternal.Instance.NPC_DestroyAllPath();
        }
        /// <summary>
        /// Gets the number of path on the server
        /// </summary>
        public virtual int Count => NpcInternal.Instance.NPC_GetPathCount();

        /// <summary>
        /// Adds a point at the end of this path
        /// </summary>
        /// <param name="pos">Position of the point</param>
        /// <param name="stopRange">The distance from point at which to considier it reached</param>
        /// <returns>True if the point has been added, false otherwise</returns>
        public virtual bool AddPoint(Vector3 pos, float stopRange)
        {
            return NpcInternal.Instance.NPC_AddPointToPath(PathId, pos.X, pos.Y, pos.Z, stopRange);
        }
        /// <summary>
        /// Removes a point from this path
        /// </summary>
        /// <param name="pointIndex">The index of the point to remove</param>
        /// <returns>True if the point has been removed, false otherwise</returns>
        public virtual bool RemovePoint(int pointIndex)
        {
            return NpcInternal.Instance.NPC_RemovePointFromPath(PathId, pointIndex);
        }
        /// <summary>
        /// Removes all the point of this path
        /// </summary>
        /// <remarks>All NPC following this path will stop moving</remarks>
        /// <returns>True on success</returns>
        public virtual bool Clear()
        {
            return NpcInternal.Instance.NPC_ClearPath(PathId);
        }
        /// <summary>
        /// Gets position and stop range from a point
        /// </summary>
        /// <param name="pointIndex">The index of the point</param>
        /// <param name="position">The position of the point</param>
        /// <param name="stopRange">The distance from point at which to considier it reached</param>
        /// <returns>True on success, false otherwise</returns>
        public virtual bool GetPoint(int pointIndex, out Vector3 position, out float stopRange)
        {
            var r = NpcInternal.Instance.NPC_GetPathPoint(PathId, pointIndex, out var x, out var y, out var z, out stopRange);
            position = new Vector3(x, y, z);
            return r;
        }

        /// <summary>
        /// Check if this path is valid
        /// </summary>
        /// <remarks>Always check if the path is valid before using it with a NPC</remarks>
        public virtual bool IsValid => NpcInternal.Instance.NPC_IsValidPath(PathId);

        /// <summary>
        /// Detect if a point is in the specified range
        /// </summary>
        /// <param name="position">The position</param>
        /// <param name="radius">The radius</param>
        /// <returns>True if a point if in the radius of the specified position</returns>
        public virtual bool HasPointInRange(Vector3 position, float radius)
        {
            return NpcInternal.Instance.NPC_HasPathPointInRange(PathId, position.X, position.Y, position.Z, radius);
        }
    }

    /// <summary>
    /// Gets the current path point this NPC is moving towards
    /// </summary>
    public virtual int CurrentPointIndex => NpcInternal.Instance.NPC_GetCurrentPathPointIndex(Id);

    /// <summary>
    /// Moves the NPC following a <see cref="Npc.Path"/>
    /// </summary>
    /// <param name="path">The <see cref="Npc.Path"/> to follow</param>
    /// <param name="moveType">The <see cref="NPCMoveType"/></param>
    /// <param name="moveSpeed">The move speed</param>
    /// <param name="reversed">Whether to follow the path in reversed</param>
    /// <returns>True on success, false otherwise</returns>
    public virtual bool MoveByPath(Path path, NPCMoveType moveType = NPCMoveType.Jog, float moveSpeed = MoveSpeedAuto, bool reversed = false)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_MoveByPath(Id, path.PathId, (int)moveType, moveSpeed, reversed);
    }

    #endregion

    /// <summary>Check which keys this <see cref="Npc" /> is pressing.</summary>
    /// <remarks>
    /// Only the FUNCTION of keys can be detected; not actual keys. You can not detect if the player presses space, but you can detect if they press sprint
    /// (which can be mapped (assigned) to ANY key, but is space by default)).
    /// </remarks>
    /// <param name="updown">Up or Down value, passed by reference.</param>
    /// <param name="leftright">Left or Right value, passed by reference.</param>
    /// <param name="keys">A set of bits containing this NPC's key states</param>
    public virtual void GetKeys(out int updown, out int leftright, out Keys keys)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_GetKeys(Id, out updown, out leftright, out var keysDown);
        keys = (Keys)keysDown;
    }

    /// <summary>Sets which keys this <see cref="Npc" /> is pressing.</summary>
    /// <remarks>
    /// Only the FUNCTION of keys can be detected; not actual keys. You can not detect if the player presses space, but you can detect if they press sprint
    /// (which can be mapped (assigned) to ANY key, but is space by default)).
    /// </remarks>
    /// <param name="updown">Up or Down value, passed by reference.</param>
    /// <param name="leftright">Left or Right value, passed by reference.</param>
    /// <param name="keys">A set of bits containing this NPC's key states</param>
    public virtual void SetKeys(int updown, int leftright, Keys keys)
    {
        AssertNotDisposed();

        NpcInternal.Instance.NPC_SetKeys(Id, updown, leftright, (int)keys);
    }

    /// <summary>
    /// Sets the skin of the NPC
    /// </summary>
    /// <param name="model">The skin model to apply</param>
    /// <returns>True if the skin model is valid, False otherwise</returns>
    public virtual bool SetSkin(int model)
    {
        AssertNotDisposed();

        if (model < 0 || model > 311)
            return false;

        NpcInternal.Instance.NPC_SetSkin(Id, model);
        return true;
    }

    /// <summary>
    /// Detect if the NPC is streamed by the <paramref name="player"/>'s client
    /// </summary>
    /// <param name="player">The player to check</param>
    /// <returns>True if the <paramref name="player"/> can see this NPC</returns>
    public virtual bool IsStreamedIn(BasePlayer player)
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_IsStreamedIn(Id, player.Id);
    }

    /// <summary>
    /// Detect if the NPC is streamed by anyone's client
    /// </summary>
    /// <returns>True if someone can see this NPC</returns>
    public virtual bool IsAnyStreamedIn()
    {
        AssertNotDisposed();

        return NpcInternal.Instance.NPC_IsAnyStreamedIn(Id);
    }



    /// <summary>Raises the <see cref="Died" /> event.</summary>
    /// <param name="e">An <see cref="EventArgs" /> that contains the event data. </param>
    public virtual void OnFinishMove(EventArgs e)
    {
        CoreLog.Log(CoreLogLevel.Info, $"Method NPC.OnNPCFinishMove called, triggering FinishedMove event ...");
        FinishedMove?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="Created" /> event.</summary>
    /// <param name="e">An <see cref="EventArgs" /> that contains the event data. </param>
    public virtual void OnCreate(EventArgs e)
    {
        Created?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="Destroyed" /> event.</summary>
    /// <param name="e">An <see cref="EventArgs" /> that contains the event data. </param>
    public virtual void OnDestroy(EventArgs e)
    {
        Destroyed?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="Spawned" /> event.</summary>
    /// <param name="e">An <see cref="SpawnEventArgs" /> that contains the event data. </param>
    public virtual void OnSpawn(SpawnEventArgs e)
    {
        Spawned?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="Respawned" /> event.</summary>
    /// <param name="e">An <see cref="SpawnEventArgs" /> that contains the event data. </param>
    public virtual void OnRespawn(SpawnEventArgs e)
    {
        Respawned?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="WeaponStateChanged" /> event.</summary>
    /// <param name="e">An <see cref="SpawnEventArgs" /> that contains the event data. </param>
    public virtual void OnWeaponStateChange(WeaponStateChangeEventArgs e)
    {
        WeaponStateChanged?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="TakeDamage" /> event.</summary>
    /// <param name="e">An <see cref="DamageEventArgs" /> that contains the event data. </param>
    public virtual void OnTakeDamage(DamageEventArgs e)
    {
        TakeDamage?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="GiveDamage" /> event.</summary>
    /// <param name="e">An <see cref="DamageEventArgs" /> that contains the event data. </param>
    public virtual void OnGiveDamage(DamageEventArgs e)
    {
        GiveDamage?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="Died" /> event.</summary>
    /// <param name="e">An <see cref="DeathEventArgs" /> that contains the event data. </param>
    public virtual void OnDeath(DeathEventArgs e)
    {
        Died?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="PlaybackStarted" /> event.</summary>
    /// <param name="e">An <see cref="NpcPlaybackEventArgs" /> that contains the event data. </param>
    public virtual void OnPlaybackStart(NpcPlaybackEventArgs e)
    {
        PlaybackStarted?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="PlaybackEnded" /> event.</summary>
    /// <param name="e">An <see cref="NpcPlaybackEventArgs" /> that contains the event data. </param>
    public virtual void OnPlaybackEnd(NpcPlaybackEventArgs e)
    {
        PlaybackEnded?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="Weaponshot" /> event.</summary>
    /// <param name="e">An <see cref="WeaponShotEventArgs" /> that contains the event data. </param>
    public virtual void OnWeaponShot(WeaponShotEventArgs e)
    {
        WeaponShot?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="FinishNodePoint" /> event.</summary>
    /// <param name="e">An <see cref="NpcFinishNodePointEventArgs" /> that contains the event data. </param>
    public virtual void OnFinishNodePoint(NpcFinishNodePointEventArgs e)
    {
        FinishNodePoint?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="FinishNodePoint" /> event.</summary>
    /// <param name="e">An <see cref="NpcFinishNodeEventArgs" /> that contains the event data. </param>
    public virtual void OnFinishNode(NpcFinishNodeEventArgs e)
    {
        FinishNode?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="ChangeNodePoint" /> event.</summary>
    /// <param name="e">An <see cref="NpcChangeNodeEventArgs" /> that contains the event data. </param>
    public virtual void OnChangeNode(NpcChangeNodeEventArgs e)
    {
        ChangeNode?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="FinishMovePath" /> event.</summary>
    /// <param name="e">An <see cref="NpcFinishMovePathEventArgs" /> that contains the event data. </param>
    public virtual void OnFinishMovePath(NpcFinishMovePathEventArgs e)
    {
        FinishMovePath?.Invoke(this, e);
    }

    /// <summary>Raises the <see cref="FinishMovePathPoint" /> event.</summary>
    /// <param name="e">An <see cref="NpcFinishMovePathPointEventArgs" /> that contains the event data. </param>
    public virtual void OnFinishMovePathPoint(NpcFinishMovePathPointEventArgs e)
    {
        FinishMovePathPoint?.Invoke(this, e);
    }
}
