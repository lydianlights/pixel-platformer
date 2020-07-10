using System;
using Godot;

public class Player : KinematicBody2D {
    const float ACCELERATION = 1500f;
    const float MAX_SPEED = 200f;
    const float FRICTION_GROUND = 0.5f;
    const float FRICTION_AIR = 0.1f;
    const float GRAVITY = 800f;
    const float JUMP_FORCE = 400f;

    Sprite sprite;
    AnimationPlayer animationPlayer;
    Vector2 motion = Vector2.Zero;

    public override void _Ready() {
        sprite = GetNode("./Sprite") as Sprite;
        animationPlayer = GetNode("./AnimationPlayer") as AnimationPlayer;
    }

    public override void _PhysicsProcess(float delta) {
        float xInput = Input.GetActionStrength("control_right") - Input.GetActionStrength("control_left");
        float yInput = Input.GetActionStrength("control_down") - Input.GetActionStrength("control_up");

        if (xInput != 0f) {
            animationPlayer.Play("Walk");
            if (Mathf.Sign(xInput) != Mathf.Sign(motion.x)) {
                motion.x = 0f;
            }
            motion.x += xInput * ACCELERATION * delta;
            motion.x = Mathf.Clamp(motion.x, -MAX_SPEED, MAX_SPEED);
            sprite.FlipH = xInput < 0f;
        } else {
            if (yInput < 0f) {
                animationPlayer.Play("LookUp");
            } else if (yInput > 0f) {
                animationPlayer.Play("Crouch");
            } else {
                animationPlayer.Play("Stand");
            }
        }

        if (IsOnFloor()) {
            // GROUNDED
            if (Input.IsActionJustPressed("control_jump")) {
                motion.y = -JUMP_FORCE;
            }
            if (xInput == 0f) {
                motion.x = Mathf.Lerp(motion.x, 0f, FRICTION_GROUND);
            }
        } else {
            // AIR
            animationPlayer.Play("Jump");
            if (Input.IsActionJustReleased("control_jump") && motion.y < -JUMP_FORCE / 2f) {
                motion.y = -JUMP_FORCE / 2f;
            }
            if (xInput == 0f) {
                motion.x = Mathf.Lerp(motion.x, 0f, FRICTION_AIR);
            }
        }

        motion.y += GRAVITY * delta;
        motion = MoveAndSlide(motion, Vector2.Up);
    }
}
