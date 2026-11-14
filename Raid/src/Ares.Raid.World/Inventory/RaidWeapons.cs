using Ares.Raid.World.Movement;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Inventory;

/// <summary>Server-owned choice of the item in hand, plus Ferro chambers, fire cadence and reload transfers.</summary>
public sealed class RaidWeapons(RaidInventory inventory)
{
    private sealed class Gun(Guid id)
    {
        internal Guid Id { get; } = id;
        internal uint ReadyAt { get; set; }
        internal int State { get; set; }
        internal int Shots { get; set; }
        internal bool AutoReload { get; set; }
    }

    private readonly Dictionary<Guid, Gun> _guns = [];
    private Guid? _active;
    private Guid? _selected;
    private Guid? _putAway;
    private Guid? _switchTo;
    private bool _switching;
    private uint _putAwayEnd;
    private byte _handledEquipChange;
    private bool _fireDown;
    private bool _reloadDown;
    private uint _frame;
    private uint _firedFrame;

    public uint Version { get; private set; }
    public int ShotsFired { get; private set; }
    public int ReloadsCompleted { get; private set; }
    /// <summary>
    /// Whether the character moves at armed speeds: anything but empty hands is in hand. Until the client picks an item
    /// the character holds the first equipped weapon, as a raid starts with it drawn.
    /// </summary>
    public bool Armed => _selected is { } chosen ? Hand(InventoryBlackboard.UnarmedSlot)?.Id != chosen : Active(Equipped(), null) is not null;

    /// <summary>
    /// The item in hand after the latest applied input, as the movement rules read it: its movement members and the
    /// weapon's state. A firearm, the Raider Tool and empty hands keep the item defaults their classes leave; empty hands
    /// do not block sprinting.
    /// </summary>
    public HeldItemInput HeldItem
    {
        get
        {
            HeldItemInput held = new(Armed ? HeldItemMovementParameters.Default : HeldItemMovementParameters.Unarmed) { Changing = _switching };
            if (_active is not { } active || !_guns.TryGetValue(active, out Gun? gun))
            {
                return held;
            }

            return held with
            {
                Weapon = true,
                Fired = _firedFrame != 0 && _firedFrame == _frame,
                Firing = gun.State == WeaponProtocol.FiringState,
                Reloading = gun.State == WeaponProtocol.ReloadingState,
            };
        }
    }

    /// <summary>Applies each accepted input frame once; packet retries cannot duplicate a shot or reload.</summary>
    /// <param name="input">The client's weapon controls.</param>
    /// <param name="itemObjects">Network ids of the held item objects the client can name as an equip target.</param>
    public void Apply(WeaponInput input, IReadOnlyDictionary<Guid, ulong>? itemObjects = null)
    {
        if (input.Frame <= _frame)
        {
            return;
        }

        // A raid starts with the gun already in hand, so the first draw has no equip time.
        bool atSpawn = _frame == 0;
        _frame = input.Frame;
        // Like the client, a switch first puts the held item away, then draws the new one.
        if (_switching && input.Frame >= _putAwayEnd && _selected != _switchTo)
        {
            _selected = _switchTo;
            Version++;
        }

        if (input.EquipChange != _handledEquipChange)
        {
            // The client names the item it wants in hand: a firearm, the Raider Tool or empty hands.
            // Other targets keep the current item.
            _handledEquipChange = input.EquipChange;
            Guid? target = itemObjects?.Where(entry => entry.Value == input.EquipTarget).Select(entry => (Guid?)entry.Key).FirstOrDefault();
            if (target is { } wanted && (Equipped().Any(item => item?.Id == wanted) || Hand(InventoryBlackboard.MeleeSlot)?.Id == wanted
                || Hand(InventoryBlackboard.UnarmedSlot)?.Id == wanted))
            {
                Request(wanted, input.Frame);
            }

            Version++;
        }

        if (_switching && input.Frame >= DrawEnd)
        {
            _switching = false;
            Version++;
        }

        ContainerItem? equipped = Active(Equipped(), _selected);
        if (_active != equipped?.Id)
        {
            if (_active is { } old && _guns.TryGetValue(old, out Gun? previous))
            {
                previous.State = WeaponProtocol.IdleState;
                previous.AutoReload = false;
            }

            _active = equipped?.Id;
            if (equipped is not null)
            {
                Gun next = Get(equipped.Id);
                next.ReadyAt = atSpawn || !_switching ? 0 : DrawEnd;
                next.State = WeaponProtocol.IdleState;
            }

            Version++;
        }

        if (equipped is not null && !_switching)
        {
            Gun gun = Get(equipped.Id);
            if (gun.State == WeaponProtocol.ReloadingState && input.Frame >= gun.ReadyAt)
            {
                ContainerItem loading = Equipped().First(item => item?.Id == gun.Id)!;
                if (inventory.SetChamber(gun.Id, Math.Min(FerroDefaults.ClipSize, loading.AmmoInClip + inventory.HeavyAmmo), consumeReserve: true))
                {
                    ReloadsCompleted++;
                }

                gun.State = WeaponProtocol.IdleState;
                Version++;
            }

            if (gun.State != WeaponProtocol.ReloadingState && input.Frame >= gun.ReadyAt)
            {
                ContainerItem current = Equipped().First(item => item?.Id == gun.Id)!;
                bool trigger = input is { AllowPrimary: true, FireBlocked: false, FireDown: true } && (!_fireDown || input.FirePressed);
                if (trigger && current is { Durability: > 0, AmmoInClip: > 0 })
                {
                    inventory.SetChamber(gun.Id, 0);
                    gun.ReadyAt = input.Frame + Frames(60 / FerroDefaults.FireRate);
                    gun.Shots++;
                    gun.State = WeaponProtocol.FiringState;
                    gun.AutoReload = true;
                    _firedFrame = input.Frame;
                    ShotsFired++;
                    Version++;
                }
                else if (current.AmmoInClip == 0 && inventory.HeavyAmmo > 0
                    && ((input.ReloadDown && !_reloadDown) || trigger || gun.AutoReload))
                {
                    gun.State = WeaponProtocol.ReloadingState;
                    gun.ReadyAt = input.Frame + Frames(FerroDefaults.ReloadSeconds);
                    gun.AutoReload = false;
                    Version++;
                }
                else if (gun.State == WeaponProtocol.FiringState)
                {
                    gun.State = WeaponProtocol.IdleState;
                    Version++;
                }
            }
        }

        _fireDown = input.FireDown;
        _reloadDown = input.ReloadDown;
    }

