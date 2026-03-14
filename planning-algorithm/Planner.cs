#nullable enable
using System;
using System.Collections.Generic;

public struct PlannerWeights {
    public Dictionary<string, double> Weights;
}

public class PlannerState {
    public Dictionary<string, double> State = new();

    public PlannerState Set(string key, double value) {
        State[key] = value;
        return this;
    }

    public PlannerState Add(string key, double value) {
        State[key] = State.GetValueOrDefault(key, 0) + value;
        return this;
    }

    public PlannerState Copy() {
        return new PlannerState { State = new Dictionary<string, double>(State) };
    }

    /// <summary>
    /// Used as part of the A* heuristic.
    /// </summary>
    /// <param name="goal">A partial state which represents the goal (any state values which aren't present in goal will be ignored)</param>
    /// <returns>A distance value which does not weight anything</returns>
    public double Distance(PlannerState goal) {
        var distance = 0.0;
        foreach (var pair in goal.State) {
            distance += State.GetValueOrDefault(pair.Key, 0.0) - pair.Value;
        }

        return distance;
    }

    public double WeightedDistance(PlannerState goal, PlannerWeights weights) {
        var distance = 0.0;
        foreach (var pair in goal.State) {
            distance += (State.GetValueOrDefault(pair.Key, 0.0) - pair.Value) *
                        weights.Weights.GetValueOrDefault(pair.Key, 1.0);
        }

        return distance;
    }
}

public struct PlannerOperatorArgs(params object[] args) {
    public object[] Args = args;
}

public struct PlannerMethodStep {
    public string Operator;
    public PlannerOperatorArgs Args;
}

public struct PlannerStep {
    public PlannerState State;
    public string Operator;
}

public class PlannerPathNotFound : Exception {
    public PlannerPathNotFound(string message) : base(message) {
    }
}

public class Planner {
    public delegate PlannerState? PlannerOperator(PlannerState state, PlannerOperatorArgs args);

    public delegate PlannerMethodStep[] PlannerMethod(PlannerState state, PlannerOperatorArgs args);

    public PlannerState InitialState = new PlannerState();
    public Dictionary<string, PlannerOperator> Operators = new();
    public Dictionary<string, PlannerMethod[]> Methods = new();

    public Planner() {
    }

    public Planner(PlannerState initialState) {
        InitialState = initialState;
    }

    public Planner AddMethod(string name, params PlannerMethod[] steps) {
        Methods[name] = steps;
        return this;
    }

    public Planner AddOperator(string name, PlannerOperator @operator) {
        Operators[name] = @operator;
        return this;
    }

    public List<PlannerStep> FindPath(PlannerMethod activity) {
        return FindPath(InitialState, activity);
    }

    public List<PlannerStep> FindPath(PlannerState initialState, PlannerMethod activity) {
        var currentState = initialState.Copy();

        throw new PlannerPathNotFound("Path not found.");
    }
}