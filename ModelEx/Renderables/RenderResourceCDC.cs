using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GexFile = CDC.GexFile;
using SR1File = CDC.SR1File;
using SR2File = CDC.SR2File;
using DefianceFile = CDC.DefianceFile;
using TRLFile = CDC.TRLFile;
using TPages = BenLincoln.TheLostWorlds.CDTextures.PSXTextureDictionary;
using TextureTile = BenLincoln.TheLostWorlds.CDTextures.PSXTextureTile;
using Gex3PSXTextureFile = BenLincoln.TheLostWorlds.CDTextures.Gex3PSXVRMTextureFile;
using SR1PCTextureFile = BenLincoln.TheLostWorlds.CDTextures.SoulReaverPCTextureFile;
using SR1PSXTextureFile = BenLincoln.TheLostWorlds.CDTextures.SoulReaverPSXCRMTextureFile;
using SR1DCTextureFile = BenLincoln.TheLostWorlds.CDTextures.SoulReaverDCTextureFile;
using SR2PCTextureFile = BenLincoln.TheLostWorlds.CDTextures.SoulReaver2PCVRMTextureFile;
using TRLPCTextureFile = BenLincoln.TheLostWorlds.CDTextures.TombRaiderPCDRMTextureFile;

namespace ModelEx
{
	public class RenderResourceCDC : RenderResource
	{
		public CDC.DataFile File { get; private set; }
		public LoadRequestCDC LoadRequest { get; private set; }

		public const string TextureExtension = ".png";
		private CDC.ExportOptions ExportOptions;

		public RenderResourceCDC(CDC.DataFile dataFile, LoadRequestCDC loadRequest)
			: base(dataFile.Name)
		{
			File = dataFile;
			LoadRequest = (LoadRequestCDC)loadRequest.Clone();
			ExportOptions = LoadRequest.ExportOptions;
		}

		public void LoadModels()
		{
			foreach (Model model in Models)
			{
				model.Dispose();
			}

			for (int modelIndex = 0; modelIndex < File.Models.Length; modelIndex++)
			{
				SRModelParser modelParser = new SRModelParser(File.Name, File);
				modelParser.BuildModel(this, modelIndex, ExportOptions);
				if (File.Asset == CDC.Asset.Unit)
				{
					Models.Add(modelParser.Model);
				}
				else
				{
					Models.Add(modelParser.Model);
				}
			}
		}