    /// <summary>Weapon nodes and their references in the inventory simulation node.</summary>
    public IReadOnlyList<PendulumVariable> Variables(uint frame, IReadOnlyDictionary<Guid, ulong>? itemObjects = null)
    {
        PendulumVariable template = PawnBlackboard.InitialState.Single(item => item.Slot == WeaponProtocol.InventorySlot);
        List<PendulumValue> values = [.. template.Values];
        ContainerItem?[] equipped = Equipped();
        int main = equipped[0] is null ? WeaponProtocol.InvalidHandle : WeaponProtocol.FirstWeaponSlot;
        int secondary = equipped[1] is null ? WeaponProtocol.InvalidHandle : WeaponProtocol.FirstWeaponSlot + 1;
        (int current, int target, int putAway, int phase, uint left) = Switch(frame);
        Set(WeaponProtocol.InventoryPrimaryField, (ulong)main);
        Set(WeaponProtocol.InventorySecondaryField, (ulong)secondary);
        Set(WeaponProtocol.InventoryCurrentField, (ulong)current);
        Set(WeaponProtocol.InventoryTargetField, (ulong)target);
        Set(WeaponProtocol.InventoryUnequipFromField, (ulong)putAway);
        // An empty offhand is the no-item handle (254); the invalid handle makes the client draw the Raider Tool.
        Set(WeaponProtocol.InventoryOffhandField, WeaponProtocol.NoItemHandle);
        Set(WeaponProtocol.InventoryEquipPhaseField, (ulong)phase);
        Set(WeaponProtocol.InventoryEquipTimerField, Signed(checked((int)left * PawnBlackboard.TimePerTick)));
        Set(WeaponProtocol.InventoryLastWantedField, (ulong)putAway);
        Set(WeaponProtocol.InventoryEquipChangeField, _handledEquipChange);
        Set(WeaponProtocol.ReserveCountField, WeaponProtocol.AmmoTypeCount);
        values.RemoveAll(value => value.Field == WeaponProtocol.ReserveArrayField);
        values.Add(new PendulumArrayValue(WeaponProtocol.ReserveArrayField,
            [.. Enumerable.Range(0, WeaponProtocol.AmmoTypeCount).Select(index => new PendulumArrayElement(index,
                [new PendulumIntegerValue(0, Signed(index == WeaponProtocol.HeavyAmmoIndex ? inventory.HeavyAmmo : 0))]))]));
        List<PendulumVariable> result = [template with { Values = values }];
        // The Raider Tool and empty hands nodes link their item objects once the client knows them.
        foreach ((int slot, int handle, int field) in ((int, int, int)[])[(InventoryBlackboard.MeleeSlot, HandItems.RaiderToolHandle, HandItems.RaiderToolItemInstanceField),
            (InventoryBlackboard.UnarmedSlot, WeaponProtocol.UnarmedHandle, HandItems.UnarmedItemInstanceField)])
        {
            if (Hand(slot) is not { } item || itemObjects?.GetValueOrDefault(item.Id) is not { } link || link == 0)
            {
                continue;
            }

            PendulumVariable node = PawnBlackboard.InitialState.Single(variable => variable.Slot == handle);
            result.Add(node with { Values = [.. node.Values.Where(value => value.Field != field), new PendulumIntegerValue(field, link)] });
        }

        for (int index = 0; index < equipped.Length; index++)
        {
            if (equipped[index] is not { } item)
            {
                continue;
            }

            Gun gun = Get(item.Id);
            int remaining = checked((int)(gun.ReadyAt > frame ? gun.ReadyAt - frame : 0));
            int slot = WeaponProtocol.FirstWeaponSlot + index;
            result.Add(new PendulumVariable(slot, WeaponProtocol.FirstWeaponPosition + index, 1, WeaponProtocol.WeaponDescriptor,
            [
                new PendulumIntegerValue(WeaponProtocol.WeaponNodeDataField, Signed(WeaponProtocol.WeaponNodeDataId)),
                new PendulumIntegerValue(WeaponProtocol.WeaponAssetField, item.AssetIndex),
                new PendulumIntegerValue(WeaponProtocol.WeaponItemInstanceField, itemObjects?.GetValueOrDefault(item.Id) ?? 0),
                // -1 selects the item's authored model rather than the skin registry's entry zero.
                new PendulumIntegerValue(WeaponProtocol.WeaponVisualIndexField, Signed(-1)),
                new PendulumIntegerValue(WeaponProtocol.WeaponItemCountField, Signed(1)),
                new PendulumIntegerValue(WeaponProtocol.WeaponDurabilityField, (ulong)(item.Durability * WeaponProtocol.DurabilityMaximum)),
                new PendulumIntegerValue(WeaponProtocol.WeaponFlagsField, WeaponProtocol.ConsumeAmmoMask | WeaponProtocol.BlockSprintMask | WeaponProtocol.PlayFiringEffectsMask),
                new PendulumIntegerValue(WeaponProtocol.WeaponEquipTimeField, Units(FerroDefaults.EquipSeconds)),
                new PendulumIntegerValue(WeaponProtocol.WeaponUnequipTimeField, Units(FerroDefaults.UnequipSeconds)),
                new PendulumIntegerValue(WeaponProtocol.WeaponReloadTimeField, Units(FerroDefaults.ReloadSeconds)),
                new PendulumIntegerValue(WeaponProtocol.WeaponZoomTimeField, Units(FerroDefaults.ZoomSeconds)),
                new PendulumIntegerValue(WeaponProtocol.WeaponAmmoField, (ulong)item.AmmoInClip),
                new PendulumIntegerValue(WeaponProtocol.WeaponReadyTimeField, Signed(checked(remaining * PawnBlackboard.TimePerTick))),
                new PendulumIntegerValue(WeaponProtocol.WeaponShotCountField, Signed(gun.Shots)),
                new PendulumIntegerValue(WeaponProtocol.WeaponAmmoIndexField, WeaponProtocol.HeavyAmmoIndex),
                new PendulumIntegerValue(WeaponProtocol.WeaponClipSizeField, FerroDefaults.ClipSize),
                new PendulumIntegerValue(WeaponProtocol.WeaponStateField, (ulong)gun.State),
                new PendulumIntegerValue(WeaponProtocol.WeaponFiringModeField, FerroDefaults.FiringMode),
                new PendulumIntegerValue(WeaponProtocol.WeaponReloadStepField, WeaponProtocol.NoReloadStep),
                new PendulumIntegerValue(WeaponProtocol.WeaponFireRateField, (ulong)Math.Round(FerroDefaults.FireRate * WeaponProtocol.FireRateSteps / WeaponProtocol.FireRateMaximum)),
                new PendulumIntegerValue(WeaponProtocol.WeaponQualityField, WeaponProtocol.QualityBias),
                // The client indexes one state per authored mod slot of the linked item.
                new PendulumIntegerValue(WeaponProtocol.WeaponModSlotCountField, FerroDefaults.ModSlotCount),
                new PendulumArrayValue(WeaponProtocol.WeaponModSlotStatesField,
                    [.. Enumerable.Range(0, FerroDefaults.ModSlotCount).Select(entry => new PendulumArrayElement(entry,
                        [new PendulumIntegerValue(0, WeaponProtocol.EmptyModSlot)]))]),
            ]));
        }

        return result;

        void Set(int field, ulong value)
        {
            values.RemoveAll(item => item.Field == field);
            values.Add(new PendulumIntegerValue(field, value));
        }
    }

