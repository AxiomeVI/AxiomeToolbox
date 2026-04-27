using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.SpeedrunTool.RoomTimer;

namespace Celeste.Mod.AxiomeToolbox.Checkpoint;

[Tracked]
public class CheckpointTrigger : Entity {

    private readonly Color beamColor;
    private readonly CheckpointPlacementManager.AxiomeCheckpointData data;
    private float flash = 0f;

    private const float Width           = 11f;
    private const float Height          = 17f;
    private const int   RenderDepth     = -99;  // render above most entities
    private const float FlashFadeRate   = 3f;
    private const float AlphaTriggered  = 0.3f;
    private const float AlphaIdle       = 0.5f;
    private const float AlphaFlashBoost = 0.6f;
    private const float OutlineTriggered = 0.6f;

    public CheckpointTrigger(Color color, CheckpointPlacementManager.AxiomeCheckpointData data) : base(data.Position) {
        beamColor = color;
        this.data = data;

        Collider = new Hitbox(Width, Height, -Width / 2f, -Height / 2f);
        Depth = RenderDepth;
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        CheckpointPlacementManager.UntrackTrigger(this);
    }

    public override void Update() {
        base.Update();

        Player player = CollideFirst<Player>();
        if (player != null && !data.IsTriggered) {
            data.IsTriggered = true;
            flash = 1f;

            RoomTimerManager.UpdateTimerState();
            Audio.Play("event:/game/general/assist_screenbottom");
        }

        if (flash > 0f) flash -= Engine.DeltaTime * FlashFadeRate;
    }

    public override void Render() {
        base.Render();

        float alpha = data.IsTriggered ? AlphaTriggered : AlphaIdle;
        alpha += flash * AlphaFlashBoost;

        float x = Position.X - Width / 2f;
        float y = Position.Y - Height;

        Draw.Rect(x, y, Width, Height, beamColor * alpha);
        Draw.HollowRect(x, y, Width, Height, beamColor * (data.IsTriggered ? OutlineTriggered : 1f));
    }
}
