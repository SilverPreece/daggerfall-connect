﻿// Project:         XNALibrary
// Description:     Simple XNA game library for DaggerfallConnect.
// Copyright:       Copyright (C) 2011 Gavin Clayton
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Web Site:        http://www.dfworkshop.net
// Contact:         Gavin Clayton (interkarma@dfworkshop.net)
// Project Page:    http://code.google.com/p/daggerfallconnect/

#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
#endregion

namespace XNALibrary
{

    /// <summary>
    /// Extends default renderer with deferred rendering.
    ///  This implementation has to make do without the Content Pipeline as we're building all the models at run-time.
    ///  Check links in this comment block for the original versions using the Content Pipeline.
    ///  Many thanks to Catalin Zima for the excellent tutorial! http://www.catalinzima.com
    ///  Also thanks to Roy Triesscheijn for the XNA 4 version! http://roy-t.nl/index.php/2010/12/28/deferred-rendering-in-xna4-0-source-code/
    /// </summary>
    public class DeferredRenderer : DefaultRenderer
    {
        #region QuadRenderer

        private class QuadRenderer
        {
            // Variables
            VertexBuffer vb;
            IndexBuffer ib;

            // Constructor
            public QuadRenderer(GraphicsDevice graphicsDevice)
            {
                // Vertices
                VertexPositionTexture[] vertices =
            {
                new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0))
            };

                // Create Vertex Buffer
                vb = new VertexBuffer(
                    graphicsDevice,
                    VertexPositionTexture.VertexDeclaration,
                    vertices.Length,
                    BufferUsage.None);
                vb.SetData<VertexPositionTexture>(vertices);

                // Indices
                ushort[] indices = { 0, 1, 2, 2, 3, 0 };

                // Create Index Buffer
                ib = new IndexBuffer(
                    graphicsDevice,
                    IndexElementSize.SixteenBits,
                    indices.Length,
                    BufferUsage.None);
                ib.SetData<ushort>(indices);
            }

            // Draw and set buffers
            public void Draw(GraphicsDevice graphicsDevice)
            {
                graphicsDevice.SetVertexBuffer(vb);
                graphicsDevice.Indices = ib;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            }

            // Just set buffers
            public void JustSetBuffers(GraphicsDevice graphicsDevice)
            {
                graphicsDevice.SetVertexBuffer(vb);
                graphicsDevice.Indices = ib;
            }

