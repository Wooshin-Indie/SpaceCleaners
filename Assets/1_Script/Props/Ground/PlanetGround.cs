using MPGame.Utils;
using UnityEngine;

namespace MPGame.Props
{
	public class PlanetGround : GroundBase
	{
		public override GravityType GravityType { get => GravityType.Point; }
		public override Vector3 GetGravityVector()
		{
			return transform.position;
		}
	}
}