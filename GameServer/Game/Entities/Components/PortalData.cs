using System;
using System.Reflection;
using Common;
using Common.Game;
using Common.Resources.World;
using Common.Structs;
using Common.Utilities;
using Common.Utilities.Collections;
using GameServer.Game.Network;
using GameServer.Game.Worlds;

namespace GameServer.Game.Entities.Components;

public struct PortalData : IEntityIdentifiable, IDisposable {
    private static readonly Dictionary<string, Type> _worldTypes = [];
    private static readonly Logger _log = new(typeof(PortalData));

    static PortalData() {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var type in asm.GetTypes()) {
            if (type == typeof(World) || !type.IsSubclassOf(typeof(World)))
                continue;

            _worldTypes[type.Name] = type;
        }
    }
    
    public EntityId Id { get; set; }

    public bool DisplayPlayerCount;
    public bool Disabled;
    
    private readonly World _world;
    private World _worldLink;
    
    public PortalData(World world, ref Entity en) {
        Id = en.Id;
        _world = world;

        LoadWorld(ref en);
    }

    private void LoadWorld(ref Entity en) {
        if (en.Desc.RealmPortal)
            return;

        var worldName = en.Desc.DungeonName;
        var worldConfig = WorldLibrary.WorldConfigs.Values.FirstOrDefault(i => i.DisplayName == worldName);
        if (worldConfig.Name == null) {
            _log.Error($"World '{worldName}' not found ({en.Desc.ObjectId})");
            return;
        }

        if (worldConfig.Id == 0 && (!WorldLibrary.MapDatas.TryGetValue(worldConfig.Name, out var maps) || maps == null || maps.Length == 0)) {
            _log.Error($"World '{worldName}' has no map ({en.Desc.ObjectId})");
            return;
        }

        // Most dungeons need no special logic (only Nexus, Realm and Vault have a class),
        // so anything without one runs as a plain World using its config and map.
        if (!_worldTypes.TryGetValue(worldConfig.Name, out var worldType))
            worldType = typeof(World);
        
        var worldInstance = worldConfig.Id == 0
            ? (World)Activator.CreateInstance(worldType, worldConfig.Id, -1, worldConfig)
            : RealmManager.Worlds[worldConfig.Id];
        
        Init(worldInstance);
    }

    public void Init(World worldLink) {
        RealmManager.AddWorld(worldLink);
        LinkTo(worldLink);
    }

    public void LinkTo(World worldLink) {
        _worldLink = worldLink;
    }

    public World GetWorldInstance(User user) {
        if (_worldLink == null)
            return null;
        return _worldLink.GetInstance(user);
    }

    public void Tick(ref RealmTime time) {
        if (_worldLink == null)
            return;

        if (_worldLink.Deleted) {
            _world.LeaveWorld(Id);
        }
        
        if (DisplayPlayerCount) {
            ref var stats = ref _world.EntityStats.Get(Id);
            stats.Set(StatType.Name, $"{_worldLink.DisplayName} ({_worldLink.Users.Count}/{_worldLink.Config.MaxPlayers})");
        }
    }
    
    public void Dispose() {
        
    }
}