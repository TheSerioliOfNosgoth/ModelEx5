using System;
using System.Collections.Generic;
using SlimDX;

namespace ModelEx
{
	public class RenderInstance : Renderable
	{
		protected Vector3 _position = new Vector3();
		protected Quaternion _rotation = new Quaternion();
		protected Vector3 _scale = new Vector3();

		public readonly VisibilityNode Root = new VisibilityNode();
		public readonly List<RenderInstance> Attachments = new List<RenderInstance>();

		protected Model Model { get; set; }

		public RenderInstance(Vector3 position, Quaternion rotation, Vector3 scale)
		{
			_position = position;
			_rotation = rotation;
			_scale = scale;
		}

		public VisibilityNode FindNode(string name)
		{
			return FindNode(name, Root);
		}

		public VisibilityNode FindNode(string name, VisibilityNode visibilityNode)
		{
			if (visibilityNode.Name == name)
			{
				return visibilityNode;
			}

			foreach (VisibilityNode child in visibilityNode.Nodes)
			{
				visibilityNode = FindNode(name, child);
				if (visibilityNode != null)
				{
					return visibilityNode;
				}
			}

			return null;
		}

		public override void Render()
		{
			Model.Render(Transform, Root);

			foreach (RenderInstance attachment in Attachments)
			{
				attachment.Render(Transform);
			}
		}

		public void Render(Matrix transform)
		{
			Matrix localTransform = transform * Transform;

			Model.Render(localTransform, Root);

			foreach (RenderInstance attachment in Attachments)
			{
				attachment.Render(localTransform);
			}
		}

		public override BoundingSphere GetBoundingSphere()
		{
			return Model.GetBoundingSphere();
		}

		public override void Dispose()
		{
			while (Attachments.Count > 0)
			{
				Attachments[0].Dispose();
				Attachments.RemoveAt(0);
			}
		}
	}
}