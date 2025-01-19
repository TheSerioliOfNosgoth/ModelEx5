using CDC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace ModelEx
{
	public class SceneCDC : Scene
	{
		// Make these private again when parsers don't need them.
		// Mesh loading is quick. No longer necessary to show progress inside DRM loading.
		public static string ProgressStage { get; set; } = "Done";
		public static long progressLevels = 0;
		public static long progressLevel = 0;

		public static int ProgressPercent
		{
			get
			{
				if (progressLevels <= 0 || progressLevel <= 0)
				{
					return 0;
				}

				return (int)((100 * progressLevel) / progressLevels);
			}
		}

		public SceneCDC(CDC.DataFile dataFile, bool includeObjects)
		{
			Name = dataFile.Name;

			if (dataFile.Asset == CDC.Asset.Object)
			{
				for (int m = 0; m < dataFile.ModelCount; m++)
				{
					DynamicInstance modelInstance = new DynamicInstance(dataFile.Name, m, new SlimDX.Vector3(), new SlimDX.Quaternion(), new SlimDX.Vector3(1.0f, 1.0f, 1.0f));
					modelInstance.Name = dataFile.Models[m].Name;
					lock (_renderInstances)
					{
						_renderInstances.Add(modelInstance);
					}
				}
			}
			else
			{
				for (int m = 0; m < dataFile.ModelCount; m++)
				{
					DynamicInstance modelInstance = new DynamicInstance(dataFile.Name, m, new SlimDX.Vector3(), new SlimDX.Quaternion(), new SlimDX.Vector3(1.0f, 1.0f, 1.0f));
					modelInstance.Name = dataFile.Models[m].Name;

					/*if (m == 0)
					{
						SR1Model sr1Model = (SR1Model)dataFile.Models[0];
						RenderResource shapesResource = RenderManager.Instance.Resources[""];
						foreach (Sphere sphere in sr1Model.Spheres)
						{
							SlimDX.Vector3 position = new SlimDX.Vector3(
								0.01f * sphere.position.x,
								0.01f * sphere.position.z,
								0.01f * sphere.position.y
							);

							SlimDX.Vector3 scale = new SlimDX.Vector3(1.0f, 1.0f, 1.0f);
							SlimDX.Quaternion rotation = SlimDX.Quaternion.RotationYawPitchRoll(0.0f, 0.0f, 0.0f);

							ModelParser sphereParser = new ModelParser("sphere");
							sphereParser.BuildModel(shapesResource, RenderResourceShapes.Shape.Sphere);
							StaticInstance sphereInstance = new StaticInstance(sphereParser.Model, position, rotation, scale);
							modelInstance.Attachments.Add(sphereInstance);
						}
					}*/

					lock (_renderInstances)
					{
						_renderInstances.Add(modelInstance);
					}

					if (!includeObjects || m >= (int)dataFile.PortalCount)
					{
						break;
					}
				}

				if (includeObjects && dataFile.IntroCount > 0)
				{
					bool foundUprightSword = false;
					SlimDX.Vector3 saveSwdPos = new SlimDX.Vector3();
					foreach (CDC.Intro intro in dataFile.Intros)
					{
						if (intro.fileName == "saveswd" && Math.Abs(intro.rotation.x) < 3.0f)
						{
							saveSwdPos = new SlimDX.Vector3(
								0.01f * intro.position.x,
								0.01f * intro.position.z,
								0.01f * intro.position.y
							);

							foundUprightSword = true;
							break;
						}
					}

					foreach (CDC.Intro intro in dataFile.Intros)
					{
						SlimDX.Vector3 position = new SlimDX.Vector3(
							0.01f * intro.position.x,
							0.01f * intro.position.z,
							0.01f * intro.position.y
						);

						SlimDX.Vector3 scale = new SlimDX.Vector3(1.0f, 1.0f, 1.0f);

						SlimDX.Quaternion rotation;
						if (dataFile.Game == CDC.Game.Gex || dataFile.Game == CDC.Game.SR1)
						{
							rotation = SlimDX.Quaternion.RotationYawPitchRoll(
								-intro.rotation.z, // Yaw - Easy to spot from direction of raziel, enemies, flagall.
								-intro.rotation.y, // Pitch - Can be seen from the angle of hndtrch in huba6.
								-intro.rotation.x // Roll - Can be seen from the angle of stdorac in oracle3.
							);
						}
						else
						{
							SlimDX.Matrix.RotationX(-intro.rotation.x, out SlimDX.Matrix rotationMatrixX);
							SlimDX.Matrix.RotationY(-intro.rotation.z, out SlimDX.Matrix rotationMatrixY);
							SlimDX.Matrix.RotationZ(-intro.rotation.y, out SlimDX.Matrix rotationMatrixZ);
							SlimDX.Matrix rotationMatrix = rotationMatrixZ * rotationMatrixY * rotationMatrixX;
							SlimDX.Quaternion.RotationMatrix(ref rotationMatrix, out rotation);

							/*rotation = SlimDX.Quaternion.RotationYawPitchRoll(
								-intro.rotation.z,
								-intro.rotation.y,
								-intro.rotation.x
							);*/
						}

						if (intro.fileName == "saveswd" && Math.Abs(intro.rotation.x) >= 3.0f)
						{
							if (foundUprightSword)
							{
								position.X = saveSwdPos.X;
								position.Z = saveSwdPos.Z;
							}

							scale.Z = -1.0f;
						}

						DynamicInstance introInstance = new DynamicInstance(intro.fileName, intro.modelIndex, position, rotation, scale);
						introInstance.Name = intro.name;

						lock (_renderInstances)
						{
							_renderInstances.Add(introInstance);
						}
					}
				}

				if (dataFile.BGInstanceCount > 0)
				{
					foreach (CDC.BGInstance bgInstance in dataFile.BGInstances)
					{
						SlimDX.Matrix rMat = new SlimDX.Matrix()
						{
							M11 = bgInstance.matrix.v0.x,
							M12 = bgInstance.matrix.v0.y,
							M13 = bgInstance.matrix.v0.z,
							M14 = bgInstance.matrix.v0.w,
							M21 = bgInstance.matrix.v1.x,
							M22 = bgInstance.matrix.v1.y,
							M23 = bgInstance.matrix.v1.z,
							M24 = bgInstance.matrix.v1.w,
							M31 = bgInstance.matrix.v2.x,
							M32 = bgInstance.matrix.v2.y,
							M33 = bgInstance.matrix.v2.z,
							M34 = bgInstance.matrix.v2.w,
							M41 = bgInstance.matrix.v3.x,
							M42 = bgInstance.matrix.v3.y,
							M43 = bgInstance.matrix.v3.z,
							M44 = bgInstance.matrix.v3.w,
						};

						SlimDX.Matrix lMat = new SlimDX.Matrix()
						{
							M11 = rMat.M11, M12 = rMat.M13, M13 = rMat.M12, M14 = rMat.M14,
							M21 = rMat.M31, M22 = rMat.M33, M23 = rMat.M32, M24 = rMat.M34,
							M31 = rMat.M21, M32 = rMat.M23, M33 = rMat.M22, M34 = rMat.M24,
							M41 = rMat.M41, M42 = rMat.M43, M43 = rMat.M42, M44 = rMat.M44
						};

						lMat.Decompose(out SlimDX.Vector3 scale, out SlimDX.Quaternion rotation, out SlimDX.Vector3 position);
						position *= 0.01f;

						/*SlimDX.Vector3 position = new SlimDX.Vector3(
							0.01f * bgInstance.matrix.v3.x,
							0.01f * bgInstance.matrix.v3.z,
							0.01f * bgInstance.matrix.v3.y
						);

						SlimDX.Vector3 scale = new SlimDX.Vector3(1.0f, 1.0f, 1.0f);

						SlimDX.Quaternion rotation = new SlimDX.Quaternion();*/

						DynamicInstance instance = new DynamicInstance(dataFile.Name, bgInstance.modelIndex + (int)dataFile.PortalCount + 1, position, rotation, scale);
						instance.Name = bgInstance.name;

						lock (_renderInstances)
						{
							_renderInstances.Add(instance);
						}
					}
				}
			}
		}

		public SceneCDC(CDC.DataFile dataFile, RenderResource resource)
		{
			Name = dataFile.Name;

			for (int m = 0; m < dataFile.ModelCount; m++)
			{
				DynamicInstance instance = new DynamicInstance(dataFile.Name, m, new SlimDX.Vector3(), new SlimDX.Quaternion(), new SlimDX.Vector3(1.0f, 1.0f, 1.0f), resource);
				instance.Name = dataFile.Models[m].Name + "-" + m.ToString();
				lock (_renderInstances)
				{
					_renderInstances.Add(instance);
				}

				if (dataFile.Asset == CDC.Asset.Unit)
				{
					break;
				}
			}
		}

		public override void Render()
		{
			SlimDX.Direct3D11.DepthStencilState oldDSState = DeviceManager.Instance.context.OutputMerger.DepthStencilState;
			SlimDX.Direct3D11.BlendState oldBlendState = DeviceManager.Instance.context.OutputMerger.BlendState;
			SlimDX.Direct3D11.RasterizerState oldRasterizerState = DeviceManager.Instance.context.Rasterizer.State;
			SlimDX.Direct3D11.VertexShader oldVertexShader = DeviceManager.Instance.context.VertexShader.Get();
			SlimDX.Direct3D11.Buffer[] oldVSCBuffers = DeviceManager.Instance.context.VertexShader.GetConstantBuffers(0, 10);
			SlimDX.Direct3D11.PixelShader oldPixelShader = DeviceManager.Instance.context.PixelShader.Get();
			SlimDX.Direct3D11.Buffer[] oldPSCBuffers = DeviceManager.Instance.context.PixelShader.GetConstantBuffers(0, 10);
			SlimDX.Direct3D11.ShaderResourceView[] oldShaderResources = DeviceManager.Instance.context.PixelShader.GetShaderResources(0, 10);
			SlimDX.Direct3D11.GeometryShader oldGeometryShader = DeviceManager.Instance.context.GeometryShader.Get();

			base.Render();

			DeviceManager.Instance.context.OutputMerger.DepthStencilState = oldDSState;
			DeviceManager.Instance.context.OutputMerger.BlendState = oldBlendState;
			DeviceManager.Instance.context.Rasterizer.State = oldRasterizerState;
			DeviceManager.Instance.context.VertexShader.Set(oldVertexShader);
			DeviceManager.Instance.context.VertexShader.SetConstantBuffers(oldVSCBuffers, 0, 10);
			DeviceManager.Instance.context.PixelShader.Set(oldPixelShader);
			DeviceManager.Instance.context.PixelShader.SetConstantBuffers(oldPSCBuffers, 0, 10);
			DeviceManager.Instance.context.PixelShader.SetShaderResources(oldShaderResources, 0, 10);
			DeviceManager.Instance.context.GeometryShader.Set(oldGeometryShader);

			lock (_renderInstances)
			{
				foreach (RenderInstance instance in _renderInstances)
				{
					if (instance.Name == null)
					{
						continue;
					}

					SlimDX.Matrix world = instance.Transform;
					SlimDX.Matrix view = CameraManager.Instance.FrameCamera.View;
					SlimDX.Matrix projection = CameraManager.Instance.FrameCamera.Perspective;

					//SlimDX.Matrix viewProj = view * projection;
					SlimDX.Matrix worldViewProjection = world * view * projection;
					SlimDX.Direct3D11.Viewport vp = DeviceManager.Instance.context.Rasterizer.GetViewports()[0];
					SlimDX.Vector3 position3D = SlimDX.Vector3.Project(SlimDX.Vector3.Zero, vp.X, vp.Y, vp.Width, vp.Height, vp.MinZ, vp.MaxZ, worldViewProjection);
					SlimDX.Vector2 position2D = new SlimDX.Vector2(position3D.X, position3D.Y);

					if (position3D.Z < vp.MaxZ)
					{
						SlimDX.Vector3 objPos = SlimDX.Vector3.Zero;
						objPos = SlimDX.Vector3.TransformCoordinate(objPos, instance.Transform);
						SlimDX.Vector3 camPos = CameraManager.Instance.FrameCamera.eye;
						SlimDX.Vector3 objOffset = objPos - camPos;
						float scale = Math.Min(2.0f, 5.0f / (float)Math.Sqrt(Math.Max(1.0f, objOffset.Length())));

						RenderManager.Instance.DrawString(instance.Name, position2D, 16 * scale, new SlimDX.Color4(1.0f, 1.0f, 1.0f));
					}
				}
			}

			DeviceManager.Instance.context.OutputMerger.DepthStencilState = oldDSState;
			DeviceManager.Instance.context.OutputMerger.BlendState = oldBlendState;
			DeviceManager.Instance.context.Rasterizer.State = oldRasterizerState;
			DeviceManager.Instance.context.VertexShader.Set(oldVertexShader);
			DeviceManager.Instance.context.VertexShader.SetConstantBuffers(oldVSCBuffers, 0, 10);
			DeviceManager.Instance.context.PixelShader.Set(oldPixelShader);
			DeviceManager.Instance.context.PixelShader.SetConstantBuffers(oldPSCBuffers, 0, 10);
			DeviceManager.Instance.context.PixelShader.SetShaderResources(oldShaderResources, 0, 10);
			DeviceManager.Instance.context.GeometryShader.Set(oldGeometryShader);
		}
	}
}