    /// <summary>Starts putting the held item away, or retargets a switch that is still doing so.</summary>
    private void Request(Guid wanted, uint frame)
    {
        if (_switching && frame < _putAwayEnd)
        {
            _switchTo = wanted;
            return;
        }

        int from = Handle(_selected);
        if (Handle(wanted) == from)
        {
            return;
        }

        if (Active(Equipped(), _selected) is { } held && _guns.TryGetValue(held.Id, out Gun? gun))
        {
            gun.State = WeaponProtocol.IdleState;
            gun.AutoReload = false;
        }

        _putAway = _selected;
        _switchTo = wanted;
        _switching = true;
        _putAwayEnd = frame + Frames(Times(from).Unequip);
        Version++;
    }

    /// <summary>First frame after the new item is fully drawn; drawing starts on the frame the put-away ends.</summary>
    private uint DrawEnd => _putAwayEnd + Frames(Times(Handle(_switchTo)).Equip);

    /// <summary>The switch state while changing items: item in hand, target, last item put away, phase and frames left.</summary>
    /// <remarks>Before any switch the item put away is the one in hand.</remarks>
    private (int Current, int Target, int PutAway, int Phase, uint Left) Switch(uint frame)
    {
        int putAway = Handle(_putAway);
        if (!_switching || frame >= DrawEnd)
        {
            return (Handle(_selected), WeaponProtocol.NoItemHandle, putAway, WeaponProtocol.EquipPhaseIdle, 0);
        }

        return frame < _putAwayEnd
            ? (putAway, Handle(_switchTo), putAway, WeaponProtocol.EquipPhaseUnequipping, _putAwayEnd - frame)
            : (Handle(_switchTo), WeaponProtocol.NoItemHandle, putAway, WeaponProtocol.EquipPhaseEquipping, DrawEnd - frame);
    }

