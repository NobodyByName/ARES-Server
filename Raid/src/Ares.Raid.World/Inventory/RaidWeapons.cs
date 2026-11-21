using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Raid.World.Movement;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Pendulum;

namespace Ares.Raid.World.Inventory;

/// <summary>
/// Server-owned choice of the item in hand, plus Ferro chambers, fire cadence, reload transfers and gadget power.
/// </summary>
/// <param name="inventory">The player's inventory, with the raid's item rules.</param>
/// <param name="data">The game data: the snap hook item's activation cost.</param>
public sealed class RaidWeapons(RaidInventory inventory, GameDataSet data)
{
    // A hook that comes back within its flight (out and back) hit nothing or was called back, and gives its power back.
    private static readonly uint s_flightFrames = Frames(SnapHookParameters.ExtendSeconds + SnapHookParameters.PullbackSeconds);
    private static readonly uint s_rechargePauseFrames = Frames(QuickItemProtocol.PowerRechargePauseSeconds);

    private readonly GameDataSet _data = data ?? throw new ArgumentNullException(nameof(data));
    private readonly RaidItems _items = inventory.Items;

    private sealed class Gun(Guid id)
    {
        internal Guid Id { get; } = id;
        internal uint ReadyAt { get; set; }
        internal int State { get; set; }
        internal int Shots { get; set; }
        internal bool AutoReload { get; set; }
    }

    /// <summary>A quick-use item's node in the character state: its slot (the item's handle), page and lifetime in frames.</summary>
    private sealed class ItemNode(Guid item, int asset, int slot, int position, uint from)
    {
        internal Guid Item { get; } = item;
        internal int Asset { get; } = asset;
        internal int Slot { get; } = slot;
        internal int Position { get; } = position;
        internal uint From { get; } = from;
        internal uint? To { get; set; }

        internal bool Live(uint frame) => frame >= From && (To is not { } end || frame < end);
    }

    /// <summary>An item the inventory state names, with its node when it is a quick-use item; no item means the first carried gun.</summary>
    private readonly record struct Held(Guid? Item, ItemNode? Node = null);

    /// <summary>The inventory state's switch fields at one frame.</summary>
    private readonly record struct HandState(Held Current, Held? Target, Held PutAway, Guid? Offhand, Guid? LastWanted, int Phase, ulong Left);

    /// <summary>
    /// One step of a switch: the item in hand is put away over its put-away time, then the next one is drawn over its draw
    /// time. Each phase lasts at least one frame (a zero time shows the phase for one frame); the frame after the draw is idle.
    /// </summary>
    private sealed record Leg(uint Start, Held From, Held To, Held Target, ulong UnequipUnits, ulong EquipUnits, Guid? OffhandPuttingAway,
        Guid? OffhandAfter, Guid? LastWanted)
    {
        internal uint DrawStart => Start + Ticks(UnequipUnits);
        internal uint End => DrawStart + Ticks(EquipUnits);
    }

    /// <summary>A switch plan from <paramref name="Start"/>: the state it starts from, then its legs.</summary>
    private sealed record Era(uint Start, HandState Settled, IReadOnlyList<Leg> Legs)
    {
        /// <summary>The last leg, or <see langword="null"/> while the hand is settled.</summary>
        public Leg? LastLeg => Legs.Count == 0 ? null : Legs[^1];
    }

    private readonly Dictionary<Guid, Gun> _guns = [];
    private readonly List<Era> _eras = [new(0, new HandState(new Held(null), null, new Held(null), null, null, WeaponProtocol.EquipPhaseIdle, 0), [])];
    private readonly List<ItemNode> _nodes = [];
    private readonly Dictionary<int, (int Position, uint From)> _itemSlots = [];
    private readonly Dictionary<uint, double> _powerByFrame = [];
    private readonly Dictionary<uint, byte> _changeByFrame = [];
    private Guid? _queued;
    private Guid? _active;
    private Guid? _selected;
    private bool _switching;
    private byte _handledEquipChange;
    private bool _fireDown;
    private bool _reloadDown;
    private uint _frame;
    private uint _firedFrame;
    private (int, int, int, int, int, int, int) _signature;
    private double _unpausedPower = SnapHookParameters.PowerMaximum;
    private (uint Thrown, uint? Released)? _hookOut;

    public uint Version { get; private set; }
    public int ShotsFired { get; private set; }
    public int ReloadsCompleted { get; private set; }

    /// <summary>Accepted requests to draw a quick-use item.</summary>
    public int QuickItemRequests { get; private set; }

    /// <summary>Snap Hook throws: each costs gadget power.</summary>
    public int HookThrows { get; private set; }

