using AshesOfVelsingrad.Interfaces;
using Godot;

namespace AshesOfVelsingrad.tutotial;

public partial class ItemDetectionArea : Area3D
{
	[Export]
	private NodePath _characterThatDetectPath = null!;

	[Export]
	private NodePath _pointOfInterestPath = null!;

	private CharacterBody3D? _characterThatDetect;
	private Marker3D? _pointOfInterest;

	private async void OnBodyEntered(Node3D body)
	{
		if (body == _characterThatDetect &&
			_pointOfInterest != null &&
			body is NpcSystem npc)
		{
			npc.ToIdle(true);
			await ToSignal(GetTree().CreateTimer(1),
				SceneTreeTimer.SignalName.Timeout);
			npc.ToFollowingSpecificPoint(_pointOfInterest);
		}
	}

	public override void _Ready()
	{
		_characterThatDetect = GetNode<CharacterBody3D>(_characterThatDetectPath);
		_pointOfInterest = GetNode<Marker3D>(_pointOfInterestPath);
		BodyEntered += OnBodyEntered;
	}
}
