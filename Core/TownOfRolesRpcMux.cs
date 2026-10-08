using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using Atomic;
using HarmonyLib;
using Hazel;
using UnityEngine;

namespace TownOfRoles.Core
{
    internal static class TownOfRolesRpcMux
    {
        private const string TransportKey = "townofroles.RpcMux";
        private static readonly Dictionary<string, MethodInfo> Handlers = new();
        internal static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("TownOfRoles RPC");
        private static bool _installed;
        internal static bool _warnedMuxDown;

        internal static bool Active => _installed;

        internal static void Install()
        {
            if (_installed) return;
            var harmony = new Harmony(TownOfRolesPlugin.Guid + ".rpcmux");
            try
            {
                harmony.CreateClassProcessor(typeof(AtomicAPI_RegisterRpcMethods_MuxPatch)).Patch();
                harmony.CreateClassProcessor(typeof(AtomicAPI_SendRpcMethod_MuxPatch)).Patch();

                AtomicAPI.RegisterRpcMethods(typeof(TownOfRolesRpcMux));
            }
            catch
            {
                harmony.UnpatchSelf();
                throw;
            }
            _installed = true;
        }

        internal static bool Register(Type type)
        {
            if (type == null || type == typeof(TownOfRolesRpcMux)) return true;
            if (type.Assembly != typeof(TownOfRolesRpcMux).Assembly) return true;
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                string key = null;
                foreach (var attribute in method.GetCustomAttributesData())
                {
                    if (attribute.AttributeType != typeof(AtomicRpcAttribute) || attribute.ConstructorArguments.Count != 1) continue;
                    key = attribute.ConstructorArguments[0].Value as string;
                    break;
                }
                if (string.IsNullOrEmpty(key)) continue;
                Handlers[key] = method;
            }
            return false;
        }

        internal static void Send(string key, params object[] args)
        {
            if (TrySend(key, args)) AtomicAPI.SendRpcMethod(key, args);
        }

        // Keys already reported as unrouted, so the warning below is one line per key rather
        // than one per send.
        private static readonly HashSet<string> _warnedUnrouted = new();

        internal static bool TrySend(string key, object[] args)
        {
            if (key == TransportKey) return true;
            if (!Handlers.ContainsKey(key))
            {
                // A townofroles. key with no handler here went straight to Atomic instead,
                // which is what happens to any type registered before the mux was installed.
                // Dropping the send, which is what this used to do, broke that RPC for the
                // rest of the session with nothing in the log to show for it. Handing the key
                // back to Atomic sends it the way its registration asked for.
                if (key.StartsWith("townofroles.", StringComparison.Ordinal) && _warnedUnrouted.Add(key))
                    Log.LogWarning("Sending " + key + " outside the mux; its handler was registered before the mux was installed.");
                return true;
            }
            try
            {
                AtomicAPI.SendRpcMethod(TransportKey, key, Convert.ToBase64String(Serialize(args)));
            }
            catch (Exception e)
            {
                Log.LogError("Could not send " + key + ": " + e.Message);
            }
            return false;
        }

        [AtomicRpc(TransportKey)]
        private static void Receive(byte senderId, string key, string payload)
        {
            if (!Handlers.TryGetValue(key, out var method))
            {
                Log.LogWarning("Received unknown RPC: " + key);
                return;
            }
            try
            {
                var parameters = method.GetParameters();
                var values = new object[parameters.Length];
                values[0] = senderId;
                var bytes = string.IsNullOrEmpty(payload) ? Array.Empty<byte>() : Convert.FromBase64String(payload);
                using var stream = new MemoryStream(bytes, false);
                using var reader = new BinaryReader(stream);
                for (var i = 1; i < parameters.Length; i++) values[i] = Read(reader, parameters[i].ParameterType);
                method.Invoke(null, values);
            }
            catch (Exception e)
            {
                Log.LogError("Could not dispatch " + key + ": " + e);
            }
        }

        private static byte[] Serialize(object[] values)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                foreach (var value in values ?? Array.Empty<object>()) Write(writer, value);
            }
            return stream.ToArray();
        }

        private static void Write(BinaryWriter writer, object value)
        {
            switch (value)
            {
                case bool v: writer.Write(v); break;
                case byte v: writer.Write(v); break;
                case sbyte v: writer.Write(v); break;
                case short v: writer.Write(v); break;
                case ushort v: writer.Write(v); break;
                case int v: writer.Write(v); break;
                case uint v: writer.Write(v); break;
                case long v: writer.Write(v); break;
                case ulong v: writer.Write(v); break;
                case float v: writer.Write(v); break;
                case double v: writer.Write(v); break;
                case string v: writer.Write(v ?? string.Empty); break;
                case byte[] v: writer.Write(v?.Length ?? -1); if (v != null) writer.Write(v); break;
                case Vector2 v: writer.Write(v.x); writer.Write(v.y); break;
                case Vector3 v: writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); break;
                default: throw new NotSupportedException("Unsupported RPC argument: " + value?.GetType());
            }
        }

        private static object Read(BinaryReader reader, Type type)
        {
            if (type == typeof(bool)) return reader.ReadBoolean();
            if (type == typeof(byte)) return reader.ReadByte();
            if (type == typeof(sbyte)) return reader.ReadSByte();
            if (type == typeof(short)) return reader.ReadInt16();
            if (type == typeof(ushort)) return reader.ReadUInt16();
            if (type == typeof(int)) return reader.ReadInt32();
            if (type == typeof(uint)) return reader.ReadUInt32();
            if (type == typeof(long)) return reader.ReadInt64();
            if (type == typeof(ulong)) return reader.ReadUInt64();
            if (type == typeof(float)) return reader.ReadSingle();
            if (type == typeof(double)) return reader.ReadDouble();
            if (type == typeof(string)) return reader.ReadString();
            if (type == typeof(byte[])) { var count = reader.ReadInt32(); return count < 0 ? null : reader.ReadBytes(count); }
            if (type == typeof(Vector2)) return new Vector2(reader.ReadSingle(), reader.ReadSingle());
            if (type == typeof(Vector3)) return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            throw new NotSupportedException("Unsupported RPC parameter: " + type);
        }
    }

    internal static class RpcRegistration
    {
        public static void Register(Type type)
        {
            if (type == null) return;
            if (!TownOfRolesRpcMux.Active)
            {
                if (!TownOfRolesRpcMux._warnedMuxDown)
                {
                    TownOfRolesRpcMux._warnedMuxDown = true;
                    TownOfRolesRpcMux.Log.LogWarning("mux is down, is Atomic installed?");
                }
                return;
            }
            AtomicAPI.RegisterRpcMethods(type);
        }
    }

    [HarmonyPatch(typeof(AtomicAPI), nameof(AtomicAPI.RegisterRpcMethods), new[] { typeof(Type) })]
    internal static class AtomicAPI_RegisterRpcMethods_MuxPatch
    {
        private static bool Prefix(Type type) => TownOfRolesRpcMux.Register(type);
    }

    [HarmonyPatch(typeof(AtomicAPI), nameof(AtomicAPI.SendRpcMethod), new[] { typeof(string), typeof(object[]) })]
    internal static class AtomicAPI_SendRpcMethod_MuxPatch
    {
        private static bool Prefix(string key, object[] args) => TownOfRolesRpcMux.TrySend(key, args);
    }
}
