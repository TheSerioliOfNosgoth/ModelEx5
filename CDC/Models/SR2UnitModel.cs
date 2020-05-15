using System;
using System.IO;
using System.Collections.Generic;

namespace CDC.Objects.Models
{
    public class SR2UnitModel : SR2Model
    {
        protected UInt32 m_uOctTreeCount;
        protected UInt32 m_uOctTreeStart;
        protected UInt32 m_uSpectralVertexStart;
        protected UInt32 m_uSpectralColourStart;

        protected SR2UnitModel(BinaryReader xReader, UInt32 uDataStart, UInt32 uModelData, String strModelName, Platform ePlatform, UInt32 uVersion)
            : base(xReader, uDataStart, uModelData, strModelName, ePlatform, uVersion)
        {
            // xReader.BaseStream.Position += 0x04;
            // m_uInstanceCount = xReader.ReadUInt32();
            // m_uInstanceStart = m_uDataStart + xReader.ReadUInt32();
            xReader.BaseStream.Position = _modelData + 0x0C;

            _vertexCount = xReader.ReadUInt32();
            _polygonCount = 0; // xReader.ReadUInt32(); // Length = 0x14
            xReader.BaseStream.Position += 0x08;
            _vertexStart = _dataStart + xReader.ReadUInt32();
            _polygonStart = 0; // m_uDataStart + xReader.ReadUInt32();
            xReader.BaseStream.Position += 0x18;
            m_uSpectralVertexStart = _dataStart + xReader.ReadUInt32();
            xReader.BaseStream.Position += 0x04; // m_uMaterialColourStart
            m_uSpectralColourStart = _dataStart + xReader.ReadUInt32();
            _materialStart = 0;
            _materialCount = 0;
            m_uOctTreeCount = xReader.ReadUInt32();
            m_uOctTreeStart = _dataStart + xReader.ReadUInt32();
            _groupCount = m_uOctTreeCount;

            _trees = new Tree[_groupCount];
        }

        public static SR2UnitModel Load(BinaryReader xReader, UInt32 uDataStart, UInt32 uModelData, String strModelName, Platform ePlatform, UInt32 uVersion)
        {
            SR2UnitModel xModel = new SR2UnitModel(xReader, uDataStart, uModelData, strModelName, ePlatform, uVersion);
            xModel.ReadData(xReader);
            return xModel;
        }

        protected override void ReadVertex(BinaryReader xReader, int v)
        {
            base.ReadVertex(xReader, v);

            _geometry.PositionsPhys[v] = _geometry.PositionsRaw[v];
            _geometry.PositionsAltPhys[v] = _geometry.PositionsPhys[v];

            _geometry.Vertices[v].colourID = v;

            _geometry.Colours[v] = xReader.ReadUInt32();
            _geometry.ColoursAlt[v] = _geometry.Colours[v];

            _geometry.Vertices[v].UVID = v;

            UInt16 vU = xReader.ReadUInt16();
            UInt16 vV = xReader.ReadUInt16();

            _geometry.UVs[v].u = Utility.BizarreFloatToNormalFloat(vU);
            _geometry.UVs[v].v = Utility.BizarreFloatToNormalFloat(vV);
        }

        protected override void ReadVertices(BinaryReader xReader)
        {
            base.ReadVertices(xReader);

            ReadSpectralData(xReader);
        }

