using UnityEngine;

namespace ArcaneSpire.Combat
{
    /// <summary>
    /// The runtime placeholder sprites (square, circle, ring) built ONCE per play session by
    /// <see cref="RunLifecycle"/> and shared by every run <see cref="CombatBootstrap.BuildRun"/> creates, so rebuilding
    /// a run never allocates (and leaks) new textures. Story 006.
    /// </summary>
    public sealed class RunArt
    {
        /// <summary>1x1-unit white square.</summary>
        public Sprite Square { get; }
        /// <summary>1-unit-diameter white circle.</summary>
        public Sprite Circle { get; }
        /// <summary>1-unit-diameter white ring.</summary>
        public Sprite Ring { get; }

        private RunArt(Sprite square, Sprite circle, Sprite ring)
        {
            Square = square;
            Circle = circle;
            Ring = ring;
        }

        /// <summary>Builds the shared sprite set.</summary>
        public static RunArt Create() =>
            new RunArt(PlaceholderArt.CreateSquare(), PlaceholderArt.CreateCircle(), PlaceholderArt.CreateRing());
    }
}
