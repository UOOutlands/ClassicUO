#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Runtime.InteropServices;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene
    {
        private static GameObject[] _renderList = new GameObject[10000];
        private static GameObject[] _foliages = new GameObject[100];
        private static readonly TreeUnion[] _treeInfos =
        {
            new TreeUnion(0x0D45, 0x0D4C),
            new TreeUnion(0x0D5C, 0x0D62),
            new TreeUnion(0x0D73, 0x0D79),
            new TreeUnion(0x0D87, 0x0D8B),
            new TreeUnion(0x12BE, 0x12C7),
            new TreeUnion(0x0D4D, 0x0D53),
            new TreeUnion(0x0D63, 0x0D69),
            new TreeUnion(0x0D7A, 0x0D7F),
            new TreeUnion(0x0D8C, 0x0D90)
        };
        private StaticTiles _empty;


        private sbyte _maxGroundZ;
        private int _maxZ;
        private Vector2 _minPixel, _maxPixel;
        private bool _noDrawRoofs;
        private Point _offset, _maxTile, _minTile, _last_scaled_offset;
        private int _oldPlayerX, _oldPlayerY, _oldPlayerZ;
        private int _renderListCount, _foliageCount;


        public Point ScreenOffset => _offset;
        public sbyte FoliageIndex { get; private set; }


        public void UpdateMaxDrawZ(bool force = false)
        {
            int playerX = World.Player.X;
            int playerY = World.Player.Y;
            int playerZ = World.Player.Z;

            if (playerX == _oldPlayerX && playerY == _oldPlayerY && playerZ == _oldPlayerZ && !force)
            {
                return;
            }

            _oldPlayerX = playerX;
            _oldPlayerY = playerY;
            _oldPlayerZ = playerZ;

            sbyte maxGroundZ = 127;
            _maxGroundZ = 127;
            _maxZ = 127;
            _noDrawRoofs = !ProfileManager.CurrentProfile.DrawRoofs;
            int bx = playerX;
            int by = playerY;
            Chunk chunk = World.Map.GetChunk(bx, by, false);

            if (chunk != null)
            {
                int x = playerX % 8;
                int y = playerY % 8;

                int pz14 = playerZ + 14;
                int pz16 = playerZ + 16;

                for (GameObject obj = chunk.GetHeadObject(x, y); obj != null; obj = obj.TNext)
                {
                    sbyte tileZ = obj.Z;

                    if (obj is Land l)
                    {
                        if (l.IsStretched)
                        {
                            tileZ = l.AverageZ;
                        }

                        if (pz16 <= tileZ)
                        {
                            maxGroundZ = (sbyte) pz16;
                            _maxGroundZ = (sbyte) pz16;
                            _maxZ = _maxGroundZ;

                            break;
                        }

                        continue;
                    }

                    if (obj is Mobile)
                    {
                        continue;
                    }


                    //if (obj is Item it && !it.ItemData.IsRoof || !(obj is Static) && !(obj is Multi))
                    //    continue;

                    if (tileZ > pz14 && _maxZ > tileZ)
                    {
                        ref StaticTiles itemdata = ref TileDataLoader.Instance.StaticData[obj.Graphic];

                        //if (GameObjectHelper.TryGetStaticData(obj, out var itemdata) && ((ulong) itemdata.Flags & 0x20004) == 0 && (!itemdata.IsRoof || itemdata.IsSurface))
                        if (((ulong) itemdata.Flags & 0x20004) == 0 && (!itemdata.IsRoof || itemdata.IsSurface))
                        {
                            _maxZ = tileZ;
                            _noDrawRoofs = true;
                        }
                    }
                }

                int tempZ = _maxZ;
                _maxGroundZ = (sbyte) _maxZ;
                playerX++;
                playerY++;
                bx = playerX;
                by = playerY;
                chunk = World.Map.GetChunk(bx, by, false);

                if (chunk != null)
                {
                    x = playerX % 8;
                    y = playerY % 8;

                    for (GameObject obj2 = chunk.GetHeadObject(x, y); obj2 != null; obj2 = obj2.TNext)
                    {
                        //if (obj is Item it && !it.ItemData.IsRoof || !(obj is Static) && !(obj is Multi))
                        //    continue;

                        if (obj2 is Mobile)
                        {
                            continue;
                        }

                        sbyte tileZ = obj2.Z;

                        if (tileZ > pz14 && _maxZ > tileZ)
                        {
                            if (!(obj2 is Land))
                            {
                                ref StaticTiles itemdata = ref TileDataLoader.Instance.StaticData[obj2.Graphic];

                                if (((ulong) itemdata.Flags & 0x204) == 0 && itemdata.IsRoof)
                                {
                                    _maxZ = tileZ;
                                    World.Map.ClearBockAccess();
                                    _maxGroundZ = World.Map.CalculateNearZ(tileZ, playerX, playerY, tileZ);
                                    _noDrawRoofs = true;
                                }
                            }

                            //if (GameObjectHelper.TryGetStaticData(obj2, out var itemdata) && ((ulong) itemdata.Flags & 0x204) == 0 && itemdata.IsRoof)
                            //{
                            //    _maxZ = tileZ;
                            //    World.Map.ClearBockAccess();
                            //    _maxGroundZ = World.Map.CalculateNearZ(tileZ, playerX, playerY, tileZ);
                            //    _noDrawRoofs = true;
                            //}
                        }
                    }

                    tempZ = _maxGroundZ;
                }

                _maxZ = _maxGroundZ;

                if (tempZ < pz16)
                {
                    _maxZ = pz16;
                    _maxGroundZ = (sbyte) pz16;
                }

                _maxGroundZ = maxGroundZ;
            }
        }

        private void IsFoliageUnion(ushort graphic, int x, int y, int z)
        {
            for (int i = 0; i < _treeInfos.Length; i++)
            {
                ref TreeUnion info = ref _treeInfos[i];

                if (info.Start <= graphic && graphic <= info.End)
                {
                    while (graphic > info.Start)
                    {
                        graphic--;
                        x--;
                        y++;
                    }

                    for (graphic = info.Start; graphic <= info.End; graphic++, x++, y--)
                    {
                        ApplyFoliageTransparency(graphic, x, y, z);
                    }

                    break;
                }
            }
        }

        private void ApplyFoliageTransparency(ushort graphic, int x, int y, int z)
        {
            GameObject tile = World.Map.GetTile(x, y);

            if (tile != null)
            {
                for (GameObject obj = tile; obj != null; obj = obj.TNext)
                {
                    ushort testGraphic = obj.Graphic;

                    if (testGraphic == graphic && obj.Z == z)
                    {
                        obj.FoliageIndex = FoliageIndex;
                    }
                }
            }
        }

        private void UpdateObjectHandles(GameObject obj, bool useObjectHandles)
        {
            if (useObjectHandles && NameOverHeadManager.IsAllowed(obj as Entity))
            {
                switch (obj.ObjectHandle)
                {
                    case GameObject.ObjectHandleState.NONE:
                    case GameObject.ObjectHandleState.CLOSING:
                        obj.ObjectHandle = GameObject.ObjectHandleState.NAME_NEEDED;
                        break;
                    case GameObject.ObjectHandleState.NAME_NEEDED:
                        break;
                    case GameObject.ObjectHandleState.DISPLAYING:
                        obj.UpdateTextCoordsV();
                        break;
                }
            }
            else if (obj.ObjectHandle != GameObject.ObjectHandleState.NONE &&
                     obj.ObjectHandle != GameObject.ObjectHandleState.CLOSING)
            {
                obj.ObjectHandle = GameObject.ObjectHandleState.CLOSING;
                obj.UpdateTextCoordsV();
            }
        }

        private bool ProcessAlpha(GameObject obj, ref StaticTiles itemData)
        {
            if (obj.Z >= _maxZ)
            {
                if (_alphaChanged)
                {
                    obj.ProcessAlpha(0);
                }

                return obj.AlphaHue != 0;
            }
            else if (_noDrawRoofs && itemData.IsRoof)
            {
                if (_alphaChanged)
                {
                    obj.ProcessAlpha(0);
                }

                return obj.AlphaHue != 0;
            }
            else if (itemData.IsTranslucent && obj.AlphaHue != 178)
            {
                if (_alphaChanged)
                {
                    obj.ProcessAlpha(178);
                }

                return true;
            }
            else if (!itemData.IsFoliage && obj.AlphaHue != 0xFF)
            {
                if (_alphaChanged)
                {
                    obj.ProcessAlpha(0xFF);
                }

                return true;
            }

            return true;
        }

        private bool InViewport(GameObject obj)
        {
            if (UpdateDrawPosition || obj.IsPositionChanged)
            {
                obj.UpdateRealScreenPosition(_offset.X, _offset.Y);
            }

            int drawX = obj.RealScreenPosition.X;
            int drawY = obj.RealScreenPosition.Y;

            if (drawX < _minPixel.X || drawX > _maxPixel.X)
            {
                return false;
            }

            if (drawY < _minPixel.Y || drawY > _maxPixel.Y)
            {
                return false;
            }

            return true;
        }

        private void AddTileToRenderList(GameObject obj, int worldX, int worldY, bool useObjectHandles)
        {
            TileDataLoader loader = TileDataLoader.Instance;

            for (; obj != null; obj = obj.TNext)
            {
                if (!obj.AllowedToDraw)
                {
                    continue;
                }

                if (UpdateDrawPosition || obj.IsPositionChanged)
                {
                    obj.UpdateRealScreenPosition(_offset.X, _offset.Y);
                }

                if (!InViewport(obj))
                {
                    continue;
                }

                ref StaticTiles itemData = ref _empty;

                ushort graphic = obj.Graphic;

                switch (obj)
                {
                    case Mobile _:
                        if (obj.Z >= _maxZ)
                        {
                            continue;
                        }

                        if (_alphaChanged && obj.AlphaHue != 0xFF)
                        {
                            obj.ProcessAlpha(0xFF);
                        }

                        UpdateObjectHandles(obj, useObjectHandles);
                        break;

                    case Land _:
                        break;

                    case Item it:
                        if (it.IsCorpse)
                        {
                            UpdateObjectHandles(obj, useObjectHandles);
                            goto default;
                        }

                        if (it.IsMulti)
                        {
                            graphic = it.MultiGraphic;
                        }

                        itemData = ref loader.StaticData[graphic];

                        if (!it.IsLocked || (it.IsLocked && itemData.IsContainer))
                        {
                            UpdateObjectHandles(obj, useObjectHandles);
                        }
                        goto default;

                    default:
                        itemData = ref loader.StaticData[graphic];

                        if (itemData.IsInternal)
                        {
                            continue;
                        }

                        if (itemData.IsFoliage && !itemData.IsMultiMovable && World.Season >= Season.Winter && !(obj is Multi))
                        {
                            continue;
                        }

                        if (!ProcessAlpha(obj, ref itemData))
                        {
                            continue;
                        }

                        //we avoid to hide impassable foliage or bushes, if present...
                        if (ProfileManager.CurrentProfile.TreeToStumps && itemData.IsFoliage && !itemData.IsMultiMovable && !(obj is Multi) || ProfileManager.CurrentProfile.HideVegetation && (obj is Multi mm && mm.IsVegetation || obj is Static st && st.IsVegetation))
                        {
                            continue;
                        }

                        if (itemData.IsFoliage)
                        {
                            if (obj.FoliageIndex != FoliageIndex)
                            {
                                sbyte index = 0;

                                bool check = World.Player.X <= worldX && World.Player.Y <= worldY;

                                if (!check)
                                {
                                    check = World.Player.Y <= worldY && World.Player.X <= worldX + 1;

                                    if (!check)
                                    {
                                        check = World.Player.X <= worldX && World.Player.Y <= worldY + 1;
                                    }
                                }

                                if (check)
                                {
                                    ArtTexture texture = ArtLoader.Instance.GetTexture(graphic);

                                    if (texture != null)
                                    {
                                        _rectangleObj.X = obj.RealScreenPosition.X - (texture.Width >> 1) + texture.ImageRectangle.X;
                                        _rectangleObj.Y = obj.RealScreenPosition.Y - texture.Height + texture.ImageRectangle.Y;
                                        _rectangleObj.Width = texture.ImageRectangle.Width;
                                        _rectangleObj.Height = texture.ImageRectangle.Height;

                                        check = Exstentions.InRect(ref _rectangleObj, ref _rectanglePlayer);

                                        if (check)
                                        {
                                            index = FoliageIndex;
                                            IsFoliageUnion(obj.Graphic, obj.X, obj.Y, obj.Z);
                                        }
                                    }
                                }

                                obj.FoliageIndex = index;
                            }

                            if (_foliageCount >= _foliages.Length)
                            {
                                int newsize = _foliages.Length + 50;
                                Array.Resize(ref _foliages, newsize);
                            }

                            _foliages[_foliageCount++] = obj;
                        }

                        break;
                }

                if (_renderListCount >= _renderList.Length)
                {
                    int newsize = _renderList.Length + 1000;
                    Array.Resize(ref _renderList, newsize);
                }

                _renderList[_renderListCount++] = obj;
            }

            return;
        }

        private void GetViewPort()
        {
            int oldDrawOffsetX = _offset.X;
            int oldDrawOffsetY = _offset.Y;
            Point old_scaled_offset = _last_scaled_offset;

            float zoom = Camera.Zoom;

            int winGameWidth = ProfileManager.CurrentProfile.GameWindowSize.X;
            int winGameHeight = ProfileManager.CurrentProfile.GameWindowSize.Y;
            int winGameCenterX = (winGameWidth >> 1);
            int winGameCenterY = (winGameHeight >> 1) + (World.Player.Z << 2);
            winGameCenterX -= (int) World.Player.Offset.X;
            winGameCenterY -= (int) (World.Player.Offset.Y - World.Player.Offset.Z);

            int tileOffX = World.Player.X;
            int tileOffY = World.Player.Y;

            int winDrawOffsetX = (tileOffX - tileOffY) * 22 - winGameCenterX;
            int winDrawOffsetY = (tileOffX + tileOffY) * 22 - winGameCenterY;

            int winGameScaledOffsetX;
            int winGameScaledOffsetY;

            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableMousewheelScaleZoom)
            {
                winGameScaledOffsetX = (int)(winGameWidth * (1f - zoom));
                winGameScaledOffsetY = (int)(winGameHeight * (1f - zoom));
            }
            else
            {
                winGameScaledOffsetX = 0;
                winGameScaledOffsetY = 0;
            }

            int width = (int) ((winGameWidth / 44 + 1) * zoom);
            int height = (int) ((winGameHeight / 44 + 1) * zoom);


            if (width < height)
            {
                width = height;
            }
            else
            {
                height = width;
            }

            int realMinRangeX = tileOffX - width;

            if (realMinRangeX < 0)
            {
                realMinRangeX = 0;
            }

            int realMaxRangeX = tileOffX + width;

            int realMinRangeY = tileOffY - height;

            if (realMinRangeY < 0)
            {
                realMinRangeY = 0;
            }

            int realMaxRangeY = tileOffY + height;

            int drawOffset = (int) (44 / zoom);

            Point p = Point.Zero;
            p.X -= drawOffset;
            p.Y -= drawOffset;
            p = Camera.ScreenToWorld(p);
            int minPixelsX = p.X;
            int minPixelsY = p.Y;

            p.X = Camera.Bounds.Width + drawOffset;
            p.Y = Camera.Bounds.Height + drawOffset;
            p = Camera.ScreenToWorld(p);
            int maxPixelsX = p.X;
            int maxPixelsY = p.Y;


            if (UpdateDrawPosition || oldDrawOffsetX != winDrawOffsetX || oldDrawOffsetY != winDrawOffsetY || old_scaled_offset.X != winGameScaledOffsetX || old_scaled_offset.Y != winGameScaledOffsetY)
            {
                UpdateDrawPosition = true;

                if (_lightRenderTarget == null || _lightRenderTarget.Width != winGameWidth || _lightRenderTarget.Height != winGameHeight)
                {
                    _lightRenderTarget?.Dispose();

                    PresentationParameters pp = Client.Game.GraphicsDevice.PresentationParameters;

                    _lightRenderTarget = new RenderTarget2D
                    (
                        Client.Game.GraphicsDevice,
                        winGameWidth,
                        winGameHeight,
                        false,
                        pp.BackBufferFormat,
                        pp.DepthStencilFormat,
                        pp.MultiSampleCount,
                        pp.RenderTargetUsage
                    );
                }
            }

            _minTile.X = realMinRangeX;
            _minTile.Y = realMinRangeY;
            _maxTile.X = realMaxRangeX;
            _maxTile.Y = realMaxRangeY;

            _minPixel.X = minPixelsX;
            _minPixel.Y = minPixelsY;
            _maxPixel.X = maxPixelsX;
            _maxPixel.Y = maxPixelsY;

            _offset.X = winDrawOffsetX;
            _offset.Y = winDrawOffsetY;

            _last_scaled_offset.X = winGameScaledOffsetX;
            _last_scaled_offset.Y = winGameScaledOffsetY;


            UpdateMaxDrawZ();
        }

        private struct TreeUnion
        {
            public TreeUnion(ushort start, ushort end)
            {
                Start = start;
                End = end;
            }

            public readonly ushort Start;
            public readonly ushort End;
        }
    }
}