        protected virtual void ReadSpectralData(BinaryReader xReader)
        {
            if (m_uSpectralColourStart != 0)
            {
                // Spectral Colours
                xReader.BaseStream.Position = m_uSpectralColourStart;
                for (int v = 0; v < _vertexCount; v++)
                {
                    UInt32 uShiftColour = xReader.ReadUInt32();
                    UInt32 uAlpha = _geometry.ColoursAlt[v] & 0xFF000000;
                    UInt32 uRGB = uShiftColour & 0x00FFFFFF;
                    _geometry.ColoursAlt[v] = uAlpha | uRGB;
                }
            }

            if (m_uSpectralVertexStart != 0)
            {
                // Spectral vertices
                xReader.BaseStream.Position = _modelData + 0x2C;
                UInt32 uCurrentIndexPosition = xReader.ReadUInt32();
                UInt32 uCurrentSpectralVertex = m_uSpectralVertexStart;
                while (true)
                {
                    xReader.BaseStream.Position = uCurrentIndexPosition;
                    Int32 iVertex = xReader.ReadInt32();
                    uCurrentIndexPosition = (UInt32)xReader.BaseStream.Position;

                    if (iVertex == -1)
                    {
                        break;
                    }

                    xReader.BaseStream.Position = uCurrentSpectralVertex;
                    ShiftVertex xShiftVertex;
                    xShiftVertex.basePos.x = (float)xReader.ReadInt16();
                    xShiftVertex.basePos.y = (float)xReader.ReadInt16();
                    xShiftVertex.basePos.z = (float)xReader.ReadInt16();
                    uCurrentSpectralVertex = (UInt32)xReader.BaseStream.Position;

                    _geometry.PositionsAltPhys[iVertex] = xShiftVertex.basePos;
                }
            }
        }

        protected override void ReadPolygons(BinaryReader xReader)
        {
            Material xMaterial = new Material();
            xMaterial.textureID = 0;
            xMaterial.colour = 0xFFFFFFFF;
            _materialsList.Add(xMaterial);

            List<Mesh> xMeshes = new List<Mesh>();
            List<int> xMeshPositions = new List<int>();
            List<TreePolygon> treePolygons = new List<TreePolygon>((Int32)_vertexCount * 3);

            for (UInt32 t = 0; t < m_uOctTreeCount; t++)
            {
                xReader.BaseStream.Position = m_uOctTreeStart + (t * 0x60);

                xReader.BaseStream.Position += 0x2C;
                bool drawTester = ((xReader.ReadUInt32() & 1) != 1);
                UInt32 uOctID = xReader.ReadUInt32();
                xReader.BaseStream.Position += 0x10;
                UInt32 uDataPos = xReader.ReadUInt32();
                xReader.BaseStream.Position += 0x08;
                // In each terrain group, vertices start from part way through the array.
                UInt32 uStartIndex = xReader.ReadUInt32();
                //UInt32 uIndexCount = xReader.ReadUInt32();

                _trees[t] = ReadOctTree(xReader, treePolygons, uDataPos, _trees[t], xMeshes, xMeshPositions, 0, uStartIndex);
            }

            _polygonCount = (UInt32)treePolygons.Count;
            _polygons = new Polygon[_polygonCount];

            int currentPosition = 0, currentPolygon = 0;
            for (int m = 0; m < xMeshes.Count; m++)
            {
                FinaliseMesh(treePolygons, currentPosition, xMeshes[m], xMaterial, ref currentPolygon);
                currentPosition = xMeshPositions[m];
            }
            _materialCount = (UInt32)_materialsList.Count;

            return;
        }

