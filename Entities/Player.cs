using System;
using System.Collections.Generic;

using Godot;

// struct PlayerState {
//     bool CAN_MOVE;
//     bool CAN_JUMP;
//     bool CAN_ATTACK;
//     bool CAN_DASH;
// };

public class Player : KinematicBody2D {
    public Vector2 Velocity = Vector2.Zero;

    PlayerStateMachine stateMachine;
    AnimationPlayer animationPlayer;
    Sprite sprite;

    public override void _Ready() {
        sprite = GetNode("./Sprite") as Sprite;
        animationPlayer = GetNode("./AnimationPlayer") as AnimationPlayer;
        stateMachine = new PlayerStateMachine(this);
    }

    public override void _Process(float dt) {
        stateMachine.Process(dt);
    }

    public override void _PhysicsProcess(float dt) {
        stateMachine.PhysicsProcess(dt);
        Velocity = MoveAndSlide(Velocity, Vector2.Up);
    }

    public void PlayAnimation(string name) {
        animationPlayer.Play(name);
    }

    public void SpriteFlipH(bool value) {
        sprite.FlipH = value;
    }
}


// === Player States === //
public static class PlayerState {
    public const string Idle = "IDLE";
    public const string Move = "MOVE";
    public const string Crouch = "CROUCH";
    public const string LookUp = "LOOK_UP";
    public const string Jump = "JUMP";
    public const string Fall = "FALL";
}

public class PlayerStateMachine : StateMachine {
    const float GROUND_ACCELERATION = 1500f;
    const float MAX_MOVE_SPEED = 200f;
    const float CROUCH_FRICTION = 0.08f;

    const float JUMP_SPEED = 420f;
    const float MAX_FALL_SPEED = 1000f;
    const float AIR_ACCELERATION = 800f;
    const float AIR_FRICTION = 0.05f;
    const float GRAVITY = 800f;

    public Player Player { get; }

    float xInput = 0f;
    float yInput = 0f;
    bool isGrounded = true;

    public PlayerStateMachine(Player player) {
        Player = player;
        RegisterStates();
        SetState(PlayerState.Idle);
    }

    public override void Process(float dt) {
        xInput = Input.GetActionStrength("control_right") - Input.GetActionStrength("control_left");
        yInput = Input.GetActionStrength("control_down") - Input.GetActionStrength("control_up");
        isGrounded = Player.IsOnFloor();
        base.Process(dt);
    }

    // === Control Helpers === //
    bool CheckFall() {
        if (!isGrounded) {
            SetState(PlayerState.Fall);
            return true;
        }
        return false;
    }

    bool CheckJump() {
        if (isGrounded && Input.IsActionJustPressed("control_jump")) {
            SetState(PlayerState.Jump);
            return true;
        }
        return false;
    }

    // === Physics Helpers === //
    void ApplyGroundMovement(float dt) {
        if (Mathf.Sign(xInput) != Mathf.Sign(Player.Velocity.x)) {
            Player.Velocity.x = 0f;
        }
        Player.Velocity.x += xInput * GROUND_ACCELERATION * dt;
        Player.Velocity.x = Mathf.Clamp(Player.Velocity.x, -MAX_MOVE_SPEED, MAX_MOVE_SPEED);
        Player.SpriteFlipH(xInput < 0f);
    }

    void ApplyCrouchFriction(float dt) {
        Player.Velocity.x = Mathf.Lerp(Player.Velocity.x, 0f, CROUCH_FRICTION);
        if (Mathf.Abs(Player.Velocity.x) < 10f) {
            Player.Velocity.x = 0f;
        }
    }

    void ApplyAirMovement(float dt) {
        if (xInput != 0f) {
            Player.Velocity.x += xInput * AIR_ACCELERATION * dt;
            Player.Velocity.x = Mathf.Clamp(Player.Velocity.x, -MAX_MOVE_SPEED, MAX_MOVE_SPEED);
        } else {
            Player.Velocity.x = Mathf.Lerp(Player.Velocity.x, 0f, AIR_FRICTION);
        }
        Player.SpriteFlipH(xInput < 0f);
    }

    void ApplyGravity(float dt) {
        if (Player.Velocity.y < MAX_FALL_SPEED) {
            Player.Velocity.y += GRAVITY * dt;
            Player.Velocity.y = Mathf.Min(Player.Velocity.y, MAX_FALL_SPEED);
        }
    }


    // === States List === //
    void RegisterStates() {
        States.Clear();

        RegisterState(new State(PlayerState.Idle) {
            OnEnter = () => {
                Player.PlayAnimation("Idle");
            },
            Process = (float dt) => {
                if (CheckFall()) return;
                else if (CheckJump()) return;
                else if (xInput != 0f) SetState(PlayerState.Move);
                else if (yInput > 0f) SetState(PlayerState.Crouch);
                else if (yInput < 0f) SetState(PlayerState.LookUp);
            },
            PhysicsProcess = (float dt) => {
                Player.Velocity = Vector2.Zero;
                ApplyGravity(dt);
            }
        });

        RegisterState(new State(PlayerState.Move) {
            OnEnter = () => {
                Player.PlayAnimation("Move");
            },
            Process = (float dt) => {
                if (CheckFall()) return;
                else if (CheckJump()) return;
                else if (xInput == 0f) SetState(PlayerState.Idle);
                else if (yInput > 0f) SetState(PlayerState.Crouch);
            },
            PhysicsProcess = (float dt) => {
                ApplyGroundMovement(dt);
                ApplyGravity(dt);
            }
        });

        RegisterState(new State(PlayerState.Crouch) {
            OnEnter = () => {
                Player.PlayAnimation("Crouch");
            },
            Process = (float dt) => {
                if (CheckFall()) return;
                else if (CheckJump()) return;
                else if (yInput <= 0f) SetState(PlayerState.Idle);
            },
            PhysicsProcess = (float dt) => {
                ApplyCrouchFriction(dt);
                ApplyGravity(dt);
            }
        });

        RegisterState(new State(PlayerState.LookUp) {
            OnEnter = () => {
                Player.PlayAnimation("LookUp");
            },
            Process = (float dt) => {
                if (CheckFall()) return;
                else if (CheckJump()) return;
                else if (yInput >= 0f) SetState(PlayerState.Idle);
            },
            PhysicsProcess = (float dt) => {
                Player.Velocity = Vector2.Zero;
                ApplyGravity(dt);
            }
        });

        RegisterState(new State(PlayerState.Jump) {
            OnEnter = () => {
                Player.PlayAnimation("Jump");
                Player.Velocity.y = -JUMP_SPEED;
            },
            Process = (float dt) => {
                if (Player.Velocity.y > 0f) SetState(PlayerState.Fall);
            },
            PhysicsProcess = (float dt) => {
                ApplyAirMovement(dt);
                ApplyGravity(dt);
            }
        });

        RegisterState(new State(PlayerState.Fall) {
            OnEnter = () => {
                Player.PlayAnimation("Jump");
            },
            Process = (float dt) => {
                if (isGrounded) SetState(PlayerState.Idle);
            },
            PhysicsProcess = (float dt) => {
                ApplyAirMovement(dt);
                ApplyGravity(dt);
            }
        });
    }
}
