using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API;

namespace ItemProhibitionFix;

[MinimumApiVersion(309)]
public class Plugin : BasePlugin
{
    public override string ModuleName => "Items Prohibition Fix";
    public override string ModuleDescription => "Since mp_items_prohibited is broken from CS2 release, we manually prohibit it :)";
    public override string ModuleAuthor => "Cruze";
    public override string ModuleVersion => "1.0.0";

    private HashSet<ushort> _prohibitedItems = new HashSet<ushort>();

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);

        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnWeaponCanAcquire, HookMode.Pre);

        if (hotReload)
        {
            GetProhibitedItems();
        }
    }

    public override void Unload(bool hotReload)
    {
        base.Unload(hotReload);

        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnWeaponCanAcquire, HookMode.Pre);
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundPoststart @event, GameEventInfo @info)
    {
        Server.NextWorldUpdate(GetProhibitedItems);
        return HookResult.Continue;
    }

    public HookResult OnWeaponCanAcquire(DynamicHook hook)
    {
        var acquireMethod = hook.GetParam<AcquireMethod>(2);
        if (acquireMethod == AcquireMethod.PickUp)
        {
            return HookResult.Continue;
        }

        var itemIdx = hook.GetParam<CEconItemView>(1).ItemDefinitionIndex;

        if (_prohibitedItems.Contains(itemIdx))
        {
            hook.SetReturn(AcquireResult.NotAllowedByProhibition);
            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    private void GetProhibitedItems()
    {
        _prohibitedItems.Clear();
        var probItems = ConVar.Find("mp_items_prohibited")?.StringValue;

        if (string.IsNullOrWhiteSpace(probItems)) return;

        var items = probItems.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var item in items.Select(i => i.Trim()))
        {
            if (ushort.TryParse(item, out var idx))
            {
                _prohibitedItems.Add(idx);
            }
        }
    }
}