            // Just draw without setting buffers
            public void JustDraw(GraphicsDevice graphicsDevice)
            {
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            }
        }

        #endregion

        #region Class Variables

        // GBuffer render targets
        private RenderTarget2D colorRT;     // Color and specular intensity
        private RenderTarget2D normalRT;    // Normals + specular power
        private RenderTarget2D depthRT;     // Depth
        private RenderTarget2D lightRT;     // Lighting

        // GBuffer effects
        private Effect clearBufferEffect;
        private Effect renderBufferEffect;
        private Effect directionalLightEffect;
        private Effect pointLightEffect;
        private Effect finalCombineEffect;

        // GBuffer textures
        private Texture2D nullNormalTexture;
        private Texture2D nullSpecularTexture;

        // Lighting geometry
        private Model sphereModel;          // Point ligt volume

        // GBuffer size
        private Vector2 size;
        private Vector2 halfPixel;

        // Quad drawing
        private QuadRenderer quadRenderer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets size of render targets.
        /// </summary>
        public Vector2 Size
        {
            get { return size; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="textureManager"></param>
        public DeferredRenderer(TextureManager textureManager)
            : base(textureManager)
        {
            // Create quad renderer
            quadRenderer = new QuadRenderer(graphicsDevice);
        }

        #endregion

        #region Public Overrides

        /// <summary>
        /// Called when the renderer should initialise.
        /// </summary>
        public override void Initialise()
        {
            base.Initialise();

            // Get size of back buffer
            int width = graphicsDevice.PresentationParameters.BackBufferWidth;
            int height = graphicsDevice.PresentationParameters.BackBufferHeight;
            size = new Vector2(width, height);
            halfPixel = new Vector2(0.5f / (float)width, 0.5f / (float)height);

            // Create render targets
            colorRT = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);
            normalRT = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
            depthRT = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Single, DepthFormat.None);
            lightRT = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
        }

        /// <summary>
        /// Called when the render should load content.
        /// </summary>
        /// <param name="content">ContentManager.</param>
        public override void LoadContent(ContentManager content)
        {
            // Load effects
            clearBufferEffect = content.Load<Effect>(@"Effects\ClearGBuffer");
            renderBufferEffect = content.Load<Effect>(@"Effects\RenderGBuffer");
            directionalLightEffect = content.Load<Effect>(@"Effects\DirectionalLight");
            finalCombineEffect = content.Load<Effect>(@"Effects\CombineFinal");
            pointLightEffect = content.Load<Effect>(@"Effects\PointLight");
            sphereModel = content.Load<Model>(@"Models\sphere");

            // Load textures
            //nullNormalTexture = content.Load<Texture2D>(@"Textures\null_normal");
            //nullSpecularTexture = content.Load<Texture2D>(@"Textures\null_specular");

            // Assign null textures to render effect
            //renderBufferEffect.Parameters["NormalMap"].SetValue(nullNormalTexture);
            //renderBufferEffect.Parameters["SpecularMap"].SetValue(nullSpecularTexture);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Render active scene.
        /// </summary>
        public override void Draw()
        {
            // Batch scene
            ClearBatches();
            BatchNode(scene.Root, true);

            // Setup gbuffer
            SetGBuffer();
            ClearGBuffer();

            // Draw scene
            DrawBatches();

            // Resolve gbuffer
            ResolveGBuffer();

            // Draw debug version
            DrawDebug();

            // TODO: Draw lights
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets GBuffer render targets.
        /// </summary>
        private void SetGBuffer()
        {
            graphicsDevice.SetRenderTargets(colorRT, normalRT, depthRT);
        }

        /// <summary>
        /// Resets GBuffer render targets.
        /// </summary>
        private void ResolveGBuffer()
        {
            graphicsDevice.SetRenderTargets(null);
        }

        /// <summary>
        /// Clear GBuffer.
        /// </summary>
        private void ClearGBuffer()
        {
            clearBufferEffect.Techniques[0].Passes[0].Apply();
            quadRenderer.Draw(graphicsDevice);
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Renders batches of visible geometry to GBuffer.
        /// </summary>
        protected new void DrawBatches()
        {
            // Update view and projection matrices
            renderBufferEffect.Parameters["View"].SetValue(camera.View);
            renderBufferEffect.Parameters["Projection"].SetValue(camera.Projection);

            // Set render states
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            // Iterate batches
            foreach (var batch in batches)
            {
                // Do nothing if batch empty
                if (batch.Value.Count == 0)
                    continue;

                // Set texture
                if (batch.Key != TextureManager.GroundBatchKey)
                    renderBufferEffect.Parameters["Texture"].SetValue(textureManager.GetTexture(batch.Key));

                // Iterate batch items
                foreach (var batchItem in batch.Value)
                {
                    // Handle ground textures
                    if (batch.Key == TextureManager.GroundBatchKey)
                        renderBufferEffect.Parameters["Texture"].SetValue(batchItem.Texture);

                    // Set vertex buffer
                    graphicsDevice.SetVertexBuffer(batchItem.VertexBuffer);

                    // Set world transform
                    renderBufferEffect.Parameters["World"].SetValue(batchItem.Matrix);

                    // Apply changes
                    renderBufferEffect.CurrentTechnique.Passes[0].Apply();

                    // Draw based on indexed flag
                    if (batchItem.Indexed)
                    {
                        // Set index buffer
                        graphicsDevice.Indices = batchItem.IndexBuffer;

                        // Draw indexed primitives
                        graphicsDevice.DrawIndexedPrimitives(
                            PrimitiveType.TriangleList,
                            0,
                            0,
                            batchItem.NumVertices,
                            batchItem.StartIndex,
                            batchItem.PrimitiveCount);
                    }
                    else
                    {
                        // Draw primitives
                        graphicsDevice.DrawPrimitives(
                            PrimitiveType.TriangleList,
                            batchItem.StartIndex,
                            batchItem.PrimitiveCount);
                    }
                }
            }
        }

        /// <summary>
        /// Draw a debug version of GBuffer.
        /// </summary>
        protected void DrawDebug()
        {
            // Width + Height
            int width = 320;
            int height = 180;

            // Begin sprite batch
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, null, null);

            //Set up Drawing Rectangle
            Rectangle rect = new Rectangle(0, 0, width, height);

            // Draw color
            spriteBatch.Draw(colorRT, rect, Color.White);

            // Draw normal
            rect.Y += height;
            spriteBatch.Draw(normalRT, rect, Color.White);

            // Draw depth
            rect.Y += height;
            spriteBatch.Draw(depthRT, rect, Color.White);

            // End sprite batch
            spriteBatch.End();
        }

        #endregion

    }

}