        protected virtual Tree ReadOctTree(BinaryReader xReader, List<TreePolygon> treePolygons, UInt32 uDataPos, Tree xParentTree, List<Mesh> xMeshes, List<int> xMeshPositions, UInt32 uDepth, UInt32 uStartIndex)
        {
            if (uDataPos == 0)
            {
                return null;
            }

            xReader.BaseStream.Position = uDataPos + 0x34;
            Int32 iSubTreeCount = xReader.ReadInt32();

            Tree xTree = null;
            Mesh xMesh = null;

            UInt32 uMaxDepth = 0;

            if (uDepth <= uMaxDepth)
            {
                xTree = new Tree();
                xMesh = new Mesh();
                xTree.mesh = xMesh;
                xMesh.startIndex = uStartIndex;

                if (xParentTree != null)
                {
                    xParentTree.Push(xTree);
                }
            }
            else
            {
                xTree = xParentTree;
                xMesh = xParentTree.mesh;
            }

            if (iSubTreeCount == 0)
            {
                xTree.isLeaf = true;

                xReader.BaseStream.Position = uDataPos + 0x30;
                ReadOctLeaf(xReader, treePolygons, xMesh, uStartIndex);
            }
            else
            {
                UInt32[] auSubTreePositions = new UInt32[iSubTreeCount];
                for (Int32 s = 0; s < iSubTreeCount; s++)
                {
                    auSubTreePositions[s] = xReader.ReadUInt32();
                }

                for (Int32 s = 0; s < iSubTreeCount; s++)
                {
                    ReadOctTree(xReader, treePolygons, auSubTreePositions[s], xTree, xMeshes, xMeshPositions, uDepth + 1, uStartIndex);
                }
            }

            if (uDepth <= uMaxDepth)
            {
                if (xMesh != null && xMesh.indexCount > 0)
                {
                    xMeshes.Add(xMesh);
                    xMeshPositions.Add(treePolygons.Count);
                }
            }

            return xTree;
        }

        protected virtual void ReadOctLeaf(BinaryReader xReader, List<TreePolygon> treePolygons, Mesh xMesh, UInt32 uStartIndex)
        {
            UInt32 uLeafData = _dataStart + xReader.ReadUInt32();
            xReader.BaseStream.Position = uLeafData;

            UInt32 uNextStrip = (UInt32)xReader.BaseStream.Position;
            while (true)
            {
                bool bShouldWrite = true; // For debug.

                UInt32 uLength = xReader.ReadUInt32();
                if (uLength == 0 || uLength == 0xFFFFFFFF)
                {
                    break;
                }

                uNextStrip += uLength;

                UInt32 uIndexCount = xReader.ReadUInt32();
                if (uIndexCount == 0)
                {
                    continue;
                }

                UInt16[] axStripIndices = new UInt16[uIndexCount];
                for (UInt32 i = 0; i < uIndexCount; i++)
                {
                    axStripIndices[i] = xReader.ReadUInt16();
                }

                if (xReader.BaseStream.Position % 4 != 0)
                {
                    xReader.BaseStream.Position += 0x02;
                }

                while (true)
                {
                    // 0xFFFF wrong?  Try uTestNextStrip
                    UInt32 uIndexCount2 = xReader.ReadUInt32();
                    if (/*(uIndexCount2 & 0x0000FFFF) == 0x0000FFFF || (uIndexCount2 & 0xFFFF0000) == 0xFFFF0000 ||*/ uIndexCount2 == 0)
                    {
                        //if (xReader.BaseStream.Position % 4 != 0)
                        //{
                        //    xReader.BaseStream.Position += 0x02;
                        //}
                        break;
                    }

                    xReader.BaseStream.Position += 0x04;
                    UInt32 uTextureID = xReader.ReadUInt32();
                    xReader.BaseStream.Position += 0x04;

                    UInt32 uTestNextStrip = xReader.ReadUInt32();

                    UInt16[] axStripIndices2 = new UInt16[uIndexCount2];
                    for (UInt32 i = 0; i < uIndexCount2; i++)
                    {
                        axStripIndices2[i] = xReader.ReadUInt16();
                    }

                    if (bShouldWrite)
                    {
                        UInt16 i = 0;
                        while (i < uIndexCount2)
                        {
                            TreePolygon newPolygon = new TreePolygon();
                            newPolygon.v1 = axStripIndices[axStripIndices2[i++]];
                            newPolygon.v2 = axStripIndices[axStripIndices2[i++]];
                            newPolygon.v3 = axStripIndices[axStripIndices2[i++]];
                            newPolygon.textureID = uTextureID;
                            treePolygons.Add(newPolygon);
                        }

                        if (xMesh != null)
                        {
                            xMesh.indexCount += uIndexCount2;
                        }
                    }

                    xReader.BaseStream.Position = uTestNextStrip;
                }

                xReader.BaseStream.Position = uNextStrip;
            }

            // Was this a special second set of polys?  Animated ones?
            while (true)
            {
                bool bShouldWrite = true; // For debug.

                UInt32 uIndexCount = xReader.ReadUInt32();
                if (uIndexCount == 0)
                {
                    break;
                }

                xReader.BaseStream.Position += 0x04;
                UInt32 uTextureID = xReader.ReadUInt32();

                xReader.BaseStream.Position += 0x04;
                uNextStrip = xReader.ReadUInt32();

                UInt16[] axStripIndices = new UInt16[uIndexCount];
                for (UInt32 i = 0; i < uIndexCount; i++)
                {
                    axStripIndices[i] = xReader.ReadUInt16();
                }

                if (bShouldWrite)
                {
                    UInt16 i = 0;
                    while (i < uIndexCount)
                    {
                        TreePolygon newPolygon = new TreePolygon();
                        newPolygon.v1 = axStripIndices[i++];
                        newPolygon.v2 = axStripIndices[i++];
                        newPolygon.v3 = axStripIndices[i++];
                        newPolygon.textureID = uTextureID;
                        treePolygons.Add(newPolygon);
                    }

                    if (xMesh != null)
                    {
                        xMesh.indexCount += uIndexCount;
                    }
                }

                xReader.BaseStream.Position = uNextStrip;
            }
        }

