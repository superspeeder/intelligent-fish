using System;
using System.Collections.Generic;

namespace IntelligentFish.behavior_tree;

public abstract class BehaviorTree<T> {
    public abstract bool Execute(T data);
}

public class Selector<T> : BehaviorTree<T> {
    private List<BehaviorTree<T>> children;

    public Selector(List<BehaviorTree<T>> children) {
        this.children = children;
    }


    public override bool Execute(T data) {
        foreach (var child in children) {
            if (child.Execute(data)) {
                return true;
            }
        }

        return false;
    }
}

public class Sequence<T> : BehaviorTree<T> {
    private List<BehaviorTree<T>> children;

    public Sequence(List<BehaviorTree<T>> children) {
        this.children = children;
    }


    public override bool Execute(T data) {
        foreach (var child in children) {
            if (!child.Execute(data)) {
                return false;
            }
        }

        return true;
    }
}

public class Inverter<T> : BehaviorTree<T> {
    private BehaviorTree<T> child;

    public Inverter(BehaviorTree<T> child) {
        this.child = child;
    }

    public override bool Execute(T data) {
        return !child.Execute(data);
    }
}

public class RepeatUntilSuccess<T> : BehaviorTree<T> {
    private BehaviorTree<T> child;

    public RepeatUntilSuccess(BehaviorTree<T> child) {
        this.child = child;
    }

    public override bool Execute(T data) {
        while (!child.Execute(data)) {
        }

        return true;
    }
}

public class RepeatUntilFailure<T> : BehaviorTree<T> {
    private BehaviorTree<T> child;

    public RepeatUntilFailure(BehaviorTree<T> child) {
        this.child = child;
    }

    public override bool Execute(T data) {
        while (child.Execute(data)) {
        }

        return false;
    }
}

public class AlwaysSuccess<T> : BehaviorTree<T> {
    private BehaviorTree<T> child;

    public AlwaysSuccess(BehaviorTree<T> child) {
        this.child = child;
    }

    public override bool Execute(T data) {
        child.Execute(data);

        return true;
    }
}

public class AlwaysFailure<T> : BehaviorTree<T> {
    private BehaviorTree<T> child;

    public AlwaysFailure(BehaviorTree<T> child) {
        this.child = child;
    }

    public override bool Execute(T data) {
        child.Execute(data);

        return false;
    }
}

public class RepeatN<T> : BehaviorTree<T> {
    private BehaviorTree<T> child;
    private int count;

    public RepeatN(BehaviorTree<T> child, int count) {
        this.child = child;
        this.count = count;
    }

    public override bool Execute(T data) {
        for (int i = 0; i < count; i++) {
            if (!child.Execute(data)) {
                return false;
            }
        }

        return true;
    }
}

public class RandomSelector<T> : BehaviorTree<T> {
    private List<BehaviorTree<T>> children;

    public RandomSelector(List<BehaviorTree<T>> children) {
        this.children = children;
    }


    public override bool Execute(T data) {
        BehaviorTree<T>[] copy = new BehaviorTree<T>[children.Count];
        children.CopyTo(copy, 0);
        Random.Shared.Shuffle(copy);
        foreach (var child in copy) {
            if (child.Execute(data)) {
                return true;
            }
        }

        return false;
    }
}

public class RandomSequence<T> : BehaviorTree<T> {
    private List<BehaviorTree<T>> children;

    public RandomSequence(List<BehaviorTree<T>> children) {
        this.children = children;
    }


    public override bool Execute(T data) {
        BehaviorTree<T>[] copy = new BehaviorTree<T>[children.Count];
        children.CopyTo(copy, 0);
        Random.Shared.Shuffle(copy);
        foreach (var child in copy) {
            if (!child.Execute(data)) {
                return false;
            }
        }

        return false;
    }
}

public class Behavior<T> : BehaviorTree<T> {
    private Func<T, bool> action;

    public Behavior(Func<T, bool> action) {
        this.action = action;
    }
    
    public override bool Execute(T data) {
        return action(data);
    }
}