using Content.Shared.Damage;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Overlays;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Client.Overlays;

/// <summary>
/// Shows a healthy icon on mobs.
/// </summary>
public sealed class ShowHealthIconsSystem : EquipmentHudSystem<ShowHealthIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;


    public HashSet<string> DamageContainers = new();

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HealthIconFine = "HealthIconFine";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HealthIconCritical = "HealthIconCritical";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HealthIconDead = "HealthIconDead";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HealthIconCatatonic = "HealthIconCatatonic";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);

    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowHealthIconsComponent> component)
    {
        base.UpdateInternal(component);

        foreach (var damageContainerId in component.Components.SelectMany(x => x.DamageContainers))
        {
            DamageContainers.Add(damageContainerId);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        DamageContainers.Clear();
    }

    private void OnGetStatusIconsEvent(Entity<DamageableComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive || args.InContainer || args.HasStealthComponent)
            return;

        var healthIcons = DecideHealthIcons(entity);

        args.StatusIcons.AddRange(healthIcons);
    }

    private IReadOnlyList<StatusIconPrototype> DecideHealthIcons(Entity<DamageableComponent> entity)
    {
        var damageableComponent = entity.Comp;

        if (damageableComponent.DamageContainerID == null ||
            !DamageContainers.Contains(damageableComponent.DamageContainerID))
        {
            return Array.Empty<StatusIconPrototype>();
        }

        var result = new List<StatusIconPrototype>();

        // Here you could check health status, diseases, mind status, etc. and pick a good icon, or multiple depending on whatever.
        if (damageableComponent?.DamageContainerID == "Biological")
        {
            if (_mobState.IsAlive(entity))
            {
                if (TryComp<SSDIndicatorComponent>(entity, out var ssdComp) && ssdComp.IsSSD && _prototypeMan.TryIndex<StatusIconPrototype>(HealthIconCatatonic, out var ssdIcon))
                {
                    result.Add(ssdIcon);
                }
                else if (_prototypeMan.TryIndex<StatusIconPrototype>(HealthIconFine, out var fineIcon))
                    result.Add(fineIcon);
            }
            else if (_mobState.IsCritical(entity) && _prototypeMan.TryIndex<StatusIconPrototype>(HealthIconCritical, out var critIcon))
                result.Add(critIcon);
            else if (_mobState.IsDead(entity) && _prototypeMan.TryIndex<StatusIconPrototype>(HealthIconDead, out var deadIcon))
                result.Add(deadIcon);
        }

        return result;
    }
}