    /// <summary>Inventory node handle of an item in hand; no choice means the first carried gun, else empty hands.</summary>
    private int Handle(Guid? item)
    {
        ContainerItem?[] equipped = Equipped();
        int held = Active(equipped, item) is { } gun ? Array.IndexOf(equipped, gun) : -1;
        return held >= 0 ? WeaponProtocol.FirstWeaponSlot + held
            : item is { } chosen && Hand(InventoryBlackboard.MeleeSlot)?.Id == chosen ? HandItems.RaiderToolHandle
            : WeaponProtocol.UnarmedHandle;
    }

    /// <summary>Authored put-away and draw durations of the item behind a handle.</summary>
    private static (double Unequip, double Equip) Times(int handle) => handle switch
    {
        HandItems.RaiderToolHandle => (HandItems.RaiderToolUnequipSeconds, HandItems.RaiderToolEquipSeconds),
        WeaponProtocol.UnarmedHandle => (HandItems.UnarmedUnequipSeconds, HandItems.UnarmedEquipSeconds),
        _ => (FerroDefaults.UnequipSeconds, FerroDefaults.EquipSeconds),
    };

    /// <summary>The gun in hand: the selected one; else, unless the Raider Tool or empty hands were chosen, the first carried.</summary>
    private ContainerItem? Active(ContainerItem?[] equipped, Guid? selected) =>
        equipped.FirstOrDefault(item => item is not null && item.Id == selected)
        ?? (selected is { } chosen && (Hand(InventoryBlackboard.MeleeSlot)?.Id == chosen || Hand(InventoryBlackboard.UnarmedSlot)?.Id == chosen)
            ? null
            : equipped.FirstOrDefault(item => item is not null));

    private ContainerItem? Hand(int slot) => inventory.Containers[inventory.Loadout].Items[slot];

    private ContainerItem?[] Equipped() => [.. inventory.Containers[inventory.Loadout].Items.Take(2)
        .Select(item => item?.AssetIndex == WeaponItems.FerroIUsableIndex ? item : null)];

    private Gun Get(Guid id)
    {
        if (_guns.TryGetValue(id, out Gun? existing))
        {
            return existing;
        }

        Gun gun = new(id);
        _guns.Add(id, gun);

        return gun;
    }

    // Client timers count simulation time units (TimePerTick per frame); server deadlines count input frames.
    private static ulong Units(double seconds) => checked((ulong)Math.Round(seconds * PawnBlackboard.TimePerTick / PawnBlackboard.TickSeconds));
    private static uint Frames(double seconds) => checked((uint)((Units(seconds) + PawnBlackboard.TimePerTick - 1) / PawnBlackboard.TimePerTick));
    private static ulong Signed(int value) => (uint)value ^ 0x80000000U;
}
