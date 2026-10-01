using System;
using System.Reflection;
using MarshAPI;
using UnityEngine;

namespace TownOfRoles.Assets
{
    /// <summary>
    /// The embedded role icons, decoded from PNG on first use.
    ///
    /// <para>The cache is <b>Unity-null-aware</b>: a runtime sprite the game has
    /// destroyed (its <c>Resources.UnloadUnusedAssets</c> pass between rounds kills
    /// <c>Sprite.Create</c> objects nothing in a scene references — exactly what a
    /// managed static is) is treated as absent, so the PNG is decoded again and the
    /// next ability button of the round gets real art. Without this, round two
    /// started with the vanilla USE sprite: the statics still held the destroyed
    /// wrappers, the clone's <c>icon != null</c> check passed on the managed
    /// reference and never noticed.</para>
    /// </summary>
    internal static class RoleArt
    {
        private static readonly Assembly Assembly = typeof(RoleArt).Assembly;
        private static readonly string ResourcePrefix = "TownOfRoles.Assets.OriginalTownOfUs.Resources.";
        private static Sprite _engineer;
        private static Sprite _medic;
        private static Sprite _seer;
        private static Sprite _janitor;
        private static Sprite _revive;
        private static Sprite _douse;
        private static Sprite _ignite;
        private static Sprite _cycle;
        private static Sprite _guess;
        private static Sprite _swapperSwitch;
        private static Sprite _morph;
        private static Sprite _sample;
        private static Sprite _camouflage;
        private static Sprite _swoop;
        private static Sprite _drag;
        private static Sprite _footprint;
        private static Sprite _rewind;
        private static Sprite _arrow;
        private static Sprite _shift;
        private static Sprite _kill;
        private static Sprite _shiftKill;
        private static Sprite _mine;
        private static Sprite _abstain;

        /// <summary>Icons resident after the last <see cref="Preload"/> (the boot report).</summary>
        private static int _preloadCount;

        /// <summary>
        /// Resource names whose decode failed this session. A destroyed sprite is
        /// re-decoded freely, but a decode that failed is never retried: the
        /// resource is not going to start existing, and the property getters run
        /// on the HUD tick, so a retry would log a warning per frame.
        /// </summary>
        private static readonly System.Collections.Generic.HashSet<string> _failed =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>True when the sprite exists in the Unity sense — alive, not a destroyed native object.</summary>
        private static bool Alive(Sprite sprite) => sprite != null && sprite;

        private static Sprite Load(string name)
        {
            if (_failed.Contains(name)) return null;
            try
            {
                var sprite = AssetUtils.LoadSpriteFromEmbeddedResource(
                    Assembly, ResourcePrefix + name, 260f, 512);
                if (sprite != null) _preloadCount++;
                else _failed.Add(name);
                return sprite;
            }
            catch (Exception ex)
            {
                _failed.Add(name);
                BepInEx.Logging.Logger.CreateLogSource("TownOfRoles").LogWarning(
                    "Could not load original Town Of Us asset " + name + ": " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Decodes every embedded role icon that is not already resident. The
        /// return is the count of icons decoded successfully this session, for the
        /// boot-screen preload to report.
        ///
        /// <para>Deliberately re-runs after the game destroyed the sprites (its
        /// <c>Resources.UnloadUnusedAssets</c> pass between rounds): a decode is
        /// cheap relative to a round of missing art. Decodes that <i>failed</i> are
        /// not retried — see <see cref="_failed"/>.</para>
        /// </summary>
        public static int Preload()
        {
            _engineer = Alive(_engineer) ? _engineer : Load("Engineer.png");
            _medic = Alive(_medic) ? _medic : Load("Medic.png");
            _seer = Alive(_seer) ? _seer : Load("Seer.png");
            _janitor = Alive(_janitor) ? _janitor : Load("Janitor.png");
            _revive = Alive(_revive) ? _revive : Load("Revive.png");
            _douse = Alive(_douse) ? _douse : Load("Douse.png");
            _ignite = Alive(_ignite) ? _ignite : Load("Ignite.png");
            _cycle = Alive(_cycle) ? _cycle : Load("Cycle.png");
            _guess = Alive(_guess) ? _guess : Load("Guess.png");
            _swapperSwitch = Alive(_swapperSwitch) ? _swapperSwitch : Load("SwapperSwitch.png");
            _morph = Alive(_morph) ? _morph : Load("Morph.png");
            _sample = Alive(_sample) ? _sample : Load("Sample.png");
            _camouflage = Alive(_camouflage) ? _camouflage : Load("Camouflage.png");
            _swoop = Alive(_swoop) ? _swoop : Load("Swoop.png");
            _drag = Alive(_drag) ? _drag : Load("Drag.png");
            _footprint = Alive(_footprint) ? _footprint : Load("Footprint.png");
            _rewind = Alive(_rewind) ? _rewind : Load("Rewind.png");
            _arrow = Alive(_arrow) ? _arrow : Load("Arrow.png");
            _shift = Alive(_shift) ? _shift : Load("Shift.png");
            _kill = Alive(_kill) ? _kill : Load("Kill.png");
            _shiftKill = Alive(_shiftKill) ? _shiftKill : Load("ShiftKill.png");
            _mine = Alive(_mine) ? _mine : Load("Mine.png");
            _abstain = Alive(_abstain) ? _abstain : Load("Abstain.png");
            return _preloadCount;
        }

        public static Sprite Engineer { get { Preload(); return _engineer; } }
        public static Sprite Medic { get { Preload(); return _medic; } }
        public static Sprite Seer { get { Preload(); return _seer; } }
        public static Sprite Janitor { get { Preload(); return _janitor; } }
        public static Sprite Revive { get { Preload(); return _revive; } }
        public static Sprite Douse { get { Preload(); return _douse; } }
        public static Sprite Ignite { get { Preload(); return _ignite; } }
        public static Sprite Cycle { get { Preload(); return _cycle; } }
        public static Sprite Guess { get { Preload(); return _guess; } }
        public static Sprite SwapperSwitch { get { Preload(); return _swapperSwitch; } }
        public static Sprite Morph { get { Preload(); return _morph; } }
        public static Sprite Sample { get { Preload(); return _sample; } }
        public static Sprite Camouflage { get { Preload(); return _camouflage; } }
        public static Sprite Swoop { get { Preload(); return _swoop; } }
        public static Sprite Drag { get { Preload(); return _drag; } }
        public static Sprite Footprint { get { Preload(); return _footprint; } }
        public static Sprite Rewind { get { Preload(); return _rewind; } }
        public static Sprite Arrow { get { Preload(); return _arrow; } }
        public static Sprite Shift { get { Preload(); return _shift; } }
        public static Sprite Kill { get { Preload(); return _kill; } }
        public static Sprite ShiftKill { get { Preload(); return _shiftKill; } }
        public static Sprite Mine { get { Preload(); return _mine; } }
        public static Sprite Abstain { get { Preload(); return _abstain; } }
    }
}