    /// <summary>Gadget power after the latest applied input (0 to <see cref="SnapHookParameters.PowerMaximum"/>).</summary>
    public double GadgetPower { get; private set; } = SnapHookParameters.PowerMaximum;

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
            HandState hand = State(_frame);
            HeldItemInput held = new(Armed ? HeldItemMovementParameters.Default : HeldItemMovementParameters.Unarmed)
            {
                Changing = _switching,
                SnapHookReady = hand.Current.Node?.Asset == _items.SnapHookIndex && hand.Phase != WeaponProtocol.EquipPhaseUnequipping,
            };
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
    /// <param name="equipBlocked">
    /// Whether the character's movement blocks equipping (ladder, roll, vault, heavy landing). The request then stays
    /// unhandled, as the client's inventory node leaves it, and goes through on the first frame the block has ended.
    /// </param>
    public void Apply(WeaponInput input, IReadOnlyDictionary<Guid, ulong>? itemObjects = null, bool equipBlocked = false)
    {
        if (input.Frame <= _frame)
        {
            return;
        }

        // A raid starts with the gun already in hand, so the first draw has no equip time.
        bool atSpawn = _frame == 0;
        uint previous = _frame;
        uint frame = input.Frame;
        _frame = frame;
        for (uint skipped = previous + 1; previous != 0 && skipped < frame; skipped++)
        {
            Recharge(skipped);
        }

        DropQuickItemsOutsideTheBelt(frame);
        StartQueuedSwitch(frame);
        if (input.EquipChange != _handledEquipChange && !equipBlocked)
        {
            // The client names the item it wants in hand: a firearm, the Raider Tool, empty hands or a quick-use item
            // in the belt. Other targets keep the current item.
            _handledEquipChange = input.EquipChange;
            Guid? target = itemObjects?.Where(entry => entry.Value == input.EquipTarget).Select(entry => (Guid?)entry.Key).FirstOrDefault();
            switch (target)
            {
                case { } wanted when Equipped().Any(item => item?.Id == wanted) || Hand(InventoryBlackboard.MeleeSlot)?.Id == wanted
                    || Hand(InventoryBlackboard.UnarmedSlot)?.Id == wanted:
                    Request(wanted, frame);
                    break;
                case { } quick when IsQuick(quick):
                    QuickItemRequests++;
                    Request(quick, frame);
                    break;
            }

            Version++;
        }

        HandState hand = State(frame);
        _selected = hand.Current.Item;
        _switching = frame < Latest.LastLeg?.End || _queued is not null;
        UseGadget(input, frame, hand);
        ContainerItem? equipped = Active(Equipped(), _selected);
        if (_active != equipped?.Id)
        {
            if (_active is { } old && _guns.TryGetValue(old, out Gun? previousGun))
            {
                previousGun.State = WeaponProtocol.IdleState;
                previousGun.AutoReload = false;
            }

            _active = equipped?.Id;
            if (equipped is not null)
            {
                Gun next = Get(equipped.Id);
                next.ReadyAt = atSpawn || !_switching ? 0 : CurrentLeg(frame)?.End ?? 0;
                next.State = WeaponProtocol.IdleState;
            }

            Version++;
        }

        if (equipped is not null && !_switching)
        {
            Gun gun = Get(equipped.Id);
            if (gun.State == WeaponProtocol.ReloadingState && frame >= gun.ReadyAt)
            {
                ContainerItem loading = Equipped().First(item => item?.Id == gun.Id)!;
                if (inventory.SetChamber(gun.Id, Math.Min(_items.FerroClipSize, loading.AmmoInClip + inventory.HeavyAmmo), consumeReserve: true))
                {
                    ReloadsCompleted++;
                }

                gun.State = WeaponProtocol.IdleState;
                Version++;
            }

            if (gun.State != WeaponProtocol.ReloadingState && frame >= gun.ReadyAt)
            {
                ContainerItem current = Equipped().First(item => item?.Id == gun.Id)!;
                bool trigger = input is { AllowPrimary: true, FireBlocked: false, FireDown: true } && (!_fireDown || input.FirePressed);
                if (trigger && current is { Durability: > 0, AmmoInClip: > 0 })
                {
                    inventory.SetChamber(gun.Id, 0);
                    gun.ReadyAt = frame + Frames(60 / _items.FerroIFirearm.FireRate);
                    gun.Shots++;
                    gun.State = WeaponProtocol.FiringState;
                    gun.AutoReload = true;
                    _firedFrame = frame;
                    ShotsFired++;
                    Version++;
                }
                else if (current.AmmoInClip == 0 && inventory.HeavyAmmo > 0
                    && ((input.ReloadDown && !_reloadDown) || trigger || gun.AutoReload))
                {
                    gun.State = WeaponProtocol.ReloadingState;
                    gun.ReadyAt = frame + Frames(_items.FerroReloadSeconds);
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

        // Each change of the switch fields (a phase, an item in hand, a node) gets a frame.
        (int, int, int, int, int, int, int) signature = Signature(frame, hand);
        if (signature != _signature)
        {
            _signature = signature;
            Version++;
        }

        _changeByFrame[frame] = _handledEquipChange;
        _changeByFrame.Remove(frame - 64);
        _fireDown = input.FireDown;
        _reloadDown = input.ReloadDown;
    }

    /// <summary>
    /// Whether no frame may be sent for this tick: a thrown Snap Hook is out (its flight and pull are the client's own
    /// state until it is cleared), or the inventory node would name an item without a node in the character state.
    /// </summary>
    /// <param name="frame">The frame's tick.</param>
    public bool HoldsFrames(uint frame) =>
        (_hookOut is { } hook && frame >= hook.Thrown && (hook.Released is not { } released || frame < released)) || !Describable(frame);

    /// <summary>
    /// Whether a frame can describe the tick: the inventory node names only items whose nodes the character state has.
    /// Frames for other clients need only this.
    /// </summary>
    /// <param name="frame">The frame's tick.</param>
    public bool Describable(uint frame) => Named(frame, State(frame));

    /// <summary>Weapon and item nodes and their references in the inventory simulation node.</summary>
    /// <exception cref="InvalidOperationException">No frame may be sent for this tick (<see cref="HoldsFrames"/>).</exception>
    public IReadOnlyList<PendulumVariable> Variables(uint frame, IReadOnlyDictionary<Guid, ulong>? itemObjects = null) =>
        HoldsFrames(frame)
            ? throw new InvalidOperationException("The inventory node would name an item without its node in the character state")
            : Nodes(frame, itemObjects, null);

    /// <summary>
    /// Weapon and item nodes as other clients see them: without item links, and with the Snap Hook's node carrying the
    /// hook's state (thrown, holding on, its target), which other clients draw the hook and its cable from. Unlike the
    /// owner's frames, these go out while a hook is out.
    /// </summary>
    /// <param name="frame">The frame's tick.</param>
    /// <param name="hook">The character's Snap Hook use after the tick.</param>
    /// <exception cref="InvalidOperationException">No frame may describe this tick (<see cref="Describable"/>).</exception>
    public IReadOnlyList<PendulumVariable> ObservedVariables(uint frame, SnapHookUse hook) =>
        Describable(frame)
            ? Nodes(frame, null, hook)
            : throw new InvalidOperationException("The inventory node would name an item without its node in the character state");

    private List<PendulumVariable> Nodes(uint frame, IReadOnlyDictionary<Guid, ulong>? itemObjects, SnapHookUse? hook)
    {
        HandState hand = State(frame);
        PendulumVariable template = PawnBlackboard.InitialState.Single(item => item.Slot == WeaponProtocol.InventorySlot);
        List<PendulumValue> values = [.. template.Values];
        ContainerItem?[] equipped = Equipped();
        int main = equipped[0] is null ? WeaponProtocol.InvalidHandle : WeaponProtocol.FirstWeaponSlot;
        int secondary = equipped[1] is null ? WeaponProtocol.InvalidHandle : WeaponProtocol.FirstWeaponSlot + 1;
        Set(WeaponProtocol.InventoryPrimaryField, (ulong)main);
        Set(WeaponProtocol.InventorySecondaryField, (ulong)secondary);
        Set(WeaponProtocol.InventoryCurrentField, (ulong)Handle(hand.Current));
        Set(WeaponProtocol.InventoryTargetField, hand.Target is { } target ? (ulong)Handle(target) : WeaponProtocol.NoItemHandle);
        Set(WeaponProtocol.InventoryUnequipFromField, (ulong)Handle(hand.PutAway));
        // An empty offhand is the no-item handle (254); the invalid handle makes the client draw the Raider Tool.
        Set(WeaponProtocol.InventoryOffhandField, hand.Offhand is { } offhand ? (ulong)Handle(offhand) : WeaponProtocol.NoItemHandle);
        Set(WeaponProtocol.InventoryEquipPhaseField, (ulong)hand.Phase);
        Set(WeaponProtocol.InventoryEquipTimerField, Signed(checked((int)hand.Left)));
        Set(WeaponProtocol.InventoryLastWantedField, (ulong)Handle(hand.LastWanted));
        Set(WeaponProtocol.InventoryEquipChangeField, _changeByFrame.GetValueOrDefault(frame, _handledEquipChange));
        Set(QuickItemProtocol.GadgetPowerField, PowerValue(_powerByFrame.GetValueOrDefault(frame, GadgetPower)));
        Set(WeaponProtocol.ReserveCountField, WeaponProtocol.AmmoTypeCount);
        values.RemoveAll(value => value.Field == WeaponProtocol.ReserveArrayField);
        values.Add(new PendulumArrayValue(WeaponProtocol.ReserveArrayField,
            [.. Enumerable.Range(0, WeaponProtocol.AmmoTypeCount).Select(index => new PendulumArrayElement(index,
                [new PendulumIntegerValue(0, Signed(index == WeaponProtocol.HeavyAmmoIndex ? inventory.HeavyAmmo : 0))]))]));
        List<PendulumVariable> result = [template with { Values = values }];
        // The Raider Tool and empty hands nodes link their item objects once the client knows them.
        foreach ((int slot, int handle, int field) in ((int, int, int)[])[(InventoryBlackboard.MeleeSlot, HandItemFields.RaiderToolHandle, HandItemFields.RaiderToolItemInstanceField),
            (InventoryBlackboard.UnarmedSlot, WeaponProtocol.UnarmedHandle, HandItemFields.UnarmedItemInstanceField)])
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
                new PendulumIntegerValue(WeaponProtocol.WeaponEquipTimeField, Units(_items.FerroI.EquipSeconds)),
                new PendulumIntegerValue(WeaponProtocol.WeaponUnequipTimeField, Units(_items.FerroI.UnequipSeconds)),
                new PendulumIntegerValue(WeaponProtocol.WeaponOffhandEquipTimeField, Units(_items.FerroI.ReEquipFromItemSeconds)),
                new PendulumIntegerValue(WeaponProtocol.WeaponOffhandUnequipTimeField, Units(_items.FerroI.UnequipToItemsSeconds)),
                new PendulumIntegerValue(WeaponProtocol.WeaponReloadTimeField, Units(_items.FerroReloadSeconds)),
                new PendulumIntegerValue(WeaponProtocol.WeaponZoomTimeField, Units(_items.FerroIFirearm.ZoomSeconds)),
                new PendulumIntegerValue(WeaponProtocol.WeaponAmmoField, (ulong)item.AmmoInClip),
                new PendulumIntegerValue(WeaponProtocol.WeaponReadyTimeField, Signed(checked(remaining * PawnBlackboard.TimePerTick))),
                new PendulumIntegerValue(WeaponProtocol.WeaponShotCountField, Signed(gun.Shots)),
                new PendulumIntegerValue(WeaponProtocol.WeaponAmmoIndexField, WeaponProtocol.HeavyAmmoIndex),
                new PendulumIntegerValue(WeaponProtocol.WeaponClipSizeField, checked((ulong)_items.FerroClipSize)),
                new PendulumIntegerValue(WeaponProtocol.WeaponStateField, (ulong)gun.State),
                new PendulumIntegerValue(WeaponProtocol.WeaponFiringModeField, FerroProtocol.FiringMode),
                new PendulumIntegerValue(WeaponProtocol.WeaponReloadStepField, WeaponProtocol.NoReloadStep),
                new PendulumIntegerValue(WeaponProtocol.WeaponFireRateField, (ulong)Math.Round(_items.FerroIFirearm.FireRate * WeaponProtocol.FireRateSteps / WeaponProtocol.FireRateMaximum)),
                new PendulumIntegerValue(WeaponProtocol.WeaponQualityField, WeaponProtocol.QualityBias),
                // The client indexes one state per authored mod slot of the linked item.
                new PendulumIntegerValue(WeaponProtocol.WeaponModSlotCountField, checked((ulong)_items.FerroModSlotCount)),
                new PendulumArrayValue(WeaponProtocol.WeaponModSlotStatesField,
                    [.. Enumerable.Range(0, _items.FerroModSlotCount).Select(entry => new PendulumArrayElement(entry,
                        [new PendulumIntegerValue(0, WeaponProtocol.EmptyModSlot)]))]),
            ]));
        }

