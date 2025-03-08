using MPGame.Utils;
using UnityEngine;

namespace MPGame.Props
{
	public abstract class GroundBase : MonoBehaviour
	{
		public abstract GravityType GravityType { get; }
		public abstract Vector3 GetGravityVector();
	}
}