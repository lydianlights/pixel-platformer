using System;
using System.Collections.Generic;
using Godot;

public class State {
    public string Id { get; }

    public Action OnEnter;
    public Action OnExit;
    public Action<float> Process;
    public Action<float> PhysicsProcess;

    public State(string id) {
        Id = id;
    }
}

public class StateMachine {
    public State State { get; protected set; }

    protected Dictionary<string, State> States { get; set; } = new Dictionary<string, State>();

    public void RegisterState(State state) {
        States.Add(state.Id, state);
    }

    public void SetState(string id) {
        if (States.TryGetValue(id, out State state)) {
            State?.OnExit?.Invoke();
            State = state;
            State?.OnEnter?.Invoke();
        }
    }

    public virtual void Process(float delta) {
        State?.Process?.Invoke(delta);
    }

    public virtual void PhysicsProcess(float delta) {
        State?.PhysicsProcess?.Invoke(delta);
    }
}