        protected virtual void FinaliseMesh(List<TreePolygon> treePolygons, int firstPolygon, Mesh xMesh, Material xMaterial, ref int currentPolygon)
        {
            xMesh.polygonCount = xMesh.indexCount / 3;
            xMesh.polygons = new Polygon[xMesh.polygonCount];
            for (int p = 0; p < xMesh.polygonCount; p++)
            {
                UInt32 uV1 = treePolygons[firstPolygon + p].v1 + xMesh.startIndex;
                UInt32 uV2 = treePolygons[firstPolygon + p].v2 + xMesh.startIndex;
                UInt32 uV3 = treePolygons[firstPolygon + p].v3 + xMesh.startIndex;

                xMesh.polygons[p].v1 = _geometry.Vertices[uV1];
                xMesh.polygons[p].v2 = _geometry.Vertices[uV2];
                xMesh.polygons[p].v3 = _geometry.Vertices[uV3];

                xMaterial = new Material();

                xMaterial.visible = true;
                xMaterial.textureID = (UInt16)(treePolygons[firstPolygon + p].textureID & 0x0FFF);
                xMaterial.colour = 0xFFFFFFFF;
                if (xMaterial.textureID > 0 && xMaterial.visible)
                {
                    xMaterial.textureUsed = true;
                }
                else
                {
                    xMaterial.textureUsed = false;
                }

                bool bFoundMaterial = false;
                for (int m = 0; m < _materialsList.Count; m++)
                {
                    if (_materialsList[m].colour == xMaterial.colour &&
                        _materialsList[m].textureID == xMaterial.textureID &&
                        _materialsList[m].textureUsed == xMaterial.textureUsed)
                    {
                        bFoundMaterial = true;
                        xMaterial = _materialsList[m];
                        break;
                    }
                }

                if (!bFoundMaterial)
                {
                    _materialsList.Add(xMaterial);
                }

                xMesh.polygons[p].material = xMaterial;
            }

            xMesh.vertices = new Vertex[xMesh.indexCount];
            for (UInt32 poly = 0; poly < xMesh.polygonCount; poly++)
            {
                _polygons[currentPolygon++] = xMesh.polygons[poly];
                xMesh.vertices[(3 * poly) + 0] = xMesh.polygons[poly].v1;
                xMesh.vertices[(3 * poly) + 1] = xMesh.polygons[poly].v2;
                xMesh.vertices[(3 * poly) + 2] = xMesh.polygons[poly].v3;
            }
        }
    }
}