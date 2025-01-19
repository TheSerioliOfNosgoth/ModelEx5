using System;
using System.Collections.Generic;
using SlimDX;

namespace ModelEx
{
	public class StaticInstance : RenderInstance
	{
		public StaticInstance(Model model, Vector3 position, Quaternion rotation, Vector3 scale)
			: base(position, rotation, scale)
		{
			Model = model;

			Root.Name = Model.Root.Name;
			Root.Visible = Model.Root.Visible;
			Root.Transform = Model.Root.Transform;
			foreach (ModelNode modelNode in Model.Root.Nodes)
			{
				VisibilityNode visibilityNode = new VisibilityNode();
				visibilityNode.Name = modelNode.Name;
				visibilityNode.Visible = modelNode.Visible;
				visibilityNode.Transform = modelNode.Transform;
				Root.Nodes.Add(visibilityNode);
			}

			Matrix.RotationQuaternion(ref _rotation, out Matrix rotationMatrix);
			Matrix scaleMatrix = Matrix.Scaling(_scale);
			Transform = scaleMatrix * rotationMatrix * Matrix.Translation(
				_position.X,
				_position.Y,
				_position.Z
			);
		}
	}
}