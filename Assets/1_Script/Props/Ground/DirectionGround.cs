using MPGame.Utils;
using UnityEngine;

namespace MPGame.Props
{
	public class DirectionGround : GroundBase
	{

		public override GravityType GravityType { get => GravityType.Direction; }
		public override Vector3 GetGravityVector()
		{
			return -transform.up;
		}
	}
}