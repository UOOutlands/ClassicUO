using System;
using System.Collections.Generic;
using static ClassicUO.Game.Scripting.Interpreter;

namespace ClassicUO.Game.Scripting
{
    public static class Aliases
    {
        // Called by upper management class to register all the desired aliases
        public static void Register()
        {
            // Player commons
            Write<uint>("mount", Mount);

            // Colors
            Write("color", "any", ushort.MaxValue);

            // Directions
            Write("direction", "southeast", "down");
            Write("direction", "southwest", "left");
            Write("direction", "northeast", "right");
            Write("direction", "northwest", "up");
        }

        // Registry of default values for parameters organized by params type-name (as stated in Command)
        // Paremeter values are stored as objects are a same parameter may not have multiple types (like int, short, string, etc)
        private static Dictionary<string, Dictionary<string, object>> _local = new Dictionary<string, Dictionary<string, object>>();
        public static bool Read<T>(string paramName, string paramValue, ref T paramDefault)
        {
            object obj = null;
            if (_local.ContainsKey(paramName) && _local[paramName].ContainsKey(paramValue) && _local[paramName].TryGetValue(paramValue, out obj))
            {
                paramDefault = (T)obj;
                return true;
            }
            else return false;
        }
        public static void Write(string paramName, string paramValue, object paramDefault)
        {
            if (!_local.ContainsKey(paramName))
                _local.Add(paramName, new Dictionary<string, object>());
            _local[paramName].Add(paramValue, paramDefault);
        }
        public static void Remove(string paramName, string paramValue)
        {
            if (_local.ContainsKey(paramName) && _local[paramName].ContainsKey(paramValue))
                _local[paramName].Remove(paramValue);
        }

        // Registry of global aliases mapping a name to a value
        // Stored based on type as an global alias is specific to a given spected type (for example, "any" color may be different from "any" range)
        // Global aliases can also be handled in runtime, as they map to a game variable
        private static Dictionary<Type, Dictionary<string, object>> _global = new Dictionary<Type, Dictionary<string, object>>();
        public delegate bool AliasHandler<T>(string alias, out T value);
        private static Dictionary<Type, Dictionary<string, object>> _globalHandlers = new Dictionary<Type, Dictionary<string, object>>();
        public static bool Read<T>(string alias, ref T value)
        {
            if (_globalHandlers.ContainsKey(typeof(T)) && _globalHandlers[typeof(T)].TryGetValue(alias, out object handlerObj))
            {
                AliasHandler<T> hander = (AliasHandler<T>)handlerObj;
                return hander(alias, out value);
            }
            else if (_global.ContainsKey(typeof(T)) && _global[typeof(T)].TryGetValue(alias, out object aliasObj))
            {
                value = (T)aliasObj;
                return true;
            }
            else return false;
        }
        public static void Write<T>(string alias, T value)
        {
            if (!_global.ContainsKey(typeof(T)))
                _global.Add(typeof(T), new Dictionary<string, object>());
            if (!_global[typeof(T)].ContainsKey(alias))
                _global[typeof(T)].Add(alias, value);
            else _global[typeof(T)][alias] = value;
        }
        public static void Write<T>(string alias, AliasHandler<T> handler)
        {
            if(!_globalHandlers.ContainsKey(typeof(T)))
                _globalHandlers.Add(typeof(T), new Dictionary<string, object>());
            _globalHandlers[typeof(T)].Add(alias, handler);
        }
        public static void Remove(Type T, string alias)
        {
            if (_global.ContainsKey(T))
                _global[T].Remove(alias);
        }
        public static void Remove<T>(string keyword, AliasHandler<T> handler)
        {
            if (_globalHandlers.ContainsKey(typeof(T)))
                _globalHandlers[typeof(T)].Remove(keyword);
        }

        private static bool Mount(string alias, out uint value)
        {
            var mount = World.Player.FindItemByLayer(Data.Layer.Mount);
            value = mount.Serial;
            return (mount != null);
        }
        private static bool Self(string alias, out uint value)
        {
            value = World.Player.Serial;
            return (value != 0);
        }
    }
}