		public void LoadTextures(string folderName)
		{
			FileShaderResourceViewDictionary.Clear();

			Type currentFileType = File.GetType();
			if (currentFileType == typeof(TRLFile))
			{
				try
				{
					string fileName = Path.Combine(folderName, File.Name + ".drm");
					TRLPCTextureFile textureFile = new TRLPCTextureFile(fileName);

					SceneCDC.progressLevel = 0;
					SceneCDC.progressLevels = textureFile.TextureCount;
					SceneCDC.ProgressStage = "Loading Textures";

					for (int t = 0; t < textureFile.TextureCount; t++)
					{
						String textureName = CDC.Utility.GetPS2TextureName(File.Name, (int)textureFile.TextureDefinitions[t].ID) + TextureExtension;

						System.IO.MemoryStream stream = textureFile.GetDataAsStream(t);
						//textureFile.ExportFile(t, "C:\\Users\\A\\Desktop\\Lara\\" + textureName);
						if (stream != null)
						{
							if (textureFile.TextureDefinitions[t].Format == BenLincoln.TheLostWorlds.CDTextures.DRMFormat.Uncompressed)
							{
								MemoryStream stream2 = textureFile.GetUncompressedDataAsStream2(t);
								AddTexture(stream2, textureName);
							}
							else
							{
								AddTexture(stream, textureName);
							}
							_TexturesAsPNGs.Add(textureName, textureFile.GetTextureAsBitmap(t));
						}

						SceneCDC.progressLevel++;
					}
				}
				catch (FileNotFoundException)
				{
					Console.WriteLine("Error: couldn't find a texture file");
				}
				catch (Exception ex)
				{
					Console.Write(ex.ToString());
				}
			}
			else if (currentFileType == typeof(SR2File) ||
					 currentFileType == typeof(DefianceFile))
			{
				try
				{
					string fileName = Path.Combine(folderName, File.Name + ".vrm");
					SR2PCTextureFile textureFile = new SR2PCTextureFile(fileName);

					SceneCDC.progressLevel = 0;
					SceneCDC.progressLevels = textureFile.TextureCount;
					SceneCDC.ProgressStage = "Loading Textures";

					for (int t = 0; t < textureFile.TextureCount; t++)
					{
						String textureName = CDC.Utility.GetPS2TextureName(File.Name, textureFile.TextureDefinitions[t].Flags1) + TextureExtension;

						System.IO.MemoryStream stream = textureFile.GetDataAsStream(t);
						//textureFile.ExportFile(t, "C:\\Users\\A\\Desktop\\" + textureName);
						if (stream != null)
						{
							AddTexture(stream, textureName);
							_TexturesAsPNGs.Add(textureName, textureFile.GetTextureAsBitmap(t));
						}

						SceneCDC.progressLevel++;
					}
				}
				catch (FileNotFoundException)
				{
					Console.WriteLine("Error: couldn't find a texture file");
				}
				catch (Exception ex)
				{
					Console.Write(ex.ToString());
				}
			}
			else if (currentFileType == typeof(SR1File))
			{
				if (File.Platform == CDC.Platform.PC)
				{
					try
					{
						string fileName = Path.Combine(folderName, "textures.big");
						SR1PCTextureFile textureFile = new SR1PCTextureFile(fileName);

						SceneCDC.progressLevel = 0;
						SceneCDC.progressLevels = File.GetNumMaterials();
						SceneCDC.ProgressStage = "Loading Textures";

						foreach (CDC.IModel cdcModel in File.Models)
						{
							foreach (CDC.Material material in cdcModel.Materials)
							{
								if (material.textureUsed)
								{
									System.IO.MemoryStream stream = textureFile.GetDataAsStream(material.textureID);
									if (stream != null)
									{
										String textureName = CDC.Utility.GetSoulReaverPCOrDreamcastTextureName(File.Name, material.textureID) + TextureExtension;
										AddTexture(stream, textureName);
										if (!_TexturesAsPNGs.ContainsKey(textureName))
										{
											_TexturesAsPNGs.Add(textureName, textureFile.GetTextureAsBitmap(material.textureID));
										}
									}
								}

								SceneCDC.progressLevel++;
							}
						}
					}
					catch (FileNotFoundException)
					{
						Console.WriteLine("Error: couldn't find a texture file");
					}
					catch (Exception ex)
					{
						Console.Write(ex.ToString());
					}
				}
				if (File.Platform == CDC.Platform.Remaster)
				{
					try
					{
						string fileName0 = Path.Combine(folderName, "TEXTURES.BIG");
						string fileName1 = Path.Combine(folderName, "MOVIES.BIG");
						string fileName2 = Path.Combine(folderName, "BONUS.BIG");
						SR1PCTextureFile textureFile0 = new SR1PCTextureFile(fileName0);
						SR1PCTextureFile textureFile1 = new SR1PCTextureFile(fileName1);
						SR1PCTextureFile textureFile2 = new SR1PCTextureFile(fileName2);

						SceneCDC.progressLevel = 0;
						SceneCDC.progressLevels = File.GetNumMaterials();
						SceneCDC.ProgressStage = "Loading Textures";

						foreach (CDC.IModel cdcModel in File.Models)
						{
							foreach (CDC.Material material in cdcModel.Materials)
							{
								if (material.textureUsed)
								{
									SR1PCTextureFile textureFile;
									MemoryStream stream;
									int textureID = 0;
									if (material.textureID >= 1312)
									{
										textureFile = textureFile2;
										textureID = material.textureID - 1312;
									}
									else if (material.textureID >= 1285)
									{
										textureFile = textureFile1;
										textureID = material.textureID - 1285;
									}
									else
									{
										textureFile = textureFile0;
										textureID = material.textureID;
									}

									stream = textureFile.GetDataAsStream(textureID);
									if (stream != null)
									{
										String textureName = CDC.Utility.GetSoulReaverPCOrDreamcastTextureName(File.Name, material.textureID) + TextureExtension;
										AddTexture(stream, textureName);
										if (!_TexturesAsPNGs.ContainsKey(textureName))
										{
											_TexturesAsPNGs.Add(textureName, textureFile.GetTextureAsBitmap(textureID));
										}
									}
								}

								SceneCDC.progressLevel++;
							}
						}
					}
					catch (FileNotFoundException)
					{
						Console.WriteLine("Error: couldn't find a texture file");
					}
					catch (Exception ex)
					{
						Console.Write(ex.ToString());
					}
				}
				else if (File.Platform == CDC.Platform.Dreamcast)
				{
					try
					{
						string fileName = Path.Combine(folderName, "textures.vq");
						SR1DCTextureFile textureFile = new SR1DCTextureFile(fileName);

						SceneCDC.progressLevel = 0;
						SceneCDC.progressLevels = File.GetNumMaterials();
						SceneCDC.ProgressStage = "Loading Textures";

						foreach (CDC.IModel cdcModel in File.Models)
						{
							foreach (CDC.Material material in cdcModel.Materials)
							{
								if (material.textureUsed)
								{
									int textureID = material.textureID;
									System.IO.MemoryStream stream = textureFile.GetDataAsStream(textureID);
									if (stream != null)
									{
										String textureName = CDC.Utility.GetSoulReaverPCOrDreamcastTextureName(File.Name, material.textureID) + TextureExtension;

										AddTexture(stream, textureName);

										if (!_TexturesAsPNGs.ContainsKey(textureName))
										{
											_TexturesAsPNGs.Add(textureName, textureFile.GetTextureAsBitmap(material.textureID));
										}
									}
								}

								SceneCDC.progressLevel++;
							}
						}
					}
					catch (FileNotFoundException)
					{
						Console.WriteLine("Error: couldn't find a texture file");
					}
					catch (Exception ex)
					{
						Console.Write(ex.ToString());
					}
				}
				else
				{
					try
					{
						TPages tPages = ((SR1File)File).TPages;

						/*foreach (SRModel srModel in File.Models)
						{
							foreach (CDC.Polygon polygon in srModel.Polygons)
							{
								TextureTile tile = new TextureTile()
								{
									textureID = polygon.material.textureID,
									tPage = polygon.material.texturePage,
									clut = polygon.material.clutValue,
									textureUsed = polygon.material.textureUsed,
									visible = polygon.material.visible,
									u = new int[3],
									v = new int[3],
								};

								tile.u[0] = (int)(srModel.Geometry.UVs[polygon.v1.UVID].u * 255);
								tile.v[0] = (int)(srModel.Geometry.UVs[polygon.v1.UVID].v * 255);
								tile.u[1] = (int)(srModel.Geometry.UVs[polygon.v2.UVID].u * 255);
								tile.v[1] = (int)(srModel.Geometry.UVs[polygon.v2.UVID].v * 255);
								tile.u[2] = (int)(srModel.Geometry.UVs[polygon.v3.UVID].u * 255);
								tile.v[2] = (int)(srModel.Geometry.UVs[polygon.v3.UVID].v * 255);

								tPages.AddTextureTile2(tile);
							}
						}*/

						bool drawGreyscaleFirst = false;
						bool quantizeBounds = true;
						string fileName = Path.Combine(folderName, File.Name + ".crm");
						SR1PSXTextureFile textureFile = new SR1PSXTextureFile(fileName);
						textureFile.BuildTexturesFromPolygonData(tPages, drawGreyscaleFirst, quantizeBounds, ExportOptions);
						//textureFile.ExportAllPaletteVariations(tPages, false);

						// For all models
						for (int t = 0; t < textureFile.TextureCount; t++)
						{
							String textureName = CDC.Utility.GetPlayStationTextureNameDefault(File.Name, t) + TextureExtension;

							System.IO.MemoryStream stream = textureFile.GetDataAsStream(t);
							if (stream != null)
							{
								AddTexture(stream, textureName);
							}
							//string exportedTextureFileName = Path.ChangeExtension(textureName, "png");
							//_TexturesAsPNGs.Add(exportedTextureFileName, textureFile.GetTextureAsBitmap(t));
							Bitmap b = textureFile.GetTextureAsBitmap(t);
							// this is a hack that's being done here for now because I don't know for sure which of the flags/attributes controls
							// textures that should be alpha-masked. Alpha-masking EVERY texture is expensive.
							if (ExportOptions.AlsoInferAlphaMaskingFromTexturePixels)
							{
								if (BitmapHasTransparentPixels(b))
								{
									for (int modelNum = 0; modelNum < File.Models.Length; modelNum++)
									{
										if (t < File.Models[modelNum].Materials.Length)
										{
											File.Models[modelNum].Materials[t].UseAlphaMask = true;
										}
									}
								}
							}
							_TexturesAsPNGs.Add(textureName, b);

							// dump all textures as PNGs for debugging
							//Bitmap exportedTexture = textureFile.GetTextureAsBitmap(t);
							//string exportedTextureFileName = Path.ChangeExtension(textureName, "png");
							//exportedTexture.Save(exportedTextureFileName, ImageFormat.Png);
							//texNum = 0;
							//foreach (Bitmap tex in _Textures)
							//{
							//    tex.Save(@"C:\Debug\Tex-" + texNum + ".png", ImageFormat.Png);
							//    texNum++;
							//}
						}

						// for models that use index/CLUT textures, if the user has enabled this option
						if (ExportOptions.UseEachUniqueTextureCLUTVariation)
						{
							foreach (int textureID in textureFile.TexturesByCLUT.Keys)
							{
								Dictionary<ushort, Bitmap> textureCLUTCollection = textureFile.TexturesByCLUT[textureID];
								foreach (ushort clut in textureCLUTCollection.Keys)
								{
									String textureName = CDC.Utility.GetPlayStationTextureNameWithCLUT(File.Name, textureID, clut) + TextureExtension;
									System.IO.MemoryStream stream = textureFile.GetTextureWithCLUTAsStream(textureID, clut);
									if (stream != null)
									{
										AddTexture(stream, textureName);
									}
									Bitmap b = textureFile.TexturesByCLUT[textureID][clut];
									_TexturesAsPNGs.Add(textureName, b);
								}
							}
						}
					}
					catch (FileNotFoundException)
					{
						Console.WriteLine("Error: couldn't find a texture file");
					}
					catch (Exception ex)
					{
						Console.Write(ex.ToString());
					}
				}
			}
			else
			{
				try
				{
					TPages tPages = ((GexFile)File).TPages;

					/*foreach (SRModel srModel in File.Models)
					{
						foreach (CDC.Polygon polygon in srModel.Polygons)
						{
							TextureTile tile = new TextureTile()
							{
								textureID = polygon.material.textureID,
								tPage = polygon.material.texturePage,
								clut = polygon.material.clutValue,
								textureUsed = polygon.material.textureUsed,
								visible = polygon.material.visible,
								u = new int[3],
								v = new int[3],
							};

							tile.u[0] = (int)(srModel.Geometry.UVs[polygon.v1.UVID].u * 255);
							tile.v[0] = (int)(srModel.Geometry.UVs[polygon.v1.UVID].v * 255);
							tile.u[1] = (int)(srModel.Geometry.UVs[polygon.v2.UVID].u * 255);
							tile.v[1] = (int)(srModel.Geometry.UVs[polygon.v2.UVID].v * 255);
							tile.u[2] = (int)(srModel.Geometry.UVs[polygon.v3.UVID].u * 255);
							tile.v[2] = (int)(srModel.Geometry.UVs[polygon.v3.UVID].v * 255);

							tPages.AddTextureTile2(tile);
						}
					}*/

					bool drawGreyscaleFirst = false;
					bool quantizeBounds = true;
					string fileName = Path.Combine(folderName, File.Name + ".vrm");
					Gex3PSXTextureFile textureFile = new Gex3PSXTextureFile(fileName);
					textureFile.BuildTexturesFromPolygonData(tPages, drawGreyscaleFirst, quantizeBounds, ExportOptions);

					// For all models
					for (int t = 0; t < textureFile.TextureCount; t++)
					{
						String textureName = CDC.Utility.GetPlayStationTextureNameDefault(File.Name, t) + TextureExtension;

						System.IO.MemoryStream stream = textureFile.GetDataAsStream(t);
						if (stream != null)
						{
							AddTexture(stream, textureName);
						}
						//string exportedTextureFileName = Path.ChangeExtension(textureName, "png");
						//_TexturesAsPNGs.Add(exportedTextureFileName, textureFile.GetTextureAsBitmap(t));
						Bitmap b = textureFile.GetTextureAsBitmap(t);
						// this is a hack that's being done here for now because I don't know for sure which of the flags/attributes controls
						// textures that should be alpha-masked. Alpha-masking EVERY texture is expensive.
						if (ExportOptions.AlsoInferAlphaMaskingFromTexturePixels)
						{
							if (BitmapHasTransparentPixels(b))
							{
								for (int modelNum = 0; modelNum < File.Models.Length; modelNum++)
								{
									if (t < File.Models[modelNum].Materials.Length)
									{
										File.Models[modelNum].Materials[t].UseAlphaMask = true;
									}
								}
							}
						}
						_TexturesAsPNGs.Add(textureName, b);

						// dump all textures as PNGs for debugging
						//Bitmap exportedTexture = textureFile.GetTextureAsBitmap(t);
						//string exportedTextureFileName = Path.ChangeExtension(textureName, "png");
						//exportedTexture.Save(exportedTextureFileName, ImageFormat.Png);
						//texNum = 0;
						//foreach (Bitmap tex in _Textures)
						//{
						//    tex.Save(@"C:\Debug\Tex-" + texNum + ".png", ImageFormat.Png);
						//    texNum++;
						//}
					}

					// for models that use index/CLUT textures, if the user has enabled this option
					if (ExportOptions.UseEachUniqueTextureCLUTVariation)
					{
						foreach (int textureID in textureFile.TexturesByCLUT.Keys)
						{
							Dictionary<ushort, Bitmap> textureCLUTCollection = textureFile.TexturesByCLUT[textureID];
							foreach (ushort clut in textureCLUTCollection.Keys)
							{
								String textureName = CDC.Utility.GetPlayStationTextureNameWithCLUT(File.Name, textureID, clut) + TextureExtension;
								System.IO.MemoryStream stream = textureFile.GetTextureWithCLUTAsStream(textureID, clut);
								if (stream != null)
								{
									AddTexture(stream, textureName);
								}
								Bitmap b = textureFile.TexturesByCLUT[textureID][clut];
								_TexturesAsPNGs.Add(textureName, b);
							}
						}
					}
				}
				catch (FileNotFoundException)
				{
					Console.WriteLine("Error: couldn't find a texture file");
				}
				catch (Exception ex)
				{
					Console.Write(ex.ToString());
				}
			}
		}
		
		// make sure that overwriting exported files isn't silently failing
		protected void DeleteExistingFile(string path)
		{
			if (System.IO.File.Exists(path))
			{
				try
				{
					System.IO.File.Delete(path);
				}
				catch (Exception ex)
				{
					Console.WriteLine(string.Format("Couldn't delete existing file '{0}': {1}", path, ex.Message));
				}
			}
		}

		public void ExportToFile(string fileName)
		{
			string filePath = Path.GetFullPath(fileName);
			DeleteExistingFile(filePath);
			File.ExportToFile(fileName, ExportOptions);
			string baseExportDirectory = Path.GetDirectoryName(fileName);
			foreach (string textureFileName in _TexturesAsPNGs.Keys)
			{
				string texturePath = Path.Combine(baseExportDirectory, textureFileName);
				DeleteExistingFile(texturePath);
				_TexturesAsPNGs[textureFileName].Save(texturePath, ImageFormat.Png);
			}
		}

		protected static bool BitmapHasTransparentPixels(Bitmap b)
		{
			for (int y = 0; y < b.Height; y++)
			{
				for (int x = 0; x < b.Width; x++)
				{
					Color c = b.GetPixel(x, y);
					if (c.A == 0)
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
