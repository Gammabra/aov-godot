using AshesOfVelsingrad.Systems;
using AshesOfVelsingrad.Utilities;

namespace UnitTests;

public class TestConcreteStatusEffect<TTarget> : StatusEffect<TTarget>
{
	public bool ApplyCalled { get; private set; }
	public bool RemoveCalled { get; private set; }
	public bool TurnPassedCalled { get; private set; }

	public IEffectTarget<TTarget>? LastApplyTarget { get; private set; }
	public IEffectTarget<TTarget>? LastRemoveTarget { get; private set; }
	public IEffectTarget<TTarget>? LastTurnTarget { get; private set; }

	public bool Stackable { get; init; }

	public TestConcreteStatusEffect(
		string name = "Test Effect",
		string description = "Test Description",
		int duration = 1,
		bool isStackable = false
	) : base(name, description, duration, isStackable)
	{
	}

	public override void OnApply(IEffectTarget<TTarget> target)
	{
		ApplyCalled = true;
		LastApplyTarget = target;
	}

	public override void OnRemove(IEffectTarget<TTarget> target)
	{
		RemoveCalled = true;
		LastRemoveTarget = target;
	}

	public override void OnTurnPassed(IEffectTarget<TTarget> target)
	{
		TurnPassedCalled = true;
		LastTurnTarget = target;

		if (Duration != Constants.PermanentStatusEffect)
			Duration--;
	}

	public void ResetFlags()
	{
		ApplyCalled = false;
		RemoveCalled = false;
		TurnPassedCalled = false;
	}
}
