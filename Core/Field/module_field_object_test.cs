﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace OpenVIII.Fields
{
    /// <summary>
    /// this works only as a preview for field models and proof-of-concept
    /// </summary>
    public class ModuleFieldObjectTest
    {
        #region Fields

        public static AlphaTestEffect Ate;
        public static BasicEffect Effect;

        //private static float _camDistance = 10.0f;
        private const float RenderCamDistance = 1200f;

        private static int _animFrame;
        private static int _animId;
        private static bool _bInitialized;
        private static Vector3 _camPosition, _camTarget;
        private static FieldCharaOne _charaOne;
        private static int _debugModelId;
        private static float _degrees;
        private static FPS_Camera _fpsCamera;
        private static int _lastFieldId = -1;
        private static Matrix _projectionMatrix, _viewMatrix, _worldMatrix;
        private static double _timer;

        #endregion Fields

        #region Methods

        public static void Draw()
        {
            Memory.Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            //Memory.graphics.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Memory.Graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            Memory.Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Memory.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            Memory.Graphics.GraphicsDevice.Clear(Color.Aqua);
            if (!_bInitialized)
                return;
            uint maxAnim = 0;
            uint maxFrame = 0;

            Ate.Projection = _projectionMatrix;
            Ate.View = _viewMatrix;
            Ate.World = _worldMatrix;
            Effect.Projection = _projectionMatrix;
            Effect.View = _viewMatrix;
            Effect.World = _worldMatrix;

            if (_charaOne.FieldModels == null)
                goto _doNotDraw;

            if (_debugModelId >= _charaOne.FieldModels.Length)
                _debugModelId = 0;
            var whichModel = _debugModelId;

            if (_charaOne.FieldModels[whichModel].MCH == null)
                goto _doNotDraw;

            _charaOne.FieldModels[whichModel].MCH.AssignTextureSizes(
                _charaOne.FieldModels[whichModel].Textures,
                Enumerable.Range(0, _charaOne.FieldModels[whichModel].Textures.Length).ToArray());

            maxAnim = _charaOne.FieldModels[whichModel].MCH.GetAnimationCount();
            if (_animId >= maxAnim)
                _animId = 0;
            maxFrame = _charaOne.FieldModels[whichModel].MCH.GetAnimationFramesCount(_animId);
            if (_animFrame >= maxFrame)
                _animFrame = 0;

            var charaCollection =
                _charaOne.FieldModels[whichModel].MCH.GetVertexPositions(Vector3.Zero, Quaternion.Identity, _animId, _animFrame);

            var vptCollection = new Dictionary<Texture2D, List<VertexPositionColorTexture>>();
            for (var i = 0; i < charaCollection.Item2.Length; i += 3)
            {
                var charaTexture = _charaOne.FieldModels[whichModel].Textures[charaCollection.Item2[i]];
                if (!vptCollection.ContainsKey(charaTexture))
                    vptCollection.Add(charaTexture, new List<VertexPositionColorTexture>());
                vptCollection[charaTexture].AddRange(charaCollection.Item1.Skip(i).Take(3).ToArray());
            }

            foreach (var kvp in vptCollection)
            {
                Ate.Texture = kvp.Key;
                foreach (var pass in Ate.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Memory.Graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, kvp.Value.ToArray(), 0, kvp.Value.Count / 3);
                }
            }

            _doNotDraw:
            Memory.SpriteBatchStartAlpha();
            if (_charaOne.FieldModels == null)
            {
                Memory.Font.RenderBasicText(
    $"FIELD AT: {Memory.FieldHolder.FieldID} - {Memory.FieldHolder.GetString()}\n" +
    $"World Map Camera: ={_camPosition}\n" +
    $"FPS camera degrees: ={_degrees}°\n" +
    "Current model is: =BROKEN\n" +
    $"Animation={_animId + 1} of {maxAnim} -- frame: {_animFrame + 1} of {maxFrame}\n" +
    "F1 - re-init (for reparsing and live code debugging)\n" +
    "F2 - Next field\n" +
    "F3 - Previous field\n" +
    "F4 - Next animation\n" +
    "LMB - Next NPC model\n" +
    "NULL: =0", 30, 20, 1f, 2f, lineSpacing: 5);
            }
            else
                Memory.Font.RenderBasicText(
    $"FIELD AT: {Memory.FieldHolder.FieldID} - {Memory.FieldHolder.GetString()}\n" +
    $"World Map Camera: ={_camPosition}\n" +
    $"FPS camera degrees: ={_degrees}°\n" +
    $"Current model is: ={_debugModelId + 1} of {_charaOne.FieldModels.Length} which is {new string(_charaOne.FieldModels[_debugModelId].ModelName, 0, 4)}\n" +
    $"Animation={_animId + 1} of {maxAnim} -- frame: {_animFrame + 1} of {maxFrame}\n" +
    "F1 - re-init (for reparsing and live code debugging)\n" +
    "F2 - Next field\n" +
    "F3 - Previous field\n" +
    "F4 - Next animation\n" +
    "LMB - Next NPC model\n" +
    "NULL: =0", 30, 20, 1f, 2f, lineSpacing: 5);
            Memory.SpriteBatchEnd();
        }

        public static void Update()
        {
            if (!_bInitialized)
            {
                _fpsCamera = new FPS_Camera();
                //init renderer
                Effect = new BasicEffect(Memory.Graphics.GraphicsDevice);
                Effect.EnableDefaultLighting();
                Effect.TextureEnabled = true;
                Effect.DirectionalLight0.Enabled = true;
                Effect.DirectionalLight1.Enabled = false;
                Effect.DirectionalLight2.Enabled = false;
                Effect.DirectionalLight0.Direction = new Vector3(
                   -0.349999f,
                    0.499999f,
                    -0.650000f
                    );
                Effect.DirectionalLight0.SpecularColor = new Vector3(0.8500003f, 0.8500003f, 0.8500003f);
                Effect.DirectionalLight0.DiffuseColor = new Vector3(1.54999f, 1.54999f, 1.54999f);
                _camTarget = new Vector3(0, 0f, 0f);
                _camPosition = new Vector3(0, 0f, 0f);
                _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                                   MathHelper.ToRadians(60),
                                   Memory.Graphics.GraphicsDevice.Viewport.AspectRatio,
                    1f, 10000f);
                _viewMatrix = Matrix.CreateLookAt(_camPosition, _camTarget,
                             new Vector3(0f, 1f, 0f));// Y up
                                                      //worldMatrix = Matrix.CreateWorld(camTarget, Vector3.
                                                      //              Forward, Vector3.Up);
                _worldMatrix = Matrix.CreateTranslation(0, 0, 0);
                //temporarily disabling this, because I'm getting more and more tired of this music playing over and over when debugging
                //Memory.musicIndex = 30;
                //AV.Music.Play();
                Ate = new AlphaTestEffect(Memory.Graphics.GraphicsDevice)
                {
                    Projection = _projectionMatrix,
                    View = _viewMatrix,
                    World = _worldMatrix,
                    FogEnabled = false,
                    FogColor = Color.CornflowerBlue.ToVector3(),
                    FogStart = 9.75f,
                    FogEnd = RenderCamDistance
                };
                _bInitialized = true;
            }
            if (_lastFieldId != Memory.FieldHolder.FieldID)
                ReInit();
            _viewMatrix = _fpsCamera.Update(ref _camPosition, ref _camTarget, ref _degrees);
            if (Input2.Button(MouseButtons.LeftButton, ButtonTrigger.OnRelease))
                _debugModelId++;
            if (Input2.Button(Microsoft.Xna.Framework.Input.Keys.F1, ButtonTrigger.OnRelease))
                ReInit();
            if (Input2.Button(Microsoft.Xna.Framework.Input.Keys.F2))
                Memory.FieldHolder.FieldID++;
            if (Input2.Button(Microsoft.Xna.Framework.Input.Keys.F3))
                Memory.FieldHolder.FieldID--;
            if (Input2.Button(Microsoft.Xna.Framework.Input.Keys.F4))
            {
                _animId++;
                _animFrame = 0;
            }

            _timer += Memory.ElapsedGameTime.TotalMilliseconds / 1000.0d;
            if (_timer > 0.033d)
            {
                _animFrame++;
                _timer = 0f;
            }
        }

        private static void ReInit()
        {
            _lastFieldId = Memory.FieldHolder.FieldID;

            _charaOne = new FieldCharaOne(Memory.FieldHolder.FieldID);
        }

        #endregion Methods
    }
}