        // A quick-use item in hand, or being drawn or put away, has its own node.
        result.AddRange(_nodes.Where(node => node.Live(frame)).Select(node => new PendulumVariable(node.Slot, node.Position,
            QuickItemProtocol.ItemNodePages, QuickItemProtocol.ItemNodeDescriptor,
            [
                new PendulumIntegerValue(QuickItemProtocol.ItemNodeDataField, Signed(QuickItemProtocol.ItemNodeDataId)),
                new PendulumIntegerValue(QuickItemProtocol.ItemNodeAssetField, (ulong)node.Asset),
                new PendulumIntegerValue(QuickItemProtocol.ItemNodeDurabilityField,
                    (ulong)((Belt(node.Item)?.Durability ?? 1) * WeaponProtocol.DurabilityMaximum)),
                new PendulumIntegerValue(QuickItemProtocol.ItemNodeItemInstanceField, itemObjects?.GetValueOrDefault(node.Item) ?? 0),
                .. hook is { } use && node.Asset == _items.SnapHookIndex ? use.ItemNodeValues(node.From) : [],
            ])));

        return result;

        void Set(int field, ulong value)
        {
            values.RemoveAll(item => item.Field == field);
            values.Add(new PendulumIntegerValue(field, value));
        }
    }

    /// <summary>
    /// Slots an earlier quick-use item node laid out that hold no node at this tick: the client keeps their place in the
    /// layout (a later node reuses the lowest free one).
    /// </summary>
    public IReadOnlyList<PendulumSlot> EmptyItemSlots(uint frame)
    {
        HashSet<int> taken = [.. FixedLayout().Select(entry => entry.Slot), .. _nodes.Where(node => node.Live(frame)).Select(node => node.Slot)];
        return [.. _itemSlots.Where(entry => entry.Value.From <= frame && !taken.Contains(entry.Key)).OrderBy(entry => entry.Key)
            .Select(entry => new PendulumSlot(entry.Key, entry.Value.Position, QuickItemProtocol.ItemNodePages, null, []))];
    }

    private Era Latest => _eras[^1];

    /// <summary>The switch fields at a frame, from the latest plan that had started by then.</summary>
    private HandState State(uint frame)
    {
        Era era = _eras.LastOrDefault(entry => entry.Start <= frame) ?? _eras[0];
        if (era.Legs.LastOrDefault(entry => entry.Start <= frame) is not { } leg)
        {
            return era.Settled;
        }

        Held? shown = leg.To.Item == leg.Target.Item && leg.To.Node == leg.Target.Node ? null : leg.Target;
        if (frame < leg.DrawStart)
        {
            return new HandState(leg.From, leg.Target, leg.From, leg.OffhandPuttingAway, leg.LastWanted, WeaponProtocol.EquipPhaseUnequipping,
                leg.UnequipUnits - ((ulong)(frame - leg.Start) * PawnBlackboard.TimePerTick));
        }

        return frame < leg.End
            ? new HandState(leg.To, shown, leg.From, leg.OffhandAfter, leg.LastWanted, WeaponProtocol.EquipPhaseEquipping,
                leg.EquipUnits - ((ulong)(frame - leg.DrawStart) * PawnBlackboard.TimePerTick))
            : new HandState(leg.To, shown, leg.From, leg.OffhandAfter, leg.LastWanted, WeaponProtocol.EquipPhaseIdle, 0);
    }

    /// <summary>The leg under way at a frame (started and not yet idle), if any.</summary>
    private Leg? CurrentLeg(uint frame) => Latest.Legs.LastOrDefault(leg => leg.Start <= frame && frame < leg.End);

    /// <summary>Starts a switch, retargets one that is still putting a plain item away, or queues the request behind it.</summary>
    private void Request(Guid wanted, uint frame)
    {
        HandState now = State(frame);
        if (frame < Latest.LastLeg?.End)
        {
            Leg? leg = CurrentLeg(frame);
            if (leg is not null && Latest.Legs.Count == 1 && frame < leg.DrawStart && leg.OffhandPuttingAway is null && leg.From.Node is null
                && leg.To.Node is null && !IsQuick(wanted))
            {
                _eras[^1] = Latest with { Legs = [leg with { To = new Held(wanted), Target = new Held(wanted), EquipUnits = Units(Times(wanted).Equip) }] };
                _queued = null;
                return;
            }

            _queued = wanted;
            return;
        }

        _queued = null;
        if ((now.Current.Item ?? DefaultItem()) == wanted)
        {
            return;
        }

        Plan(now, wanted, frame);
    }

    /// <summary>
    /// Plans a switch from the settled state at <paramref name="start"/>. A firearm or hand item stays the offhand while a
    /// quick-use item is in hand; putting the quick-use item away draws the offhand back first, then switches on.
    /// </summary>
    private void Plan(HandState now, Guid wanted, uint start)
    {
        if (Active(Equipped(), now.Current.Item) is { } held && _guns.TryGetValue(held.Id, out Gun? gun))
        {
            gun.State = WeaponProtocol.IdleState;
            gun.AutoReload = false;
        }

        Guid current = now.Current.Item ?? DefaultItem();
        List<Leg> legs = [];
        if (now.Current.Node is { } node)
        {
            Guid offhand = now.Offhand ?? DefaultUnarmed();
            Guid target = IsQuick(wanted) ? offhand : wanted;
            Leg restore = new(start, now.Current, new Held(offhand), new Held(target), Units(QuickUse(node.Asset).UnequipSeconds), Units(ReEquipSeconds(offhand)),
                now.Offhand, null, target);
            legs.Add(restore);
            if (IsQuick(wanted))
            {
                _queued = wanted;
            }
            else if (wanted != offhand)
            {
                legs.Add(Draw(offhand, wanted, restore.End + 1));
            }
        }
        else
        {
            legs.Add(Draw(current, wanted, start));
        }

        // A node lives while the inventory node names it: from its leg's start until a later leg puts something else away.
        foreach (Leg leg in legs)
        {
            foreach (ItemNode live in _nodes.Where(item => item.To is null && item != leg.From.Node && item != leg.To.Node && item.From <= leg.Start))
            {
                live.To = leg.Start;
            }
        }

        _eras.Add(new Era(start, now, legs));
        if (_eras.Count > 8)
        {
            _eras.RemoveAt(0);
        }
    }

    /// <summary>One leg from a firearm or hand item to the wanted item; a quick-use item gets a new node.</summary>
    private Leg Draw(Guid from, Guid wanted, uint start)
    {
        if (Belt(wanted) is not { } quick || _items.Traits.Of(quick.AssetIndex).QuickUse is not { } times)
        {
            return new Leg(start, new Held(from), new Held(wanted), new Held(wanted), Units(Times(from).Unequip), Units(Times(wanted).Equip), null, null, from);
        }

        Held drawn = new(wanted, Allocate(wanted, quick.AssetIndex, start));
        return new Leg(start, new Held(from), drawn, drawn, Units(UnequipToItemsSeconds(from)), Units(times.EquipSeconds), from, from, from);
    }

    /// <summary>
    /// A new node for a quick-use item: the lowest slot after the other variables that no node holds (an emptied slot keeps
    /// its page), else a new slot on the pages after every other variable.
    /// </summary>
    private ItemNode Allocate(Guid item, int asset, uint frame)
    {
        List<(int Slot, int Position, int Size)> fixedLayout = FixedLayout();
        int slot = fixedLayout.Max(entry => entry.Slot) + 1;
        while (_nodes.Any(node => node.Slot == slot && (node.To is null || node.Live(frame))) || fixedLayout.Any(entry => entry.Slot == slot))
        {
            slot++;
        }

        if (!_itemSlots.TryGetValue(slot, out (int Position, uint From) laidOut))
        {
            int position = Math.Max(fixedLayout.Max(entry => entry.Position + entry.Size),
                _itemSlots.Count == 0 ? 0 : _itemSlots.Values.Max(entry => entry.Position + QuickItemProtocol.ItemNodePages));
            laidOut = (position, frame);
            _itemSlots[slot] = laidOut;
        }

        _nodes.RemoveAll(node => node.To is { } end && end + 64 < frame);
        ItemNode created = new(item, asset, slot, laidOut.Position, frame);
        _nodes.Add(created);
        return created;
    }

    /// <summary>
    /// A quick-use item that left the belt ends whatever named it: in hand it leaves empty hands; put away or being drawn,
    /// the item in hand stays. Its node is gone from this frame.
    /// </summary>
    private void DropQuickItemsOutsideTheBelt(uint frame)
    {
        if (_queued is { } queued && Belt(queued) is null && inventory.Containers[inventory.Loadout].Items.All(item => item?.Id != queued))
        {
            _queued = null;
        }

        List<ItemNode> gone = [.. _nodes.Where(node => (node.To is null || node.Live(frame)) && Belt(node.Item) is null)];
        if (gone.Count == 0)
        {
            return;
        }

        HandState now = State(frame);
        Held current = now.Current.Node is { } held && gone.Contains(held) ? new Held(DefaultUnarmed()) : now.Current;
        Held putAway = now.PutAway.Node is { } away && gone.Contains(away) ? current : now.PutAway;
        HandState settled = new(current, null, putAway, current.Node is null ? null : now.Offhand, now.LastWanted, WeaponProtocol.EquipPhaseIdle, 0);
        foreach (ItemNode node in _nodes.Where(node => node.To is null || node.To > frame))
        {
            if (node != settled.Current.Node && node != settled.PutAway.Node)
            {
                node.To = frame;
            }
        }

        _eras.Add(new Era(frame, settled, []));
        Version++;
    }

    /// <summary>A request that arrived during a switch starts the frame after that switch is over.</summary>
    private void StartQueuedSwitch(uint frame)
    {
        uint start = Latest.LastLeg is { } last ? last.End + 1 : frame;
        if (_queued is not { } wanted || start > frame)
        {
            return;
        }

        _queued = null;
        HandState now = State(start);
        if (now.Current.Item is { } held && held == wanted)
        {
            return;
        }

        Plan(now, wanted, start);
    }

    /// <summary>
    /// Gadget power and Snap Hook throws. A throw costs power on the frame the client throws (the hook in hand, its
    /// primary pressed and allowed); power recharges each frame up to full. The hook's flight and pull are the client's: it
    /// allows its next use once the hook is cleared and power is back above the cost, which ends the throw here. A hook
    /// cleared within its flight gives the power back (and recharging stopped from the pause time until then); one that held
    /// on was cleared once power was back.
    /// </summary>
    private void UseGadget(WeaponInput input, uint frame, HandState hand)
    {
        if (_hookOut is { Released: null } hook && frame > hook.Thrown && (input.AllowPrimary || hand.Current.Node is null))
        {
            GadgetPower = frame - 1 - hook.Thrown <= s_flightFrames
                ? Math.Min(SnapHookParameters.PowerMaximum, GadgetPower + _data.Movement.SnapHookItem.ActivationCost)
                : PowerWhenUsableAgain(_unpausedPower);
            _unpausedPower = GadgetPower;
            _powerByFrame[frame - 1] = GadgetPower;
            _hookOut = hook with { Released = frame };
            Version++;
        }

        if (_hookOut is not { Released: null } && hand.Current.Node is not null && hand.Phase != WeaponProtocol.EquipPhaseUnequipping
            && input is { FirePressed: true, AllowPrimary: true, FireBlocked: false } && SnapHookThrow.CanThrow(_data, SnapHookHook.Stowed, GadgetPower))
        {
            GadgetPower = SnapHookThrow.PowerAfterThrow(_data, GadgetPower);
            _unpausedPower = GadgetPower;
            _hookOut = (frame, null);
            HookThrows++;
            Version++;
        }

        Recharge(frame);
    }

    /// <summary>
    /// Power on the frame before the client allowed the next use after a hook that held on: the first value of its
    /// recharge at or above the cost. Raid's recharge, which does not know when the hook stopped recharging, is ahead by
    /// whole frames of it; much further ahead, the hook was cleared after power was already back.
    /// </summary>
    private double PowerWhenUsableAgain(double unpaused)
    {
        const double Step = SnapHookParameters.PowerRechargePerSecond * PawnBlackboard.TickSeconds;
        double cost = _data.Movement.SnapHookItem.ActivationCost;
        double excess = unpaused - cost;
        return excess < 0 ? cost : excess > Step * s_flightFrames ? unpaused : cost + (excess % Step);
    }

    /// <summary>One frame of gadget power recharge, paused for a thrown hook from the pause time on.</summary>
    private void Recharge(uint frame)
    {
        _unpausedPower = Math.Min(SnapHookParameters.PowerMaximum, SnapHookThrow.RechargePower(_unpausedPower, PawnBlackboard.TickSeconds));
        if (_hookOut is not { Released: null } hook || frame < hook.Thrown + s_rechargePauseFrames)
        {
            GadgetPower = Math.Min(SnapHookParameters.PowerMaximum, SnapHookThrow.RechargePower(GadgetPower, PawnBlackboard.TickSeconds));
        }

        _powerByFrame[frame] = GadgetPower;
        _powerByFrame.Remove(frame - 64);
    }

    private static ulong PowerValue(double power) =>
        (ulong)Math.Round(power / SnapHookParameters.PowerMaximum * QuickItemProtocol.GadgetPowerMaximum, MidpointRounding.AwayFromZero);

    /// <summary>Whether every quick-use item the frame's inventory node names has its node at that frame.</summary>
    private bool Named(uint frame, HandState hand)
    {
        HashSet<int> fixedSlots = [.. FixedLayout().Select(entry => entry.Slot)];
        return new[] { hand.Current, hand.Target ?? default, hand.PutAway }.All(held => held.Node is { } node
            ? node.Live(frame) && !fixedSlots.Contains(node.Slot)
            : held.Item is not { } item || !IsQuick(item))
            && _nodes.Where(node => node.Live(frame)).All(node => !fixedSlots.Contains(node.Slot));
    }

    private (int, int, int, int, int, int, int) Signature(uint frame, HandState hand) =>
        (Handle(hand.Current), hand.Target is { } target ? Handle(target) : WeaponProtocol.NoItemHandle, Handle(hand.PutAway),
            hand.Offhand is { } offhand ? Handle(offhand) : WeaponProtocol.NoItemHandle, hand.Phase, Handle(hand.LastWanted),
            _nodes.Where(node => node.Live(frame)).Aggregate(0, (mask, node) => mask | (1 << (node.Slot & 31))));

    /// <summary>The other variables' slots and pages: the initial state and the equipped guns.</summary>
    private List<(int Slot, int Position, int Size)> FixedLayout()
    {
        List<(int Slot, int Position, int Size)> layout = [.. PawnBlackboard.InitialState.Select(variable => (variable.Slot, variable.Position, variable.Size))];
        ContainerItem?[] equipped = Equipped();
        for (int index = 0; index < equipped.Length; index++)
        {
            if (equipped[index] is not null)
            {
                layout.Add((WeaponProtocol.FirstWeaponSlot + index, WeaponProtocol.FirstWeaponPosition + index, 1));
            }
        }

        return layout;
    }

    private int Handle(Held held) => held.Node?.Slot ?? Handle(held.Item);

    /// <summary>Inventory node handle of a firearm or hand item; no choice means the first carried gun, else empty hands.</summary>
    private int Handle(Guid? item)
    {
        ContainerItem?[] equipped = Equipped();
        int held = Active(equipped, item) is { } gun ? Array.IndexOf(equipped, gun) : -1;
        return held >= 0 ? WeaponProtocol.FirstWeaponSlot + held
            : item is { } chosen && Hand(InventoryBlackboard.MeleeSlot)?.Id == chosen ? HandItemFields.RaiderToolHandle
            : WeaponProtocol.UnarmedHandle;
    }

    /// <summary>Authored put-away and draw durations of a firearm or hand item.</summary>
    private (double Unequip, double Equip) Times(Guid item) => Handle(item) switch
    {
        HandItemFields.RaiderToolHandle => (_items.RaiderTool.UnequipSeconds, _items.RaiderTool.EquipSeconds),
        WeaponProtocol.UnarmedHandle => (_items.Unarmed.UnequipSeconds, _items.Unarmed.EquipSeconds),
        _ => (_items.FerroI.UnequipSeconds, _items.FerroI.EquipSeconds),
    };

    /// <summary>Time to put a firearm or hand item away for a quick-use item (a firearm's from its node's offhand unequip time).</summary>
    private double UnequipToItemsSeconds(Guid item) => Handle(item) switch
    {
        HandItemFields.RaiderToolHandle => _items.RaiderTool.UnequipToItemsSeconds,
        WeaponProtocol.UnarmedHandle => _items.Unarmed.UnequipToItemsSeconds,
        _ => _items.FerroI.UnequipToItemsSeconds,
    };

    /// <summary>Time to draw a firearm or hand item back after a quick-use item (a firearm's from its node's offhand equip time).</summary>
    private double ReEquipSeconds(Guid item) => Handle(item) switch
    {
        HandItemFields.RaiderToolHandle => _items.RaiderTool.ReEquipFromItemSeconds,
        WeaponProtocol.UnarmedHandle => _items.Unarmed.ReEquipFromItemSeconds,
        _ => _items.FerroI.ReEquipFromItemSeconds,
    };

    /// <summary>Whether the item is a quick-use item in the belt.</summary>
    private bool IsQuick(Guid item) => Belt(item) is { } carried && _items.Traits.Of(carried.AssetIndex).QuickUse is not null;

    private QuickUseTimes QuickUse(int asset) => _items.Traits.Of(asset).QuickUse ?? throw new InvalidOperationException("Not a quick-use item");

    private Guid DefaultItem() => Active(Equipped(), null)?.Id ?? DefaultUnarmed();

    private Guid DefaultUnarmed() => Hand(InventoryBlackboard.UnarmedSlot)?.Id ?? Guid.Empty;

    /// <summary>
    /// The gun in hand: the selected one; else, unless the Raider Tool, empty hands or a quick-use item were chosen, the
    /// first carried.
    /// </summary>
    private ContainerItem? Active(ContainerItem?[] equipped, Guid? selected) =>
        equipped.FirstOrDefault(item => item is not null && item.Id == selected)
        ?? (selected is { } chosen && (Hand(InventoryBlackboard.MeleeSlot)?.Id == chosen || Hand(InventoryBlackboard.UnarmedSlot)?.Id == chosen
            || _nodes.Any(node => node.Item == chosen))
            ? null
            : equipped.FirstOrDefault(item => item is not null));

    private ContainerItem? Hand(int slot) => inventory.Containers[inventory.Loadout].Items[slot];

    private ContainerItem? Belt(Guid item) => inventory.Containers[inventory.Belt].Items.FirstOrDefault(entry => entry?.Id == item);

    private ContainerItem?[] Equipped() => [.. inventory.Containers[inventory.Loadout].Items.Take(2)
        .Select(item => item?.AssetIndex == _items.FerroIIndex ? item : null)];

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
    private static uint Ticks(ulong units) => Math.Max(1, checked((uint)((units + PawnBlackboard.TimePerTick - 1) / PawnBlackboard.TimePerTick)));
    private static ulong Signed(int value) => (uint)value ^ 0x80000000U;
}
