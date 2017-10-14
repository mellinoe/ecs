using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ecs
{
    public abstract class GameSystem
    {
        private readonly List<int> _entities = new List<int>();

        public void Process(float deltaTime)
        {
            foreach (int entity in _entities)
            {
                ProcessEntity(deltaTime, entity);
            }
        }

        public abstract void ProcessEntity(float deltaTime, int entity);

        public abstract int GetTypeMask();

        internal void TrackNewEntity(int entity)
        {
            _entities.Add(entity);
        }
    }

    public abstract class GameSystem<T1, T2> : GameSystem
    {
        public override void ProcessEntity(float deltaTime, int entity)
        {
            ref T1 t1Ref = ref Storages.GetStorage<T1>().GetComponent<T1>(entity);
            ref T2 t2Ref = ref Storages.GetStorage<T2>().GetComponent<T2>(entity);

            ProcessEntity(ref t1Ref, ref t2Ref);
        }

        public abstract void ProcessEntity(ref T1 t1Rfef, ref T2 t2Ref);
    }


    public class EntityManager
    {
        private readonly Dictionary<int, int> _entityComponentMasks = new Dictionary<int, int>();
        private readonly SystemProcessor _systemProcessor;
        private readonly ComponentManager _componentManager = new ComponentManager();

        private int _nextID = 1;

        public EntityManager(SystemProcessor sp)
        {
            _systemProcessor = sp;
        }

        public int CreateEntity()
        {
            int ret = _nextID;
            _nextID += 1;
            return ret;
        }

        public void AddComponent<T>(int entity)
        {
            AddComponent(entity, default(T));
        }

        public void AddComponent<T>(int entity, T component)
        {
            ComponentStorage<T> storage = Storages.GetStorage<T>();
            int index = storage.Store(entity, component);
            OnComponentAdded(entity, _componentManager.GetID<T>());
        }

        internal void OnComponentAdded(int entity, int typeID)
        {
            _entityComponentMasks.TryGetValue(entity, out int mask);
            mask |= (1 << typeID);
            _entityComponentMasks[entity] = mask;
            _systemProcessor.EntityChangedMask(entity, mask);
        }
    }

    internal class ComponentManager
    {
        public readonly Dictionary<Type, int> _typeIDs = new Dictionary<Type, int>();

        private int _nextID = 0;

        public int GetID<T>()
        {
            Type type = typeof(T);
            if (!_typeIDs.TryGetValue(type, out int ret))
            {
                ret = _nextID;
                _nextID += 1;
                _typeIDs[type] = ret;
            }

            return ret;
        }
    }

    public static class Storages
    {
        private static readonly Dictionary<Type, ComponentStorageBase> _storages = new Dictionary<Type, ComponentStorageBase>();

        public static ComponentStorage<T> GetStorage<T>()
        {
            Type t = typeof(T);
            if (!_storages.TryGetValue(t, out ComponentStorageBase storage))
            {
                storage = new ComponentStorage<T>();
                _storages.Add(t, storage);
            }

            return (ComponentStorage<T>)storage;
        }
    }

    public abstract class ComponentStorageBase
    {
        public abstract int Store<T>(int entity, T component);
        public abstract ref T GetComponent<T>(int entity);
        public abstract int GetStorageIndex(int entity);
    }

    public class ComponentStorage<T> : ComponentStorageBase
    {
        public T[] Components { get; } = new T[100];
        private Dictionary<int, int> _componentIndicesByEntity = new Dictionary<int, int>();
        private int _nextID = 1;

        public unsafe override int Store<T2>(int entity, T2 component)
        {
            int ret = _nextID;
            void* ptr = Unsafe.AsPointer(ref component);
            Components[ret] = Unsafe.AsRef<T>(ptr);
            _componentIndicesByEntity[entity] = ret;
            _nextID += 1;
            return ret;
        }

        public override int GetStorageIndex(int entity)
        {
            return _componentIndicesByEntity[entity];
        }

        public unsafe override ref T1 GetComponent<T1>(int entity)
        {
            ref T refVal = ref Components[GetStorageIndex(entity)];
            return ref Unsafe.AsRef<T1>(Unsafe.AsPointer(ref refVal));
        }
    }

    public class SystemProcessor
    {
        private readonly List<GameSystem> _systems = new List<GameSystem>();

        public void RegisterSystem(GameSystem s)
        {
            _systems.Add(s);
        }

        public void Process(float deltaTime)
        {
            foreach (GameSystem system in _systems)
            {
                system.Process(deltaTime);
            }
        }

        internal void EntityChangedMask(int entity, int mask)
        {
            foreach (GameSystem system in _systems)
            {
                int systemMask = system.GetTypeMask();
                if ((systemMask & mask) == systemMask)
                {
                    system.TrackNewEntity(entity);
                }
            }
        }
    }
}
